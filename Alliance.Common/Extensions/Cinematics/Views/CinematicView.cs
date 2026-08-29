#if !SERVER
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;
using Alliance.Common.Patch.HarmonyPatch;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Client-side playback of cinematics started by <c>PlayCinematicMessage</c> (and by the editor preview).
	/// Owns the custom camera, the Gauntlet overlay layer and the local <see cref="CinematicPlayer"/>.
	/// <para>A client never plays multiple cinematics simultaneously - starting one stops the previous.</para>
	/// </summary>
	public class CinematicView : MissionView, ICinematicPlaybackSink, ICinematicBindings
	{
		private const float Deg2Rad = 0.017453292f;

		private Camera _camera;
		private bool _wasFirstPerson;
		private bool _agentHidden;
		private AgentControllerType _savedController;
		private bool _controllerSaved;

		private CinematicPlayer _player;
		private bool _useViewerOrigin;
		private bool _skippable;

		private GauntletLayer _overlayLayer;
		private ScreenBase _overlayScreen;
		private CinematicOverlayVM _overlayVM;
		private bool _photoModeOn;

		private SceneView _editorSceneView;

		public SceneView EditorSceneView { get => _editorSceneView; set => _editorSceneView = value; }
		public bool IsEditorMode => MissionScreen == null && _editorSceneView != null;
		public bool IsPlaying => _player != null && _player.IsPlaying;
		public float CurrentTime => _player?.CurrentTime ?? 0f;
		/// <summary>Id of the currently playing cinematic, or null. Used for stop-by-id matching.</summary>
		public string PlayingCinematicId => _player?.Cinematic?.Id;

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
				_player.Tick(dt);
		}

#if DEBUG
		private static readonly AgentBehaviorMode[] _debugModes =
			{ AgentBehaviorMode.Lock, AgentBehaviorMode.Hide, AgentBehaviorMode.Free };
		private int _debugModeIndex;
		private int _authoredIndex;

		private void PlayTestCinematic()
		{
			Agent main = Agent.Main;
			Vec3 eye = main != null
				? main.Position + new Vec3(0f, 0f, main.GetEyeGlobalHeight())
				: (MissionScreen?.CombatCamera?.Frame.origin ?? Vec3.Zero);

			Cinematic cinematic = new Cinematic
			{
				DurationSec = 6f, FadeInSec = 0.4f, FadeOutSec = 0.4f,
				IsSkippable = true, AgentBehavior = _debugModes[_debugModeIndex],
				Name = "Test cinematic"
			};
			_debugModeIndex = (_debugModeIndex + 1) % _debugModes.Length;

			CameraTrack camTrack = new CameraTrack();
			camTrack.Keyframes.Add(new CameraKeyframe(0f) { Frame = FrameValue.FromFrame(Orbit(eye, 4f, 1.2f, 0.6f)), Fov = 60f });
			camTrack.Keyframes.Add(new CameraKeyframe(3f) { Frame = FrameValue.FromFrame(Orbit(eye, 5f, 1.6f, 2.1f)), Fov = 50f, Interpolation = Interpolation.CatmullRom });
			camTrack.Keyframes.Add(new CameraKeyframe(6f) { Frame = FrameValue.FromFrame(Orbit(eye, 4f, 1.2f, 3.6f)), Fov = 60f, Interpolation = Interpolation.CatmullRom });
			cinematic.Tracks.Add(camTrack);

			OverlayTrack OverlayTrack = new OverlayTrack();
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(0f) { Letterbox = 0.12f, FadeAlpha = 1f });
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(0.5f) { Letterbox = 0.12f, FadeAlpha = 0f, Interpolation = Interpolation.CatmullRom });
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(5.5f) { Letterbox = 0.12f, FadeAlpha = 0f });
			OverlayTrack.Keyframes.Add(new OverlayKeyframe(6f) { Letterbox = 0.12f, FadeAlpha = 1f, Interpolation = Interpolation.CatmullRom });
			cinematic.Tracks.Add(OverlayTrack);

			SubtitleTrack subTrack = new SubtitleTrack();
			subTrack.Keyframes.Add(new SubtitleKeyframe(0.4f) { Text = new LocalizedString("Test - " + cinematic.AgentBehavior), Duration = 3f });
			cinematic.Tracks.Add(subTrack);

			PlayCinematic(cinematic, MissionTime.Now.NumberOfTicks / 10000000f, true, false);
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
			PlayCinematic(cinematic, MissionTime.Now.NumberOfTicks / 10000000f, cinematic.IsSkippable, false);
			Log($"[Cinematic] previewing authored '{cinematic.Name}' ({_authoredIndex}/{list.Count})", LogLevel.Debug);
		}

		private static MatrixFrame Orbit(Vec3 target, float radius, float height, float angle)
		{
			Vec3 pos = target + new Vec3((float)System.Math.Sin(angle) * radius, (float)System.Math.Cos(angle) * radius, height);
			return CameraMath.LookAtFrame(pos, target);
		}
