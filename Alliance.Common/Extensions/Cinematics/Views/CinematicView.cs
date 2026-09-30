#if !SERVER
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;
using TaleWorlds.InputSystem;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Client-side playback of cinematics started by PlayCinematicMessage (and by the editor preview).
	/// Owns the custom camera, the Gauntlet overlay layer and the local CinematicPlayer.
	/// Only one cinematic plays at a time - starting one stops the previous.
	/// </summary>
	public class CinematicView : MissionView, ICinematicPlaybackSink, ICinematicBindings
	{
		private const float Deg2Rad = 0.017453292f;
		/// <summary>Below this elapsed time, receivers start the cinematic from the beginning instead of
		/// seeking (see the catch-up logic in PlayCinematic).</summary>
		private const float CatchUpLeniencySec = 0.2f;

		private Camera _camera;
		private bool _wasFirstPerson;
		private readonly List<Agent> _hiddenAgents = new List<Agent>();
		/// <summary>Fake agents spawned for staged-extras tracks of the current cinematic (per-machine).</summary>
		private readonly Dictionary<AgentActionTrack, List<FakeAgent>> _stagedFakes = new Dictionary<AgentActionTrack, List<FakeAgent>>();

		private CinematicPlayer _player;
		private bool _skippable;
		private bool _mainAgentControllerDisabled;
		private bool _savedMainAgentControllerDisabled;
		private bool _savedAllowInputWithCustomCamera;
		/// <summary>The pose the player was watching from when the cinematic started (ViewerCamera target).</summary>
		private MatrixFrame? _viewerCameraFrame;

		private GauntletLayer _overlayLayer;
		private ScreenBase _overlayScreen;
		private CinematicOverlayVM _overlayVM;
		private bool _photoModeOn;
		private readonly HashSet<GauntletLayer> _hiddenUiLayers = new HashSet<GauntletLayer>();

		private SceneView _editorSceneView;

		public SceneView EditorSceneView { get => _editorSceneView; set => _editorSceneView = value; }
		public bool IsEditorMode => MissionScreen == null && _editorSceneView != null;
		public bool IsPlaying => _player != null && _player.IsPlaying;
		public float CurrentTime => _player?.CurrentTime ?? 0f;
		/// <summary>Name of the currently playing cinematic, or null. Used for stop-by-name matching.</summary>
		public string PlayingCinematicName => _player?.Cinematic?.Name;

		public void ReapplyEditorCamera()
		{
			if (_camera != null && _editorSceneView != null)
				_editorSceneView.SetCamera(_camera);
		}

		public CinematicView() { ViewOrderPriority = 30; }

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			StopCinematic();
			// Mission end: nothing survives the scene (entities die with it) - drop the registries.
			FakeAgent.DespawnAll();
			FakeAgentStore.Clear();
		}

		public override void OnPreDisplayMissionTick(float dt)
		{
			base.OnPreDisplayMissionTick(dt);
#if DEBUG
			if (Mission != null)
			{
				if (TaleWorlds.InputSystem.Input.InputManager != null)
				{
					if (TaleWorlds.InputSystem.Input.IsKeyPressed(InputKey.Home) && Mission.Current != null && Mission.Current.Agents.Count > 0)
						PlayTestCinematic();
					else if (TaleWorlds.InputSystem.Input.IsKeyPressed(InputKey.End))
					{
						// Kit test missions: play the cinematic currently open in the editor for real
						// (staged extras, agent actions, camera - the full pipeline in a live mission).
						Cinematic active = EditorToolsManager.ActiveEditingCinematic;
						if (active != null)
						{
							Log($"[Cinematic] Playing edited cinematic '{active.Name}' in the test mission.", LogLevel.Debug);
							PlayCinematic(active, MissionTime.Now.NumberOfTicks / 10000000f, true, null);
						}
						else
						{
							PlayAuthoredCinematic();
						}
					}
				}
			}
#endif
			// Skip is local-only by design: never broadcast, other players keep watching.
			if (Mission != null && !IsEditorMode && IsPlaying && _skippable && TaleWorlds.InputSystem.Input.IsKeyPressed(InputKey.Space))
				Skip();

			if (_player != null && _player.IsPlaying)
			{
				_player.Tick(dt);
				HideOtherUiLayers();
				// Staged extras move on their own blockout locomotion (client-deterministic).
				FakeAgent.TickAll(dt, MissionScreen?.CombatCamera?.Frame.origin);
			}
		}

		/// <summary>Spawns per-machine FakeAgents for every staged-extras track of the cinematic.
		/// Staged commands are then executed locally by every client, deterministically.</summary>
		private void SpawnStagedFakes(Cinematic cinematic)
		{
			if (Mission?.Scene == null) return;
			// Context diagnostic: what the kit test-mission provides vs the in-game mission.
			int characterCount = -1;
			try { characterCount = MBObjectManager.Instance?.GetObjectTypeList<BasicCharacterObject>()?.Count ?? -1; }
			catch { }
			Log($"[Cinematic] Mission context: Game.Current={Game.Current != null}, BasicCharacters={characterCount}", LogLevel.Debug);

			for (int i = 0; i < cinematic.Tracks.Count; i++)
			{
				if (cinematic.Tracks[i] is not AgentActionTrack track || track.Target?.IsStagedMode != true) continue;
				// Key matches the server-side persistent group key (cinematic name # track index).
				string key = $"{cinematic.Name}#{i}";
				List<FakeAgent> fakes = track.Target.SpawnFakes(Mission.Scene);
				if (fakes.Count > 0)
				{
					_stagedFakes[track] = fakes;
					FakeAgentStore.Track(key, fakes);
				}
			}
		}

		private void DespawnStagedFakes(bool evenPersistent)
		{
			foreach (KeyValuePair<AgentActionTrack, List<FakeAgent>> kv in _stagedFakes)
			{
				bool persist = kv.Key?.Target?.Persist == true;
				if (evenPersistent || !persist)
					foreach (FakeAgent fake in kv.Value) fake?.Despawn();
			}
			_stagedFakes.Clear();
		}

