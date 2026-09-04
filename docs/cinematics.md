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
| `EntityVisibilityTrack` | Entity reference + visible flag | Stub. |
| `AgentAnimationTrack` | Role, action name, facial animation, loop | Stub (planned rework). |

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

## Extending in code

To add a new track type:

1. Create the keyframe and track classes under `Models/Tracks/` (derive from `CinematicKeyframe`/`CinematicTrack`, expose `GetKeyframes()`). The serializer discovers derived types automatically.
2. Sample it: continuous values go in a `Sample*` method of `CinematicPlayer` (gated by `RequiresVisualSampling`), one-shot effects in `FireCrossings`.
3. Add the callback to `ICinematicPlaybackSink` and implement it in `CinematicView` (client) and the server sink (usually a no-op).
4. Editor: add a lane color and `CreateTrack` entry in `CinematicTimelineVM`, an `AddKey` case, a `GenericKeyframeVM` subclass and its XAML data template.

## Known limitations and roadmap

- Only one cinematic at a time per client (last-started wins the camera); the server supports concurrent cinematics for different audiences.
- Chat and server announcements remain visible during cinematics (accepted; only Gauntlet layers are hidden).
- `AgentAnimationTrack` needs a rework: action-name pickers, proper body-action looping, target model usage.
- `AudioTrack` ignores volume and loop.
- Editor preview: `ViewerCamera` targets cannot resolve in the modding-kit preview (no combat camera); `SpecificAgent` targets resolve in-game only.
- Entity pickers in the timeline inspector are plain RefId text fields for now (the full entity picker integration is a follow-up).
