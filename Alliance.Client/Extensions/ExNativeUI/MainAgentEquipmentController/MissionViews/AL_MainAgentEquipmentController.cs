using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.ExtendedXML.Models;
using NetworkMessages.FromClient;
using System;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;

namespace Alliance.Client.Extensions.ExNativeUI.MainAgentEquipmentController.MissionViews
{
	//replace ViewCreator.CreateMissionMainAgentEquipmentController(mission)
	public class AL_MainAgentEquipmentController : MissionView
	{

		public event Action<bool> OnEquipmentDropInteractionViewToggled;
		public event Action<bool> OnEquipmentEquipInteractionViewToggled;

		private bool _IsRaceConditionRespected;

		private bool IsDisplayingADialog
		{
			get
			{
				IMissionScreen missionScreenAsInterface = _missionScreenAsInterface;
				return missionScreenAsInterface != null && missionScreenAsInterface.GetDisplayDialog();
			}
		}

		private bool EquipHoldHandled
		{
			get
			{
				return _equipHoldHandled;
			}
			set
			{
				_equipHoldHandled = value;
				MissionScreen missionScreen = MissionScreen;
				if (missionScreen == null)
				{
					return;
				}
				missionScreen.SetRadialMenuActiveState(value);
			}
		}

		private bool DropHoldHandled
		{
			get
			{
				return _dropHoldHandled;
			}
			set
			{
				_dropHoldHandled = value;
				MissionScreen missionScreen = MissionScreen;
				if (missionScreen == null)
				{
					return;
				}
				missionScreen.SetRadialMenuActiveState(value);
			}
		}