#if DEBUG
		private static readonly AgentBehaviorMode[] _debugModes =
			{ AgentBehaviorMode.Lock, AgentBehaviorMode.HidePlayers, AgentBehaviorMode.HideAll, AgentBehaviorMode.Free };
		private int _debugModeIndex;
		private int _authoredIndex;

		private void PlayTestCinematic()
		{
			if (Mission.Current == null || Mission.Current.Agents.Count == 0)
			{
				Log("[Cinematic] Test cinematic skipped: no agents in the mission to stage on.", LogLevel.Warning);
				return;
			}
			Agent main = Mission.Current.Agents.GetRandomElement();
			Vec3 eye = main != null
				? main.Position + new Vec3(0f, 0f, main.GetEyeGlobalHeight())
				: (MissionScreen?.CombatCamera?.Frame.origin ?? Vec3.Zero);
			MatrixFrame frame = MissionScreen?.CombatCamera?.Frame ?? MatrixFrame.Zero;
			float fov = MissionScreen?.CombatCamera?.GetFovVertical() * 60 ?? 70f;

			Cinematic cinematic = new Cinematic
			{
				DurationSec = 6f, FadeInSec = 0.4f, FadeOutSec = 0.4f,
				IsSkippable = true, AgentBehavior = _debugModes[_debugModeIndex],
				Name = "Test cinematic"
			};
			_debugModeIndex = (_debugModeIndex + 1) % _debugModes.Length;

			CameraTrack camTrack = new CameraTrack();
			camTrack.Keyframes.Add(new CameraKeyframe(0f) { Frame = FrameValue.FromFrame(frame), Fov = fov });
			camTrack.Keyframes.Add(new CameraKeyframe(3f) { Frame = FrameValue.FromFrame(Orbit(eye, 5f, 1.6f, 2.1f)), Fov = 70f, Interpolation = Interpolation.CatmullRom });
			camTrack.Keyframes.Add(new CameraKeyframe(6f) { Frame = FrameValue.FromFrame(frame), Fov = fov, Interpolation = Interpolation.CatmullRom });
			cinematic.Tracks.Add(camTrack);

			OverlayTrack OverlayTrack = new OverlayTrack();
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(0f) { Letterbox = 0f, FadeAlpha = 0f });
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(1.5f) { Letterbox = 0.2f, FadeAlpha = 0f, Interpolation = Interpolation.CatmullRom });
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(5f) { Letterbox = 0.2f, FadeAlpha = 0f });
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(6f) { Letterbox = 0f, FadeAlpha = 0f, Interpolation = Interpolation.CatmullRom });
			cinematic.Tracks.Add(OverlayTrack);

			SubtitleTrack subTrack = new SubtitleTrack();
			subTrack.Keyframes.Add(new SubtitleKeyframe(0.4f) { Text = new LocalizedString("Test - " + cinematic.AgentBehavior), Duration = 3f, HPosition = SubtitleHPosition.Center });
			cinematic.Tracks.Add(subTrack);

			PlayCinematic(cinematic, MissionTime.Now.NumberOfTicks / 10000000f, true, null);
			Log($"[Cinematic] test - mode={cinematic.AgentBehavior} (Home=cycle, End=authored)", LogLevel.Debug);
		}

		private void PlayAuthoredCinematic()
		{
			Cinematic cinematic = null;
			var list = ScenarioManager.Instance?.CurrentScenario?.Cinematics;
			if (list != null && list.Count > 0)
			{
				cinematic = list[_authoredIndex % list.Count];
				_authoredIndex++;
			}
			if (cinematic == null)
			{
				Log("[Cinematic] no authored cinematic on the current scenario", LogLevel.Debug);
				return;
			}
			PlayCinematic(cinematic, MissionTime.Now.NumberOfTicks / 10000000f, cinematic.IsSkippable, null);
			Log($"[Cinematic] previewing authored '{cinematic.Name}' ({_authoredIndex}/{list.Count})", LogLevel.Debug);
		}

		private static MatrixFrame Orbit(Vec3 target, float radius, float height, float angle)
		{
			Vec3 pos = target + new Vec3((float)System.Math.Sin(angle) * radius, (float)System.Math.Cos(angle) * radius, height);
			return CameraMath.LookAtFrame(pos, target);
		}
