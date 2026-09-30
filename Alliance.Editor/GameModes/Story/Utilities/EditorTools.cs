using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Views;
using Alliance.Editor.Extensions.Cinematics;
using Alliance.Editor.Extensions.Cinematics.ViewModels;
using Alliance.Editor.GameModes.Story.ViewModels;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Core.Utils;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using Alliance.Editor.GameModes.Story.Views;
using Alliance.Editor.Patch.HarmonyPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	public class EditorTools : IEditorTools
	{
		private ObjectEditorWindow _objectEditorWindow;
		private PlayerSpawnMenuView _playerSpawnMenuView;
		private readonly CinematicView _cinematicView;
		private readonly Dictionary<AgentActionTrack, List<FakeAgent>> _previewPuppets = new Dictionary<AgentActionTrack, List<FakeAgent>>();
		private Action<float> _onPreviewTime;
		private Action _onPreviewFinished;
		private bool _previewPaused;

		public EditorTools()
		{
			_playerSpawnMenuView = new PlayerSpawnMenuView();
			_playerSpawnMenuView.OnBehaviorInitialize();

			_cinematicView = new CinematicView();
			_cinematicView.OnBehaviorInitialize();
		}

		public void Tick(float dt)
		{
			PumpEditorGameLoad();
			_playerSpawnMenuView.OnMissionTick(dt);
			TickPreview(dt);
			DrawCameraPath();
			DrawDestinationMarkers();
			EditZoneView.Tick(dt);
			EditEntityView.Tick(dt);
			EditFrameView.Tick(dt);
		}

		// Editor game lazy load (TaleWorlds' CharacterSpawner recipe): the kit scene editor starts
		// with no Game (no FaceGen/object manager/characters). Started when the cinematic editor
		// opens, then DoLoadingForGameManager is pumped every tick until finished - the loading
		// state pushed at module init is too early and never runs.
		private MBGameManager _editorGameManager;
		private bool _editorGameLoadStarted;
		private bool _editorGameLoaded;

		/// <summary>Starts the editor game creation on first call (idempotent). Once loaded,
		/// Game.Current/FaceGen/items exist and fakes can use the AgentVisuals path.</summary>
		public void EnsureEditorGame()
		{
			if (_editorGameLoaded) return;
			if (Game.Current != null)
			{
				_editorGameLoaded = true;
				AnimationSystem.Instance.Init();
				return;
			}
			if (!_editorGameLoadStarted)
			{
				_editorGameLoadStarted = true;
				_editorGameManager = new EditorGameManager();
				Log("Loading editor game (characters, items, animations)...", LogLevel.Debug);
			}
		}

		private void PumpEditorGameLoad()
		{
			if (!_editorGameLoadStarted || _editorGameLoaded) return;
			if (_editorGameManager == null) return;
			if (_editorGameManager.DoLoadingForGameManager())
			{
				_editorGameLoaded = true;
				AnimationSystem.Instance.Init();
				// The editor pipeline loads NPCCharacters but not MPCharacters - pull them in and
				// refresh the character lists (staged extras and stand-ins are MP characters).
				try
				{
					if (!MBObjectManager.Instance.HasType(typeof(BasicCharacterObject)))
						MBObjectManager.Instance.RegisterType<BasicCharacterObject>("NPCCharacter", "MPCharacters", 43U, true, false);
					Game.Current?.ObjectManager?.LoadXML("MPCharacters");
					Characters.Instance.RefreshAvailableCharacters();
					Log("MPCharacters loaded.", LogLevel.Debug);
				}
				catch (Exception ex)
				{
					Log($"Failed to load MPCharacters in the editor game: {ex.Message}", LogLevel.Error);
				}
				Log("Editor game loaded.", LogLevel.Debug);
			}
		}

		/// <summary>Draws destination markers for every destination-carrying keyframe, plus the spawn
		/// origin of staged-extras tracks ("lane - fake agents xN"), like the camera path markers.
		/// Hidden while the preview plays so the scene view stays clean.</summary>
		private void DrawDestinationMarkers()
		{
			if (_cinematicView.IsPlaying) return;
			Cinematic cinematic = EditorToolsManager.ActiveEditingCinematic;
			if (cinematic == null) return;

			foreach (CinematicTrack track in cinematic.Tracks ?? Enumerable.Empty<CinematicTrack>())
			{
				if (track == null || !track.Enabled) continue;
				uint color = LaneColor(track);
				string lane = string.IsNullOrWhiteSpace(track.Name)
					? track.GetType().Name.Replace("Track", "")
					: track.Name;

				switch (track)
				{
					case AgentActionTrack agentTrack:
					{
						if (agentTrack.Target?.IsStagedMode == true)
						{
							// Staged origin marker: where the fake-agent formation spawns.
							MatrixFrame origin = agentTrack.Target.Origin.ToFrame();
							string originLabel = $"{lane} - fake agents x{Math.Max(1, agentTrack.Target.Count)}";
							Debug.RenderDebugSphere(origin.origin, 0.35f, color, true);
							Debug.RenderDebugLine(origin.origin, origin.rotation.f * 2f, color, true);
							Debug.RenderDebugText3D(origin.origin + new Vec3(0f, 0f, 1.5f), originLabel, color, -100);
						}
						foreach (AgentActionKeyframe kf in agentTrack.Keyframes ?? Enumerable.Empty<AgentActionKeyframe>())
						{
							if (kf == null || (kf.Kind != AgentActionKind.Teleport && kf.Kind != AgentActionKind.MoveTo)) continue;
							DrawDestinationMarker(kf, kf.Destination, color, $"{lane} - {kf.Kind}");
						}
						break;
					}
					case EntityTrack entityTrack:
						foreach (EntityActionKeyframe kf in entityTrack.Keyframes ?? Enumerable.Empty<EntityActionKeyframe>())
						{
							if (kf == null || (kf.Kind != EntityActionKind.Teleport && kf.Kind != EntityActionKind.MoveTo)) continue;
							DrawDestinationMarker(kf, kf.Destination, color, $"{lane} - {kf.Kind}");
						}
						break;
				}
			}
		}

		private static void DrawDestinationMarker(CinematicKeyframe kf, CinematicTarget destination, uint color, string label)
		{
			MatrixFrame? frame = destination?.ResolveWorldFrame(null);
			if (!frame.HasValue) return;

			Vec3 origin = frame.Value.origin;
			Debug.RenderDebugSphere(origin, 0.35f, color, true);
			Debug.RenderDebugLine(origin, frame.Value.rotation.f * 2f, color, true);
			Debug.RenderDebugText3D(origin + new Vec3(0f, 0f, 1.5f), label, color, -100);
		}

		private static uint LaneColor(CinematicTrack track)
		{
			System.Windows.Media.Color lane = CinematicTimelineVM.BrushForTrack(track).Color;
			return new Color(lane.R / 255f, lane.G / 255f, lane.B / 255f).ToUnsignedInteger();
		}

		public void OpenPlayerSpawnMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback)
		{
			_playerSpawnMenuView.OpenMenu(playerSpawnMenu, onCloseCallback, true);
		}

		public MatrixFrame? CaptureEditorCameraFrame()
		{
			if (MBEditor._editorScene == null) return null;
			return MBEditor._editorScene.LastFinalRenderCameraFrame;
		}

		private void DrawCameraPath()
		{
			if (_cinematicView.IsPlaying) return;
			Cinematic cinematic = EditorToolsManager.ActiveEditingCinematic;
			if (cinematic == null) return;
			CameraTrack track = cinematic.FindTrack<CameraTrack>();
			if (track == null || track.Keyframes == null || track.Keyframes.Count == 0) return;

			uint pathColor = new Color(0.3f, 0.7f, 1f).ToUnsignedInteger();
			uint keyColor = new Color(1f, 0.8f, 0.2f).ToUnsignedInteger();
			uint cutColor = new Color(0.5f, 0.5f, 0.5f).ToUnsignedInteger();

			var frames = new System.Collections.Generic.List<MatrixFrame>();
			foreach (var kf in track.Keyframes)
			{
				MatrixFrame f = kf.Frame.ToFrame();
				frames.Add(f);
				Debug.RenderDebugSphere(f.origin, 0.25f, keyColor, true);
				Debug.RenderDebugText3D(f.origin + new Vec3(0, 0, 0.6f), $"{kf.Time:F1}s", keyColor, -100);
				Debug.RenderDebugLine(f.origin, -f.rotation.u * 2f, keyColor, true);
			}

			for (int i = 0; i < frames.Count - 1; i++)
			{
				Interpolation interp = track.Keyframes[i + 1].Interpolation;
				Vec3 p1 = frames[i].origin;
				Vec3 p2 = frames[i + 1].origin;

				if (interp == Interpolation.Constant)
				{
					Vec3 delta = p2 - p1;
					float len = delta.Length;
					Vec3 stubDir = len > 0.001f ? delta / len : Vec3.Zero;
					Debug.RenderDebugLine(p1, stubDir * 0.5f, cutColor, true);
					Debug.RenderDebugLine(p2, -stubDir * 0.5f, cutColor, true);
					continue;
				}

				if (interp == Interpolation.CatmullRom)
				{
					Vec3 p0 = i > 0 ? frames[i - 1].origin : p1;
					Vec3 p3 = i + 2 < frames.Count ? frames[i + 2].origin : p2;
					Vec3 last = p1;
					for (int j = 1; j <= 16; j++)
					{
						float t = (float)j / 16;
						Vec3 pt = KeyframeEvaluator.CatmullRom(p0, p1, p2, p3, t, track.Keyframes[i + 1].Tension);
						Debug.RenderDebugLine(last, pt - last, pathColor, true);
						last = pt;
					}
				}
				else
				{
					Debug.RenderDebugLine(p1, p2 - p1, pathColor, true);
				}
			}
		}

		public bool IsPreviewing => _cinematicView.IsPlaying;
		public bool IsPreviewPaused => _previewPaused;

		public void PlayPreview(Cinematic cinematic, Action<float> onTime, Action onFinished)
		{
			if (cinematic == null) return;

			// The editor preview runs against the editing scene. While a test mission is running the
			// editor scene is gone (the mission replaced it) - creating entities there access-violates.
			// In a test mission, cinematics play for real via the debug keys (Home = test, End = authored).
			if (Mission.Current != null)
			{
				Log("[Cinematic] Editor preview unavailable while a test mission is running. Play cinematics in the mission with the debug keys (Home / End) for full-fidelity preview.", LogLevel.Warning);
				return;
			}

			if (_cinematicView.IsPlaying)
			{
				_onPreviewFinished = null;
				_cinematicView.StopCinematic();
			}

			_onPreviewTime = onTime;
			_onPreviewFinished = onFinished;
			_previewPaused = false;

			_cinematicView.EditorSceneView = MBEditor.GetEditorSceneView();
			Patch_SceneEditorScreen.ActiveCinematicView = _cinematicView;
			CinematicPreviewBridge.PreviewAgentActionHandler = HandlePreviewAgentAction;
			SpawnPreviewPuppets(cinematic);
			_cinematicView.PlayCinematic(cinematic, 0f, false, null);
		}

		public void PausePreview() { _previewPaused = true; }
		public void ResumePreview() { _previewPaused = false; }
		public void SeekPreview(float time) => _cinematicView.Seek(time);
		public void SamplePreview() => _cinematicView.SampleOnce();

		public void StopPreview()
		{
			Action finished = _onPreviewFinished;
			_onPreviewTime = null;
			_onPreviewFinished = null;
			_previewPaused = false;

			_cinematicView.StopCinematic();
			_cinematicView.EditorSceneView = null;
			Patch_SceneEditorScreen.ActiveCinematicView = null;
			CinematicPreviewBridge.PreviewAgentActionHandler = null;
			DespawnPreviewPuppets();

			finished?.Invoke();
		}

		private void TickPreview(float dt)
		{
			if (!_cinematicView.IsPlaying) return;
			if (_previewPaused) return;
			_cinematicView.OnPreDisplayMissionTick(dt);
			_onPreviewTime?.Invoke(_cinematicView.CurrentTime);
			if (!_cinematicView.IsPlaying) StopPreview();
		}

		/// <summary>Spawns preview stand-ins per agent track:
		/// - StagedExtras mode: FakeAgents spawned from the authored formation (always the cheap
		///   raw path).
		/// - TrueAgents mode: a single full-fidelity stand-in per track (AgentVisuals when a Game
		///   exists - the true agents are unpredictable).
		/// FakeAgents are per-machine visuals; movement is a straight-line blockout.</summary>
		private void SpawnPreviewPuppets(Cinematic cinematic)
		{
			// Make sure the editor game creation is started (loaded per-tick; if it is not finished
			// yet this preview runs with raw-entity fakes and the next one uses AgentVisuals).
			EnsureEditorGame();

			// Context diagnostic: reports what the kit test-mission / scene-editing mode provides.
			// Debug-level: stripped from Release builds.
			int characterCount = -1;
			try { characterCount = MBObjectManager.Instance?.GetObjectTypeList<BasicCharacterObject>()?.Count ?? -1; }
			catch { }
			Log($"[Cinematic] Preview context: Game.Current={Game.Current != null}, Mission.Current={Mission.Current != null}, " +
				$"BasicCharacters={characterCount}, Monster 'human'={MBObjectManager.Instance?.GetObject<Monster>("human") != null}", LogLevel.Debug);

			Scene scene = MBEditor._editorScene;
			DespawnPreviewPuppets();
			if (scene == null)
			{
				Log("[Cinematic] Preview skipped: no editor scene.", LogLevel.Warning);
				return;
			}

			foreach (AgentActionTrack track in cinematic.Tracks.OfType<AgentActionTrack>())
			{
				if (track == null) continue;

				MatrixFrame frame = FindInitialStagingFrame(track) ?? InFrontOfEditorCamera(scene);
				var fakes = new List<FakeAgent>();

				if (track.Target?.IsStagedMode == true)
				{
					fakes.AddRange(track.Target.SpawnFakes(scene));
				}
				else
				{
					NativeMpData.CharacterDefinition standIn = NativeMpData.Instance.GetCharacter(track.Target?.PreviewCharacterId);
					fakes.Add(new FakeAgent(scene, frame, standIn, name: $"Stand-in - {track.Name}"));
				}
				_previewPuppets[track] = fakes;
			}
		}

		private static MatrixFrame? FindInitialStagingFrame(AgentActionTrack track)
		{
			foreach (AgentActionKeyframe kf in track.Keyframes ?? Enumerable.Empty<AgentActionKeyframe>())
			{
				if (kf == null) continue;
				MatrixFrame? frame = kf.Destination?.ResolveWorldFrame(null);
				if (frame.HasValue) return frame.Value;
			}
			return null;
		}

		private static MatrixFrame InFrontOfEditorCamera(Scene scene)
		{
			MatrixFrame camera = scene?.LastFinalRenderCameraFrame ?? MatrixFrame.Zero;
			MatrixFrame frame = MatrixFrame.Identity;
			frame.origin = camera.origin - camera.rotation.u * 2.5f;
			return frame;
		}

		private void DespawnPreviewPuppets()
		{
			// Batched: removing the shared parent entity clears all bodies in one engine call.
			FakeAgent.DespawnAll();
			_previewPuppets.Clear();
		}

		/// <summary>Executes agent action keyframes against the track's preview stand-ins. MoveTo is
		/// a straight-line kinematic blockout here; in-game true agents use the native scripted
		/// movement, and staged extras run the same commands deterministically on every client.</summary>
		private void HandlePreviewAgentAction(AgentActionTrack track, AgentActionKeyframe kf)
		{
			if (!_previewPuppets.TryGetValue(track, out List<FakeAgent> fakes) || fakes == null) return;

			switch (kf.Kind)
			{
				case AgentActionKind.Teleport:
					MatrixFrame? teleportTarget = kf.Destination?.ResolveWorldFrame(null);
					if (teleportTarget.HasValue)
						for (int i = 0; i < fakes.Count; i++) fakes[i]?.Teleport(kf.KeepFormationOffset ? track.Target.GetMemberFrame(teleportTarget.Value, i) : teleportTarget.Value);
					break;
				case AgentActionKind.MoveTo:
					MatrixFrame? destination = kf.Destination?.ResolveWorldFrame(null);
					if (destination.HasValue)
						for (int i = 0; i < fakes.Count; i++) fakes[i]?.MoveTo(kf.KeepFormationOffset ? track.Target.GetMemberFrame(destination.Value, i) : destination.Value, kf.Speed == AgentMoveSpeed.Run, kf.MoveAnimation,
							kf.Speed == AgentMoveSpeed.Custom ? Math.Max(0.1f, kf.CustomSpeed) : (float?)null, kf.MountMoveAnimation);
					break;
				case AgentActionKind.PlayAnimation:
					foreach (FakeAgent fake in fakes)
					{
						fake?.PlayClip(kf.ClipName, kf.ActionSpeed, kf.Loop);
						fake?.PlayMountClip(kf.MountClipName, kf.ActionSpeed, kf.Loop);
					}
					break;
				case AgentActionKind.PlayFacial:
					foreach (FakeAgent fake in fakes) fake?.SetFacialAnimation(kf.FacialAnimName, kf.FacialLoop);
					break;
				case AgentActionKind.SetVisible:
					foreach (FakeAgent fake in fakes) fake?.SetVisible(kf.Visible);
					break;
			}
		}

		public void AddZoneToEditor(Zone zone, string zoneName, Action onEditCallback) => EditZoneView.AddZone(zone, zoneName, onEditCallback);
		public void RemoveZoneFromEditor(Zone zone) => EditZoneView.RemoveZone(zone);
		public void ClearZones() => EditZoneView.ClearZones();
		public void SetEditableZone(Zone zone) => EditZoneView.SetEditableZone(zone);
		public void BeginEntityPick(Action<GameEntityRef> onPicked) => EditEntityView.BeginPick(onPicked);

		public void OpenEditor(object obj, Action<object> onCloseCallback, Scenario explicitScenario = null)
		{
			if (_objectEditorWindow == null || !_objectEditorWindow.IsLoaded)
			{
				_objectEditorWindow = new ObjectEditorWindow(obj, null, null, "Object Editor", explicitScenario);
				_objectEditorWindow.Show();
				_objectEditorWindow.Closed += (s, e) =>
				{
					object modified = (_objectEditorWindow.DataContext as ObjectEditorViewModel)?.Object;
					onCloseCallback?.Invoke(modified);
					_objectEditorWindow = null;
				};
			}
			else
			{
				_objectEditorWindow.Focus();
			}
		}
	}
}
