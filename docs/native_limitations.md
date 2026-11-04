# TaleWorlds Engine Limitations & Workarounds

This file is the single record of native (TaleWorlds) engine limitations, bugs and
behaviors that Alliance works around or relies on. It exists so that:

- **Why-comments stay short.** Inline comments that justify a workaround point here
  instead of telling the whole story: `// Workaround: <title>, see docs/native_limitations.md#<anchor>`.
- **Claims stay accountable.** Every entry carries a verification status and evidence.
  Unfounded folklore (a workaround paid forever for a bug that no longer exists) is a bug.

## Rules

1. Every entry must have an **anchor heading** (the slug is referenced from code comments — never rename an existing slug).
2. Every entry must carry a **Status**:
   - `verified` — confirmed against decompiled source (`Alliance.Common/TW_References/BannerlordSource/`), a repro, or observed engine behavior with steps.
   - `legacy-unverified` — claim inherited from an older pipeline or written from memory; must be re-verified the next time the workaround's code is touched, then either confirmed (`verified`) or removed together with its workaround.
3. Evidence goes in the entry: decompiled source path (`BannerlordSource/TaleWorlds/...`), repro steps, or the commit that observed it.
4. When a workaround is no longer needed, **delete the workaround and the entry**. Dead workarounds cost performance and mislead readers.
5. Verification method: ask the `tw-native-explorer` agent to read the decompiled source and cite the exact file/line before flipping a status to `verified`.

---

## A. Limitations & bugs (we pay a workaround)

### tableau-inventory-scene-deadlock
Reading the native `inventory_character_scene` deadlocks intermittently when the
modding kit editor is open — its resource pipeline is not pumped there.

- **Symptom**: freeze/deadlock when the preview acquires the native scene in the kit editor.
- **Workaround**: build a procedural empty scene (`Scene.CreateNewScene`) for the character
  tableaus instead of reading the native one. Backdrop is lost (black background); previews stay functional everywhere.
- **Affected**: `ALCharacterTableauSceneCache.Initialize()`.
- **Status**: `legacy-unverified` — observed historically in the kit editor; no repro written down yet.

### tableau-scene-lifetime
Destroying or recreating the shared tableau scene while list refreshes recreate widgets
in the same frame deadlocks the render thread on the next `CreateNewScene`/read of that scene.

- **Symptom**: render-thread deadlock on scene swap during widget refresh.
- **Workaround**: the shared scene (and its slots) lives for the whole session, like the native
  `ThumbnailCacheManager` inventory scene. Slots are recycled, the scene never dies.
- **Affected**: `ALCharacterTableauSceneCache.ReleaseSlot()` (deliberately does nothing but free the slot).
- **Status**: `legacy-unverified` — matches native behavior observed in `ThumbnailCacheManager` (scene kept alive); no decompiled citation yet.

### tableau-skeleton-advance-per-render
The tableau scene advances its fake skeletons on **every** tableau render. Rendering several
tableaus of the same scene in one frame multiplies their animation speed.

- **Symptom**: characters animate N× faster when N tableaus render simultaneously.
- **Workaround**: exactly one tableau renders per frame — continuous ones (selected preview) take
  priority, the others round-robin. Fakes advance with real elapsed time via `FakeAgent.TickOnce`,
  not with the render rate.
- **Affected**: `AL_CharacterTableau.ShouldRenderThisFrame()`, `AL_CharacterTableau.OnTick()`.
- **Status**: `legacy-unverified` — the scheduler exists to bound this; confirm the per-render skeleton advance in decompiled `TableauView`/scene update code.

### tableau-requires-postfx
A tableau that does not render with postfx may not render its textures fully.

- **Symptom**: partially rendered / black tableau textures.
- **Workaround**: `tableauView.SetRenderWithPostfx(true)` on every render callback.
- **Affected**: `AL_CharacterTableau.CharacterTableauContinuousRenderFunction()`, `AL_FormationTableau.FormationTableauRenderFunction()`.
- **Status**: `legacy-unverified` — comment claims it is "extremely important"; no repro steps recorded.

### scene-default-fog-hazes-tableau
A freshly created scene carries a default fog that hazes everything the tableau renders.