#endif

		public void PlayCinematic(Cinematic cinematic, float startTimeInSeconds, bool isSkippable, List<object> dynamicValues)
		{
			if (cinematic == null) return;
			StopCinematic();

			_skippable = isSkippable;
			// Rewrite the local copy's dynamic slots with the server-resolved values
			ApplyDynamicValues(cinematic, dynamicValues);
			// ViewerCamera targets resolve to the pose the player was watching from when the cinematic
			// started - captured before the cinematic camera takes over.
			_viewerCameraFrame = MissionScreen?.CombatCamera?.Frame;

			TakeCamera();
			ApplyAgentBehaviorOnStart(cinematic.AgentBehavior);
			SpawnStagedFakes(cinematic);
			// Free mode: keep player input alive while the cinematic camera renders.
			if (MissionScreen != null)
			{
				_savedAllowInputWithCustomCamera = MissionScreen.AllowInputWithCustomCamera;
				MissionScreen.AllowInputWithCustomCamera = cinematic.AgentBehavior == AgentBehaviorMode.Free;
			}
			StartEffects();

			_player = new CinematicPlayer(cinematic, this, this);
			_player.Start();

			// Catch-up from the shared anchor: elapsed = mission time now - mission time at cinematic start.
			// (except if we're within the leniency window).
			if (!IsEditorMode && Mission.Current != null && startTimeInSeconds > 0f)
			{
				float elapsed = (MissionTime.Now.NumberOfTicks / 10000000f) - startTimeInSeconds;
				if (elapsed > CatchUpLeniencySec) _player.Seek(elapsed);
			}

			if (!_player.IsPlaying) StopCinematic();
		}

		public void StopCinematic()
		{
			if (_player != null) { _player.Stop(); _player = null; }
			if (MissionScreen != null) MissionScreen.AllowInputWithCustomCamera = _savedAllowInputWithCustomCamera;
			_viewerCameraFrame = null;
			ReleaseCamera();
			RestoreHiddenAgents();
			// Non-persistent staged extras die with the cinematic; persistent ones survive (synced to
			// late joiners by the server registry) and are cleaned up at mission end.
			DespawnStagedFakes(evenPersistent: false);
			if (_mainAgentControllerDisabled)
			{
				MissionMainAgentController mainAgentController = Mission?.GetMissionBehavior<MissionMainAgentController>();
				if (mainAgentController != null) mainAgentController.IsDisabled = _savedMainAgentControllerDisabled;
				_mainAgentControllerDisabled = false;
			}
			// Photo-mode off + color-grade restore happen in StopEffects (also covers editor scene).
			StopEffects();
		}

		/// <summary>Local, immediate stop - never broadcast: skipping only affects the local player.</summary>
		public void Skip() { if (_skippable) StopCinematic(); }
		public void Seek(float time) => _player?.Seek(time);
		public void SampleOnce() => _player?.SampleOnce();

		private void TakeCamera()
		{
			if (_camera == null)
			{
				_camera = Camera.CreateCamera();
				if (MissionScreen?.CombatCamera != null) _camera.FillParametersFrom(MissionScreen.CombatCamera);
			}
			if (MissionScreen != null)
			{
				_wasFirstPerson = Mission.CameraIsFirstPerson;
				Mission.CameraIsFirstPerson = false;
				MissionScreen.CustomCamera = _camera;
			}
			else if (_editorSceneView != null)
			{
				_editorSceneView.SetCamera(_camera);
			}
		}

		private void ReleaseCamera()
		{
			if (_camera == null) return;
			if (MissionScreen != null)
			{
				MatrixFrame frame = _camera.Frame;
				MissionScreen.CustomCamera = null;
				MissionScreen.UpdateFreeCamera(frame);
				Mission.CameraIsFirstPerson = _wasFirstPerson;
			}
			_camera.ReleaseCamera();
			_camera = null;
		}

		private void ApplyAgentBehaviorOnStart(AgentBehaviorMode mode)
		{
			if (Mission == null) return;

			switch (mode)
			{
				case AgentBehaviorMode.HidePlayers:
					HideAgents(agent => agent.IsPlayerControlled);
					LockLocalAgent();
					break;
				case AgentBehaviorMode.HideAll:
					HideAgents(_ => true);
					LockLocalAgent();
					break;
				case AgentBehaviorMode.Lock:
					LockLocalAgent();
					break;
				// Free: agent stays player-controlled; input stays live via Patch_MissionScreen.
			}
		}

		/// <summary>Hides the agents matching the predicate, plus their mounts.
		/// Everything hidden is tracked and restored by RestoreHiddenAgents when playback stops.</summary>
		private void HideAgents(Func<Agent, bool> predicate)
		{
			foreach (Agent agent in Mission.Agents)
			{
				if (!predicate(agent)) continue;
				HideAgent(agent);
				HideAgent(agent.MountAgent);
			}
		}

		private void HideAgent(Agent agent)
		{
			if (agent == null || _hiddenAgents.Contains(agent)) return;
			agent.AgentVisuals?.SetVisible(false);
			_hiddenAgents.Add(agent);
		}

		private void RestoreHiddenAgents()
		{
			foreach (Agent agent in _hiddenAgents)
			{
				// Agents removed from the mission since (death, mission end) are long gone - skip them.
				if (Mission == null || !Mission.Agents.Contains(agent)) continue;
				agent.AgentVisuals?.SetVisible(true);
			}
			_hiddenAgents.Clear();
		}

		// Freezes the local player's agent.
		private void LockLocalAgent()
		{
			Agent main = Agent.Main;
			if (main != null) ClearAgentInput(main);
			MissionMainAgentController mainAgentController = Mission.GetMissionBehavior<MissionMainAgentController>();
			if (mainAgentController != null)
			{
				_savedMainAgentControllerDisabled = mainAgentController.IsDisabled;
				_mainAgentControllerDisabled = true;
				mainAgentController.IsDisabled = true;
			}
		}

		// Zero it like native conversation mode does.
		private static void ClearAgentInput(Agent main)
		{
			main.MovementFlags = Agent.MovementControlFlag.None;
			main.EventControlFlags = Agent.EventControlFlag.None;
			main.MovementInputVector = Vec2.Zero;
		}

		/// <summary>Scene receiving visual effects (color grade, photo-mode DoF): the mission scene in
		/// live play, the modding-kit scene in editor preview (Mission is null there).</summary>
		private Scene EffectsScene => Mission?.Scene ?? (IsEditorMode ? MBEditor._editorScene : null);

		public bool RequiresVisualSampling => true;

		public void OnCameraState(in CameraState state)
		{
			if (_camera == null) return;

			MatrixFrame frame = state.Frame;
			if (state.Roll != 0f) frame.rotation.RotateAboutForward(state.Roll);
			_camera.Frame = frame;
			_camera.SetFovVertical(state.Fov * Deg2Rad, Screen.AspectRatio, state.Near, state.Far);

			if (IsEditorMode)
				_editorSceneView?.SetCamera(_camera);

			Scene scene = EffectsScene;
			if (scene != null)
			{
				if (state.DoFEnabled)
				{
					if (!_photoModeOn) { scene.SetPhotoModeOn(true); _photoModeOn = true; }
					scene.SetPhotoModeFocus(state.DoFStart, state.DoFEnd, state.FocusDistance, state.Exposure);
				}
				else if (_photoModeOn) { scene.SetPhotoModeOn(false); _photoModeOn = false; }
				SoundManager.SetListenerFrame(frame);
			}
		}

		private void StartEffects()
		{
			try
			{
				_overlayVM = new CinematicOverlayVM { IsSkippable = _skippable };
				_overlayScreen = MissionScreen ?? ScreenManager.TopScreen;
				if (_overlayScreen != null)
				{
					_overlayLayer = new GauntletLayer("CinematicOverlay", 40);
					_overlayLayer.LoadMovie("CinematicOverlay", _overlayVM);
					_overlayScreen.AddLayer(_overlayLayer);
					HideOtherUiLayers();
				}
			}
			catch (Exception ex) { Log($"Cinematic overlay init failed: {ex.Message}", LogLevel.Warning); }
		}

		private void StopEffects()
		{
			RestoreUiLayers();
			if (_overlayLayer != null && _overlayScreen != null)
			{
				try { _overlayScreen.RemoveLayer(_overlayLayer); } catch { }
				_overlayLayer = null;
				_overlayVM = null;
				_overlayScreen = null;
				_subtitleWidgets = null;
				_subtitleWidgetsVersion = -1;
			}
			Scene restoreScene = EffectsScene;
			if (_photoModeOn && restoreScene != null) { restoreScene.SetPhotoModeOn(false); _photoModeOn = false; }
		}

		/// <summary>Hides every Gauntlet layer of the screen except the cinematic overlay, so no HUD shows over
		/// the cinematic. Restored by RestoreUiLayers when playback stops.</summary>
		private void HideOtherUiLayers()
		{
			if (IsEditorMode || _overlayLayer == null) return;
			ScreenBase screen = _overlayScreen;
			if (screen == null) return;
			foreach (ScreenLayer layer in screen.Layers)
			{
				if (layer == _overlayLayer || !(layer is GauntletLayer gauntletLayer)) continue;
				Widget root = gauntletLayer.UIContext?.Root;
				if (root == null || !root.IsVisible) continue;
				root.IsVisible = false;
				_hiddenUiLayers.Add(gauntletLayer);
			}
		}

		private void RestoreUiLayers()
		{
			foreach (GauntletLayer layer in _hiddenUiLayers)
			{
				if (layer.IsFinalized) continue;
				Widget root = layer.UIContext?.Root;
				if (root != null) root.IsVisible = true;
			}
			_hiddenUiLayers.Clear();
		}

		public void OnScreen(float letterbox, float fadeAlpha)
		{
			if (_overlayVM == null) return;
			_overlayVM.FadeAlpha = fadeAlpha;
			_overlayVM.LetterboxHeight = (int)(Screen.RealScreenResolution.y * 0.5f * letterbox);
		}

		public void OnSubtitles(List<SubtitleState> subtitles)
		{
			if (_overlayVM == null) return;
			_overlayVM.UpdateSubtitles(subtitles);
			ApplySubtitleTextStyles(subtitles);
		}

		/// <summary>Writes glow/blur/shadow/outline on each subtitle widget's own brush. Widgets render
		/// from a per-widget CLONE of the brush (BrushWidget.Brush), and these style properties have no
		/// one-level pass-through on Brush to bind from the prefab like FontSize - so they are set from
		/// code, matched to the subtitle items by list order. The widget list is cached and re-collected
		/// only when the subtitle item set changes; styles themselves are re-applied every call.</summary>
		private void ApplySubtitleTextStyles(List<SubtitleState> states)
		{
			Widget root = _overlayLayer?.UIContext?.Root;
			if (root == null || states == null || states.Count == 0) return;

			if (_subtitleWidgets == null || _subtitleWidgetsVersion != _overlayVM.StructureVersion)
			{
				List<Widget> widgets = _subtitleWidgets ??= new List<Widget>();
				widgets.Clear();
				CollectWidgetsById(root, "SubtitleText", widgets);
				_subtitleWidgetsVersion = _overlayVM.StructureVersion;
			}

			int count = Math.Min(_subtitleWidgets.Count, states.Count);
			for (int i = 0; i < count; i++)
			{
				if (_subtitleWidgets[i] is not BrushWidget brushWidget) continue;
				TaleWorlds.GauntletUI.Style style = brushWidget.Brush.DefaultStyle;
				SubtitleState state = states[i];
				if (style.TextGlowRadius != state.GlowRadius) style.TextGlowRadius = state.GlowRadius;
				if (style.TextBlur != state.Blur) style.TextBlur = state.Blur;
				if (style.TextShadowOffset != state.ShadowOffset) style.TextShadowOffset = state.ShadowOffset;
				if (style.TextOutlineAmount != state.OutlineAmount) style.TextOutlineAmount = state.OutlineAmount;
			}

			// Item widgets are created during layout - a subtitle added this frame may have no widget
			// yet. Force a re-collect next call until the tree catches up.
			if (_subtitleWidgets.Count < states.Count) _subtitleWidgetsVersion = -1;
		}

		private List<Widget> _subtitleWidgets;
		private int _subtitleWidgetsVersion = -1;

		private static void CollectWidgetsById(Widget widget, string id, List<Widget> found)
		{
			if (widget.Id == id) found.Add(widget);
			for (int i = 0; i < widget.ChildCount; i++) CollectWidgetsById(widget.GetChild(i), id, found);
		}

		public void OnAudio(string soundEvent, float volume, bool loop)
		{
			if (string.IsNullOrEmpty(soundEvent) || Mission?.Scene == null) return;
			try { SoundEvent.CreateEventFromString(soundEvent, Mission.Scene).Play(); }
			catch (Exception ex) { Log($"Cinematic audio '{soundEvent}' failed: {ex.Message}", LogLevel.Warning); }
		}

		/// <summary>Entity held by a ValueSource slot: literal scene-entity slots resolve locally through
		/// the marker index; variable/function slots were rewritten with the server-resolved entity.</summary>
		public WeakGameEntity ResolveEntity(ValueSource<WeakGameEntity> slot)
			=> slot?.Resolve(null) ?? WeakGameEntity.Invalid;

		public void OnEntityAction(EntityActionKeyframe kf)
		{
			WeakGameEntity entity = ResolveEntity(kf?.Entity);
			if (!entity.IsValid)
			{
				if (_warnedEntityKeyframes.Add(kf))
					Log($"[Cinematic] Entity action '{kf.Kind}' skipped: its entity could not be resolved on this machine.", LogLevel.Warning);
				return;
			}

			switch (kf.Kind)
			{
				case EntityActionKind.SetVisible:
					entity.SetVisibilityExcludeParents(kf.Visible);
					break;
				case EntityActionKind.Teleport:
					{
						MatrixFrame? destination = kf.Destination?.ResolveWorldFrame(null);
						if (destination.HasValue) entity.SetGlobalFrame(destination.Value);
						break;
					}
				case EntityActionKind.Fx:
					switch (kf.Fx)
					{
						case EntityFxMode.Burst: entity.BurstEntityParticle(kf.FxChildren); break;
						case EntityFxMode.Pause: entity.PauseParticleSystem(kf.FxChildren); break;
						case EntityFxMode.Resume: entity.ResumeParticleSystem(kf.FxChildren); break;
					}
					break;
			}
		}

		/// <summary>Client-local kinematic MoveTo: position and rotation interpolated between the frame
		/// captured at keyframe start and the resolved destination. Deterministic from the shared clock.</summary>
		public void OnEntityMove(EntityActionKeyframe kf, MatrixFrame startFrame, float t)
		{
			WeakGameEntity entity = ResolveEntity(kf?.Entity);
			if (!entity.IsValid) return;
			MatrixFrame? destinationFrame = kf.Destination?.ResolveWorldFrame(null);
			if (!destinationFrame.HasValue) return;
			MatrixFrame destination = destinationFrame.Value;
			MatrixFrame frame = new MatrixFrame(
				Mat3.Lerp(startFrame.rotation, destination.rotation, t),
				Vec3.Lerp(startFrame.origin, destination.origin, t));
			entity.SetGlobalFrame(frame, false);
		}

		public void OnAgentAction(AgentActionTrack track, AgentActionKeyframe kf)
		{
			if (track == null || kf == null) return;

			if (IsEditorMode)
			{
				// Modding-kit preview: the editor's staged fakes execute the command.
				CinematicPreviewBridge.PreviewAgentActionHandler?.Invoke(track, kf);
				return;
			}

			// Staged extras: per-machine fakes - every client executes everything locally,
			// deterministically from the shared cinematic clock.
			if (track.Target?.IsStagedMode == true && _stagedFakes.TryGetValue(track, out List<FakeAgent> fakes))
			{
				MatrixFrame? destination = kf.Kind is AgentActionKind.Teleport or AgentActionKind.MoveTo
					? kf.Destination?.ResolveWorldFrame(null)
					: null;
				for (int i = 0; i < fakes.Count; i++)
				{
					FakeAgent fake = fakes[i];
					if (fake == null || !fake.IsValid) continue;
					switch (kf.Kind)
					{
						case AgentActionKind.Teleport:
							if (destination.HasValue) fake.Teleport(kf.KeepFormationOffset ? track.Target.GetMemberFrame(destination.Value, i) : destination.Value);
							break;
						case AgentActionKind.MoveTo:
							if (destination.HasValue)
							{
								MatrixFrame member = kf.KeepFormationOffset ? track.Target.GetMemberFrame(destination.Value, i) : destination.Value;
								fake.MoveTo(member, kf.Speed == AgentMoveSpeed.Run, kf.MoveAnimation,
									kf.Speed == AgentMoveSpeed.Custom ? kf.CustomSpeed : (float?)null, kf.MountMoveAnimation);
							}
							break;
						case AgentActionKind.PlayAnimation:
							fake.PlayClip(kf.ClipName, kf.ActionSpeed, kf.Loop);
							fake.PlayMountClip(kf.MountClipName, kf.ActionSpeed, kf.Loop);
							break;
						case AgentActionKind.PlayFacial:
							if (!string.IsNullOrEmpty(kf.FacialAnimName)) fake.SetFacialAnimation(kf.FacialAnimName, kf.FacialLoop);
							break;
						case AgentActionKind.SetVisible:
							fake.SetVisible(kf.Visible);
							break;
					}
				}
				return;
			}

			// True agents, live play: the server executes teleport/move/body animations authoritatively
			// and they replicate natively. Visibility and facial animations do not replicate reliably -
			// apply them locally too (the agent slots are the server-resolved literals).
			foreach (Agent agent in track.Target.ResolveAgents(null))
			{
				if (agent == null) continue;
				switch (kf.Kind)
				{
					case AgentActionKind.SetVisible:
						SetAgentVisible(agent, kf.Visible, kf.IncludeMount);
						break;
					case AgentActionKind.PlayFacial:
						if (!string.IsNullOrEmpty(kf.FacialAnimName))
							agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, kf.FacialAnimName, kf.FacialLoop);
						break;
				}
			}
		}

		private static void SetAgentVisible(Agent agent, bool visible, bool includeMount)
		{
			agent.AgentVisuals?.SetVisible(visible);
			if (includeMount && agent.MountAgent != null) agent.MountAgent.AgentVisuals?.SetVisible(visible);
		}

		public void OnEventActions(List<ActionBase> actions)
		{
			if (actions == null) return;
			foreach (ActionBase action in actions)
			{
				try { action?.ExecuteClient(); }
				catch (Exception ex) { Log($"Cinematic event action failed: {ex.Message}", LogLevel.Warning); }
			}
		}

		public void OnFinished() => StopCinematic();

		private readonly HashSet<CinematicTargetType> _warnedTargetTypes = new HashSet<CinematicTargetType>();
		private readonly HashSet<CinematicKeyframe> _warnedEntityKeyframes = new HashSet<CinematicKeyframe>();

		/// <summary>Resolves a target to a full world frame on the local machine: viewer targets against
		/// the local player (or editor preview), specific agents through their literal slots (rewritten
		/// with the server-resolved values), entities through the BuildSystem marker index.</summary>
		public MatrixFrame? ResolveTargetFrame(CinematicTarget target)
		{
			if (target == null || target.Type == CinematicTargetType.None) return null;

			switch (target.Type)
			{
				case CinematicTargetType.Position:
					return target.Position?.ToFrame();

				case CinematicTargetType.ViewerAgent:
					{
						Agent main = Agent.Main;
						// Editor preview has no agents - unresolvable there.
						return main != null
							? new MatrixFrame(main.Frame.rotation, main.Position + new Vec3(0f, 0f, main.GetEyeGlobalHeight()))
							: (MatrixFrame?)null;
					}

				case CinematicTargetType.ViewerCamera:
					// Captured at cinematic start; null in editor preview (no combat camera there).
					return _viewerCameraFrame;

				case CinematicTargetType.SpecificAgent:
					{
						// The slot was rewritten to a Literal by ApplyDynamicValues, so resolving it is a
						// plain local lookup of the server-picked agent. Unrewritten variable slots (editor
						// preview) fall back to the scenario globals store (null in editor preview without one).
						Agent agent = target.AgentVariable?.Resolve(null);
						if (agent != null)
						{
							return new MatrixFrame(agent.Frame.rotation, agent.Position + new Vec3(0f, 0f, agent.GetEyeGlobalHeight()));
						}
						WarnUnresolvedOnce(target.Type, null);
						return null;
					}

				case CinematicTargetType.SpecificEntity:
					{
						// Literal scene-entity slots resolve locally; variable/function slots were
						// rewritten with the server-resolved entity.
						WeakGameEntity entity = target.Entity?.Resolve(null) ?? WeakGameEntity.Invalid;
						if (entity.IsValid) return entity.GetGlobalFrame();
						WarnUnresolvedOnce(target.Type, null);
						return null;
					}

				default:
					return null;
			}
		}

		// Rewrites the local cinematic copy's dynamic slots with the server-resolved values
		private static void ApplyDynamicValues(Cinematic cinematic, List<object> dynamicValues)
		{
			if (dynamicValues == null || dynamicValues.Count == 0) return;
			List<ValueSourceHelper.DynamicSlot> slots = ValueSourceHelper.CollectDynamicSlots(cinematic);
			if (slots.Count != dynamicValues.Count)
			{
				Log($"[Cinematic] Dynamic data mismatch (server sent {dynamicValues.Count} values, local copy has {slots.Count} slots) - skipping rewrite.", LogLevel.Warning);
				return;
			}
			for (int i = 0; i < slots.Count; i++)
			{
				ValueSourceHelper.SetSlotLiteral(slots[i], dynamicValues[i]);
			}
		}

		private void WarnUnresolvedOnce(CinematicTargetType type, string detail)
		{
			if (_warnedTargetTypes.Add(type))
				Log($"[Cinematic] Target {type} '{detail ?? ""}' could not be resolved - camera falls back to the stored frame.", LogLevel.Warning);
		}
	}
}
#endif
