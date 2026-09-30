# Cinematic System

This document explains how Alliance cinematics are authored, triggered and played. Cinematics are timeline-driven cutscenes: keyframed camera paths, letterbox/fade overlays, subtitles and timed scenario actions, playable at any point of a scenario or standalone from a scene entity.

For the surrounding scenario concepts (acts, scripted events, actions), see `docs/scenario-system.md`.

## Source-of-truth files

| Area | Main files |
|---|---|
| Data model | `Alliance.Common/Extensions/Cinematics/Models/` (`Cinematic.cs`, `CinematicTrack.cs`, `CinematicKeyframe.cs`, `Audience.cs`, `Tracks/`) |
| Player | `Alliance.Common/Extensions/Cinematics/CinematicPlayer.cs`, `Utilities/KeyframeEvaluator.cs`, `Utilities/CameraMath.cs` |
| Client playback | `Alliance.Common/Extensions/Cinematics/Views/CinematicView.cs`, `ViewModels/CinematicOverlayVM.cs` |
| Server timelines | `Alliance.Server/GameModes/Story/Behaviors/CinematicServerBehavior.cs` |
| Trigger | `Alliance.Common/GameModes/Story/Actions/PlayCinematicAction.cs`, `Alliance.Server/GameModes/Story/Actions/Server_PlayCinematicAction.cs` |
| Network | `Alliance.Common/GameModes/Story/NetworkMessages/FromServer/PlayCinematicMessage.cs`, `StopCinematicMessage.cs`, `SetCinematicTimeMessage.cs` |
| Client routing | `Alliance.Client/GameModes/Story/Handlers/StoryHandler.cs` |
| Editor | `Alliance.Editor/Extensions/Cinematics/` (timeline window + view-models) |
| Overlay UI | `Alliance.Common/_Module/GUI/Prefabs/CinematicOverlay/CinematicOverlay.xml`, `GUI/Brushes/Brushes.xml` (`Cinematic.Subtitle`) |
| Targets | `Alliance.Common/Extensions/Cinematics/Models/CinematicTarget.cs` |

## Concepts

A `Cinematic` is a named timeline of tracks. Each track holds keyframes sampled or fired over time.

| Cinematic field | Purpose |
|---|---|
| `Name` | Unique name identifying the cinematic within the scenario (must be unique - validated). Used by `[CinematicRef]` dropdowns and network addressing, like variable names. |
| `DurationSec` | Total length in seconds. `0` derives the duration from the last keyframe end. |
| `Loop` | Restarts from the beginning when finished. |
| `IsSkippable` | Shows a "Press Space to skip" hint; skipping is local to each player. |
| `FadeInSec` / `FadeOutSec` | Fade from/to black when no Overlay track is present. |
| `AgentBehavior` | What happens to agents: `Free` (player keeps control; input stays live), `Lock` (local player frozen), `HidePlayers` (local player frozen + all player-controlled agents and their mounts hidden locally), `HideAll` (same, but every agent is hidden). Hiding is local-only per receiver. |
| `Invulnerability` | Who is made invulnerable (server-side, mission-wide) for the duration: `None`, `Players`, `Bots` or `All`. Mortality states are restored when the cinematic ends. |
| `Audience` | Who receives the cinematic (see below). |
| `Tracks` | The timeline tracks. |

### Tracks and keyframes

Every keyframe has a `Time` (seconds), an `Interpolation` mode (`CatmullRom` smooth, `Linear`, `Constant` hold-until-reached) and a `Tension` for Catmull-Rom paths.

