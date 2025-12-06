#if !SERVER
using Alliance.Common.Extensions.AnimationPlayer;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
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
	public class AL_CharacterTableau
	{
		private static int _lastTableauIndex = 0;
		private static int _nbTableauEnabled = 0;

		private int _nbTickSkipped;
		private float _delaySinceLastUpdate;
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
		private float _animationFrequencyThreshold = 2.5f;
		private MatrixFrame _characterSpawnPoint;
		private MatrixFrame _mountSpawnPointSwapped;
		private MatrixFrame _characterSpawnPointSwapped;
		private AgentVisuals _agentVisuals;
		private AgentVisuals _mountVisuals;
		private int _agentVisualLoadingCounter;
		private int _mountVisualLoadingCounter;
		private int _initialLoadingCounter;
		private string _idleFaceAnim;
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
		private string _mountCreationKey = "";
		private string _equipmentCode = "";
		private bool _isEquipmentAnimActive;
		private float _animationGap;
		private float _mainCharacterRotation;
		private bool _isEnabled;
		private float RenderScale = 1f;
		private float _customRenderScale = 1f;
		private int _latestWidth = -1;
		private int _latestHeight = -1;
		private string _bodyPropertiesCode;
		private BodyProperties _bodyProperties = BodyProperties.Default;
		private bool _isFemale;
		private CharacterViewModel.StanceTypes _stanceIndex;
		private Equipment _equipment;
		private Banner _banner;
		private int _race;
		private bool _isBannerShownInBackground;
		private ItemObject _bannerItem;
		private GameEntity _bannerEntity;
		private int _leftHandEquipmentIndex;
		private int _rightHandEquipmentIndex;
		private bool _isEquipmentIndicesDirty;

		private ActionIndexCache _idleAction = ActionIndexCache.act_none;
		private float _idleAnimationTimer;

		private bool _customAnimationStartScheduled;
		private float _customAnimationTimer;
		private string _customAnimationName;
		private ActionIndexCache _customAnimation = ActionIndexCache.act_none;
		private MBActionSet _characterActionSet;

		private bool _isVisualsDirty;
		private Light _light;

		private static readonly ActionIndexCache act_cheer_1 = ActionIndexCache.Create("act_arena_winner_1");
		private static readonly ActionIndexCache act_inventory_idle_start = ActionIndexCache.Create("act_inventory_idle_start");
		private static readonly ActionIndexCache act_inventory_glove_equip = ActionIndexCache.Create("act_inventory_glove_equip");
		private static readonly ActionIndexCache act_inventory_cloth_equip = ActionIndexCache.Create("act_inventory_cloth_equip");
		private static readonly ActionIndexCache act_horse_stand = ActionIndexCache.Create("act_horse_idle_1");
		private static readonly ActionIndexCache act_camel_stand = ActionIndexCache.Create("act_camel_stand_1");

		public Texture Texture { get; private set; }

		public bool IsRunningCustomAnimation
		{
			get
			{
				return _customAnimation != ActionIndexCache.act_none || _customAnimationStartScheduled;
			}
		}

		public bool ShouldLoopCustomAnimation { get; set; }

		public float CustomAnimationWaitDuration { get; set; }

		private TableauView View
		{
			get
			{
				if (Texture != null)
				{
					return Texture.TableauView;
				}

				return null;
			}
		}

		public AL_CharacterTableau()
		{
			_leftHandEquipmentIndex = -1;
			_rightHandEquipmentIndex = -1;
			_isVisualsDirty = false;
			_equipment = new Equipment();
			SetEnabled(enabled: true);
			FirstTimeInit();
		}

		public void OnTick(float dt)
		{
			// Hack mechanic to counter sped up animations.
			// We're using the same scene across tableaus for better performance, but it causes animations to speed up
			// due to how tableau scenes render.
			// So we skip most ticks to "slow down" animations and keep them at correct speed.
			_delaySinceLastUpdate += dt;
			_nbTickSkipped++;
			if (_nbTickSkipped < _nbTableauEnabled) return;

			if (_agentVisuals == null) return;

			// Custom animation scheduling
			if (_customAnimationStartScheduled)
			{
				StartCustomAnimation();
			}

			// Custom animation looping (except for act_none)
			if (_customAnimation != ActionIndexCache.act_none && _characterActionSet.IsValid)
			{
				_customAnimationTimer += _delaySinceLastUpdate;
				float duration = MBActionSet.GetActionAnimationDuration(_characterActionSet, _customAnimation);

				if (_customAnimationTimer > duration)
				{
					if (_customAnimationTimer > duration + CustomAnimationWaitDuration)
					{
						if (ShouldLoopCustomAnimation)
							StartCustomAnimation();
						else
							StopCustomAnimationIfCantContinue();
					}
					else
					{
						AnimationSystem.Instance.PlayAnimation(_agentVisuals, GetIdleAction().GetName());
					}
				}
			}
			// Idle animation looping (except for act_none)
			else if (_idleAction != ActionIndexCache.act_none)
			{
				// Only handle idle looping when no custom animation is playing
				_idleAnimationTimer += _delaySinceLastUpdate;
				float idleDuration = MBActionSet.GetActionAnimationDuration(_characterActionSet, _idleAction);

				if (idleDuration > 0 && _idleAnimationTimer >= idleDuration)
				{
					// Add optional wait delay between loops
					if (_idleAnimationTimer > idleDuration)
					{
						AnimationSystem.Instance.PlayAnimation(_agentVisuals, _idleAction.GetName());
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
					_camAnimTimer += _delaySinceLastUpdate;

					float t = Math.Min(1f, _camAnimTimer / _camAnimDuration);
					t = t * t * (3f - 2f * t); // smoothstep

					_camPos.origin = Vec3.Lerp(_camStartFrame.origin, _camTargetFrame.origin, t);
					_camPos.rotation = Mat3.Lerp(_camStartFrame.rotation, _camTargetFrame.rotation, t);
					_verticalFov = _fovStart + (_fovTarget - _fovStart) * t;

					if (t >= 1f)
						_isCamAnimating = false;
				}
			}

			// Animation gap timer
			if (_animationFrequencyThreshold > _animationGap)
			{
				_animationGap += _delaySinceLastUpdate;
			}

			// Tick visuals
			if (_isEnabled)
			{
				_agentVisuals?.TickVisuals();
				_mountVisuals?.TickVisuals();
			}

			// Render texture view
			if (View != null)
			{
				View.SetDoNotRenderThisFrame(false);
				View.SetContinuousRendering(true);
			}

			// Refresh visuals if dirty
			if (_isVisualsDirty)
			{
				RefreshCharacterTableau();
				_isVisualsDirty = false;
			}

			// Check resource loading
			if (_agentVisualLoadingCounter > 0 && _agentVisuals.GetEntity().CheckResources(true, true))
				_agentVisualLoadingCounter--;

			if (_mountVisualLoadingCounter > 0 && _mountVisuals.GetEntity().CheckResources(true, true))
				_mountVisualLoadingCounter--;

			// Once loading is done, enable visuals
			if (_mountVisualLoadingCounter == 0 && _agentVisualLoadingCounter == 0)
			{
				_mountVisuals?.SetVisible(_bodyProperties != BodyProperties.Default);
				_agentVisuals?.SetVisible(_bodyProperties != BodyProperties.Default);
				if (_pendingLightEnable)
				{
					_pendingLightEnable = false;
					EnableLightInner();
				}
				if (_pendingCameraAnim)
				{
					_pendingCameraAnimTick++;
					if (_pendingCameraAnimTick > 2)
					{
						_pendingCameraAnimTick = 0;
						_pendingCameraAnim = false;
						AnimateCameraInner(_pendingCamData.Elev, _pendingCamData.Strafe, _pendingCamData.Zoom, _pendingCamData.Pitch, _pendingCamData.Yaw, _pendingCamData.Roll, _pendingCamData.Fov, _pendingCamData.Duration);
					}
				}
			}

			// Update weapon indices
			if (_isEquipmentIndicesDirty)
			{
				_agentVisuals.GetVisuals().SetWieldedWeaponIndices(_rightHandEquipmentIndex, _leftHandEquipmentIndex);
				_isEquipmentIndicesDirty = false;
			}

			_delaySinceLastUpdate = 0f;
			_nbTickSkipped = 0;
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
			if (_customAnimation != null && _characterActionSet.IsValid)
			{
				float actionAnimationDuration = MBAnimation.GetAnimationDuration(_customAnimation.Index);
				if (actionAnimationDuration == 0f)
				{
					return -1f;
				}

				return _customAnimationTimer / actionAnimationDuration;
			}

			return -1f;
		}

		private void StopCustomAnimationIfCantContinue()
		{
			bool flag = false;
			if (_agentVisuals != null && _customAnimation != ActionIndexCache.act_none && !string.IsNullOrEmpty(_customAnimationName))
			{
				ActionIndexCache actionAnimationContinueToAction = MBActionSet.GetActionAnimationContinueToAction(_characterActionSet, in _customAnimation);
				if (actionAnimationContinueToAction.Index >= 0)
				{
					_customAnimationName = actionAnimationContinueToAction.GetName();
					StartCustomAnimation();
					flag = true;
				}
			}
			if (!flag)
			{
				StopCustomAnimation();
				_customAnimationTimer = -1f;
			}
		}

		public void SetEnabled(bool enabled)
		{
			if (enabled == _isEnabled) return;

			_isEnabled = enabled;
			View?.SetEnable(_isEnabled);
			if (enabled) _nbTableauEnabled++;
			else _nbTableauEnabled--;
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
		}

		public void SetCharStringID(string charStringId)
		{
			if (_charStringId != charStringId)
			{
				_charStringId = charStringId;
			}
		}

		public void OnFinalize()
		{
			Camera continuousRenderCamera = _continuousRenderCamera;
			if (continuousRenderCamera != null)
			{
				continuousRenderCamera.ReleaseCameraEntity();
				_continuousRenderCamera = null;
			}
			_agentVisuals?.ResetNextFrame();
			_agentVisuals = null;
			_mountVisuals?.ResetNextFrame();
			_mountVisuals = null;
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
			_isFinalized = true;
		}

		public void SetBodyProperties(string bodyPropertiesCode)
		{
			if (_bodyPropertiesCode != bodyPropertiesCode)
			{
				_bodyPropertiesCode = bodyPropertiesCode;
				if (!string.IsNullOrEmpty(bodyPropertiesCode) && BodyProperties.FromString(bodyPropertiesCode, out var bodyProperties))
				{
					_bodyProperties = bodyProperties;
				}
				else
				{
					_bodyProperties = BodyProperties.Default;
				}

				_isVisualsDirty = true;
			}
		}

		public void SetStanceIndex(int index)
		{
			if (_stanceIndex != (CharacterViewModel.StanceTypes)index)
			{
				_stanceIndex = (CharacterViewModel.StanceTypes)index;
				_isVisualsDirty = true;
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
			switch (_stanceIndex)
			{
				case CharacterViewModel.StanceTypes.EmphasizeFace:
					_initialCamPos.Elevate(-2f);
					_initialCamPos.Advance(0.5f);
					AnimationSystem.Instance.PlayAnimation(_agentVisuals, GetIdleAction().GetName());
					_idleAnimationTimer = 0f;
					break;
				case CharacterViewModel.StanceTypes.SideView:
				case CharacterViewModel.StanceTypes.OnMount:
					if (_agentVisuals != null)
					{
						if (_equipment[10].Item != null)
						{
							_initialCamPos.Advance(0.5f);

							AnimationSystem.Instance.PlayAnimation(_agentVisuals, _mountVisuals.GetEntity().Skeleton.GetActionAtChannel(0).GetName());
							_idleAnimationTimer = 0f;
						}
						else
						{
							_initialCamPos.Elevate(-2f);
							_initialCamPos.Advance(0.5f);
							AnimationSystem.Instance.PlayAnimation(_agentVisuals, GetIdleAction().GetName());
							_idleAnimationTimer = 0f;
						}
					}

					break;
				case CharacterViewModel.StanceTypes.CelebrateVictory:
					AnimationSystem.Instance.PlayAnimation(_agentVisuals, act_cheer_1.GetName());
					_idleAnimationTimer = 0f;
					break;
				case CharacterViewModel.StanceTypes.None:
					AnimationSystem.Instance.PlayAnimation(_agentVisuals, GetIdleAction().GetName());
					_idleAnimationTimer = 0f;
					break;
			}
			_camPos = _initialCamPos;
			_camTargetFrame = _initialCamPos;
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
			_isVisualsDirty = true;
		}

		public void SetRace(int race)
		{
			_race = race;
			_isVisualsDirty = true;
		}

		public void SetIdleAction(string idleAction)
		{
			_idleAction = ActionIndexCache.Create(idleAction);
			_isVisualsDirty = true;
		}

		public void SetCustomAnimation(string animation)
		{
			_customAnimationName = animation;
		}

		public void StartCustomAnimation()
		{
			if (_isVisualsDirty || _agentVisuals == null || string.IsNullOrEmpty(_customAnimationName))
			{
				_customAnimationStartScheduled = true;
				return;
			}

			StopCustomAnimation();
			_customAnimation = ActionIndexCache.Create(_customAnimationName);
			if (_customAnimation.Index >= 0)
			{
				AnimationSystem.Instance.PlayAnimation(_agentVisuals, _customAnimationName);
				_customAnimationStartScheduled = false;
				_customAnimationTimer = 0f;
			}
			else
			{
				Debug.FailedAssert("Invalid custom animation in character tableau: " + _customAnimationName, "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.View\\Tableaus\\CharacterTableau.cs", "StartCustomAnimation", 599);
			}
		}

		public void StopCustomAnimation()
		{
			if (_agentVisuals != null && _customAnimation != ActionIndexCache.act_none)
			{
				if (MBActionSet.GetActionAnimationContinueToAction(_characterActionSet, in _customAnimation).Index < 0)
				{
					AgentVisuals agentVisuals = _agentVisuals;
					ActionIndexCache idleAction = GetIdleAction();
					AnimationSystem.Instance.PlayAnimation(agentVisuals, idleAction.GetName());
				}
				_customAnimation = ActionIndexCache.act_none;
			}
		}

		public void SetIdleFaceAnim(string idleFaceAnim)
		{
			if (!string.IsNullOrEmpty(idleFaceAnim))
			{
				_idleFaceAnim = idleFaceAnim;
				_isVisualsDirty = true;
			}
		}

		public void SetEquipmentCode(string equipmentCode)
		{
			if (_equipmentCode != equipmentCode && !string.IsNullOrEmpty(equipmentCode))
			{
				_equipmentCode = equipmentCode;
				_equipment = Equipment.CreateFromEquipmentCode(equipmentCode);
				_bannerItem = GetAndRemoveBannerFromEquipment(ref _equipment);
				_agentVisuals.GetVisuals().SetWieldedWeaponIndices(4, 1);
				_isVisualsDirty = true;
			}
		}

		public void SetIsEquipmentAnimActive(bool value)
		{
			_isEquipmentAnimActive = value;
		}

		public void SetMountCreationKey(string value)
		{
			if (_mountCreationKey != value)
			{
				_mountCreationKey = value;
				_isVisualsDirty = true;
			}
		}

		public void SetBannerCode(string value)
		{
			_banner = string.IsNullOrEmpty(value) ? null : new Banner(value);
			_isVisualsDirty = true;
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

		private ActionIndexCache GetIdleAction()
		{
			if (_idleAction == ActionIndexCache.act_none)
			{
				return ActionIndexCache.act_inventory_idle_start;
			}
			return _idleAction;
		}

		private void RefreshCharacterTableau(Equipment oldEquipment = null)
		{
			UpdateMount(_stanceIndex == CharacterViewModel.StanceTypes.OnMount);
			UpdateBannerItem();
			if (_mountVisuals == null && _isCharacterMountPlacesSwapped)
			{
				_isCharacterMountPlacesSwapped = false;
				_mainCharacterRotation = 0f;
			}

			if (_agentVisuals != null)
			{
				AgentVisuals agentVisuals = _agentVisuals;
				_agentVisualLoadingCounter = 1;
				AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
				MatrixFrame frame = _isCharacterMountPlacesSwapped ? _characterSpawnPointSwapped : _characterSpawnPoint;
				if (!_isCharacterMountPlacesSwapped)
				{
					frame.rotation.RotateAboutUp(_mainCharacterRotation);
				}

				_characterActionSet = MBGlobals.GetActionSetWithSuffix(copyAgentVisualsData.MonsterData, _isFemale, "_warrior");
				copyAgentVisualsData.BodyProperties(_bodyProperties).SkeletonType(_isFemale ? SkeletonType.Female : SkeletonType.Male).Frame(frame)
					.ActionSet(_characterActionSet)
					.Equipment(_equipment)
					.Banner(_banner)
					.UseMorphAnims(useMorphAnims: true)
					.ClothColor1(_clothColor1)
					.ClothColor2(_clothColor2)
					.Race(_race);
				if (_initialLoadingCounter > 0)
				{
					_initialLoadingCounter--;
				}

				_agentVisuals.Refresh(needBatchedVersionForWeaponMeshes: false, copyAgentVisualsData);
				_agentVisuals.SetVisible(value: false);

				if (oldEquipment != null && _animationFrequencyThreshold <= _animationGap && _isEquipmentAnimActive)
				{
					if (_equipment[EquipmentIndex.Gloves].Item != null && oldEquipment[EquipmentIndex.Gloves].Item != _equipment[EquipmentIndex.Gloves].Item)
					{
						_agentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, act_inventory_glove_equip);
						_animationGap = 0f;
					}
					else if (_equipment[EquipmentIndex.Body].Item != null && oldEquipment[EquipmentIndex.Body].Item != _equipment[EquipmentIndex.Body].Item)
					{
						_agentVisuals.GetVisuals().GetSkeleton().SetAgentActionChannel(0, act_inventory_cloth_equip);
						_animationGap = 0f;
					}
				}
				UpdateWieldedWeapons();
			}

			AdjustCharacterForStanceIndex();
		}

		private void UpdateWieldedWeapons()
		{
			_agentVisuals.GetVisuals().SetWieldedWeaponIndices(_rightHandEquipmentIndex, _leftHandEquipmentIndex);
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
			if (_agentVisuals != null)
			{
				float num = mouseMoveX * 0.005f;
				_mainCharacterRotation += num;
				if (_isCharacterMountPlacesSwapped)
				{
					MatrixFrame frame = _mountVisuals.GetEntity().GetFrame();
					frame.rotation.RotateAboutUp(num);
					_mountVisuals.GetEntity().SetFrame(ref frame);
				}
				else
				{
					MatrixFrame frame2 = _agentVisuals.GetEntity().GetFrame();
					frame2.rotation.RotateAboutUp(num);
					_agentVisuals.GetEntity().SetFrame(ref frame2);
				}
			}
		}

		private void FirstTimeInit()
		{
			if (_continuousRenderCamera == null)
			{
				_continuousRenderCamera = Camera.CreateCamera();
			}

			if (_equipment == null)
			{
				return;
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

			InitializeAgentVisuals();
			_isVisualsDirty = true;
		}

		private void InitializeAgentVisuals()
		{
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(_race);
			_characterActionSet = MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _isFemale, "_warrior");

			_agentVisuals = AgentVisuals.Create(new AgentVisualsData().Banner(_banner).Equipment(_equipment).BodyProperties(_bodyProperties)
				.Race(_race)
				.Frame(_characterSpawnPoint)
				.UseMorphAnims(useMorphAnims: true)
				.ActionSet(_characterActionSet)
				.ActionCode(GetIdleAction())
				.Scene(_tableauScene)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(prepareImmediately: false)
				.SkeletonType(_isFemale ? SkeletonType.Female : SkeletonType.Male)
				.ClothColor1(_clothColor1)
				.ClothColor2(_clothColor2)
				.CharacterObjectStringId(_charStringId), "AL_AgentVisuals_" + _slotIndex, isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			_agentVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
			_agentVisuals.SetVisible(value: false);
			_initialLoadingCounter = 2;
			if (!string.IsNullOrEmpty(_idleFaceAnim))
			{
				_agentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
			}
		}

		private void UpdateMount(bool isRiderAgentMounted = false)
		{
			if (_mountVisuals != null)
			{
				_mountVisuals.ResetNextFrame();
				_mountVisuals = null;
				_mountVisualLoadingCounter = 0;
			}

			if (_equipment[EquipmentIndex.ArmorItemEndSlot].Item?.HorseComponent != null)
			{
				ItemObject item = _equipment[EquipmentIndex.ArmorItemEndSlot].Item;
				Monster monster = item.HorseComponent.Monster;
				Equipment equipment = new Equipment
				{
					[EquipmentIndex.ArmorItemEndSlot] = _equipment[EquipmentIndex.ArmorItemEndSlot],
					[EquipmentIndex.HorseHarness] = _equipment[EquipmentIndex.HorseHarness]
				};
				MatrixFrame frame = _isCharacterMountPlacesSwapped ? _mountSpawnPointSwapped : _mountSpawnPoint;
				if (_isCharacterMountPlacesSwapped)
				{
					frame.rotation.RotateAboutUp(_mainCharacterRotation);
				}

				_mountVisualLoadingCounter = 3;
				_characterActionSet = MBGlobals.GetActionSet(monster.ActionSetCode);
				ActionIndexCache idleAction = ActionIndexCache.act_none;
				AgentVisualsData agentVisualsData = new AgentVisualsData();
				agentVisualsData.Banner(_banner).Equipment(equipment).Frame(frame)
					.Scale(item.ScaleFactor)
					.ActionSet(MBGlobals.GetActionSet(monster.ActionSetCode))
					.ActionCode(idleAction)
					.Scene(_tableauScene)
					.Monster(monster)
					.PrepareImmediately(prepareImmediately: false)
					.ClothColor1(_clothColor1)
					.ClothColor2(_clothColor2)
					.MountCreationKey(_mountCreationKey);
				_mountVisuals = AgentVisuals.Create(agentVisualsData, "MountTableau", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
				_mountVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
				_mountVisuals.SetVisible(value: false);
				_mountVisuals.GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
			}
		}

		private void UpdateBannerItem()
		{
			if (_bannerEntity != null)
			{
				_tableauScene.RemoveEntity(_bannerEntity, 0);
				_bannerEntity = null;
			}

			if (!_isBannerShownInBackground || _bannerItem == null)
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

		private ItemObject GetAndRemoveBannerFromEquipment(ref Equipment equipment)
		{
			ItemObject result = null;
			ItemObject item = equipment[EquipmentIndex.ExtraWeaponSlot].Item;
			if (item != null && item.IsBannerItem)
			{
				result = equipment[EquipmentIndex.ExtraWeaponSlot].Item;
				equipment[EquipmentIndex.ExtraWeaponSlot] = EquipmentElement.Invalid;
			}

			return result;
		}

		internal void CharacterTableauContinuousRenderFunction(Texture sender, EventArgs e)
		{
			TableauView tableauView = View;
			if (tableauView == null)
				return;

			tableauView.SetRenderWithPostfx(value: true);

			if (_continuousRenderCamera != null)
			{
				// use custom animated FOV and frame
				_continuousRenderCamera.SetFovVertical(
					_verticalFov,
					_cameraRatio,
					0.2f, 200f
				);
				_continuousRenderCamera.Frame = _camPos;

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
			if (_agentVisuals == null || _agentVisuals.GetEntity() == null)
			{
				return;
			}

			if (IsLightEnabled) return;

			IsLightEnabled = true;

			GameEntity entity = _agentVisuals.GetEntity();

			_light = Light.CreatePointLight(4f);
			_light.Intensity = 0.2f;
			_light.LightColor = new Vec3(180f, 180f, 255f);
			_light.SetShadowType(Light.ShadowType.DynamicShadow);
			_light.ShadowEnabled = false;
			_light.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0.8f, 0.2f));

			entity.AddLight(_light);
			Log("light");

			_pendingLightEnable = false;
		}

		public void DisableLight()
		{
			IsLightEnabled = false;

			GameEntity entity = _agentVisuals?.GetEntity();
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

			SceneInitializationData initData = new SceneInitializationData(true)
			{
				InitPhysicsWorld = true,
				DoNotUseLoadingScreen = true
			};

			_scene = Scene.CreateNewScene();
			_scene.SetName("AL_CharacterTableau");
			_scene.DisableStaticShadows(value: true);
			_scene.SetClothSimulationState(state: true);
			_scene.Read("inventory_character_scene", ref initData);

			_controller = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_scene);
			_controller.SetDoTimerBasedForcedSkeletonUpdates(false);

			_characterSlots = new MatrixFrame[SlotCount];
			_mountSlots = new MatrixFrame[SlotCount];
			_bannerSlots = new MatrixFrame[SlotCount];
			_cameraSlots = new MatrixFrame[SlotCount];
			_slotUsed = new bool[SlotCount];

			GenerateSlots();
		}

		public static void OnFinalize()
		{
			if (_controller != null)
			{
				MBAgentRendererSceneController.DestructAgentRendererSceneController(_scene, _controller, deleteThisFrame: false);
				_controller = null;
			}
			if (_scene != null)
			{
				_scene.ManualInvalidate();
				_scene = null;
			}
		}

		private static void GenerateSlots()
		{
			MatrixFrame baseCharacter = _scene.FindEntityWithTag("agent_inv").GetGlobalFrame();
			MatrixFrame baseMount = _scene.FindEntityWithTag("horse_inv").GetGlobalFrame();
			MatrixFrame baseBanner = _scene.FindEntityWithTag("banner_inv").GetGlobalFrame();
			MatrixFrame baseCamera = _scene.FindEntityWithTag("camera_instance").GetGlobalFrame();

			for (int i = 0; i < SlotCount; i++)
			{
				float offset = i * SlotSpacing;

				// Character
				MatrixFrame character = baseCharacter;
				character.origin.y += offset;
				_characterSlots[i] = character;

				// Mount
				MatrixFrame mount = baseMount;
				mount.origin.y += offset;
				_mountSlots[i] = mount;

				// Banner
				MatrixFrame banner = baseBanner;
				banner.origin.y += offset;
				_bannerSlots[i] = banner;

				// Camera
				MatrixFrame camera = baseCamera;
				camera.origin.y += offset;
				_cameraSlots[i] = camera;
			}
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
					return new SlotData
					{
						Index = i,
						CharacterFrame = _characterSlots[i],
						MountFrame = _mountSlots[i],
						BannerFrame = _bannerSlots[i],
						CameraFrame = _cameraSlots[i]
					};
				}
			}

			throw new Exception("ALCharacterTableauSceneCache: All slots are in use.");
		}

		public static void ReleaseSlot(int index)
		{
			if (index >= 0 && index < SlotCount)
			{
				_slotUsed[index] = false;
			}

			if (_slotUsed.All(x => !x))
			{
				// all slots are free, we can finalize the scene
				OnFinalize();
			}
		}
	}
}
#endif