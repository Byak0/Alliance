#if !SERVER
using Alliance.Common.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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
	/// elapsed time, so sharing one scene no longer speeds animations up (the former tick-skip hack).
	/// </summary>
	public class AL_CharacterTableau
	{
		// Two tableau ticks less than this apart are considered part of the same rendered frame.
		private static readonly TimeSpan FrameGateInterval = TimeSpan.FromMilliseconds(4);

		// Registered tableaus, for the render scheduler.
		private static readonly List<AL_CharacterTableau> _instances = new List<AL_CharacterTableau>();
		private static DateTime _lastFrameUtc = DateTime.MinValue;
		private static int _rrCursor;
		private static AL_CharacterTableau _rrCurrent;

		private FakeAgent _fake;
		private string _fakePoolKey;
		private int _equipmentSeed;

		private int _slotIndex;
		private MatrixFrame _camPos;
		private float _verticalFov = (float)Math.PI / 4f;
		private bool _isCamAnimating;
		private MatrixFrame _camStartFrame, _camTargetFrame;
		private float _fovStart, _fovTarget;
		private float _camAnimTimer, _camAnimDuration;

		public float CameraFov
		{
			get => _verticalFov;
			set
			{
				_verticalFov = value;
			}
		}

		private int _pendingCameraAnimTick;
		private bool _pendingCameraAnim;
		private struct PendingCamData
		{
			public float Elev, Strafe, Zoom, Pitch, Yaw, Roll, Fov, Duration;
		}
		private PendingCamData _pendingCamData;

		private bool _pendingLightEnable;
		public bool IsLightEnabled { get; internal set; }

		private bool _isFinalized;
		private MatrixFrame _mountSpawnPoint;
		private MatrixFrame _bannerSpawnPoint;
		private MatrixFrame _characterSpawnPoint;
		private MatrixFrame _mountSpawnPointSwapped;
		private MatrixFrame _characterSpawnPointSwapped;
		private Scene _tableauScene;
		private MBAgentRendererSceneController _agentRendererSceneController;
		private Camera _continuousRenderCamera;
		private float _cameraRatio;
		private MatrixFrame _initialCamPos;
		private string _charStringId;
		private int _tableauSizeX;
		private int _tableauSizeY;
		private uint _clothColor1 = new Color(1f, 1f, 1f).ToUnsignedInteger();
		private uint _clothColor2 = new Color(1f, 1f, 1f).ToUnsignedInteger();
		private bool _isRotatingCharacter;
		private bool _isCharacterMountPlacesSwapped;
		private string _equipmentCode = "";
		private float _mainCharacterRotation;
		private bool _isEnabled;
		private float RenderScale = 1f;
		private float _customRenderScale = 1f;
		private int _latestWidth = -1;
		private int _latestHeight = -1;
		private string _bodyPropertiesCode;
		private bool _isFemale;
		private CharacterViewModelStance _stanceIndex;
		private Banner _banner;
		private bool _isBannerShownInBackground;
		private ItemObject _bannerItem;
		private GameEntity _bannerEntity;
		private int _leftHandEquipmentIndex = -1;
		private int _rightHandEquipmentIndex = -1;
		private bool _isEquipmentIndicesDirty;
		private bool _isVisualsDirty;
		private bool _forceRenderOnce;
		private Light _light;

		private string _idleActionName;
		private string _idleFaceAnim;
		private float _idleAnimationTimer;

		private string _customAnimationName;
		private string _customAnimation;
		private float _customAnimationTimer;
		private bool _customAnimationStartScheduled;

		public Texture Texture { get; private set; }

		public bool IsRunningCustomAnimation => _customAnimation != null || _customAnimationStartScheduled;

		public bool ShouldLoopCustomAnimation { get; set; }

		public float CustomAnimationWaitDuration { get; set; }

		/// <summary>When true (default) the tableau renders every frame. When false it shares the
		/// frame's render budget with the other non-continuous tableaus (round-robin) - bind it to
		/// the item selection so only the selected preview animates smoothly.</summary>
		public bool Continuous { get; set; } = true;

		private TableauView View => Texture?.TableauView;

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

			// Shared-scene animation speedup workaround (former tick-skip hack): the fake advances
			// with the real time elapsed since its last rendered tick, not with the render rate.
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

			// Custom animation scheduling
			if (_customAnimationStartScheduled)
			{
				StartCustomAnimation();
			}

			// Custom animation looping
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

			// Animate camera
			if (_isCamAnimating)
			{
				if (_camAnimDuration == 0)
				{
					_camPos.origin = _camTargetFrame.origin;
					_camPos.rotation = _camTargetFrame.rotation;
					_verticalFov = _fovTarget;
					_isCamAnimating = false;
				}
				else
				{
					_camAnimTimer += elapsed;

					float t = Math.Min(1f, _camAnimTimer / _camAnimDuration);
					t = t * t * (3f - 2f * t); // smoothstep

					_camPos.origin = Vec3.Lerp(_camStartFrame.origin, _camTargetFrame.origin, t);
					_camPos.rotation = Mat3.Lerp(_camStartFrame.rotation, _camTargetFrame.rotation, t);
					_verticalFov = _fovStart + (_fovTarget - _fovStart) * t;

					if (t >= 1f)
						_isCamAnimating = false;
				}
			}

			// Update weapon indices
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

		private float _delaySinceLastUpdate;

		/// <summary>True when this tableau may render this frame: continuous tableaus always,
		/// the others one at a time (round-robin) so the shared scene renders at a bounded rate.</summary>
		private bool ShouldRenderThisFrame()
		{
			if (_forceRenderOnce)
			{
				_forceRenderOnce = false;
				return true;
			}

			// The shared scene advances its skeletons on EVERY tableau render: rendering several
			// tableaus in the same frame multiplies their animation speed. Exactly ONE tableau
			// renders per frame - continuous ones are preferred (the selected preview animates at
			// full rate when alone), the others take turns when no continuous one is enabled.
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

		/// <summary>
		/// Smoothly interpolate camera from its current frame/FOV
		/// to the given target over the given duration (seconds).
		/// </summary>
		public void AnimateCamera(float cameraElevation, float cameraStrafe, float cameraZoom, float cameraPitch, float cameraYaw, float cameraRoll, float cameraFov, float cameraAnimDuration)
		{
			_pendingCameraAnim = true;
			_pendingCamData = new PendingCamData
			{
				Elev = cameraElevation,
				Strafe = cameraStrafe,
				Zoom = cameraZoom,
				Pitch = cameraPitch,
				Yaw = cameraYaw,
				Roll = cameraRoll,
				Fov = cameraFov,
				Duration = cameraAnimDuration
			};
		}

		private void AnimateCameraInner(float cameraElevation, float cameraStrafe, float cameraZoom, float cameraPitch, float cameraYaw, float cameraRoll, float cameraFov, float cameraAnimDuration)
		{
			MatrixFrame newCameraFrame = _camTargetFrame;
			newCameraFrame.Advance(cameraElevation);
			newCameraFrame.Strafe(cameraStrafe);
			newCameraFrame.Elevate(cameraZoom);
			newCameraFrame.rotation.ApplyEulerAngles(new Vec3(cameraPitch, cameraYaw, cameraRoll));
			_camStartFrame = _camPos;
			_camTargetFrame = newCameraFrame;

			_fovStart = _verticalFov;
			_fovTarget = cameraFov;

			_camAnimDuration = cameraAnimDuration;
			_camAnimTimer = 0f;
			_isCamAnimating = true;
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
				RenderScale = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.ResolutionScale) / 100f;
				_tableauSizeX = (int)(width * _customRenderScale * RenderScale);
				_tableauSizeY = (int)(height * _customRenderScale * RenderScale);
			}

			_cameraRatio = _tableauSizeX / (float)_tableauSizeY;
			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.Release();
			Texture = TableauView.AddTableau("AL_CharacterTableau_" + _lastTableauIndex++, new RenderTargetComponent.TextureUpdateEventHandler(CharacterTableauContinuousRenderFunction), _tableauScene, _tableauSizeX, _tableauSizeY);
			Texture.TableauView.SetSceneUsesContour(value: false);
			Texture.TableauView.SetFocusedShadowmap(enable: true, ref _characterSpawnPoint.origin, 2.55f);

			View.SetCamera(_continuousRenderCamera);
			View.SetScene(_tableauScene);
			_forceRenderOnce = true;
		}

		private static int _lastTableauIndex = 0;

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
			// is driven by its idle clip (and mounts are part of its hierarchy).
			switch (_stanceIndex)
			{
				case CharacterViewModelStance.EmphasizeFace:
					_initialCamPos.Elevate(-2f);
					_initialCamPos.Advance(0.5f);
					break;
				case CharacterViewModelStance.SideView:
				case CharacterViewModelStance.OnMount:
					_initialCamPos.Advance(0.5f);
					break;
			}
			_camPos = _initialCamPos;
			_camTargetFrame = _initialCamPos;
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
			// No-op kept for prefab/VM compatibility: the FakeAgent dresses from the character
			// definition's own race, the raw race index is unused on this pipeline.
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
			// Resolve the VM's name against the character's own action set: an action-type name
			// ("act_inventory_idle") gives the race's clip, a raw clip name passes through.
			string clip = AllianceData.Instance.GetAnimationOfAction(_fake.BodyActionSetId, _idleActionName) ?? _idleActionName;
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
			if (_equipmentCode != equipmentCode && !string.IsNullOrEmpty(equipmentCode))
			{
				_equipmentCode = equipmentCode;
				_bannerItem = null;
				_isVisualsDirty = true;
				UpdateBannerItem();
			}
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

		/// <summary>(Re)spawns the FakeAgent dressed from the catalog definition of
		/// <see cref="_charStringId"/>, with slot overrides from the equipment code when present.</summary>
		private void RebuildFake()
		{
			ReturnFakeToPool();
			ForceRenderOnce();

			if (_tableauScene == null || string.IsNullOrEmpty(_charStringId)) return;

			AllianceData.CharacterDefinition definition = BuildDefinition();
			if (definition == null || definition.EquipmentRosters.Count == 0) return;

			MatrixFrame frame = _isCharacterMountPlacesSwapped ? _mountSpawnPoint : _characterSpawnPoint;
			frame.rotation.RotateAboutUp(_mainCharacterRotation);

			// Native inventory-preview convention: the mount stands at its dedicated slot BESIDE
			// the character (never under the camera), and the rider stays on foot.
			MatrixFrame mountFrame = _isCharacterMountPlacesSwapped ? _characterSpawnPoint : _mountSpawnPoint;

			// Simple-skeleton pipeline on purpose: the same one cinematics use, proven in every
			// context (kit included). The native AgentVisuals pipeline deadlocks the kit editor on
			// materials whose shader variants are missing from the kit's shader sack (runtime
			// shader compilation hangs the render thread), and its deferred resource queue is
			// never pumped there (invisible previews). No facial animations on previews - accepted.
			// Colors come from the definition's own culture (FakeAgent resolves them).
			// Fakes are pooled per character key: a formation refresh recreates every widget, and
			// rebuilding each fake from scratch reloads all their meshes (the add-agent lag).
			_fakePoolKey = $"{definition.Id}|{definition.CultureId}|{definition.IsFemale}|{_equipmentSeed}";
			_fake = FakeAgentPool.Acquire(_fakePoolKey, () => new FakeAgent(_tableauScene, frame, definition, definition.CultureId,
				name: "Tableau_" + _charStringId,
				useFullAgentVisuals: false,
				weaponsDrawn: false,
				equipmentSeed: _equipmentSeed,
				register: false,
				mountFrame: mountFrame));
			if (!_fake.IsValid)
			{
				FakeAgentPool.Return(_fakePoolKey, _fake);
				_fake = null;
				return;
			}
			// Place the fake at this widget's slot (pooled fakes come back from their old slot)
			// and show it right away: the kit never pumps the deferred resource queue, so a
			// CheckResources gate left previews invisible forever (meshes are loaded synchronously
			// by the simple pipeline; face textures pop in as they finish instead).
			_fake.Teleport(frame);
			_fake.SetVisible(true);
			PlayIdleAction();
			if (IsLightEnabled)
			{
				// The stale light went back to the pool with the previous fake - re-arm it on the
				// newly acquired one.
				IsLightEnabled = false;
				EnableLightInner();
			}
			if (_pendingLightEnable)
			{
				_pendingLightEnable = false;
				EnableLightInner();
			}
			_customAnimationStartScheduled = IsRunningCustomAnimation;
			if (!string.IsNullOrEmpty(_idleFaceAnim)) SetIdleFaceAnim(_idleFaceAnim);
		}

		private void ReturnFakeToPool()
		{
			if (_fake != null) FakeAgentPool.Return(_fakePoolKey, _fake);
			_fake = null;
		}

		/// <summary>Builds the FakeAgent input: the catalog definition of the character, cloned with
		/// an equipment roster derived from the equipment code when one was set (perk alternatives...).
		/// The catalog object is never mutated. Unknown ids fall back to dressing from the equipment
		/// code alone (e.g. code-registered characters).</summary>
		private AllianceData.CharacterDefinition BuildDefinition()
		{
			AllianceData.CharacterDefinition template = AllianceData.Instance.GetCharacter(_charStringId);
			if (template == null && !string.IsNullOrEmpty(_equipmentCode))
			{
				template = new AllianceData.CharacterDefinition { Id = _charStringId, RaceId = "human", IsFemale = _isFemale };
			}
			if (template == null) return null;

			if (string.IsNullOrEmpty(_equipmentCode)) return template;

			Dictionary<int, string> slots = new Dictionary<int, string>();
			try
			{
				Equipment equipment = Equipment.CreateFromEquipmentCode(_equipmentCode);
				_bannerItem = GetBannerFromEquipment(equipment);
				for (EquipmentIndex index = EquipmentIndex.Weapon0; index <= EquipmentIndex.ArmorItemEndSlot; index++)
				{
					ItemObject item = equipment[index].Item;
					if (item != null) slots[(int)index] = item.StringId;
				}
			}
			catch (Exception ex)
			{
				Log($"[AL_CharacterTableau] Failed to parse equipment code of '{_charStringId}' ({ex.Message}).", LogLevel.Warning);
				return template;
			}
			if (slots.Count == 0) return template;

			AllianceData.CharacterDefinition definition = new AllianceData.CharacterDefinition
			{
				Id = template.Id,
				Name = template.Name,
				CultureId = template.CultureId,
				RaceId = template.RaceId,
				IsFemale = _isFemale || template.IsFemale,
				IsMultiplayer = template.IsMultiplayer,
				ClassTypes = template.ClassTypes
			};
			definition.EquipmentRosters.Add(slots);
			return definition;
		}

		private ItemObject GetBannerFromEquipment(Equipment equipment)
		{
			ItemObject item = equipment[EquipmentIndex.ExtraWeaponSlot].Item;
			return item != null && item.IsBannerItem ? item : null;
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
				_mountSpawnPointSwapped = new MatrixFrame(_mountSpawnPoint.rotation, _characterSpawnPoint.origin);
				_mountSpawnPointSwapped.Strafe(-0.25f);
				_characterSpawnPointSwapped = new MatrixFrame(_characterSpawnPoint.rotation, _mountSpawnPoint.origin);
				_characterSpawnPointSwapped.Strafe(0.25f);
				_initialCamPos = slot.CameraFrame;
				_camPos = slot.CameraFrame;
				_camTargetFrame = slot.CameraFrame;
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

			tableauView.SetRenderWithPostfx(value: true);

			if (_continuousRenderCamera != null)
			{
				// use custom animated FOV, and aim the camera at the character's chest (the
				// procedural scene provides no authored camera rotation - LookAt removes the guesswork)
				_continuousRenderCamera.SetFovVertical(
					_verticalFov,
					_cameraRatio,
					0.2f, 200f
				);
				Vec3 lookTarget = _characterSpawnPoint.origin + new Vec3(0f, 0f, 0.9f);
				_continuousRenderCamera.LookAt(_camPos.origin, lookTarget, new Vec3(0f, 0f, 1f));

				tableauView.SetCamera(_continuousRenderCamera);
				tableauView.SetSceneUsesSkybox(value: false);
				tableauView.SetContinuousRendering(true);
				tableauView.SetDeleteAfterRendering(value: false);
				tableauView.SetDoNotRenderThisFrame(value: true);
				tableauView.SetClearColor(0u);
				tableauView.SetFocusedShadowmap(enable: true, ref _characterSpawnPoint.origin, 1.55f);
			}
		}

		public void EnableLight()
		{
			_pendingLightEnable = true;
		}

		private void EnableLightInner()
		{
			GameEntity entity = _fake?.Root;
			if (entity == null)
			{
				return;
			}

			if (IsLightEnabled) return;

			IsLightEnabled = true;

			_light = Light.CreatePointLight(4f);
			_light.Intensity = 0.2f;
			_light.LightColor = new Vec3(180f, 180f, 255f);
			_light.SetShadowType(Light.ShadowType.DynamicShadow);
			_light.ShadowEnabled = false;
			_light.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0.8f, 0.2f));

			entity.AddLight(_light);

			_pendingLightEnable = false;
		}

		public void DisableLight()
		{
			IsLightEnabled = false;

			GameEntity entity = _fake?.Root;
			if (entity == null) return;

			if (_light != null && _light.IsValid && entity.HasComponent(_light))
			{
				entity.RemoveComponent(_light);
			}
			_light = null;
		}
	}

	public static class ALCharacterTableauSceneCache
	{
		private static Scene _scene;
		private static MBAgentRendererSceneController _controller;

		private static MatrixFrame[] _characterSlots;
		private static MatrixFrame[] _mountSlots;
		private static MatrixFrame[] _bannerSlots;
		private static MatrixFrame[] _cameraSlots;

		private static bool[] _slotUsed;

		private const int SlotCount = 32;
		private const float SlotSpacing = 4.0f;

		public static void Initialize()
		{
			if (_scene != null)
				return;

			// Procedural empty scene: reading the native inventory_character_scene deadlocks
			// intermittently in the kit editor (its resource pipeline is not pumped there), and the
			// preview only needs the agent, a fill light and camera slots - all built here. The
			// backdrop is lost (black background), the previews stay functional everywhere.
			_scene = Scene.CreateNewScene(initialize_physics: false, enable_decals: false);			
			_scene.SetName("AL_CharacterTableau");
			_scene.DisableStaticShadows(value: true);
			// Same atmosphere as the native inventory scene (bright neutral ambient, no fog):
			// without it the scene renders dark and hazy - the "blurry" look.
			//_scene.SetAtmosphereWithName("scene_atmosphere");
			//Vec3 sceneAmbient = new Vec3(0.63f, 0.66f, 0.71f);
			//_scene.SetFogAmbientColor(ref sceneAmbient);
			// A fresh scene carries a default fog that hazes everything the tableau renders.
			//Vec3 fogColor = Vec3.Zero;
			//_scene.SetFog(0f, ref fogColor, 0f);

			//_controller = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_scene);
			//_controller.SetDoTimerBasedForcedSkeletonUpdates(false);

			_characterSlots = new MatrixFrame[SlotCount];
			_mountSlots = new MatrixFrame[SlotCount];
			_bannerSlots = new MatrixFrame[SlotCount];
			_cameraSlots = new MatrixFrame[SlotCount];

			// The scene has no sun and (in the kit) no registered atmosphere: fill lights along the
			// slot line plus frontal lights (camera side) so every agent is evenly and brightly lit.
			for (int i = 0; i < SlotCount; i += 2)
			{
				GameEntity lightEntity = GameEntity.CreateEmpty(_scene, true, false, false);
				Light fill = Light.CreatePointLight(10f);
				fill.Intensity = 0.5f;
				fill.LightColor = new Vec3(240f, 240f, 240f);
				fill.Frame = new MatrixFrame(Mat3.Identity, new Vec3(1.2f, i * SlotSpacing, 2.2f));
				lightEntity.AddLight(fill);
			}

			// Frontal lights (camera side of the slots): light the agents' faces/details.
			for (int i = 0; i < 4; i++)
			{
				GameEntity frontEntity = GameEntity.CreateEmpty(_scene, true, false, false);
				Light front = Light.CreatePointLight(12f);
				front.Intensity = 0.6f;
				front.LightColor = new Vec3(255f, 245f, 225f);
				front.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 2.5f + i * 16f, 2.4f));
				frontEntity.AddLight(front);
			}

			for (int i = 0; i < SlotCount; i++)
			{
				MatrixFrame character = MatrixFrame.Identity;
				character.origin.y += i * SlotSpacing;
				_characterSlots[i] = character;

				MatrixFrame mount = character;
				mount.origin.x -= 3f;
				_mountSlots[i] = mount;

				MatrixFrame banner = character;
				banner.origin.x += 2f;
				banner.origin.y -= 0.5f;
				_bannerSlots[i] = banner;

				// Characters face +Y with identity rotation - the camera sits on their facing side.
				MatrixFrame camera = character;
				camera.origin.y += 3.6f;
				camera.origin.z += 1.15f;
				_cameraSlots[i] = camera;
			}

			_slotUsed = new bool[SlotCount];
		}

		public struct SlotData
		{
			public int Index;
			public MatrixFrame CharacterFrame;
			public MatrixFrame MountFrame;
			public MatrixFrame BannerFrame;
			public MatrixFrame CameraFrame;
		}

		public static Scene GetScene() => _scene;

		public static SlotData AcquireSlot()
		{
			for (int i = 0; i < SlotCount; i++)
			{
				if (!_slotUsed[i])
				{
					_slotUsed[i] = true;
					return SlotDataOf(i);
				}
			}

			// Exhausted (a list refresh can create the new widgets before finalizing the old
			// ones): share a slot instead of crashing - the two previews briefly overlap.
			Log("ALCharacterTableauSceneCache: all slots in use - sharing a slot.", LogLevel.Warning);
			return SlotDataOf(_sharedCursor++ % SlotCount);
		}

		private static int _sharedCursor;

		private static SlotData SlotDataOf(int index)
		{
			return new SlotData
			{
				Index = index,
				CharacterFrame = _characterSlots[index],
				MountFrame = _mountSlots[index],
				BannerFrame = _bannerSlots[index],
				CameraFrame = _cameraSlots[index]
			};
		}

		public static void ReleaseSlot(int index)
		{
			if (index >= 0 && index < SlotCount)
			{
				_slotUsed[index] = false;
			}

			// The shared scene intentionally stays alive for the whole session (like the native
			// ThumbnailCacheManager inventory scene): destroying it when the last slot frees -
			// while list refreshes recreate widgets in the same frame - deadlocks the render
			// thread on the next CreateNewScene/Read of the same scene.
		}
	}

	/// <summary>
	/// Pool of dressed tableaus fakes, keyed by character (id + culture + gender + equipment seed).
	/// Formation/team refreshes recreate every widget: without the pool, each refresh destroyed and
	/// recreated all the fakes, reloading all their meshes (the add-agent lag). Parked fakes stay
	/// invisible in the tableau scene until a widget showing the same character acquires them.
	/// </summary>
	public static class FakeAgentPool
	{
		private const int Capacity = 64;

		private static readonly Dictionary<string, List<FakeAgent>> _byKey = new Dictionary<string, List<FakeAgent>>();
		private static readonly Queue<(string Key, FakeAgent Fake)> _parked = new Queue<(string, FakeAgent)>();

		/// <summary>Returns a parked fake for the key, or creates a new one through <paramref name="create"/>.</summary>
		public static FakeAgent Acquire(string key, Func<FakeAgent> create)
		{
			if (_byKey.TryGetValue(key, out List<FakeAgent> parked))
			{
				while (parked.Count > 0)
				{
					FakeAgent fake = parked[parked.Count - 1];
					parked.RemoveAt(parked.Count - 1);
					if (fake.IsValid) return fake;
				}
			}
			return create();
		}

		/// <summary>Parks a fake invisible under its key, ready for the next acquire. Evicts the
		/// oldest parked fake past the capacity (despawned - its meshes reload on next use).</summary>
		public static void Return(string key, FakeAgent fake)
		{
			if (fake == null || !fake.IsValid) return;
			fake.SetVisible(false);
			if (!_byKey.TryGetValue(key, out List<FakeAgent> parked))
			{
				_byKey[key] = parked = new List<FakeAgent>();
			}
			parked.Add(fake);
			_parked.Enqueue((key, fake));
			while (_parked.Count > Capacity)
			{
				(string evictedKey, FakeAgent evicted) = _parked.Dequeue();
				evicted.Despawn();
				_byKey[evictedKey]?.Remove(evicted);
			}
		}
	}
}
#endif
