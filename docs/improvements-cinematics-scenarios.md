# Scenario & Cinematic Improvements — Next Release Plan

Working plan for the next release (base `0.5.9.0`, target `0.6.0.0`). Items come out of the recent MP
test sessions on the cinematic system and the scenario rework. Nothing is deployed yet — no
migration or backwards compatibility constraints, models can change freely. Note for testers:
scenario XMLs authored with earlier builds of this branch must be re-saved in the editor —
`CinematicTarget.Entity` is now a value source, cinematics are referenced by name (not Id) and
audiences are variable-driven (no more player-name strings).

> ## ✅ RESOLVED — KIT CHARACTER/ITEM DATA
>
> The modding-kit fidelity gap is closed **without** the risky MBObjectManager load (that route
> hits `Game.Current` NREs in `ItemObject.Deserialize` and would need the whole game context).
> Instead, the native MP data files (mpcharacters / mpitems / mpcultures) are **exported into the
> Alliance module** (`ModuleData/Characters|Items|Cultures/NativeMP/`) and parsed with plain XML
> (`NativeMpData`, Common.Core.Utils): per-character equipment item ids, per-item mesh names and
> per-culture colors. `FakeAgent` uses this to dress at full fidelity in the kit; the blockout
> outfit remains only when a character id is unknown.
>
> **Why not native MBObjectManager loading**: verified against the decompiled source —
> `ItemObject.Deserialize` touches `Game.Current` (null in the kit) and would NRE per item;
> character equipment entries resolve items eagerly, so without loaded items the equipment ends
> up empty regardless; and `_RGL_KEEP_ASSERTS` would turn the failed asserts into log spam or
> breaks. A full `Game.CreateGame` in the editor process (to get the real pipeline) conflicts
> with the editor's own screen/object-manager state. NativeMpData parses the same source XMLs
> managed-side — the remaining gap (body *shape* morphing) is an AgentVisuals-pipeline
> limitation, not a loading one: real bodies appear in test missions via the debug keys and
> in-game. A full `MBObjectManager` load (real
> `BasicCharacterObject`s with skills/body gen) stays optional if another system ever needs it.

## Status snapshot

Working (test sessions, commits `e1287209` / `77d506d7`):
- Lock/Hide agent freeze in MP (input zeroed + `MissionMainAgentController.IsDisabled`,
  agent kept player-controlled so the input channel to the server stays open).
- Mission UI hidden during cinematics (all Gauntlet layers except the cinematic overlay, restored on stop).
- Subtitle horizontal/vertical alignment.
- Skip hint readable, positioned inside the letterbox bar.

Accepted limitations (not in scope):
- Chat and server announcements remain visible during a cinematic. Fine for now — they are the least
  intrusive UIs; hiding them would also hide legitimately useful info.

Backlog (not in this release): `AudioTrack` volume/loop. Agent and entity staging tracks are the
next block — items 7 and 8 below.

---

## 1. Agent behavior: hide modes + invulnerability during cinematics — **implemented**

**Current**: `AgentBehaviorMode { Hide, Lock, Free }`. `Hide` makes only the local player's agent
invisible (its mount stays visible), and the restore path uses `Agent.Main` at stop time — an agent
that died/respawned mid-cinematic stays invisible forever. No protection against damage.

**Goal**: granular hide modes, mounts hidden, correct restore, and optional invulnerability for the
duration of the cinematic.

**Proposal**
- New enum: `AgentBehaviorMode { Free, Lock, HidePlayers, HideAll }`.
  - `HidePlayers`: hide every player-controlled agent **and their mounts**. Bots stay visible.
  - `HideAll`: hide every agent (players + AI) and their mounts.
  - Both hide modes keep the local-player freeze (hide = lock + invisibility), local only — hiding
    remote agents is a purely visual, per-receiver operation.
- `CinematicView` tracks every agent it hid (list, not just `Agent.Main`) and restores exactly those
  on stop/skip/mission-end; agents removed from the mission since are skipped.
- New cinematic-level dropdown `Invulnerability { None, Players, Bots, All }`: for the whole duration
  of the cinematic, the server sets the matching agents to `Agent.CurrentMortalityState =
  MortalityState.Invulnerable` (native mechanism, see `Agent.ToggleInvulnerable()`) and restores the
  saved mortality states when it ends. Driven by `CinematicServerBehavior` alongside its timeline
  record (health/damage is server-authoritative, so this cannot be client-side).