| Track | Keyframe values | Status |
|---|---|---|
| `CameraTrack` | Camera frame — absolute world frame or a target-relative frame (see Targets below) — FOV, near/far planes, roll, depth-of-field | Fully implemented. Camera frames follow a Catmull-Rom path through the keyframes; absolute and relative keyframes can mix in one track. |
| `OverlayTrack` | Letterbox amount (0..1) and fade-to-black alpha (0..1) | Fully implemented. |
| `SubtitleTrack` | Localized text with `{0}`/`{1}` value sources, scrolling (credits-style) option, display duration, fade, font, size, color, alignment, glow/blur/shadow/outline | Fully implemented. Custom fonts live in `_Module/GUI/Fonts/`. Text arguments are resolved by the server and shipped with the dynamic data. |
| `EventTrack` | A list of scenario `ActionBase`, fired once when the playhead crosses the keyframe | Implemented. Actions execute **server-side** (authoritative) and client-side through their usual client overrides. |
| `LookAtTrack` | Aim override: a target (see below), at times independent of the camera path | Implemented. The single aiming mechanism - camera rotation is overridden to follow the target. |
| `AudioTrack` | Sound event name, volume, loop | Partial. Plays the sound event; volume and loop are not applied yet. |
| `EntityTrack` | Typed keyframes: `SetVisible`, `Teleport` (instant), `MoveTo` (position + rotation interpolated over a duration, Linear/SmoothStep), `Fx` (`Burst`/`Pause`/`Resume` particle command) | Fully implemented. Movement is client-local kinematic - derived from the shared clock, so it syncs without traffic and late joiners compute the correct mid-travel pose. FX are explicit commands, not visibility side-effects. |
| `AgentActionTrack` | One lane per staged subject (track target = a variable/function holding one or more Agents, server-resolved — e.g. "agents in zone", "list with agent"). Typed keyframes: `Teleport`, `MoveTo` (destination: position, marker/variable entity or an agent's position; native scripted movement, Walk/Run/Custom speed (m/s), arrival facing; both share *Keep formation offset*, default on: each agent keeps its spot relative to the group - true agents keep their current arrangement (centroid + mean facing, rotated into the destination's orientation), fake agents keep their authored formation slot - instead of stacking on the point; mounted true agents move as a pair: the teleport/scripted order targets the mount, which carries the rider), `PlayAnimation` (true agents: `Action` + optional `Mount action`; fake agents: `Clip` + optional `Mount clip`, raw animation clips; channel, speed), `PlayFacial`, `SetVisible` (local per receiver) | Implemented. Commands execute server-side (`CinematicServerBehavior`) for every agent of the list and replicate natively. Editor preview stages a dressed GameEntity per track moved by keyframes with animations played as raw clips; falls back to sphere blockout when character resources are unavailable (availability in the kit is lazy - see *The modding kit environment*). Not AgentVisuals - those are mission-side and crash in the kit scene. |

### Targets

Camera keyframes and look-at overrides reference targets through the unified `CinematicTarget` model:

| Target | Same for everyone? | Resolves to |
|---|---|---|
| `Position` | yes | an explicit world position |
| `SpecificAgent` | yes | an agent referenced by a `ValueSource<Agent>` slot (variable, like any other slot); the server resolves it when the cinematic starts and ships the value - the client's slot is rewritten to a literal |
| `SpecificEntity` | yes | a scene entity picked visually (literal) or a variable holding one; dynamic entity slots are synced as their marker RefId |
| `ViewerAgent` | no — per receiver | the receiving player's own agent |
| `ViewerCamera` | no — per receiver | the receiving player's camera pose captured when the cinematic starts |

Camera keyframes use targets with `FrameMode = Relative` (plus a `FrameOffset` applied in target
space and a `TrackMode`: `Frozen` captures the target frame once at start, `Track` follows it every
tick). Typical dynamic shots: first keyframe relative to `ViewerCamera` (frozen), middle keyframes
absolute on a point of interest, last keyframe relative to `ViewerAgent` (tracking) — every player's
cinematic ends on their own character.

### Audience

| Scope | Behavior |
|---|---|
| `All` | Broadcast to every peer. |
| `Team` | Only peers on the chosen `Team` side. |
| `Players` | Only the peers controlling the agents held by the referenced scenario variables (`PlayerVariables`). Resolved by the server at start time — no username strings. |

Per-receiver framing is not an audience concern: it is done with `ViewerAgent`/`ViewerCamera` targets,
so a plain `All`-audience cinematic can still frame every player on themselves.

## Triggering

