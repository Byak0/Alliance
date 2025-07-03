#if !SERVER
using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Tableaus;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	public class AL_CharacterTableau
	{
		private MatrixFrame _camPos;
		private float _verticalFov = (float)Math.PI / 4f;
		private bool _isCamAnimating;
		private MatrixFrame _camStartFrame, _camTargetFrame;
		private float _fovStart, _fovTarget;
		private float _camAnimTimer, _camAnimDuration;

		public MatrixFrame CameraFrame
		{
			get => _camPos;
			set
			{
				_camPos = value;
				_isCamAnimating = false;
			}
		}

		public float CameraFov
		{
			get => _verticalFov;
			set
			{
				_verticalFov = value;
			}
		}

		public bool IsLightEnabled { get; internal set; }

		private bool _isFinalized;
		private MatrixFrame _mountSpawnPoint;
		private MatrixFrame _bannerSpawnPoint;
		private float _animationFrequencyThreshold = 2.5f;
		private MatrixFrame _initialSpawnFrame;
		private MatrixFrame _characterMountPositionFrame;
		private MatrixFrame _mountCharacterPositionFrame;
		private AgentVisuals _agentVisuals;
		private AgentVisuals _mountVisuals;
		private int _agentVisualLoadingCounter;
		private int _mountVisualLoadingCounter;
		private AgentVisuals _oldAgentVisuals;
		private AgentVisuals _oldMountVisuals;
		private int _initialLoadingCounter;
		private ActionIndexCache _idleAction;
		private string _idleFaceAnim;
		private Scene _tableauScene;
		private MBAgentRendererSceneController _agentRendererSceneController;
		private Camera _continuousRenderCamera;
		private float _cameraRatio;
		private MatrixFrame _camPosGatheredFromScene;
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
		private bool _customAnimationStartScheduled;
		private float _customAnimationTimer;
		private string _customAnimationName;
		private ActionIndexCache _customAnimation;
		private MBActionSet _characterActionSet;
		private bool _isVisualsDirty;
		private Equipment _oldEquipment;
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
				if (!(_customAnimation != null))
				{
					return _customAnimationStartScheduled;
				}

				return true;
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
			// Custom animation scheduling
			if (_customAnimationStartScheduled)
			{
				StartCustomAnimation();
			}

			// Update custom animation timer and progress
			if (_customAnimation != null && _characterActionSet.IsValid)
			{
				_customAnimationTimer += dt;
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
						_agentVisuals?.SetAction(GetIdleAction());
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
				_camAnimTimer += dt;
				float t = Math.Min(1f, _camAnimTimer / _camAnimDuration);
				t = t * t * (3f - 2f * t); // smoothstep

				_camPos.origin = Vec3.Lerp(_camStartFrame.origin, _camTargetFrame.origin, t);
				_camPos.rotation = Mat3.Lerp(_camStartFrame.rotation, _camTargetFrame.rotation, t);
				_verticalFov = _fovStart + (_fovTarget - _fovStart) * t;

				if (t >= 1f)
					_isCamAnimating = false;
			}

			// Animation gap timer
			if (_animationFrequencyThreshold > _animationGap)
			{
				_animationGap += dt;
			}

			// Tick visuals
			if (_isEnabled)
			{
				_agentVisuals?.TickVisuals();
				_oldAgentVisuals?.TickVisuals();
				_mountVisuals?.TickVisuals();
				_oldMountVisuals?.TickVisuals();
			}

			// Ensure camera and tableau view exist
			if (View != null)
			{
				if (_continuousRenderCamera == null)
				{
					_continuousRenderCamera = Camera.CreateCamera();
				}

				View.SetDoNotRenderThisFrame(false);
				View.SetContinuousRendering(true); // Keep this active
			}

			// Refresh visuals if dirty
			if (_isVisualsDirty)
			{
				RefreshCharacterTableau(_oldEquipment);
				_oldEquipment = null;
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
				_oldMountVisuals?.SetVisible(false);
				_mountVisuals?.SetVisible(_bodyProperties != BodyProperties.Default);
				_oldAgentVisuals?.SetVisible(false);
				_agentVisuals?.SetVisible(_bodyProperties != BodyProperties.Default);
			}

			// Update weapon indices
			if (_isEquipmentIndicesDirty)
			{
				_agentVisuals.GetVisuals().SetWieldedWeaponIndices(_rightHandEquipmentIndex, _leftHandEquipmentIndex);
				_isEquipmentIndicesDirty = false;
			}
		}

		/// <summary>
		/// Smoothly interpolate camera from its current frame/FOV
		/// to the given target over the given duration (seconds).
		/// </summary>
		public void AnimateCamera(MatrixFrame targetFrame, float targetFov, float duration)
		{
			_camStartFrame = _camPos;
			_camTargetFrame = targetFrame;
			_fovStart = _verticalFov;
			_fovTarget = targetFov;
			_camAnimDuration = Math.Max(0.001f, duration);
			_camAnimTimer = 0f;
			_isCamAnimating = true;
		}

		public float GetCustomAnimationProgressRatio()
		{
			if (_customAnimation != null && _characterActionSet.IsValid)
			{
				float actionAnimationDuration = MBActionSet.GetActionAnimationDuration(_characterActionSet, _customAnimation);
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
			if (_agentVisuals != null && _customAnimation != null && _customAnimation.Index >= 0)
			{
				ActionIndexValueCache actionAnimationContinueToAction = MBActionSet.GetActionAnimationContinueToAction(_characterActionSet, ActionIndexValueCache.Create(_customAnimation));
				if (actionAnimationContinueToAction.Index >= 0)
				{
					_customAnimationName = actionAnimationContinueToAction.Name;
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

		private void SetEnabled(bool enabled)
		{
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
				_tableauSizeX = (int)((float)width * _customRenderScale * RenderScale);
				_tableauSizeY = (int)((float)height * _customRenderScale * RenderScale);
			}

			_cameraRatio = (float)_tableauSizeX / (float)_tableauSizeY;
			View?.SetEnable(value: false);
			View?.AddClearTask(clearOnlySceneview: true);
			Texture?.ReleaseNextFrame();
			Texture = TableauView.AddTableau("CharacterTableau", CharacterTableauContinuousRenderFunction, _tableauScene, _tableauSizeX, _tableauSizeY);
			Texture.TableauView.SetSceneUsesContour(value: false);
			Texture.TableauView.SetFocusedShadowmap(enable: true, ref _initialSpawnFrame.origin, 2.55f);
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
			_oldAgentVisuals?.ResetNextFrame();
			_oldAgentVisuals = null;
			_oldMountVisuals?.ResetNextFrame();
			_oldMountVisuals = null;
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
					TableauCacheManager.Current.ReturnCachedInventoryTableauScene();
					TableauCacheManager.Current.ReturnCachedInventoryTableauScene();
					view?.AddClearTask(clearOnlySceneview: true);
					_tableauScene = null;
				}
			}

			Texture?.ReleaseNextFrame();
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
					_camPos = _camPosGatheredFromScene;
					_camPos.Elevate(-2f);
					_camPos.Advance(0.5f);
					_agentVisuals?.SetAction(GetIdleAction());
					_oldAgentVisuals?.SetAction(GetIdleAction());
					break;
				case CharacterViewModel.StanceTypes.SideView:
				case CharacterViewModel.StanceTypes.OnMount:
					if (_agentVisuals != null)
					{
						_camPos = _camPosGatheredFromScene;
						if (_equipment[10].Item != null)
						{
							_camPos.Advance(0.5f);
							_agentVisuals.SetAction(_mountVisuals.GetEntity().Skeleton.GetActionAtChannel(0), _mountVisuals.GetEntity().Skeleton.GetAnimationParameterAtChannel(0));
							_oldAgentVisuals.SetAction(_mountVisuals.GetEntity().Skeleton.GetActionAtChannel(0), _mountVisuals.GetEntity().Skeleton.GetAnimationParameterAtChannel(0));
						}
						else
						{
							_camPos.Elevate(-2f);
							_camPos.Advance(0.5f);
							_agentVisuals.SetAction(GetIdleAction());
							_oldAgentVisuals.SetAction(GetIdleAction());
						}
					}

					break;
				case CharacterViewModel.StanceTypes.CelebrateVictory:
					_agentVisuals?.SetAction(act_cheer_1);
					_oldAgentVisuals?.SetAction(act_cheer_1);
					break;
				case CharacterViewModel.StanceTypes.None:
					_agentVisuals?.SetAction(GetIdleAction());
					_oldAgentVisuals?.SetAction(GetIdleAction());
					break;
			}

			if (_agentVisuals != null)
			{
				GameEntity entity = _agentVisuals.GetEntity();
				Skeleton skeleton = entity.Skeleton;
				skeleton.TickAnimations(0.01f, _agentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: true);
				if (!string.IsNullOrEmpty(_idleFaceAnim))
				{
					skeleton.SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
				}

				entity.ManualInvalidate();
				skeleton.ManualInvalidate();
			}

			if (_oldAgentVisuals != null)
			{
				GameEntity entity2 = _oldAgentVisuals.GetEntity();
				Skeleton skeleton2 = entity2.Skeleton;
				skeleton2.TickAnimations(0.01f, _oldAgentVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: true);
				if (!string.IsNullOrEmpty(_idleFaceAnim))
				{
					skeleton2.SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
				}

				entity2.ManualInvalidate();
				skeleton2.ManualInvalidate();
			}

			if (_mountVisuals != null)
			{
				GameEntity entity3 = _mountVisuals.GetEntity();
				Skeleton skeleton3 = entity3.Skeleton;
				skeleton3.TickAnimations(0.01f, _mountVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: true);
				if (!string.IsNullOrEmpty(_idleFaceAnim))
				{
					skeleton3.SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
				}

				entity3.ManualInvalidate();
				skeleton3.ManualInvalidate();
			}

			if (_oldMountVisuals != null)
			{
				GameEntity entity4 = _oldMountVisuals.GetEntity();
				Skeleton skeleton4 = entity4.Skeleton;
				skeleton4.TickAnimations(0.01f, _oldMountVisuals.GetVisuals().GetGlobalFrame(), tickAnimsForChildren: true);
				entity4.ManualInvalidate();
				skeleton4.ManualInvalidate();
			}
		}

		private void ForceRefresh()
		{
			int stanceIndex = (int)_stanceIndex;
			_stanceIndex = CharacterViewModel.StanceTypes.None;
			SetStanceIndex(stanceIndex);
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
				_agentVisuals.SetAction(_customAnimation);
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
			if (_agentVisuals != null && _customAnimation != null)
			{
				if (MBActionSet.GetActionAnimationContinueToAction(_characterActionSet, ActionIndexValueCache.Create(_customAnimation)).Index < 0)
				{
					_agentVisuals.SetAction(GetIdleAction());
				}

				_customAnimation = null;
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
				_oldEquipment = Equipment.CreateFromEquipmentCode(_equipmentCode);
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
			if (string.IsNullOrEmpty(value))
			{
				_banner = null;
			}
			else
			{
				_banner = BannerCode.CreateFrom(value).CalculateBanner();
			}

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
			return _idleAction ?? act_inventory_idle_start;
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
				bool visibilityExcludeParents = _oldAgentVisuals.GetEntity().GetVisibilityExcludeParents();
				AgentVisuals agentVisuals = _agentVisuals;
				_agentVisuals = _oldAgentVisuals;
				_oldAgentVisuals = agentVisuals;
				_agentVisualLoadingCounter = 1;
				AgentVisualsData copyAgentVisualsData = _agentVisuals.GetCopyAgentVisualsData();
				MatrixFrame frame = (_isCharacterMountPlacesSwapped ? _characterMountPositionFrame : _initialSpawnFrame);
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
				if (_initialLoadingCounter == 0)
				{
					_oldAgentVisuals.SetVisible(visibilityExcludeParents);
				}

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

				_agentVisuals.GetEntity().CheckResources(addToQueue: true, checkFaceResources: true);
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
				float num = (float)mouseMoveX * 0.005f;
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
				if (TableauCacheManager.Current.IsCachedInventoryTableauSceneUsed())
				{
					_tableauScene = Scene.CreateNewScene(initialize_physics: true, enable_decals: false);
					_tableauScene.SetName("CharacterTableau");
					_tableauScene.DisableStaticShadows(value: true);
					_tableauScene.SetClothSimulationState(state: true);
					_agentRendererSceneController = MBAgentRendererSceneController.CreateNewAgentRendererSceneController(_tableauScene, 32);
					SceneInitializationData initData = new SceneInitializationData(initializeWithDefaults: true);
					initData.InitPhysicsWorld = false;
					initData.DoNotUseLoadingScreen = true;
					_tableauScene.Read("inventory_character_scene", ref initData);
				}
				else
				{
					_tableauScene = TableauCacheManager.Current.GetCachedInventoryTableauScene();
				}

				_tableauScene.SetShadow(shadowEnabled: true);
				_tableauScene.SetClothSimulationState(state: true);
				_camPos = (_camPosGatheredFromScene = TableauCacheManager.Current.InventorySceneCameraFrame);
				_mountSpawnPoint = _tableauScene.FindEntityWithTag("horse_inv").GetGlobalFrame();
				_bannerSpawnPoint = _tableauScene.FindEntityWithTag("banner_inv").GetGlobalFrame();
				_initialSpawnFrame = _tableauScene.FindEntityWithTag("agent_inv").GetGlobalFrame();
				_characterMountPositionFrame = new MatrixFrame(_initialSpawnFrame.rotation, _mountSpawnPoint.origin);
				_characterMountPositionFrame.Strafe(-0.25f);
				_mountCharacterPositionFrame = new MatrixFrame(_mountSpawnPoint.rotation, _initialSpawnFrame.origin);
				_mountCharacterPositionFrame.Strafe(0.25f);
				if (_agentRendererSceneController != null)
				{
					_tableauScene.RemoveEntity(_tableauScene.FindEntityWithTag("agent_inv"), 99);
					_tableauScene.RemoveEntity(_tableauScene.FindEntityWithTag("horse_inv"), 100);
					_tableauScene.RemoveEntity(_tableauScene.FindEntityWithTag("banner_inv"), 101);
				}
			}

			InitializeAgentVisuals();
			_isVisualsDirty = true;
		}

		private void InitializeAgentVisuals()
		{
			Monster baseMonsterFromRace = TaleWorlds.Core.FaceGen.GetBaseMonsterFromRace(_race);
			_characterActionSet = MBGlobals.GetActionSetWithSuffix(baseMonsterFromRace, _isFemale, "_warrior");
			_oldAgentVisuals = AgentVisuals.Create(new AgentVisualsData().Banner(_banner).Equipment(_equipment).BodyProperties(_bodyProperties)
				.Race(_race)
				.Frame(_initialSpawnFrame)
				.UseMorphAnims(useMorphAnims: true)
				.ActionSet(_characterActionSet)
				.ActionCode(GetIdleAction())
				.Scene(_tableauScene)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(prepareImmediately: false)
				.SkeletonType(_isFemale ? SkeletonType.Female : SkeletonType.Male)
				.ClothColor1(_clothColor1)
				.ClothColor2(_clothColor2)
				.CharacterObjectStringId(_charStringId), "CharacterTableau", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			_oldAgentVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
			_oldAgentVisuals.SetVisible(value: false);
			_agentVisuals = AgentVisuals.Create(new AgentVisualsData().Banner(_banner).Equipment(_equipment).BodyProperties(_bodyProperties)
				.Race(_race)
				.Frame(_initialSpawnFrame)
				.UseMorphAnims(useMorphAnims: true)
				.ActionSet(_characterActionSet)
				.ActionCode(GetIdleAction())
				.Scene(_tableauScene)
				.Monster(baseMonsterFromRace)
				.PrepareImmediately(prepareImmediately: false)
				.SkeletonType(_isFemale ? SkeletonType.Female : SkeletonType.Male)
				.ClothColor1(_clothColor1)
				.ClothColor2(_clothColor2)
				.CharacterObjectStringId(_charStringId), "CharacterTableau", isRandomProgress: false, needBatchedVersionForWeaponMeshes: false, forceUseFaceCache: false);
			_agentVisuals.SetAgentLodZeroOrMaxExternal(makeZero: true);
			_agentVisuals.SetVisible(value: false);
			_initialLoadingCounter = 2;
			if (!string.IsNullOrEmpty(_idleFaceAnim))
			{
				_agentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
				_oldAgentVisuals.GetVisuals().GetSkeleton().SetFacialAnimation(Agent.FacialAnimChannel.Mid, _idleFaceAnim, playSound: false, loop: true);
			}
		}

		private void UpdateMount(bool isRiderAgentMounted = false)
		{
			if (_equipment[EquipmentIndex.ArmorItemEndSlot].Item?.HorseComponent != null)
			{
				ItemObject item = _equipment[EquipmentIndex.ArmorItemEndSlot].Item;
				Monster monster = item.HorseComponent.Monster;
				Equipment equipment = new Equipment
				{
					[EquipmentIndex.ArmorItemEndSlot] = _equipment[EquipmentIndex.ArmorItemEndSlot],
					[EquipmentIndex.HorseHarness] = _equipment[EquipmentIndex.HorseHarness]
				};
				MatrixFrame frame = (_isCharacterMountPlacesSwapped ? _mountCharacterPositionFrame : _mountSpawnPoint);
				if (_isCharacterMountPlacesSwapped)
				{
					frame.rotation.RotateAboutUp(_mainCharacterRotation);
				}

				if (_oldMountVisuals != null)
				{
					_oldMountVisuals.ResetNextFrame();
				}

				_oldMountVisuals = _mountVisuals;
				_mountVisualLoadingCounter = 3;
				ActionIndexCache idleAction = monster.StringId == "camel" ? act_camel_stand : act_horse_stand;
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
			else if (_mountVisuals != null)
			{
				_mountVisuals.Reset();
				_mountVisuals = null;
				_mountVisualLoadingCounter = 0;
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
			if (_banner != null)
			{
				_banner.GetTableauTextureLarge(delegate (Texture t)
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
			Scene scene = (Scene)sender.UserData;
			TableauView tableauView = sender.TableauView;
			if (scene == null)
			{
				tableauView.SetContinuousRendering(value: false);
				tableauView.SetDeleteAfterRendering(value: true);
				return;
			}

			scene.EnsurePostfxSystem();
			scene.SetDofMode(mode: false);
			scene.SetMotionBlurMode(mode: false);
			scene.SetBloom(mode: true);
			scene.SetDynamicShadowmapCascadesRadiusMultiplier(0.31f);
			tableauView.SetRenderWithPostfx(value: true);
			float cameraRatio = _cameraRatio;
			MatrixFrame camPos = _camPos;
			Camera continuousRenderCamera = _continuousRenderCamera;
			if (continuousRenderCamera != null)
			{
				// use custom animated FOV and frame
				_continuousRenderCamera.SetFovVertical(
					_verticalFov,
					_cameraRatio,
					0.2f, 200f
				);
				_continuousRenderCamera.Frame = _camPos;

				tableauView.SetCamera(_continuousRenderCamera);
				tableauView.SetScene(scene);
				tableauView.SetSceneUsesSkybox(value: false);
				tableauView.SetDeleteAfterRendering(value: false);
				tableauView.SetContinuousRendering(true);
				tableauView.SetDoNotRenderThisFrame(value: true);
				tableauView.SetClearColor(0u);
				tableauView.SetFocusedShadowmap(enable: true, ref _initialSpawnFrame.origin, 1.55f);
			}
		}

		public void EnableLight()
		{
			IsLightEnabled = true;

			GameEntity entity = _agentVisuals?.GetEntity();
			if (entity == null) return;

			MatrixFrame frame = entity.GetFrame();
			_light = Light.CreatePointLight(4f);
			_light.Intensity = 0.2f;
			_light.LightColor = new Vec3(180f, 180f, 255f);
			_light.SetShadowType(Light.ShadowType.DynamicShadow);
			_light.ShadowEnabled = false;
			_light.Frame = new MatrixFrame(Mat3.Identity, new Vec3(0f, 0.8f, 0.2f));
			entity.AddLight(_light);
		}

		public void DisableLight()
		{
			IsLightEnabled = false;

			GameEntity entity = _agentVisuals?.GetEntity();
			if (entity == null) return;

			if (_light != null) entity.RemoveComponent(entity.GetLight());
			_light = null;
		}
	}
}
#endif