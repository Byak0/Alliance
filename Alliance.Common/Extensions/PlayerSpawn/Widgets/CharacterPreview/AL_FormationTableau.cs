#if !SERVER
using System;
using System.Collections.Generic;
using Alliance.Common.Core.Utils;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.Library;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Shared formation preview: ONE scene row, ONE camera, one render for the whole character
	/// list of a formation. Characters are FakeAgents placed at the same normalized positions as their UI items,
	/// so the UI items overlay their own character.
	/// </summary>
	public class AL_FormationTableau
	{
		private const float RowY = -40f;
		private const float CameraDistance = 7.5f;
		private const float CameraHeight = 0.9f;
		private const float AimHeight = 1.9f;
		private const float VerticalFOV = .85f;

		/// <summary>Normalized row position of the hovered character (-1 = none), driven by the character item widgets.</summary>
		public static float HoveredX { get; set; } = -1f;

		private FakeAgent _selectedFake;
		private readonly List<FakeAgent> _fakes = new List<FakeAgent>();
		private readonly List<string> _fakeKeys = new List<string>();
		private readonly List<float> _fractions = new List<float>();
		private readonly List<Light> _stateLights = new List<Light>();

		private Scene _scene;
		private Camera _camera;
		private float _selectionX = -1f;
		private float _cameraRatio = 1.4f;
		private bool _isFinalized;
		private bool _isVisualsDirty = false;
		private List<(string Id, string Code, float X)> _characters = new List<(string Id, string Code, float X)>();

		public Texture Texture { get; private set; }

		private TableauView View => Texture?.TableauView;

		private float VisibleHalfWidth => CameraDistance * MathF.Tan((float)(Math.PI / 4f) * 0.5f) * _cameraRatio;

		/// <summary>Parses the VM's "id@x;id@equipmentCode@x..." row data into (id, equipment code,
		/// normalized x) triplets. The code is optional per entry: empty keeps the character's
		/// catalog outfit (codes carry no '@', they are dot-separated item ids).</summary>
		public static List<(string Id, string Code, float X)> ParseCharactersData(string data)
		{
			List<(string Id, string Code, float X)> result = new List<(string Id, string Code, float X)>();
			if (string.IsNullOrEmpty(data)) return result;
			foreach (string part in data.Split(';'))
			{
				int lastAt = part.LastIndexOf('@');
				if (lastAt <= 0) continue;
				int firstAt = part.IndexOf('@');
				string id = part.Substring(0, firstAt);
				string code = firstAt == lastAt ? null : part.Substring(firstAt + 1, lastAt - firstAt - 1);
				if (string.IsNullOrEmpty(code)) code = null;
				if (float.TryParse(part.Substring(lastAt + 1), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x))
				{
					result.Add((id, code, x));
				}
			}
			return result;
		}

		public void SetCharacters(List<(string Id, string Code, float X)> characters)
		{
			_characters = characters;

			if (_isFinalized || _scene == null) return;

			ParkCurrentFakes();

			float visibleWidth = VisibleHalfWidth * 2f;
			for (int i = 0; i < characters.Count; i++)
			{
				MatrixFrame frame = MatrixFrame.Identity;
				frame.origin.x = (0.5f - characters[i].X) * visibleWidth;
				frame.origin.y = RowY;
				// Catalog outfit, or the equipment code's outfit (perk alternatives).
				AllianceData.ParsedEquipmentCode parsedCode = AllianceData.ParseEquipmentCode(characters[i].Code);
				AllianceData.CharacterDefinition character = AllianceData.Instance.GetDisplayCharacter(characters[i].Id, parsedCode);
				if (character == null || character.EquipmentRosters.Count == 0) continue;
				string fakeKey = FakeAgentPool.BuildKey(character, equipmentSeed: 0, characters[i].Code, weaponsDrawn: true);
				FakeAgent fake = FakeAgentPool.Acquire(fakeKey, () => new FakeAgent(
					_scene,
					frame,
					character,
					character.CultureId,
					name: "Row_" + characters[i].Id,
					useFullAgentVisuals: false,
					weaponsDrawn: true,
					register: false,
					maxSheathedItems: int.MaxValue));
				if (!fake.IsValid) continue;
				// Pooled fakes come back from their old row position - put them at this one.
				fake.Teleport(frame);
				fake.SetVisible(true);
				Light stateLight = Light.CreatePointLight(2f);
				stateLight.Intensity = 0f;
				stateLight.LightColor = new Vec3(255f, 235f, 190f);
				stateLight.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 1.0f, .5f));
				fake.Root?.AddLight(stateLight);
				PlayIdle(fake);
				_fakes.Add(fake);
				_fakeKeys.Add(fakeKey);
				_fractions.Add(characters[i].X);
				_stateLights.Add(stateLight);
			}
			UpdateLights();
			_isVisualsDirty = false;
		}

		/// <summary>Returns the current row's fakes to the shared pool, their state lights detached
		/// (lights are per-set and recreated on the next acquire). Identical characters (same code)
		/// come back from the pool without reloading their meshes.</summary>
		private void ParkCurrentFakes()
		{
			for (int i = 0; i < _fakes.Count; i++)
			{
				FakeAgent fake = _fakes[i];
				Light stateLight = _stateLights[i];
				GameEntity root = fake?.Root;
				if (stateLight != null && stateLight.IsValid && root != null && root.HasComponent(stateLight))
				{
					root.RemoveComponent(stateLight);
				}
				FakeAgentPool.Return(_fakeKeys[i], fake);
			}
			_fakes.Clear();
			_fakeKeys.Clear();
			_fractions.Clear();
			_stateLights.Clear();
			// The selected fake went back to the pool with the others - re-resolve on next SetSelected.
			_selectedFake = null;
		}

		private void PlayIdle(FakeAgent fake)
		{
			// TODO, variants if fake is mounted, has shield, 2h, bow, etc.
			fake?.PlayClip(AllianceData.Instance.GetAnimationOfAction(fake.BodyActionSetId, "act_idle_1h_with_shield_5"), loop: true);
		}

		private void PlaySpecialAnim(FakeAgent fake)
		{
			// TODO, variants if fake is mounted, has shield, 2h, bow, etc.
			fake?.PlayClip(AllianceData.Instance.GetAnimationOfAction(fake.BodyActionSetId, "act_arena_winner_2"), loop: true);
		}

		/// <summary>Highlights the selected character (normalized row position, -1 = none).</summary>
		public void SetSelected(float x)
		{
			_selectionX = x;
			UpdateLights();
			UpdateSelectedFake();
		}

		private void UpdateSelectedFake()
		{
			PlayIdle(_selectedFake);
			for (int i = 0; i < _fractions.Count; i++)
			{
				if (MathF.Abs(_fractions[i] - _selectionX) < 0.02f)
				{
					_selectedFake = _fakes[i];
					break;
				}
			}
			PlaySpecialAnim(_selectedFake);
		}

		/// <summary>Toggles each agent's state light: off, dim (hovered) or bright (selected).</summary>
		private void UpdateLights()
		{
			for (int i = 0; i < _fractions.Count; i++)
			{
				bool selected = MathF.Abs(_fractions[i] - _selectionX) < 0.02f;
				bool hovered = HoveredX >= 0f && MathF.Abs(_fractions[i] - HoveredX) < 0.02f;
				_stateLights[i].Intensity = selected ? 0.5f : hovered ? 0.25f : 0f;
			}
		}

		public void SetTargetSize(int width, int height)
		{
			if (_isFinalized) return;
			if (width <= 0 || height <= 0) return;
			_cameraRatio = width / (float)height;

			int renderWidth = width;
			int renderHeight = height;
			if (width <= 0 || height <= 0)
			{
				renderWidth = 10;
				renderHeight = 10;
			}
			else
			{
				float RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
				renderWidth = (int)(width * RenderScale);
				renderHeight = (int)(height * RenderScale);
			}
			Log($"{renderWidth} / {renderHeight}", LogLevel.Debug);

			if (_scene == null)
			{
				ALCharacterTableauSceneCache.Initialize();
				_scene = ALCharacterTableauSceneCache.GetScene();
			}
			if (_scene == null) return;

			if (_camera == null) _camera = Camera.CreateCamera();

			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.Release();
			Texture = TableauView.AddTableau("AL_FormationTableau", new RenderTargetComponent.TextureUpdateEventHandler(FormationTableauRenderFunction), _scene, renderWidth, renderHeight);
			Texture.TableauView.SetSceneUsesContour(value: false);
			Texture.TableauView.SetClearColor(0u);
			ApplyCamera();
			View.SetCamera(_camera);
			View.SetScene(_scene);
			_isVisualsDirty = true;
		}

		public void Tick(float dt)
		{
			if (_isFinalized) return;
			if (_isVisualsDirty) SetCharacters(_characters);			
			foreach (FakeAgent fake in _fakes) fake?.TickOnce(dt);
			UpdateLights();
			ApplyCamera();
			if (View != null)
			{
				View.SetCamera(_camera);
				View.SetDoNotRenderThisFrame(false);
				View.SetContinuousRendering(true);				
			}
		}

		private void ApplyCamera()
		{
			if (_camera == null) return;
			_camera.SetFovVertical(VerticalFOV, _cameraRatio, 0.1f, 200f);
			Vec3 eye = new Vec3(0f, RowY + CameraDistance, CameraHeight);
			_camera.LookAt(eye, new Vec3(0f, RowY, AimHeight), new Vec3(0f, 0f, 1f));
		}

		private void FormationTableauRenderFunction(Texture sender, EventArgs e)
		{
			TableauView tableauView = View;
			if (tableauView == null)
				return;

			// This line is extremely important to ensure the tableau renders correctly (otherwise it might not render textures fully).
			tableauView.SetRenderWithPostfx(true);

			ApplyCamera();
			tableauView.SetCamera(_camera);
			tableauView.SetSceneUsesSkybox(false);
			tableauView.SetContinuousRendering(true);
			tableauView.SetDeleteAfterRendering(false);
			tableauView.SetDoNotRenderThisFrame(true);
			tableauView.SetClearColor(0u);
		}

		public void OnFinalize()
		{
			if (_isFinalized) return;
			_isFinalized = true;
			ParkCurrentFakes();

			if (_camera != null)
			{
				_camera.ReleaseCameraEntity();
				_camera = null;
			}
			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.Release();
			Texture = null;
			// The shared scene stays alive for the whole session (see ALCharacterTableauSceneCache).
		}
	}
}
#endif