- Editor: both dropdowns are auto-rendered by `ConfigProperty`; update the debug Home-cycle
  (`_debugModes`) to exercise the new modes.

**Decisions**
- Invulnerability scope is mission-wide for the chosen category (not audience-scoped): predictable and
  simpler; a Team-scoped cinematic doesn't leave half the battlefield killable by accident.

**Acceptance**
- With bots on the field: `HidePlayers` → only bots visible; `HideAll` → no agents visible.
- Mounted players: horse hidden with rider in both modes.
- Player dies mid-cinematic then respawns: no agent stays invisible after the cinematic.
- With `Invulnerability = Players`: shooting a frozen/hidden player deals no damage during the
  cinematic, normal damage resumes after.

---

## 2. Unified target model + camera keyframe modes (`Absolute` / `Relative`) — **implemented**

**Current**: `CameraKeyframe.Frame` is an absolute world-space frame. The role concept used by
camera/look-at resolves `MainAgent`/`Viewer`/`Player` — three names for the same thing (the local
player's agent), with no way to target a specific agent or entity shared by everyone, and no way to
distinguish "the player's agent" from "the player's camera".

**Goal**: one explicit target model, used by camera keyframes, look-at, (and later by the animation
track and audiences), plus relative camera keyframes for dynamic shots.

**Target model** — replaces roles everywhere:
| Target | Same for everyone? | Resolves to |
|---|---|---|
| `Specific Agent` | yes | an agent held by a scenario variable (`VariableType.Agent` — e.g. stored by `ObjectUsedCondition`) or set by an action |
| `Specific Entity` | yes | a `GameEntityRef` (BuildSystem marker) |
| `ViewerAgent` | no — per receiver | the receiving player's own agent |
| `ViewerCamera` | no — per receiver | the receiving player's camera pose |

Per-receiver resolution is a property of the **target**, never of the audience. This is what makes
"every player sees the shot framed on themselves" work with a plain All-audience cinematic.

**Camera keyframes** — extend `CameraKeyframe`:
- `FrameMode { Absolute, Relative }` (default `Absolute`).
- `FrameTarget` (when `Relative`): one of the four targets above.
- `FrameOffset`: a `MatrixFrame` offset applied in target space ("3 m behind, 1 m above the entity").
- `TrackMode { Frozen, Track }`: `Frozen` captures the target frame once at cinematic start (stable
  basis, e.g. "from where I stood"), `Track` re-resolves every tick (live follow).

**Pipeline**
- Resolution at sample time in `CinematicPlayer` through `ICinematicBindings`: replace
  `ResolveRolePosition` with target resolution returning full frames (`ResolveTargetFrame(target)`).
  `CinematicView` implements it: specific agents resolve through their rewritten literal slots,
  specific entities through the marker index, viewer targets against the local player. As shipped,
  `ViewerCamera` cannot resolve in the modding-kit preview (no combat camera) and `Specific Agent`
  resolves in-game only — unresolved targets warn once and fall back to the previous resolved frame.
- Interpolation (Catmull-Rom/Linear/Constant) runs on the **resolved world frames**, so absolute and
  relative keyframes mix freely in one track.

**Combos this enables**
- Player camera → POI (kf 1 relative `ViewerCamera`/`Frozen`, later keyframes absolute).
- Entity → entity travelling.
- Cinematic ending dynamically on each player's character: last keyframe relative
  `ViewerAgent`/`Track` → every player gets their own ending shot.

**Editor**: inspector gets mode dropdown, target picker (entity picker + variable picker for
`Specific Agent`/`Specific Entity`, dedicated entries for the two viewer targets) and a "capture
current view as offset" button. Timeline lane unchanged. `LookAtTrack` migrates to the same target
model.

**Acceptance**
- A cinematic authored as (relative `ViewerCamera` start → absolute POI → relative `ViewerAgent`
  track end) plays correctly in-game; in the editor it previews with the unresolved-target fallback
  (see Pipeline above).
- Look-at and camera keyframes accept the same targets, resolved consistently.

---

## 3. Audience rework (runtime + editor) — **implemented**

**Current** (`Audience.cs`): `All`, `Team` (battle side), `Players` (**comma-separated display
names** — flagged TODO at `Audience.cs:31`), and `RelativeToViewer` (redundant now that per-receiver
resolution lives in the target model, item 2).

**Goal**: three clear scopes, peer-based targeting, proper editor UI.

**Proposal**
- Scopes: `All`, `Team` (battle side), `Players`.
- `Players` is resolved at runtime from **references, not usernames**: an audience sources its peer
  list from a scenario variable (e.g. an `Agent` variable set by a condition/action — resolve to its
  `MissionPeer`/`NetworkCommunicator`). Typical use: "reward cinematic for the players who completed
  the objective". The comma-separated `PlayerNames` string is removed.
- Server-side resolution: `CinematicServerBehavior` resolves an `Audience` to a set of
  `NetworkCommunicator`s when the cinematic starts. `Team` resolves to the peers on that side at
  start time; team switches mid-cinematic do not change the current run's viewers.
- `RelativeToViewer` scope and `UseViewerOrigin` are removed — per-receiver framing is done with
  viewer targets (item 2).
- **Editor UI**: dedicated audience editor — scope dropdown, side picker, variable picker for the
  `Players` scope.

**Acceptance**
- A variable-driven `Players` audience (populated by a condition) receives exactly its cinematic.
- A `Team` audience plays only for that side.
- No username strings anywhere in the model.

---

## 4. Late-join mid-cinematic (synced) — **implemented**

**Current**: cinematics are sent once at start; players joining mid-play see nothing. The sync
primitives already exist: `PlayCinematicMessage` carries the shared start timestamp and the client
seeks to `elapsed` on receipt; `CinematicServerBehavior` keeps one record per running cinematic.

**Goal**: a player joining during a cinematic receives it, already in progress, in sync with everyone
else.

**Proposal**
- Server hooks (in `CinematicServerBehavior`):
  - `All`-scoped: when a peer becomes mission-ready → send `PlayCinematicMessage` with the original
    start timestamp (defer until the client finished loading).
  - `Team`-scoped: when a peer joins the targeted side.
  - `Players`-scoped (variable-driven): the peer set is fixed at start; late join is not applicable.
- Client: no new mechanism — the existing seek path handles it. `Seek` clears the fired-events set
  (`CinematicPlayer.Seek`), so elapsed one-shot `EventTrack` actions are not replayed; the server
  never re-executes them either. Documented consequence: client-side (`ExecuteClient`) effects of
  elapsed event actions do not happen for the late joiner.
- Decisions:
  - Cinematic nearly over (e.g. < 5 s remaining): still send or skip? Propose a small threshold,
    configurable per cinematic.
  - Looping cinematics: late joiner enters at the current loop position (`elapsed % duration`).

**Acceptance**
- Client B joins 30 s into a 60 s cinematic: sees the same frame content as client A at every moment.
- Player switching into the targeted team mid-cinematic receives it.
- Late joiner does not re-trigger any event actions.

---

## 5. Act start flow rework (merged into `SpawnLogic`) — **implemented**

Implemented: always-on waiting screen (full-black client view with ready/total counter driven by
`WaitingScreenStateMessage`); `SpawnLogic` reorganized into clear editor categories — **Team
assignment** (`PlayerSelection` / `Auto` with `Balanced` / `AllAttackers` / `AllDefenders` rules,
applied server-side at spawn start) and **Character assignment** (`PlayerSelection` / `Auto` with
`DefaultUnits` / `RandomFromMenu` rules, default units only visible for the `DefaultUnits` rule);
officer selection under Character assignment; timings under Start flow with the intro cinematic;
editor validation for all mode/rule combinations; `IntroCinematic` slot played when the act starts.

Remaining polish: the timeline inspector entity fields are plain RefId text entries rather than the
scene entity picker.

**Current**: `ActState { Invalid, AwaitingPlayerJoin, SpawningParticipants, InProgress,
DisplayingResults, Completed }`. `AwaitingPlayerJoin` waits for players to load (server logs
"Waiting for players to load... (X/Y)") with no dedicated client screen. Then the flow is implicit:
`Act.SpawnLogic.PlayerSpawnMenu` defined → native menu (team + formation + character + officer vote),
undefined → default characters (`DefaultCharacterAttacker/Defender`). No clean place for an opening
cinematic, no explicit team handling, no auto path with visible feedback.

**Goal**: one explicit, editor-friendly act-start pipeline inside `SpawnLogic` (merged with the
intro-flow ideas instead of a separate `IntroFlow` model). The waiting screen is always enabled —
not an option.

**Proposal** — rework `SpawnLogic` into a clear "Start flow" section:
- **Waiting stage** (always on): full-black client screen hiding everything behind, showing
  ready/total player count. Server broadcasts the counter through the intro state sync.
- **Team stage** — `TeamSelection { Select, Auto }`:
  - `Select`: team choice through the spawn menu (as today).
  - `Auto`: server assigns sides directly (balanced by default, scenario-overridable rule).
- **Character stage** — `CharacterSelection { Select, Auto }`:
  - `Select`: the existing `PlayerSpawnMenu` (formations, characters, officer vote).
  - `Auto`: spawn everyone with the default characters (`DefaultCharacterAttacker/Defender`).
- **Intro cinematic** (optional): a `CinematicRef` played once team + character stages are resolved
  and agents spawned. Combined with item 2 (`ViewerAgent`/`Track` targets) the cinematic can end on
  each player's own character and hand over control seamlessly.
- `TimeBeforeSpawn` stays as the menu-stage timeout for the `Select` paths; `Auto` stages complete as
  soon as everyone is ready (with the same timeout as a ceiling).

**Server** — extend the `ScenarioBehavior` state machine with start-flow sub-states (Waiting →
Team → Characters → IntroCinematic → `InProgress`); `Auto` stages assign/spawn server-side without
waiting for menu-driven requests.

**Client** — new waiting-screen mission view (black overlay + ready counter; reuse the cinematic
UI-hiding approach so nothing else shows through), plus intro state sync (extend
`UpdateScenarioMessage` or a dedicated message).

**Editor** — reorganize `SpawnLogic`'s config into ordered categories reflecting the pipeline
(Start flow: waiting → team → characters → cinematic; then initial spawn and respawn settings as
today). Validation warnings: `Auto` team + `Select` characters allowed (players pick their loadout,
side forced), `Select` team + `Auto` characters allowed (players pick a side, get the default unit);
`Auto` characters with no default character defined → error.

**Two reference flows to validate**
1. *Cinematic-first*: waiting black screen → (auto team + auto characters in background) → intro
   cinematic ending dynamically on each player's character → control handed over.
2. *Menu-driven*: waiting black screen → spawn menu (team + characters) → optional cinematic →
   gameplay.

**Acceptance**
- Both flows play out correctly with 2+ clients and a dedicated server.
- Disconnect/reconnect during the start flow lands the player in the correct stage.
- The waiting screen is always the first thing a joining player sees.

---

## 6. Carried-over cleanups (small) — **implemented**

- **Removed `Patch_MissionScreen`**: Free mode now uses the native `MissionScreen.AllowInputWithCustomCamera` field (same check, no transpiler).
- **Localized the skip hint** (`CinematicOverlayVM.SkipHintText`, bound in the prefab).
- Refreshed `docs/cinematics.md`; bump `AllianceVersion` in `CommonProps.props` at release time; this document's status is kept up to date.

---

## 7. Agent action track (teleport / move / animate / visibility) + editor preview puppet — **implemented**

Implemented: `AgentActionTrack` (track-level `Agent` variable target + `PreviewCharacter` for the
puppet) with typed keyframes `Teleport` / `MoveTo` / `PlayAnimation` / `PlayFacial` / `SetVisible`;
authoritative execution in `CinematicServerBehavior` via `SetScriptedPositionAndDirection`
(`DoNotRun` = Walk) and `SetActionChannel`; clients re-apply visibility/facial locally (the rest
replicates natively); the modding-kit preview spawns one real-character `AgentVisuals` puppet per
track (character selectable per track) and stages commands against it with straight-line movement.
Follow-ups: action-name picker dropdown (typed by name for now), timeline estimated-arrival marker
for MoveTo, facial animation on the puppet, body-action looping semantics.

**Current**: `AgentAnimationTrack` is a stub (`TargetRole`, `ActionName`, `FacialAnimation`, `Loop`):
role resolution is not implemented and there is no movement or positioning — agents cannot be staged
at all. This item replaces the track entirely (`AgentAnimationTrack` is removed; scenarios containing
it must be re-authored).

**Goal**: one track per agent subject that stages everything that agent does over the cinematic —
teleport, move, play body/facial animations, visibility. Server-authoritative like `EventTrack`,
synced through the existing dynamic `ValueSource` machinery, and previewable in the modding kit
through a real-character puppet.

**Track model** — `AgentActionTrack`:
- Track-level `Target`: a `ValueSource<List<Agent>>` — a variable or function holding one or more
  agents (`agents in zone`, `list with agent` to wrap a single-agent variable). Resolved server-side
  at start and shipped like camera targets today; commands apply to every agent of the list. The
  lane shows the target (and the preview character) as clickable hyperlinks in place of the name.
- Keyframes are typed commands: a `Kind` enum drives the visible fields through the `ConfigProperty`
  dependency system (same pattern as `CinematicTarget.Type`). Instant commands and sections coexist
  in one keyframe list (`SubtitleKeyframe.Duration` precedent).

| Kind | Timing | Fields | Native mechanism |
|---|---|---|---|
| `Teleport` | instant | destination (position target or variable), facing | `Agent.TeleportToPosition` / frame set |
| `MoveTo` | section | destination target, `Walk`/`Run`, arrival facing | `Agent.SetScriptedPositionAndDirection` — TaleWorlds' own cutscene/conversation move primitive (`AIScriptedFrameFlags.InConversation`); real locomotion + footsteps, avoids obstacles |
| `PlayAnimation` | section | action name (actions.xml picker), channel (0/1), loop, speed; overlay-on-locomotion via channel/priority | `Agent.SetActionChannel` (blend, priority, speed, startProgress parameters) |
| `PlayFacial` | instant | facial anim name, loop | `Agent.SetAgentFacialAnimation` |
| `SetVisible` | instant | visible flag (+ mount) | same machinery as `CinematicView.HideAgent`, tracked & restored |

- `MoveTo` arrival time is approximate (path-dependent). The editor shows an estimated arrival
  marker (distance / speed) and follow-up keyframes are authored with margin. An exact-timing
  server-driven kinematic mode is a possible later addition.
- Squad targets: `MoveTo` / `PlayAnimation` may target a variable holding a `List<Agent>` — one
  keyframe advances a whole formation. The sync codec already carries agent lists (tag 8).
- Gameplay effects (damage, kills) stay in `EventTrack`: choreography = `AgentActionTrack` (visual)
  + `EventTrack` (gameplay) on the same beats. `Agent.Die(Blow, KillInfo)` exists if a convenience
  kill kind is ever needed.

**Decisions**
- One track per subject, kinds inside — not one track type per action: lanes must read as an actor's
  performance, and three lanes per agent would flood the timeline.
- `ViewerAgent` is excluded as an actor: the server cannot execute four different performances at
  once. Acting agents are variables, resolved server-side.
- Facial animation is its own keyframe kind, not a field of `PlayAnimation` (different lifetime,
  independent re-triggering).

**Runtime**
- Keyframes execute server-side (`CinematicServerBehavior`, like `EventTrack` action execution)
  against the resolved agents; locomotion and animation state reach clients through native agent
  replication — no per-frame custom sync. Late joiners see agents already staged or mid-move.

**Editor preview — entity puppets**
- Real-character `AgentVisuals` puppets (the mission-less lobby-preview factory) crash with an
  AccessViolation in the kit scene: their native skeleton/monster infrastructure is mission-side and
  is not initialized there (FaceGen's monster lookup also goes through `Game.Current`, null in the
  kit). The AV is a corrupted-state exception and cannot be caught safely, so that path is off.
- The preview stages **entity puppets** instead, using TaleWorlds' own AnimationPoint recipe
  (Sandbox/Objects/AnimationPoints): `GameEntity.CreateEmpty` + `CreateAgentSkeleton("human_skeleton",
  humanoid, actionSet 0, "human" monster)` + simple body metameshes (tunic, boots, hands, head),
  actions played through the raw skeleton action channels (`SetAgentActionChannel`), moved by keyframes.
  Each resolved target agent gets a puppet (line-abreast stagger); when the list does not resolve a
  single stand-in is spawned. If character resources are unavailable the puppet degrades to a
  sphere blockout.
- The lane's preview-character field is kept on the model (reserved for a future in-scene path).
- Variables resolve with authored defaults (`ScenarioManager.LoadGlobals` is already wired into
  preview). Unresolvable targets (specific in-game agents) skip with a warn-once — same contract as
  `ViewerCamera` today.

**Acceptance**
- MP: players locked → black fade → their agents advance (`MoveTo`, arrival facing) and play
  animations facing a boss lane that animates on its own beats → camera returns to each `ViewerAgent`
  → control restored on end.
- A squad variable advances on a single `MoveTo` keyframe; every client sees the same order.
- Execution beat: victim kneels (anim), executioner strikes (anim), `EventTrack` damage on the same
  beat kills the victim.
- Editor: puppets play authored animations, walk `MoveTo` with locomotion anims, estimated-arrival
  markers show on the timeline.

---

## 8. Entity track rework (visibility / teleport / travel / fx) — **implemented**

Implemented: `EntityTrack` with typed keyframes `SetVisible` / `Teleport` / `MoveTo` (full-frame
interpolation incl. rotation, `Linear`/`SmoothStep`) / `Fx` (`Burst`/`Pause`/`Resume` + children
flag); client-local kinematic movement derived from the shared clock (seek/late-join correct);
previews natively in the kit scene. Follow-up: visual entity picker in the timeline inspector
(RefId/value-source popup for now).

**Current**: `EntityVisibilityTrack` is a stub (entity ref + visible flag). Entity references are now
synced `ValueSource<WeakGameEntity>` slots (this branch), so only the behavior is missing. This item
replaces the track (`EntityVisibilityTrack` is removed; scenarios containing it must be re-authored).

**Goal**: stage scene entities — show/hide, reposition instantly or over time (gates opening, ships
sailing), and trigger particle effects on the beat — all previewable natively in the kit scene.

**Track model** — `EntityTrack` (typed keyframes, same pattern as the agent track):

| Kind | Timing | Fields | Native mechanism |
|---|---|---|---|
| `SetVisible` | instant | visible flag | `SetVisibilityExcludeParents` (exists) |
| `Teleport` | instant | destination frame (position + facing) | `GameEntity.SetGlobalFrame` |
| `MoveTo` | section | destination frame, duration, interpolation `Linear`/`SmoothStep` | client-local kinematic interpolation |
| `Fx` | instant | `Burst` / `Pause` / `Resume`, children flag | `GameEntity.BurstEntityParticle` / `PauseParticleSystem` / `ResumeParticleSystem` (one-shots vs continuous) |

- Destinations are full `MatrixFrame`s: rotation interpolates with position (gate hinge rotation,
  ship translation with slight pitch/roll).
- Movement is derived deterministically from the shared cinematic start timestamp — zero extra
  network traffic, late joiners compute the correct mid-travel pose.
- Chained `MoveTo` keyframes form multi-leg routes (sail past, then turn).
- `Fx` is explicit rather than a visibility side-effect: an entity that should appear together with
  its effect gets `SetVisible` + `Fx: Burst` on the same beat (cannon broadside); continuous effects
  (`Resume`) ride along when the entity moves (burning ship).
- Caveat: moving entities with gameplay meaning (spawn markers) is visual-only per receiver —
  document for authors.

**Editor preview**: fully native — kit scenes render, move and emit particles on real scene
entities, so this track previews exactly, no puppet needed.

**Acceptance**
- A gate rotates open across clients in sync; a ship travels a two-leg route carrying a continuous
  smoke fx; a broadside `Burst` lands on the camera-pass beat together with an `AudioTrack` sound.
- A late joiner sees the ship/gate at the correct mid-travel state.

## Suggested order

| # | Item | Depends on |
|---|---|---|
| 1 | Hide modes + invulnerability | — |
| 6 | Cleanups (patch removal, localization) | — |
| 2 | Target model + camera keyframe modes | — |
| 3 | Audience rework | 2 (variable-driven peers reuse the target/variable plumbing) |
| 4 | Late-join mid-cinematic | 3 |
| 5 | Act start flow rework | 2 (end-on-player shots), 1 |
| 8 | Entity track rework (visibility / teleport / travel / fx) | 2 (synced entity value sources) |
| 7 | Agent action track + editor preview puppet | 2 (variable plumbing), 8 (shared typed-keyframe patterns) |

Items 1, 2 and 6 are independent and quick to validate; 3 and 4 build on them. Item 5 is the largest
and deserves its own test scenario in `ExampleScenarios` as soon as its data model lands.
Items 7 and 8 are implemented (see the follow-up notes in each item); 8 landed first and the shared
typed-keyframe patterns carried over into 7. The puppet infrastructure can later also stand in for
`ViewerAgent` camera-target framing previews.
