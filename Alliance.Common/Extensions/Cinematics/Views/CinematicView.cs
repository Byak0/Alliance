#if !SERVER
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
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;

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
		}

		public override void OnPreDisplayMissionTick(float dt)
		{
			base.OnPreDisplayMissionTick(dt);
#if DEBUG
			if (Mission != null)
			{
				if (Input.IsKeyPressed(InputKey.Home)) PlayTestCinematic();
				else if (Input.IsKeyPressed(InputKey.End)) PlayAuthoredCinematic();
			}
#endif
			// Skip is local-only by design: never broadcast, other players keep watching.
			if (Mission != null && !IsEditorMode && IsPlaying && _skippable && Input.IsKeyPressed(InputKey.Space))
				Skip();

			if (_player != null && _player.IsPlaying)
			{
				_player.Tick(dt);
				HideOtherUiLayers();
			}
		}

#if DEBUG
		private static readonly AgentBehaviorMode[] _debugModes =
			{ AgentBehaviorMode.Lock, AgentBehaviorMode.HidePlayers, AgentBehaviorMode.HideAll, AgentBehaviorMode.Free };
		private int _debugModeIndex;
		private int _authoredIndex;

		private void PlayTestCinematic()
		{
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
			_overlayVM?.UpdateSubtitles(subtitles);
		}

		public void OnAudio(string soundEvent, float volume, bool loop)
		{
			if (string.IsNullOrEmpty(soundEvent) || Mission?.Scene == null) return;
			try { SoundEvent.CreateEventFromString(soundEvent, Mission.Scene).Play(); }
			catch (Exception ex) { Log($"Cinematic audio '{soundEvent}' failed: {ex.Message}", LogLevel.Warning); }
		}

		public void OnEntityVisibility(GameEntityRef entity, bool visible)
		{
			if (entity == null || string.IsNullOrEmpty(entity.RefId)) return;
			try
			{
				WeakGameEntity wge = Alliance.Common.Extensions.BuildSystem.EntityMarkerIndex.Resolve(entity.RefId);
				if (wge.IsValid) wge.SetVisibilityExcludeParents(visible);
			}
			catch (Exception ex) { Log($"Cinematic entity-visibility failed: {ex.Message}", LogLevel.Warning); }
		}

		public void OnAgentAnimation(string role, string actionName, string facialAnimation, bool loop)
		{
			// Stub track: no role resolution yet, only the local player's agent.
			Agent agent = Agent.Main;
			if (agent == null) return;
			try
			{
				if (!string.IsNullOrEmpty(actionName))
					agent.SetActionChannel(0, ActionIndexCache.Create(actionName));
				if (!string.IsNullOrEmpty(facialAnimation))
					agent.SetAgentFacialAnimation(Agent.FacialAnimChannel.High, facialAnimation, loop);
			}
			catch (Exception ex) { Log($"Cinematic agent-animation failed: {ex.Message}", LogLevel.Warning); }
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
						// plain local lookup of the server-picked agent (null in editor preview).
						Agent agent = target.AgentVariable?.Resolve(null, null);
						if (agent != null)
						{
							return new MatrixFrame(agent.Frame.rotation, agent.Position + new Vec3(0f, 0f, agent.GetEyeGlobalHeight()));
						}
						WarnUnresolvedOnce(target.Type, null);
						return null;
					}

				case CinematicTargetType.SpecificEntity:
					{
						if (target.Entity == null || string.IsNullOrEmpty(target.Entity.RefId)) return null;
						try
						{
							WeakGameEntity wge = BuildSystem.EntityMarkerIndex.Resolve(target.Entity.RefId);
							if (wge.IsValid) return wge.GetGlobalFrame();
						}
						catch { }
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
