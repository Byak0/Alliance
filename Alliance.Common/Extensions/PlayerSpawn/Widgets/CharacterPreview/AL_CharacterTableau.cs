#if !SERVER
using Alliance.Common.Core.Utils;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Renders a live 3D character preview into a tableau texture. The dressed body is a
	/// <see cref="FakeAgent"/> (single dressing pipeline shared with cinematics/editor, native
	/// AgentVisuals when available, cheap raw skeleton otherwise), spawned into the shared
	/// <see cref="ALCharacterTableauSceneCache"/> scene.
	///
	/// Rendering is scheduled: tableaus flagged as continuous (e.g. the selected menu item) render
	/// every frame, the others take turns so that at most one non-continuous tableau renders per
	/// frame. Animations are driven manually through <see cref="FakeAgent.TickOnce"/> with real
	/// elapsed time, so sharing one scene no longer speeds animations up.
	/// </summary>
	public class AL_CharacterTableau
	{
		// Render scheduling (shared across all instances)
		// Two tableau ticks less than this apart are considered part of the same rendered frame.
		private static readonly TimeSpan FrameGateInterval = TimeSpan.FromMilliseconds(4);
		private static readonly List<AL_CharacterTableau> _instances = new List<AL_CharacterTableau>();
		private static DateTime _lastFrameUtc = DateTime.MinValue;
		private static int _rrCursor;
		private static AL_CharacterTableau _rrCurrent;
		private static int _lastTableauIndex;

		// Scene slot (assigned by ALCharacterTableauSceneCache)
		private int _slotIndex;
		private MatrixFrame _characterSpawnPoint;
		private MatrixFrame _mountSpawnPoint;
		private MatrixFrame _bannerSpawnPoint;
		private MatrixFrame _initialCamPos;

		// Fake dressing
		private FakeAgent _fake;
		private string _fakePoolKey;
		private int _equipmentSeed;
		private string _charStringId;
		private string _equipmentCode = "";
		private string _bodyPropertiesCode;
		private bool _isFemale;
		private int _raceIndex = -1;
		private uint _clothColor1 = new Color(1f, 1f, 1f).ToUnsignedInteger();
		private uint _clothColor2 = new Color(1f, 1f, 1f).ToUnsignedInteger();
		private bool _isVisualsDirty;
		private bool _isCharacterMountPlacesSwapped;
		private float _mainCharacterRotation;

		// Camera
		private MatrixFrame _camPos;
		private float _verticalFov = (float)Math.PI / 4f;
		private CharacterViewModelStance _stanceIndex;

		// Animations
		private string _idleActionName;
		private string _idleFaceAnim;
		private float _idleAnimationTimer;
		private string _customAnimationName;
		private string _customAnimation;
		private float _customAnimationTimer;
		private bool _customAnimationStartScheduled;

		// Banner
		private Banner _banner;
		private bool _isBannerShownInBackground;
		private ItemObject _bannerItem;
		private GameEntity _bannerEntity;

		// Wielded weapons
		private int _leftHandEquipmentIndex = -1;
		private int _rightHandEquipmentIndex = -1;
		private bool _isEquipmentIndicesDirty;

		// Scene & texture resources
		private Scene _tableauScene;
		private MBAgentRendererSceneController _agentRendererSceneController;
		private Camera _continuousRenderCamera;
		private TableauView View => Texture?.TableauView;

		// Target size
		private float _renderScale = 1f;
		private float _customRenderScale = 1f;
		private int _latestWidth = -1;
		private int _latestHeight = -1;
		private int _tableauSizeX;
		private int _tableauSizeY;
		private float _cameraRatio;
		private float _delaySinceLastUpdate;

		// State flags
		private bool _isEnabled;
		private bool _isFinalized;
		private bool _isRotatingCharacter;
		private bool _forceRenderOnce;

		public Texture Texture { get; private set; }

		public bool IsRunningCustomAnimation => _customAnimation != null || _customAnimationStartScheduled;

		public bool ShouldLoopCustomAnimation { get; set; }

		public float CustomAnimationWaitDuration { get; set; }

		/// <summary>When true (default) the tableau renders every frame. When false it shares the
		/// frame's render budget with the other non-continuous tableaus (round-robin) - bind it to
		/// the item selection so only the selected preview animates smoothly.</summary>
		public bool Continuous { get; set; } = true;

		public AL_CharacterTableau()
		{
			SetEnabled(enabled: true);
			FirstTimeInit();
			_instances.Add(this);
		}

		public void OnTick(float dt)
		{
			if (_isFinalized) return;

			// The provider ticks once per rendered UI frame - multiple instances ticking within the
			// same frame are gated so only the scheduled ones actually render.
			bool shouldRender = ShouldRenderThisFrame();

			_delaySinceLastUpdate += dt;
			if (!shouldRender) return;

			// The fake advances with the real time elapsed since its last rendered tick, not with
			// the render rate.
			float elapsed = _delaySinceLastUpdate;
			_delaySinceLastUpdate = 0f;

			// Rebuild BEFORE the null check below: with no fake yet (first open, or a rebuild that
			// found nothing to dress) this is the only place that creates it - returning first would
			// deadlock the tableau in its empty state. (The old pipeline built its visuals eagerly
			// in FirstTimeInit; the FakeAgent rebuild is lazy now.)
			if (_isVisualsDirty)
			{
				RebuildFake();
				_isVisualsDirty = false;
			}

			if (_fake == null) return;

			_fake.TickOnce(elapsed);

			if (_customAnimationStartScheduled)
			{
				StartCustomAnimation();
			}

			if (_customAnimation != null)
			{
				float duration = AllianceData.Instance.GetClipDuration(_customAnimation);
				if (duration > 0f)
				{
					_customAnimationTimer += elapsed;
					if (_customAnimationTimer > duration)
					{
						if (_customAnimationTimer > duration + CustomAnimationWaitDuration)
						{
							if (ShouldLoopCustomAnimation) StartCustomAnimation();
							else StopCustomAnimation();
						}
					}
				}
			}
			// Idle animation looping (restart non-cyclic idle clips)
			else if (_idleActionName != null)
			{
				float idleDuration = AllianceData.Instance.GetClipDuration(_idleActionName);
				if (idleDuration > 0f)
				{
					_idleAnimationTimer += elapsed;
					if (_idleAnimationTimer > idleDuration)
					{
						PlayIdleAction();
						_idleAnimationTimer = 0f;
					}
				}
			}

			// Rotate character
			if (_isEnabled && _isRotatingCharacter)
			{
				UpdateCharacterRotation((int)Input.MouseMoveX);
			}

			// Update wielded weapon indices
			if (_isEquipmentIndicesDirty && _fake.UsesAgentVisuals)
			{
				_fake.AgentVisuals?.GetVisuals().SetWieldedWeaponIndices(_rightHandEquipmentIndex, _leftHandEquipmentIndex);
				_isEquipmentIndicesDirty = false;
			}

			// Render texture view
			if (View != null && _isEnabled)
			{
				View.SetDoNotRenderThisFrame(false);
				View.SetContinuousRendering(true);
			}
		}

		/// <summary>True when this tableau may render this frame: continuous tableaus always,
		/// the others one at a time (round-robin) so the shared scene renders at a bounded rate.
		/// Exactly one tableau renders per frame - see docs/native_limitations.md#tableau-skeleton-advance-per-render.</summary>
		private bool ShouldRenderThisFrame()
		{
			if (_forceRenderOnce)
			{
				_forceRenderOnce = false;
				return true;
			}

			if (DateTime.UtcNow - _lastFrameUtc > FrameGateInterval)
			{
				_lastFrameUtc = DateTime.UtcNow;
				_rrCurrent = null;
				for (int pass = 0; pass < 2 && _rrCurrent == null; pass++)
				{
					for (int probe = 0; probe < _instances.Count; probe++)
					{
						AL_CharacterTableau candidate = _instances[(_rrCursor + probe) % _instances.Count];
						if (candidate != null && candidate._isEnabled && (pass == 0 ? candidate.Continuous : true))
						{
							_rrCurrent = candidate;
							_rrCursor = (_rrCursor + probe + 1) % _instances.Count;
							break;
						}
					}
				}
			}
			return _rrCurrent == this;
		}

		public float GetCustomAnimationProgressRatio()
		{
			if (_customAnimation == null) return -1f;
			float duration = AllianceData.Instance.GetClipDuration(_customAnimation);
			if (duration <= 0f) return -1f;
			return _customAnimationTimer / duration;
		}

		public void SetEnabled(bool enabled)
		{
			if (enabled == _isEnabled) return;

			_isEnabled = enabled;
			View?.SetEnable(_isEnabled);
		}

		public void SetLeftHandWieldedEquipmentIndex(int index)
		{
			_leftHandEquipmentIndex = index;
			_isEquipmentIndicesDirty = true;
		}

		public void SetRightHandWieldedEquipmentIndex(int index)
		{
			_rightHandEquipmentIndex = index;
			_isEquipmentIndicesDirty = true;
		}

		public void SetTargetSize(int width, int height)
		{
			_isRotatingCharacter = false;
			_latestWidth = width;
			_latestHeight = height;
			if (width <= 0 || height <= 0)
			{
				_tableauSizeX = 10;
				_tableauSizeY = 10;
			}
			else
			{
				_renderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
				_tableauSizeX = (int)(width * _customRenderScale * _renderScale);
				_tableauSizeY = (int)(height * _customRenderScale * _renderScale);
			}

			_cameraRatio = _tableauSizeX / (float)_tableauSizeY;
			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.Release();
			Texture = TableauView.AddTableau("AL_CharacterTableau_" + _lastTableauIndex++, new RenderTargetComponent.TextureUpdateEventHandler(CharacterTableauContinuousRenderFunction), _tableauScene, _tableauSizeX, _tableauSizeY);
			Texture.TableauView.SetSceneUsesContour(value: false);

			View.SetCamera(_continuousRenderCamera);
			View.SetScene(_tableauScene);
			_forceRenderOnce = true;
		}

		public void SetCharStringID(string charStringId)
		{
			if (_charStringId != charStringId)
			{
				_charStringId = charStringId;
				_equipmentSeed = string.IsNullOrEmpty(charStringId) ? 0 : (int)(TaleWorlds.Library.Common.GetDJB2(charStringId) % 1000);
				_isVisualsDirty = true;
			}
		}

		public void OnFinalize()
		{
			_isFinalized = true;
			_instances.Remove(this);
			if (_rrCurrent == this) _rrCurrent = null;

			// The fake goes back to the shared pool (parked invisible) - destroying it would
			// reload all its meshes on the next widget showing the same character.
			FakeAgentPool.Return(_fakePoolKey, _fake);
			_fake = null;

			Camera continuousRenderCamera = _continuousRenderCamera;
			if (continuousRenderCamera != null)
			{
				continuousRenderCamera.ReleaseCameraEntity();
				_continuousRenderCamera = null;
			}
			TableauView view = View;
			view?.SetEnable(value: false);
			if (_tableauScene != null)
			{
				if (_bannerEntity != null)
				{
					_tableauScene.RemoveEntity(_bannerEntity, 0);
					_bannerEntity = null;
				}

				if (_agentRendererSceneController != null)
				{
					view?.SetEnable(value: false);
					view?.AddClearTask();
					MBAgentRendererSceneController.DestructAgentRendererSceneController(_tableauScene, _agentRendererSceneController, deleteThisFrame: false);
					_agentRendererSceneController = null;
					_tableauScene.ManualInvalidate();
					_tableauScene = null;
				}
				else
				{
					view?.AddClearTask(clearOnlySceneview: true);
					_tableauScene = null;
				}
			}

			ALCharacterTableauSceneCache.ReleaseSlot(_slotIndex);
			SetEnabled(false);

			Texture?.Release();
			Texture = null;
		}

		public void SetBodyProperties(string bodyPropertiesCode)
		{
			if (_bodyPropertiesCode != bodyPropertiesCode)
			{
				_bodyPropertiesCode = bodyPropertiesCode;
				_isVisualsDirty = true;
			}
		}

		public void SetStanceIndex(int index)
		{
			if (_stanceIndex != (CharacterViewModelStance)index)
			{
				_stanceIndex = (CharacterViewModelStance)index;
				AdjustCharacterForStanceIndex();
			}
		}

		public void SetCustomRenderScale(float value)
		{
			if (!_customRenderScale.ApproximatelyEqualsTo(value))
			{
				_customRenderScale = value;
				if (_latestWidth != -1 && _latestHeight != -1)
				{
					SetTargetSize(_latestWidth, _latestHeight);
				}
			}
		}

		private void AdjustCharacterForStanceIndex()
		{
			// Stances adjust the camera framing only - the body itself is a FakeAgent whose pose
			// is driven by its idle clip (and mounts are part of its hierarchy). Derived from the
			// base slot frame every time so switching stances doesn't accumulate offsets.
			MatrixFrame frame = _initialCamPos;
			switch (_stanceIndex)
			{
				case CharacterViewModelStance.EmphasizeFace:
					frame.Elevate(-2f);
					frame.Advance(0.5f);
					break;
				case CharacterViewModelStance.SideView:
				case CharacterViewModelStance.OnMount:
					frame.Advance(0.5f);
					break;
			}
			_camPos = frame;
		}

		private enum CharacterViewModelStance
		{
			None,
			EmphasizeFace,
			SideView,
			CelebrateVictory,
			OnMount
		}

		public void SetIsFemale(bool isFemale)
		{
			if (_isFemale != isFemale)
			{
				_isFemale = isFemale;
				_isVisualsDirty = true;
			}
		}

		public void SetIsBannerShownInBackground(bool isBannerShownInBackground)
		{
			_isBannerShownInBackground = isBannerShownInBackground;
			UpdateBannerItem();
		}

		public void SetRace(int race)
		{
			if (_raceIndex == race) return;
			_raceIndex = race;
			_isVisualsDirty = true;
		}

		public void SetIdleAction(string idleAction)
		{
			if (_idleActionName != idleAction)
			{
				_idleActionName = idleAction;
				PlayIdleAction();
			}
		}

		private void PlayIdleAction()
		{
			_idleAnimationTimer = 0f;
			if (_fake == null || string.IsNullOrEmpty(_idleActionName)) return;
			// Resolve the VM's name against the character's own action set, then any action set
			// compatible with its skeleton (action types are spread across compatible sets). A raw
			// clip name passes through.
			string clip = AllianceData.Instance.GetAnimationOfActionCompatible(_fake.BodyActionSetId, _idleActionName) ?? _idleActionName;
			_fake.PlayClip(clip, loop: true);
		}

		public void SetCustomAnimation(string animation)
		{
			_customAnimationName = animation;
		}

		public void StartCustomAnimation()
		{
			if (_fake == null || string.IsNullOrEmpty(_customAnimationName))
			{
				_customAnimationStartScheduled = true;
				return;
			}

			_customAnimation = _customAnimationName;
			_fake.PlayClip(_customAnimationName, loop: false);
			_customAnimationStartScheduled = false;
			_customAnimationTimer = 0f;
		}

		public void StopCustomAnimation()
		{
			_customAnimation = null;
			_customAnimationStartScheduled = false;
			PlayIdleAction();
		}

		public void SetIdleFaceAnim(string idleFaceAnim)
		{
			if (!string.IsNullOrEmpty(idleFaceAnim))
			{
				_idleFaceAnim = idleFaceAnim;
				try
				{
					_fake?.RiderSkeleton?.SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
				}
				catch { }
			}
		}

		public void SetEquipmentCode(string equipmentCode)
		{
			if (string.IsNullOrEmpty(equipmentCode)) equipmentCode = null;
			if (_equipmentCode == equipmentCode) return;
			_equipmentCode = equipmentCode;
			_bannerItem = null;
			_isVisualsDirty = true;
			UpdateBannerItem();
		}

		public void SetIsEquipmentAnimActive(bool value)
		{
			// Equipment-change animations are not replayed on the FakeAgent pipeline.
		}

		public void SetMountCreationKey(string value)
		{
			// Mount pelt keys are derived deterministically by the FakeAgent itself.
		}

		public void SetBannerCode(string value)
		{
			_banner = string.IsNullOrEmpty(value) ? null : new Banner(value);
			UpdateBannerItem();
		}

		public void SetArmorColor1(uint clothColor1)
		{
			if (_clothColor1 != clothColor1)
			{
				_clothColor1 = clothColor1;
				_isVisualsDirty = true;
			}
		}

		public void SetArmorColor2(uint clothColor2)
		{
			if (_clothColor2 != clothColor2)
			{
				_clothColor2 = clothColor2;
				_isVisualsDirty = true;
			}
		}

		/// <summary>(Re)spawns the FakeAgent dressed for display: the equipment code's outfit when
		/// one is set (perk alternatives, native Lobby armory...), the catalog outfit of
		/// <see cref="_charStringId"/> otherwise.</summary>
		private void RebuildFake()
		{
			ReturnFakeToPool();
			ForceRenderOnce();

			if (_tableauScene == null || string.IsNullOrEmpty(_charStringId)) return;

			AllianceData.CharacterDefinition definition = BuildDefinition();
			if (definition == null || definition.EquipmentRosters.Count == 0) return;

			// The code's banner item (when any) is known only after BuildDefinition parsed it - (re)create the background banner now.
			UpdateBannerItem();

			MatrixFrame frame = _isCharacterMountPlacesSwapped ? _mountSpawnPoint : _characterSpawnPoint;
			frame.rotation.RotateAboutUp(_mainCharacterRotation);

			// Native inventory-preview convention: the mount stands at its dedicated slot BESIDE
			// the character (never under the camera), and the rider stays on foot.
			MatrixFrame mountFrame = _isCharacterMountPlacesSwapped ? _characterSpawnPoint : _mountSpawnPoint;

			_fakePoolKey = FakeAgentPool.BuildKey(definition, _equipmentSeed, _equipmentCode, weaponsDrawn: false);
			_fake = FakeAgentPool.Acquire(_fakePoolKey, () => new FakeAgent(_tableauScene, frame, definition, definition.CultureId,
				name: "Tableau_" + _charStringId,
				useFullAgentVisuals: false,
				weaponsDrawn: false,
				equipmentSeed: _equipmentSeed,
				register: false,
				mountFrame: mountFrame,
				maxSheathedItems: 5));
			if (!_fake.IsValid)
			{
				FakeAgentPool.Return(_fakePoolKey, _fake);
				_fake = null;
				return;
			}
			// Place the fake at this widget's slot and show it right away: the kit never pumps the
			// deferred resource queue, so a CheckResources gate left previews invisible forever.
			// see docs/native_limitations.md#kit-resource-queue-not-pumped
			_fake.Teleport(frame);
			_fake.SetVisible(true);
			PlayIdleAction();
			_customAnimationStartScheduled = IsRunningCustomAnimation;
			if (!string.IsNullOrEmpty(_idleFaceAnim)) SetIdleFaceAnim(_idleFaceAnim);
		}

		private void ReturnFakeToPool()
		{
			if (_fake != null) FakeAgentPool.Return(_fakePoolKey, _fake);
			_fake = null;
		}

		/// <summary>Builds the FakeAgent input: the display definition (catalog or equipment-code
		/// outfit, overridden by the VM's race when it differs, see
		/// <see cref="AllianceData.GetDisplayCharacter"/>), plus the background banner item
		/// carried by the code.</summary>
		private AllianceData.CharacterDefinition BuildDefinition()
		{
			string raceId = _raceIndex != -1 ? AllianceData.Instance.GetRaceName(_raceIndex) : null;
			if (raceId == null)
			{
				BasicCharacterObject characterObject = AllianceData.Instance.GetCharacterObject(_charStringId);
				if (characterObject != null) raceId = AllianceData.Instance.GetRaceName(characterObject.Race);
			}
			AllianceData.ParsedEquipmentCode parsedCode = AllianceData.ParseEquipmentCode(_equipmentCode);
			_bannerItem = parsedCode?.BannerItem;
			return AllianceData.Instance.GetDisplayCharacter(_charStringId, parsedCode, _isFemale, raceId);
		}

		public void RotateCharacter(bool value)
		{
			_isRotatingCharacter = value;
		}

		public void TriggerCharacterMountPlacesSwap()
		{
			_mainCharacterRotation = 0f;
			_isCharacterMountPlacesSwapped = !_isCharacterMountPlacesSwapped;
			_isVisualsDirty = true;
		}

		public void OnCharacterTableauMouseMove(int mouseMoveX)
		{
			UpdateCharacterRotation(mouseMoveX);
		}

		private void UpdateCharacterRotation(int mouseMoveX)
		{
			GameEntity entity = _fake?.Root;
			if (entity == null) return;
			float num = mouseMoveX * 0.005f;
			_mainCharacterRotation += num;
			MatrixFrame frame = entity.GetGlobalFrame();
			frame.rotation.RotateAboutUp(num);
			entity.SetGlobalFrame(frame);
		}

		private void ForceRenderOnce()
		{
			_forceRenderOnce = true;
		}

		private void FirstTimeInit()
		{
			if (_continuousRenderCamera == null)
			{
				_continuousRenderCamera = Camera.CreateCamera();
			}

			if (_tableauScene == null)
			{
				ALCharacterTableauSceneCache.Initialize();
				var slot = ALCharacterTableauSceneCache.AcquireSlot();

				_slotIndex = slot.Index;
				_tableauScene = ALCharacterTableauSceneCache.GetScene();

				_characterSpawnPoint = slot.CharacterFrame;
				_mountSpawnPoint = slot.MountFrame;
				_bannerSpawnPoint = slot.BannerFrame;
				_bannerSpawnPoint.Strafe(-1f);
				_initialCamPos = slot.CameraFrame;
				_camPos = slot.CameraFrame;
			}

			_isVisualsDirty = true;
		}

		private void UpdateBannerItem()
		{
			if (_bannerEntity != null)
			{
				_tableauScene?.RemoveEntity(_bannerEntity, 0);
				_bannerEntity = null;
			}

			if (!_isBannerShownInBackground || _bannerItem == null || _tableauScene == null)
			{
				return;
			}

			_bannerEntity = GameEntity.CreateEmpty(_tableauScene);
			_bannerEntity.SetFrame(ref _bannerSpawnPoint);
			_bannerEntity.AddMultiMesh(_bannerItem.GetMultiMeshCopy());
			_bannerEntity.SetClothComponentKeepStateOfAllMeshes(true);
			if (_banner != null)
			{
				BannerDebugInfo bannerDebugInfo = new BannerDebugInfo();
				_banner.GetTableauTextureLarge(bannerDebugInfo, delegate (Texture t)
				{
					OnBannerTableauRenderDone(t);
				});
			}
		}

		private void OnBannerTableauRenderDone(Texture newTexture)
		{
			if (_isFinalized || _bannerEntity == null)
			{
				return;
			}

			foreach (Mesh item in _bannerEntity.GetAllMeshesWithTag("banner_replacement_mesh"))
			{
				ApplyBannerTextureToMesh(item, newTexture);
			}

			if (_bannerEntity.Skeleton?.GetAllMeshes() == null)
			{
				return;
			}

			foreach (Mesh item2 in _bannerEntity.Skeleton?.GetAllMeshes())
			{
				if (item2.HasTag("banner_replacement_mesh"))
				{
					ApplyBannerTextureToMesh(item2, newTexture);
				}
			}
		}

		private void ApplyBannerTextureToMesh(Mesh bannerMesh, Texture bannerTexture)
		{
			if (bannerMesh != null)
			{
				Material material = bannerMesh.GetMaterial().CreateCopy();
				material.SetTexture(Material.MBTextureType.DiffuseMap2, bannerTexture);
				uint num = (uint)material.GetShader().GetMaterialShaderFlagMask("use_tableau_blending");
				ulong shaderFlags = material.GetShaderFlags();
				material.SetShaderFlags(shaderFlags | num);
				bannerMesh.SetMaterial(material);
			}
		}

		internal void CharacterTableauContinuousRenderFunction(Texture sender, EventArgs e)
		{
			TableauView tableauView = View;
			if (tableauView == null)
				return;

			// Workaround: tableau-requires-postfx, see docs/native_limitations.md
			tableauView.SetRenderWithPostfx(value: true);

			if (_continuousRenderCamera != null)
			{
				// Use the custom FOV, and aim the camera at the character's chest (the
				// procedural scene provides no authored camera rotation - LookAt removes the guesswork).
				_continuousRenderCamera.SetFovVertical(
					_verticalFov,
					_cameraRatio,
					0.1f, 200f
				);

				Vec3 lookTarget = _characterSpawnPoint.origin + new Vec3(0f, 0f, 1.2f);
				_continuousRenderCamera.LookAt(_camPos.origin, lookTarget, new Vec3(0f, 0f, 1f));

				tableauView.SetCamera(_continuousRenderCamera);
				tableauView.SetSceneUsesSkybox(value: false);
				tableauView.SetContinuousRendering(true);
				tableauView.SetDeleteAfterRendering(value: false);
				tableauView.SetDoNotRenderThisFrame(value: true);
				tableauView.SetClearColor(0u);
			}
		}

	}
}
#endif