- **Symptom**: "blurry/hazy" preview renders.
- **Workaround status**: **unresolved** — the attempted fixes (`SetAtmosphereWithName`,
  `SetFogAmbientColor`, `SetFog`) are sitting commented-out in `ALCharacterTableauSceneCache.Initialize()`.
  Either confirm the fog entry is still needed and apply a clean fix, or delete the dead experiments.
- **Status**: `legacy-unverified` — claims conflict across iterations; settle before the next touch of that file.

### kit-resource-queue-not-pumped
The modding kit never pumps the deferred resource queue, so a `CheckResources` gate on
preview visuals never passes there.

- **Symptom**: previews stay invisible forever in the kit editor when dressed visuals wait on `CheckResources`.
- **Workaround**: acquire/dress fakes with synchronous mesh loading (meshes pop in; face textures appear as they finish),
  and skip the `CheckResources` gate for tableau previews.
- **Affected**: `FakeAgent` resource pipeline, `AL_CharacterTableau.RebuildFake()`.
- **Status**: `legacy-unverified`.

### banner-drop-physics
Dropped banners fly above the ground instead of falling (native `SpawnedItemEntity.OnTickParallel2`
banner branch lacks the ground checks the generic branch has). As of game version 1.3 a client-side
patch is additionally required to make dropped banners focusable/pickable.

- **Symptom**: banners hovering in mid-air after being dropped; not pickable on clients.
- **Workaround**: Harmony patch reimplementing `OnTickParallel2` with ground detection + frame
  synchronization to clients (`Patch_SpawnedItemEntity`), plus the client-side focusability patch.
- **Affected**: `Alliance.Server/Patch/HarmonyPatch/Patch_SpawnedItemEntity.cs`.
- **Status**: `verified` (behavior reproduced in game; the hovering banner is observable).

### native-compression-limits
Native network compression ranges are sized for native game modes and crash or corrupt
sync beyond them (agents per mission > 255, mission objects > ~2k, health/gold/time ranges).

- **Constraint**: `CompressionInfo` values are serialized on the wire — **client and server must
  set identical values** or the stream desyncs. Both `Alliance.Client/SubModule.cs` and
  `Alliance.Server/SubModule.cs` call `DirtyCommonPatcher.IncreaseNativeLimits()`.
- **Workaround**: override the static compression infos (`AgentOffsetCompressionInfo`,
  `MissionObjectIDCompressionInfo`, health/missile-speed/formation/gold/time limits) — see
  `Alliance.Common/Patch/DirtyCommonPatcher.cs` for the exact table.
- **Open question**: which overrides are still necessary after the 1.2 optimizations (TODO in code).
  Any change must ship to client **and** server simultaneously.
- **Status**: `verified` (deliberate design; ranges chosen by us, mirrored on both sides).

### native-intermission-vote-append-only
Native intermission vote messages are append-only: `MultiplayerIntermissionMapItemAdded` etc.
only add items, and vote counts are later updated by index. Items cannot be removed or reordered.

- **Symptom**: stale vote items from the previous mission survive into the next intermission.
- **Workaround**: on mission end, broadcast the native `CountingForMission` state (clears `MPIntermissionVM`
  displayed lists) plus a custom clear message (clears `MultiplayerIntermissionVotingManager` items on the client),
  then re-add all candidates.
- **Affected**: `Alliance.Server/Extensions/NativeIntermissionVote/NetworkMessages/NativeIntermissionVoteMsg.cs`,
  `Alliance.Client/Extensions/NativeIntermissionVote/Handlers/NativeIntermissionVoteHandler.cs`.
- **Status**: `legacy-unverified` — behavior inferred from native VM code; cite `MultiplayerIntermissionVotingManager` to verify.

### create-mission-object-callbacks
`GameEntity.CreateMissionObject` only calls `OnInit` on the created script — never `AfterMissionStart`.

- **Symptom**: scripts added at runtime never receive their start-phase callbacks.
- **Workaround**: invoke the start callback manually after creation (`BuildBehavior.InvokeAfterMissionStart`).
- **Affected**: `Alliance.Common/Extensions/BuildSystem/Behaviors/BuildBehavior.cs`.
- **Status**: `legacy-unverified` — easy to confirm in decompiled `MissionObject`/`GameEntity` creation code.

### native-sounds-in-editor
Native sound events do not play in the modding kit editor. `MBSoundEvent.PlaySound` and
`SoundEvent.Play()` both appear to work only for a few rare sounds there.