#endif

		public void PlayCinematic(Cinematic cinematic, float startTimeInSeconds, bool isSkippable, bool useViewerOrigin)
		{
			if (cinematic == null) return;
			StopCinematic();

			_useViewerOrigin = useViewerOrigin;
			_skippable = isSkippable;

			TakeCamera();
			ApplyAgentBehaviorOnStart(cinematic.AgentBehavior);
			// Free mode: keep player input alive while the cinematic camera renders (Patch_MissionScreen gate).
			Patch_MissionScreen.FreeInputEnabled =
				MissionScreen != null && cinematic.AgentBehavior == AgentBehaviorMode.Free;
			StartEffects();

			_player = new CinematicPlayer(cinematic, this, this);
			_player.Start();

			if (!IsEditorMode && Mission.Current != null && startTimeInSeconds > 0f)
			{
				float elapsed = (MissionTime.Now.NumberOfTicks / 10000000f) - startTimeInSeconds;
				if (elapsed > 0f) _player.Seek(elapsed);
			}

			if (!_player.IsPlaying) StopCinematic();
		}

		public void StopCinematic()
		{
			if (_player != null) { _player.Stop(); _player = null; }
			Patch_MissionScreen.FreeInputEnabled = false;
			ReleaseCamera();
			if (_agentHidden) { Agent.Main?.AgentVisuals?.SetVisible(true); _agentHidden = false; }
			if (_controllerSaved && Agent.Main != null)
			{
				Agent.Main.SetIsAIPaused(false);
				Agent.Main.Controller = _savedController;
				_controllerSaved = false;
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
			Agent main = Agent.Main;
			if (main == null) return;

			_savedController = main.Controller;
			_controllerSaved = true;

			switch (mode)
			{
				case AgentBehaviorMode.Hide:
					main.AgentVisuals?.SetVisible(false);
					_agentHidden = true;
					main.Controller = AgentControllerType.AI;
					main.SetIsAIPaused(true);
					break;
				case AgentBehaviorMode.Lock:
					main.Controller = AgentControllerType.AI;
					main.SetIsAIPaused(true);
					break;
				// Free: agent stays player-controlled; input stays live via Patch_MissionScreen.
			}
		}

		/// <summary>Scene receiving visual effects (color grade, photo-mode DoF): the mission scene in
		/// live play, the modding-kit scene in editor preview (Mission is null there).</summary>
		private Scene EffectsScene => Mission?.Scene ?? (IsEditorMode ? MBEditor._editorScene : null);

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
				}
			}
			catch (Exception ex) { Log($"Cinematic overlay init failed: {ex.Message}", LogLevel.Warning); }
		}

		private void StopEffects()
		{
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
			Agent agent = ResolveAgent(role);
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

		public MatrixFrame? ViewerFrame
		{
			get
			{
				Agent main = Agent.Main;
				return main != null
					? new MatrixFrame(main.Frame.rotation, main.Position + new Vec3(0f, 0f, main.GetEyeGlobalHeight()))
					: (MatrixFrame?)null;
			}
		}

		// Roles known so far: MainAgent / Viewer / Player (all resolve to the local player's agent).
		// Custom named roles (e.g. "Boss") are a planned extension - unknown roles resolve to null with a
		// one-time warning instead of silently falling back to the main agent.
		private static bool IsViewerRole(string role) => role == "MainAgent" || role == "Viewer" || role == "Player";
		private readonly HashSet<string> _warnedUnknownRoles = new HashSet<string>();

		public Vec3? ResolveRolePosition(string role)
		{
			if (string.IsNullOrEmpty(role)) return null;
			Agent agent = ResolveAgent(role, warn: false);
			// Eye height so look-at targets aim consistently with ViewerFrame.
			return agent != null ? agent.Position + new Vec3(0f, 0f, agent.GetEyeGlobalHeight()) : (Vec3?)null;
		}

		public Vec3? ResolveEntityPosition(string entityRefId)
		{
			if (string.IsNullOrEmpty(entityRefId)) return null;
			try
			{
				WeakGameEntity wge = BuildSystem.EntityMarkerIndex.Resolve(entityRefId);
				if (wge.IsValid) return wge.GetGlobalFrame().origin;
			}
			catch { }
			return null;
		}

		private Agent ResolveAgent(string role, bool warn = true)
		{
			if (string.IsNullOrEmpty(role) || IsViewerRole(role)) return Agent.Main;
			if (warn && _warnedUnknownRoles.Add(role))
			{
				Log($"[Cinematic] Unknown role '{role}' - supported roles: MainAgent, Viewer, Player.", LogLevel.Warning);
			}
			return null;
		}
	}
}
#endif