Cinematics are played by `PlayCinematicAction`, usable from any action host:

- act `ConditionalActions` and `VictoryLogic` inside a scenario;
- `AL_TriggerAction` scene entities — the classic map-intro case.

The action references the cinematic in one of two ways:

- **By name** (a `[CinematicRef]` string field, rendered as a dropdown of the scenario's cinematics in the editor): resolves a cinematic stored on the scenario (`Scenario.Cinematics`). The reference is small and the cinematic is shared.
- **Inline** (`Cinematic` field): a self-contained copy riding inside the action — required for `AL_TriggerAction` map intros where no scenario exists.

Actions only execute on the server. `Server_PlayCinematicAction` broadcasts a `PlayCinematicMessage` to the chosen audience and registers an authoritative server timeline. Clients resolve the message either by cinematic name or by action reference `(ScopeId, ActionId)` through the shared action registry, then start local playback.

## Runtime flow

### Server

`CinematicServerBehavior` (in every game mode's default behaviors) keeps one record per running cinematic; several cinematics can run concurrently for different audiences. The server player is event-only: it does not sample camera/overlay/subtitle tracks, it just advances a clock and executes `EventTrack` actions server-side when their keyframes are crossed. Event action tasks (e.g. `WaitAction`) are ticked to completion by the behavior. It also applies/restores the cinematic's `Invulnerability` setting.

Re-triggering a cinematic with the same Id replaces the running record.

**Late join**: each running record keeps its broadcast message and audience. Peers who join (or synchronize) mid-cinematic receive it with the original shared start timestamp, so they play it in sync with everyone else — `All` scope on connection, `Team` scope on team join, `Players` scope for its fixed peer set once synchronized. Elapsed one-shot `EventTrack` actions are not replayed for late joiners; near the end of a non-looping cinematic (< 5 s left) the message is no longer sent.

### Client

`StoryHandler` receives `PlayCinematicMessage` and hands the cinematic to `CinematicView` (a mission view present in every game mode):

1. The view takes over the camera (`MissionScreen.CustomCamera`), handles the agent behavior mode, hides other mission UI layer (only the cinematic overlay stays visible) and shows the Gauntlet overlay layer (letterbox bars, fade quad, subtitles, skip hint).
2. Each frame, `CinematicPlayer` samples the tracks and pushes camera/overlay/subtitle state to the view. Targets resolve at sample time through the view (viewer targets locally, specific agents through their rewritten literal slots, entities through the marker index).
3. Playback ends on its own clock, when a `StopCinematicMessage` names this cinematic, or when the player skips (Space, local-only). The camera, agent state, first-person mode and scene effects are then restored.

Only one cinematic plays at a time on a client — starting a new one stops the previous.

In `Free` agent mode, the native `MissionScreen.AllowInputWithCustomCamera` flag keeps player input alive while the custom camera renders.

### Network messages

| Message | Direction | Purpose |
|---|---|---|
| `PlayCinematicMessage` | server -> client | Start playback. Addressed by cinematic name (scenario-scoped) or by `(ScopeId, ActionId)` (inline). Carries a shared start timestamp so receivers sync without per-frame traffic, plus the cinematic's resolved dynamic data: for every dynamic (non-literal) `ValueSource` slot in the cinematic, the server ships the resolved value in deterministic walk order (same value codec as `ExecuteActionMessage`); the client rewrites slot N with value N as a literal (mirroring `InjectSyncData`), so replays and late joins simply rewrite again. Skippability is read from the cinematic data itself. |
| `StopCinematicMessage` | server -> client | Stop the named cinematic. Empty Id = stop any (scenario aborts). |
| `SetCinematicTimeMessage` | server -> client | Seek the named running cinematic to an absolute time (resync/admin tooling). |

## Authoring in the editor

1. Open the scenario editor (`LeftCtrl + P` with `Alliance.Editor` loaded).
2. Cinematics are authored two ways:
   - scenario-scoped: add entries under `Scenario -> Cinematics`, then reference them from a `PlayCinematicAction` by name;
   - inline: open a `PlayCinematicAction` and click its editor button to create/edit the inline copy.
   Both open the cinematic timeline window.
3. The timeline window provides:
   - a transport bar (play/pause/stop/loop) with **live preview** rendered from the editor scene camera;
   - the camera lane with draggable keyframes, and additional lanes for other tracks;
   - an inspector for the selected keyframe (time, interpolation, lens, position/rotation, look-at);
   - **Capture View** — snapshots the current editor camera into a camera keyframe;
   - copy/paste for keyframes and whole cinematics.
4. Debug hotkeys in game (debug builds): `Home` plays a generated test cinematic (cycles agent modes), `End` plays the next authored cinematic of the current scenario.

### Minimal example

A short intro cinematic: add a `Cinematic` to the scenario, add a `CameraTrack` with 2-3 keyframes captured from the editor view, an `OverlayTrack` with a letterbox of 0.1 fading in from black, then a `PlayCinematicAction` (by name, audience `All`) in the act's `ConditionalActions` behind a mission-start condition.

## The modding kit environment

The kit loads **no game-type XML at startup** — no `Game` exists until a tool needs one. TaleWorlds' editor scripts create it lazily (`if (Game.Current == null) { new EditorGameManager().DoLoadingForGameManager(); }` in `CharacterSpawner`, `ItemVisualizer`, `CharacterDebugSpawner`, ...). Game creation then loads, in order:

| Stage | XML ids |
|---|---|
| Game texts (at game creation) | every `GameText` id declared by the active modules (incl. our `Languages`) |
| `LoadBasicFiles` | `Monsters`, `SkeletonScales`, `ItemModifiers`, `ItemModifierGroups`, `CraftingPieces`, `WeaponDescriptions`, `CraftingTemplates`, `BodyProperties`, `SkillSets` |
| `LoadCustomGameXmls` | `Items`, `EquipmentRosters`, `NPCCharacters`, `SPCultures` |

Additional facts:

- `MPCharacters` / `MPClassDivisions` (declared in `Alliance.Editor/_Module/SubModule.xml`) are never loaded by the editor game; custom ids like `ItemsExtended` only load if our code calls `LoadXML` — `ExtendedXMLLoader.Init()` is currently wired in the Client/SP/Server submodules only, not the Editor one.
- `action_sets.xml` / `action_types.xml` are loaded earlier as **native module data** (not through `SubModule.xml <Xmls>`). They back every action name used by `AgentActionTrack` and are what the kit Model Viewer animation list displays; raw animation clips only exist inside `.tpac` resources (Resource Browser) and are usable only once registered in an `action_sets.xml`.
- Game creation runs `InitializeGameStarter` in our submodules — i.e. `AnimationSystem.Instance.Init()` only happens once a Game exists; before that its action dictionaries are empty.

TW reference: `TW_References/BannerlordSource/TaleWorlds/MountAndBlade/EditorGameManager.cs`, `EditorGame.cs` (`LoadCustomGameXmls`), `Core/Game.cs` (`LoadBasicFiles`), `View/Scripts/CharacterSpawner.cs`.

### Impact on the cinematic preview

`EditorTools.SpawnPreviewPuppets` stages `AgentActionTrack` stand-ins from `BasicCharacterObject`s (`PreviewCharacterId`) and the `Monster` "human" resources, and logs a diagnostic line (`Game.Current`, `BasicCharacters`, `Monster 'human'`) each time. Until a Game has been created in the kit, those lookups fail and stand-ins fall back to the generic human/blockout.

### FakeAgent dressing model

`FakeAgent` mirrors the native agent-visuals recipe on raw entities:

- **Skeleton & animation**: fakes have **two rendering paths**, chosen by the track's target mode. `AgentsFromScene` tracks preview their stand-in as a full native **AgentVisuals** (one per track, mirroring TaleWorlds' `CharacterSpawner`: facegen action set `as_*_facegen`, generated skin, morph node) - facial animations work (native Mid channel), weapons/holsters are placed natively, and the monster's walking speed applies; their keyframes use **actions** (`Action`/`Mount action`, plus `PlayFacial`). `NewFakeAgents` tracks (staged extras, crowds) **always** use the cheap **raw entity** path (simple skeleton + hand-bound meshes - the AgentVisuals path is also budget-capped at `MaxAgentVisualsFakes` = 64 for any other caller) and their keyframes use **animation clips** (`Clip`/`Mount clip`); facial animations are impossible there (the native facial system is agent-visuals-side) and the UI hides them. In both paths body animation is **raw clips** (`SetAnimationAtChannel`, from `skins.xml`/`action_sets.xml` via `NativeMpData`, which also parses `monsters.xml` - walking speed, item bones, rider-sit bone, `base_monster` inheritance); clips authored as cyclic loop natively, others are re-fired by watching the skeleton channel parameter (normalized clip progress) run out - no duration math, correct at any playback speed. `NativeMpData` also keeps the catalog of registered clip names (editor suggestions).
- **Body meshes** come from the same `skins.xml` entry (per race + gender: head, body, shoulders, hands, feet, underwear), with parts hidden by armor filtered out exactly like the native `Equipment.GetSkinMeshesMask`: every equipped armor's coverage (`covers_head/body/hands/legs`) is AND-ed into the visible-parts mask.
- **Armor/quiver meshes** are skinned onto the skeleton (`AddMultiMeshToSkeleton`).
- **Sheathed weapons** are placed per holster family, matching what each engine path produces: standard holsters (hips, quivers, bows, backs) use the engine's root-frame query (`MBItem.GetHolsterFrameByIndex` + the item's rotation-aware `holster_position_shift`); back-carried shields (the only case the engine frame reads wrong, ~0.3 low) instead place from **`item_holsters.xml`** - the holster bone (generic `biped_*` name resolved through the monster's bone table, mirroring the engine's HumanBone mapping) with its authored local frame. Both compose the item's own Weapon component frame (`position`/`rotation`) when the **bare weapon mesh** is strapped (its pivot is the grip - axes, maces, polearms, each shield); authored scabbard/quiver meshes already contain the weapon oriented and take it as identity. Holster slots are allocated distinct per simultaneously holstered item (first free of each item's list). The authored **`show_holster_when_drawn`** flag decides whether the empty holster stays visible when the weapon is drawn. **Drawn** wields at most one weapon per hand: the first weapon of the equipment (slot order) goes to the main hand, the shield to the off-hand - MP rosters never pair shields with two-handed setups, so a present shield is always wielded (spears/lances are template-typed `TwoHandedPolearm` yet one-handable, exactly how native cavalry holds them). Drawn weapons bind their wielded mesh to the skeleton's item bone (`r_finger0` / `l_finger0` per monsters.xml, resolved at runtime, hand bones as fallback); shields sit on the off-hand *secondary* item bone (`l_foretwist1`, native `ForceAttachOffHandSecondaryItemBone` flag). **Crafted weapons** (MP weapons are piece-composed and carry no authored mesh) are composed through the game's crafting cache (`CraftedDataView` - the smithy UI source, cached per design; needs the game objects, skipped otherwise) and sheathe on the same `item_holsters.xml` bones. Ammo is always worn as its quiver (its base mesh is the projectile). The full-visuals path places weapons natively either way.
- **Equipment variety**: characters carry several battle rosters in their XML (the game picks `RandomBattleEquipment` at spawn). `NativeMpData` parses them all; each fake picks one deterministically from its creation seed (staged groups pass the member index, so every machine - late joiners included - dresses the group identically).
- **Mounts** (character equipment slots `Horse` + `HorseHarness`): the simple path spawns the mount as an entity with the mount monster's skeleton (`CreateSimpleSkeleton`; the monster id comes from the horse item's `<Horse monster="...">` component), wearing the horse body (+mane) and harness meshes. The rider is re-parented onto it with its frame pinned to the mount's rider-sit bone (read once the mount's idle pose is applied - fresh skeletons sit in a meaningless bind pose; upright rider frame, the native riding convention that mounted animations are authored for - no constants, any mount/rider build seats itself). When mounted, the mount is the hierarchy root: movement moves the mount and the rider follows. The mount idles on `horse_stand_1` and mounted riders on `horse_rider_stand_1` (the native mounted idle - standing `inventory_idle` when on foot); `MoveTo` strides the mount automatically (`horse_walkfast` / `horse_gait_trot_2` by pace, or the authored *Mount move clip* keyframe field) and both idle again on arrival. `PlayAnimation` keyframes expose `Clip` + `Mount clip` fields for fake agents (raw clips) alongside `Action` + `Mount action` for true agents (server-side through the AnimationSystem sync). The full-visuals path does not stage a separate mount (its inventory-preview mount support skews the meshes on the rider skeleton); true-agent teleports move rider+mount as a pair instead.
- **Character data**: fakes are built from a `NativeMpData.CharacterDefinition` (the character's parsed XML: race, female flag, culture, equipment slots) - never from a `BasicCharacterObject`. The definition drives everything; unresolvable values (race, monster, skeleton, skins/action-set entry, item data) are logged as errors and the fake (or the affected feature) is skipped instead of being built from wrong data.

### Force-loading the editor game from our submodule

To have characters/items/animations available immediately, start TaleWorlds' own editor pipeline from the submodule (gated on editor mode; `MBGameManager.StartNewGame` is public):

```csharp
protected override void OnBeforeInitialModuleScreenSetAsRoot()
{
    if (MBEditor.IsEditModeOn && Game.Current == null)
        MBGameManager.StartNewGame(new EditorGameManager());
}
```

Afterwards any additional id can be pulled with the usual per-id API (e.g. in `OnGameInitializationFinished`): `game.ObjectManager.LoadXML("MPCharacters")` (registered types permitting), and `ExtendedXMLLoader.Init()` becomes usable in the kit.

Do **not** cherry-pick XMLs before any Game exists: `MBObjectManager.LoadXML` needs registered types and the `Default*` singletons, which all register through `Game.Current.ObjectManager` (and `DefaultCharacterAttributes`' constructor is `internal`). A synchronous alternative is `Game.CreateGame(new EditorGame(), new EditorGameManager()).DoLoading()`, but it skips native `LoadModuleData` (engine data such as action sets) — prefer `StartNewGame`.

## Extending in code

To add a new track type:

1. Create the keyframe and track classes under `Models/Tracks/` (derive from `CinematicKeyframe`/`CinematicTrack`, expose `GetKeyframes()`). The serializer discovers derived types automatically.
2. Sample it: continuous values go in a `Sample*` method of `CinematicPlayer` (gated by `RequiresVisualSampling`), one-shot effects in `FireCrossings`.
3. Add the callback to `ICinematicPlaybackSink` and implement it in `CinematicView` (client) and the server sink (usually a no-op).
4. Editor: add a lane color and `CreateTrack` entry in `CinematicTimelineVM`, an `AddKey` case, a `GenericKeyframeVM` subclass and its XAML data template.

## Known limitations and roadmap

- Only one cinematic at a time per client (last-started wins the camera); the server supports concurrent cinematics for different audiences.
- Chat and server announcements remain visible during cinematics (accepted; only Gauntlet layers are hidden).
- `AgentActionTrack`: action names are free text with a filterable suggestion list (`FilterableComboBox`, no live preview of the action); body-action looping follows the native action set flags; agent `MoveTo` arrival time is approximate (path-dependent) - the timeline arrival marker is a follow-up; facial animations have no puppet preview; stand-in dressing depends on the kit's lazy game loading (see *The modding kit environment*).
- Keyframes authored at exactly t = 0 are not fired as crossings (they must sit past the first tick) - start them at ~0.05 s.
- `AudioTrack` ignores volume and loop.
- Editor preview: `ViewerCamera` targets cannot resolve in the modding-kit preview (no combat camera); `SpecificAgent` targets resolve in-game only.
- Entity pickers in the timeline inspector are plain RefId text fields for now (the full entity picker integration is a follow-up).