- **Symptom**: cinematics/gameplay sounds are silent in editor.
- **Workaround**: 2D fallback `SoundEvent.PlaySound2D(soundIndex)` when `MBEditor.IsEditModeOn`
  (works for some sounds only — no full solution found yet).
- **Affected**: `Alliance.Common/Extensions/Audio/NativeAudioPlayer.cs`.
- **Status**: `legacy-unverified` — root cause (editor audio device/scene routing) unknown.

### sound-event-volume-parameter
Setting the `volume` parameter on a `SoundEvent` (`eventRef.SetParameter("volume", ...)`)
has no audible effect.

- **Symptom**: per-event volume control is ignored.
- **Workaround**: none currently (volume is controlled at source/atom level elsewhere in the audio system).
- **Affected**: `Alliance.Common/Extensions/Audio/NativeAudioPlayer.cs`.
- **Status**: `legacy-unverified` — single observation; confirm against decompiled FMOD binding.

---

## B. Native behaviors we rely on (documented, no workaround)

### mat3-euler-convention
`Mat3.ApplyEulerAngles` interprets components as x → side (pitch), y → forward (yaw), z → up (roll), in radians.

- **Used by**: `FakeAgent.WeaponFrameOf` (item rotation from XML degrees).
- **Status**: `verified` (decompiled `Mat3.ApplyEulerAngles`).

### monster-usage-paces
`monster_usage` movement data paces: 1 = stand, 2 = walk, 3 = trot, 4 = canter, 5 = gallop.
The fake's run speed (2× walk) corresponds to a trot.

- **Used by**: `FakeAgent` mounted movement clip resolution.
- **Status**: `legacy-unverified` — cite the native movement-data code/serialization to verify.

### team-color-flag-gating
Native only applies team colors to items flagged `use_team_color` (else the tableau flag decides).
A crafted weapon keeps its authored materials otherwise (`AgentVisuals.AddSkinArmorWeaponMultiMeshesToEntity`).

- **Used by**: `FakeAgent` weapon dressing (mirrors the native check instead of always recoloring).
- **Status**: `legacy-unverified` — cite `AgentVisuals.AddSkinArmorWeaponMultiMeshesToEntity` to verify.

### native-sound-catalog-location
Native declares its sounds in `sound_event_data.xml`; the Native module's `module_sound.xml` is empty.
Our sound catalog therefore reads `sound_event_data.xml` from native module paths.

- **Used by**: `AllianceData.LoadSounds()`.
- **Status**: `verified` (readable in the Native module's own XML files).

### native-audio-loop-semantics
Native sound events: loops are state (they persist until stopped), one-shots are events
(a past fire never replays). Custom files we know the duration of can be treated as
state + offset (`elapsed % duration`).

- **Used by**: `CinematicAudioLanes` sounding-window computation.
- **Status**: `legacy-unverified` — matches observed FMOD behavior; cite to verify.

---

## C. Candidates for extraction / verification

Claims still living as inline comments or stale TODOs that should either become entries
above (with status) or be deleted. Work through them when touching the related code.

| Location | Claim to settle |
|---|---|
| `DirtyCommonPatcher.cs` (commented-out block) | Does `BigRangeLowResLocalPositionCompressionInfo` widening still help arrow-heavy scenarios after 1.2? |
| `Patch_MultiplayerOptions*.cs`, `Patch_MissionPeer.cs`, `Patch_ActionSetCode.cs`, `Patch_AgentStatCalculateModel.cs`, `Patch_GameNetworkMessage.cs`, `Patch_AddTeam.cs`, `Patch_KeyBinder.cs` | Each patch's "why a clean override wasn't possible" should be stated in its header comment and, when it is a native bug, become an entry in section A. |
| `FakeAgent.cs` | 1400+ lines carrying multiple pipeline claims (synchronous mesh loading, facial animation quirks, holster conventions). Split and extract its claims here. |
| `PvCFlagDominationSpawningBehavior.cs` | "Copied from native, unusable" — is native's spawning behavior replaceable by `PvCSpawnBehavior` now? |
| `TeamSelectView.cs` | Team-switch agent/controller ownership bug — our bug or native? Investigate, then document or delete the note. |
| `SceneList.cs` | Full scene preload takes tens of seconds every mission load — engine cost or our scan cost? Measure before optimizing. |