		public AL_MainAgentEquipmentController()
		{
			_missionScreenAsInterface = MissionScreen;
			EquipHoldHandled = false;
			DropHoldHandled = false;
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			_gauntletLayer = new GauntletLayer(2, "GauntletLayer", false);
			_dataSource = new MissionMainAgentEquipmentControllerVM(new Action<EquipmentIndex>(OnDropEquipment), new Action<SpawnedItemEntity, EquipmentIndex>(OnEquipItem));
			_gauntletLayer.LoadMovie("MainAgentEquipmentController", _dataSource);
			_gauntletLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.Invalid);
			MissionScreen.AddLayer(_gauntletLayer);
			Mission.OnMainAgentChanged += OnMainAgentChanged;
		}

		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			Mission.OnMainAgentChanged -= OnMainAgentChanged;
			MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_dataSource.OnFinalize();
			_dataSource = null;
		}

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (IsMainAgentAvailable() && Mission.IsMainAgentItemInteractionEnabled)
			{
				DropWeaponTick(dt);
				EquipWeaponTick(dt);
				return;
			}
			_prevDropKeyDown = false;
			_prevEquipKeyDown = false;
		}

		public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
		{
			base.OnFocusGained(agent, focusableObject, isInteractable);
			UsableMissionObject usableMissionObject;
			SpawnedItemEntity spawnedItemEntity;
			if ((usableMissionObject = focusableObject as UsableMissionObject) != null && (spawnedItemEntity = usableMissionObject as SpawnedItemEntity) != null)
			{
				_isCurrentFocusedItemInteractable = isInteractable;
				if (!spawnedItemEntity.WeaponCopy.IsEmpty)
				{
					Agent main = Agent.Main;
					// Check if Item have a race restriction and prevent picking up objet if needed (Custom)
					ItemObject objecttampon = spawnedItemEntity.WeaponCopy.Item;
					ExtendedItem itemEx = objecttampon.GetExtendedItem();
					IsRaceConditionRespected(main, itemEx);
					if (!_IsRaceConditionRespected && itemEx != null && itemEx.Race_condition != null && itemEx.Race_condition != "" && Config.Instance.EnableRaceRestrictionOnStuff)
					{
						_isFocusedOnEquipment = false;
					}
					else
					{
						_isFocusedOnEquipment = true;
					}
					// End of custom code, native end always end with his._isFocusedOnEquipment = true
					_focusedWeaponItem = spawnedItemEntity;
					_dataSource.SetCurrentFocusedWeaponEntity(_focusedWeaponItem);
				}
			}
		}

		public override void OnFocusLost(Agent agent, IFocusable focusableObject)
		{
			base.OnFocusLost(agent, focusableObject);
			_isCurrentFocusedItemInteractable = false;
			_isFocusedOnEquipment = false;
			_focusedWeaponItem = null;
			_dataSource.SetCurrentFocusedWeaponEntity(_focusedWeaponItem);
			if (EquipHoldHandled)
			{
				EquipHoldHandled = false;
				_equipHoldTime = 0f;
				_dataSource.OnCancelEquipController();
				Action<bool> onEquipmentEquipInteractionViewToggled = OnEquipmentEquipInteractionViewToggled;
				if (onEquipmentEquipInteractionViewToggled != null)
				{
					onEquipmentEquipInteractionViewToggled(false);
				}
				_equipmentWasInFocusFirstFrameOfEquipDown = false;
			}
		}

		private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Mission.MainAgent == null)
			{
				if (EquipHoldHandled)
				{
					EquipHoldHandled = false;
					Action<bool> onEquipmentEquipInteractionViewToggled = OnEquipmentEquipInteractionViewToggled;
					if (onEquipmentEquipInteractionViewToggled != null)
					{
						onEquipmentEquipInteractionViewToggled(false);
					}
				}
				_equipHoldTime = 0f;
				_dataSource.OnCancelEquipController();
				if (DropHoldHandled)
				{
					Action<bool> onEquipmentDropInteractionViewToggled = OnEquipmentDropInteractionViewToggled;
					if (onEquipmentDropInteractionViewToggled != null)
					{
						onEquipmentDropInteractionViewToggled(false);
					}
					DropHoldHandled = false;
				}
				_dropHoldTime = 0f;
				_dataSource.OnCancelDropController();
			}
		}

		private void EquipWeaponTick(float dt)
		{
			if (MissionScreen.SceneLayer.Input.IsGameKeyDown(13) && !_prevDropKeyDown && !IsDisplayingADialog && IsMainAgentAvailable() && !MissionScreen.Mission.IsOrderMenuOpen)
			{
				if (!_firstFrameOfEquipDownHandled)
				{
					_equipmentWasInFocusFirstFrameOfEquipDown = _isFocusedOnEquipment;
					_firstFrameOfEquipDownHandled = true;
				}
				if (_equipmentWasInFocusFirstFrameOfEquipDown)
				{
					_equipHoldTime += dt;
					if (_equipHoldTime > 0.5f && !EquipHoldHandled && _isFocusedOnEquipment && _isCurrentFocusedItemInteractable)
					{
						HandleOpeningHoldEquip();
						EquipHoldHandled = true;
					}
				}
				_prevEquipKeyDown = true;
				return;
			}
			if (_prevEquipKeyDown && !MissionScreen.SceneLayer.Input.IsGameKeyDown(13))
			{
				if (_equipmentWasInFocusFirstFrameOfEquipDown)
				{
					if (_equipHoldTime < 0.5f)
					{
						if (_focusedWeaponItem != null)
						{
							Agent main = Agent.Main;
							if (main != null && main.CanQuickPickUp(_focusedWeaponItem))
							{
								HandleQuickReleaseEquip();
							}
						}
					}
					else
					{
						HandleClosingHoldEquip();
					}
				}
				if (EquipHoldHandled)
				{
					EquipHoldHandled = false;
				}
				_equipHoldTime = 0f;
				_firstFrameOfEquipDownHandled = false;
				_prevEquipKeyDown = false;
			}
		}

		private void DropWeaponTick(float dt)
		{
			if (MissionScreen.SceneLayer.Input.IsGameKeyDown(22) && !_prevEquipKeyDown && !IsDisplayingADialog && IsMainAgentAvailable() && IsMainAgentHasAtLeastOneItem() && !MissionScreen.Mission.IsOrderMenuOpen)
			{
				_dropHoldTime += dt;
				if (_dropHoldTime > 0.5f && !DropHoldHandled)
				{
					HandleOpeningHoldDrop();
					DropHoldHandled = true;
				}
				_prevDropKeyDown = true;
				return;
			}
			if (_prevDropKeyDown && !MissionScreen.SceneLayer.Input.IsGameKeyDown(22))
			{
				if (_dropHoldTime < 0.5f)
				{
					HandleQuickReleaseDrop();
				}
				else
				{
					HandleClosingHoldDrop();
				}
				DropHoldHandled = false;
				_dropHoldTime = 0f;
				_prevDropKeyDown = false;
			}
		}

		private void HandleOpeningHoldEquip()
		{
			MissionMainAgentEquipmentControllerVM dataSource = _dataSource;
			if (dataSource != null)
			{
				dataSource.OnEquipControllerToggle(true);
			}
			Action<bool> onEquipmentEquipInteractionViewToggled = OnEquipmentEquipInteractionViewToggled;
			if (onEquipmentEquipInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentEquipInteractionViewToggled(true);
		}

		private void HandleClosingHoldEquip()
		{
			MissionMainAgentEquipmentControllerVM dataSource = _dataSource;
			if (dataSource != null)
			{
				dataSource.OnEquipControllerToggle(false);
			}
			Action<bool> onEquipmentEquipInteractionViewToggled = OnEquipmentEquipInteractionViewToggled;
			if (onEquipmentEquipInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentEquipInteractionViewToggled(false);
		}

		private void HandleQuickReleaseEquip()
		{
			OnEquipItem(_focusedWeaponItem, EquipmentIndex.None);
		}

		private void HandleOpeningHoldDrop()
		{
			MissionMainAgentEquipmentControllerVM dataSource = _dataSource;
			if (dataSource != null)
			{
				dataSource.OnDropControllerToggle(true);
			}
			Action<bool> onEquipmentDropInteractionViewToggled = OnEquipmentDropInteractionViewToggled;
			if (onEquipmentDropInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentDropInteractionViewToggled(true);
		}

		private void HandleClosingHoldDrop()
		{
			MissionMainAgentEquipmentControllerVM dataSource = _dataSource;
			if (dataSource != null)
			{
				dataSource.OnDropControllerToggle(false);
			}
			Action<bool> onEquipmentDropInteractionViewToggled = OnEquipmentDropInteractionViewToggled;
			if (onEquipmentDropInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentDropInteractionViewToggled(false);
		}

		private void HandleQuickReleaseDrop()
		{
			OnDropEquipment(EquipmentIndex.None);
		}

		private void OnEquipItem(SpawnedItemEntity itemToEquip, EquipmentIndex indexToEquipItTo)
		{
			if (itemToEquip.GameEntity != null)
			{
				Agent main = Agent.Main;
				if (main == null)
				{
					return;
				}
				main.HandleStartUsingAction(itemToEquip, (int)indexToEquipItTo);
			}
		}

		private void OnDropEquipment(EquipmentIndex indexToDrop)
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new DropWeapon(Input.IsGameKeyDown(10), indexToDrop));
				GameNetwork.EndModuleEventAsClient();
				return;
			}
			Agent.Main.HandleDropWeapon(Input.IsGameKeyDown(10), indexToDrop);
		}

		private bool IsMainAgentAvailable()
		{
			Agent main = Agent.Main;
			return main != null && main.IsActive();
		}

		private bool IsMainAgentHasAtLeastOneItem()
		{
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!Agent.Main.Equipment[equipmentIndex].IsEmpty)
				{
					return true;
				}
			}
			return false;
		}

		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}

		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}

		// Custom new fonction
		private void IsRaceConditionRespected(Agent main, ExtendedItem itemEx)
		{
			if (itemEx == null || itemEx.Race_condition == "")
			{
				_IsRaceConditionRespected = false;
				return;
			}
			string[] _listofrace = itemEx.Race_condition.Split(',');

			foreach (string race in _listofrace)
			{
				if (main.Character.Race == TaleWorlds.Core.FaceGen.GetRaceOrDefault(race))
				{
					_IsRaceConditionRespected = true;
					return;
				}
			}
			_IsRaceConditionRespected = false;
		}

		private const float _minHoldTime = 0.5f;

		private readonly IMissionScreen _missionScreenAsInterface;

		private bool _equipmentWasInFocusFirstFrameOfEquipDown;

		private bool _firstFrameOfEquipDownHandled;

		private bool _equipHoldHandled;

		private bool _isFocusedOnEquipment;

		private float _equipHoldTime;

		private bool _prevEquipKeyDown;

		private SpawnedItemEntity _focusedWeaponItem;

		private bool _dropHoldHandled;

		private float _dropHoldTime;

		private bool _prevDropKeyDown;

		private bool _isCurrentFocusedItemInteractable;

		private GauntletLayer _gauntletLayer;

		private MissionMainAgentEquipmentControllerVM _dataSource;
	}
}
