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
| Input patch | `Alliance.Common/Patch/HarmonyPatch/Patch_MissionScreen.cs` |

## Concepts

A `Cinematic` is a named timeline of tracks. Each track holds keyframes sampled or fired over time.

| Cinematic field | Purpose |
|---|---|
| `Id` | Auto-generated stable identifier, used by `CinematicRef` and network addressing. |
| `Name` | Editor-facing display name. |
| `DurationSec` | Total length in seconds. `0` derives the duration from the last keyframe end. |
| `Loop` | Restarts from the beginning when finished. |
| `IsSkippable` | Shows a "Press Space to skip" hint; skipping is local to each player. |
| `FadeInSec` / `FadeOutSec` | Fade from/to black when no Overlay track is present. |
| `AgentBehavior` | What happens to the player's agent: `Hide` (invisible + frozen), `Lock` (frozen) or `Free` (player keeps control; input stays live). |
| `Audience` | Who receives the cinematic (see below). |
| `Tracks` | The timeline tracks. |

### Tracks and keyframes

Every keyframe has a `Time` (seconds), an `Interpolation` mode (`CatmullRom` smooth, `Linear`, `Constant` hold-until-reached) and a `Tension` for Catmull-Rom paths.

| Track | Keyframe values | Status |
|---|---|---|
| `CameraTrack` | Camera frame (position/rotation), FOV, near/far planes, roll, depth-of-field, optional look-at target | Fully implemented. Camera positions follow a Catmull-Rom path through the keyframes. |
| `OverlayTrack` | Letterbox amount (0..1) and fade-to-black alpha (0..1) | Fully implemented. |
| `SubtitleTrack` | Localized text, display duration, fade, font, size, color, alignment | Fully implemented. Custom fonts live in `_Module/GUI/Fonts/`. |
| `EventTrack` | A list of scenario `ActionBase`, fired once when the playhead crosses the keyframe | Implemented. Actions execute **server-side** (authoritative) and client-side through their usual client overrides. |
| `LookAtTrack` | Aim override: role name or explicit position | Partial. Roles currently resolve to `MainAgent` / `Viewer` / `Player` only. |
| `AudioTrack` | Sound event name, volume, loop | Partial. Plays the sound event; volume and loop are not applied yet. |
| `EntityVisibilityTrack` | Entity reference + visible flag | Stub. |
| `AgentAnimationTrack` | Role, action name, facial animation, loop | Stub (planned rework). |

### Audience

| Scope | Behavior |
|---|---|
| `All` | Broadcast to every peer. |
| `Team` | Only peers on the chosen `Team` side. |
| `Players` | Only the peers listed in `PlayerNames` (comma-separated display names). |
| `RelativeToViewer` | Same data for everyone, but the `Viewer` role resolves to each receiver's own agent — every player sees the same shot framed on themselves. |

## Triggering

Cinematics are played by `PlayCinematicAction`, usable from any action host:

- act `ConditionalActions` and `VictoryLogic` inside a scenario;
- `AL_TriggerAction` scene entities — the classic map-intro case.

The action references the cinematic in one of two ways:

- **By Id** (`CinematicRef`): resolves a cinematic stored on the scenario (`Scenario.Cinematics`). The reference is small and the cinematic is shared.
- **Inline** (`Cinematic` field): a self-contained copy riding inside the action — required for `AL_TriggerAction` map intros where no scenario exists.

Actions only execute on the server. `Server_PlayCinematicAction` broadcasts a `PlayCinematicMessage` to the chosen audience and registers an authoritative server timeline. Clients resolve the message either by cinematic Id or by action reference `(ScopeId, ActionId)` through the shared action registry, then start local playback.

## Runtime flow

### Server

`CinematicServerBehavior` (in every game mode's default behaviors) keeps one record per running cinematic; several cinematics can run concurrently for different audiences. The server player is event-only: it does not sample camera/overlay/subtitle tracks, it just advances a clock and executes `EventTrack` actions server-side when their keyframes are crossed. Event action tasks (e.g. `WaitAction`) are ticked to completion by the behavior.

Re-triggering a cinematic with the same Id replaces the running record.

### Client

`StoryHandler` receives `PlayCinematicMessage` and hands the cinematic to `CinematicView` (a mission view present in every game mode):

1. The view takes over the camera (`MissionScreen.CustomCamera`), handles the agent behavior mode and shows the Gauntlet overlay layer (letterbox bars, fade quad, subtitles, skip hint).
2. Each frame, `CinematicPlayer` samples the tracks and pushes camera/overlay/subtitle state to the view.
3. Playback ends on its own clock, when a `StopCinematicMessage` names this cinematic, or when the player skips (Space, local-only). The camera, agent state, first-person mode and scene effects are then restored.

Only one cinematic plays at a time on a client — starting a new one stops the previous.

In `Free` agent mode, a Harmony transpiler (`Patch_MissionScreen`) keeps player input alive while the custom camera renders.

### Network messages

| Message | Direction | Purpose |
|---|---|---|
| `PlayCinematicMessage` | server -> client | Start playback. Addressed by cinematic Id (scenario-scoped) or by `(ScopeId, ActionId)` (inline). Carries a shared start timestamp so receivers sync without per-frame traffic. |
| `StopCinematicMessage` | server -> client | Stop the named cinematic. Empty Id = stop any (scenario aborts). |
| `SetCinematicTimeMessage` | server -> client | Seek the named running cinematic to an absolute time (resync/admin tooling). |

## Authoring in the editor

1. Open the scenario editor (`LeftCtrl + P` with `Alliance.Editor` loaded).
2. Cinematics are authored two ways:
   - scenario-scoped: add entries under `Scenario -> Cinematics`, then reference them from a `PlayCinematicAction` by Id;
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

A short intro cinematic: add a `Cinematic` to the scenario, add a `CameraTrack` with 2-3 keyframes captured from the editor view, an `OverlayTrack` with a letterbox of 0.1 fading in from black, then a `PlayCinematicAction` (by Id, audience `All`) in the act's `ConditionalActions` behind a mission-start condition.

## Extending in code

To add a new track type:

1. Create the keyframe and track classes under `Models/Tracks/` (derive from `CinematicKeyframe`/`CinematicTrack`, expose `GetKeyframes()`). The serializer discovers derived types automatically.
2. Sample it: continuous values go in a `Sample*` method of `CinematicPlayer` (gated by `RequiresVisualSampling`), one-shot effects in `FireCrossings`.
3. Add the callback to `ICinematicPlaybackSink` and implement it in `CinematicView` (client) and the server sink (usually a no-op).
4. Editor: add a lane color and `CreateTrack` entry in `CinematicTimelineVM`, an `AddKey` case, a `GenericKeyframeVM` subclass and its XAML data template.

## Known limitations and roadmap

- Only one cinematic at a time per client (last-started wins the camera); the server supports concurrent cinematics for different audiences.
- Roles resolve to `MainAgent`/`Viewer`/`Player` only — a named-role table (e.g. "Boss") is a planned extension.
- `AgentAnimationTrack` needs a rework: action-name pickers, proper body-action looping, role targeting.
- `AudioTrack` ignores volume and loop.
- Late-join mid-cinematic is not wired: joining clients only receive the cinematic if it is re-broadcast.
