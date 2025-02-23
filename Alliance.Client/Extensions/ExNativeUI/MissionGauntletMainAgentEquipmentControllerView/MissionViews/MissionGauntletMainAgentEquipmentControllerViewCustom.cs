using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.Configuration.Models;

namespace Alliance.Client.Extensions.ExNativeUI.MissionMainAgentEquipmentController.MissionViews
{
	//replace ViewCreator.CreateMissionMainAgentEquipmentController(mission)
	public class MissionGauntletMainAgentEquipmentControllerViewCustom : MissionView
	{

		public event Action<bool> OnEquipmentDropInteractionViewToggled;
		public event Action<bool> OnEquipmentEquipInteractionViewToggled;

		private bool _IsRaceConditionRespected;

		private bool IsDisplayingADialog
		{
			get
			{
				IMissionScreen missionScreenAsInterface = this._missionScreenAsInterface;
				return missionScreenAsInterface != null && missionScreenAsInterface.GetDisplayDialog();
			}
		}

		private bool EquipHoldHandled
		{
			get
			{
				return this._equipHoldHandled;
			}
			set
			{
				this._equipHoldHandled = value;
				MissionScreen missionScreen = base.MissionScreen;
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
				return this._dropHoldHandled;
			}
			set
			{
				this._dropHoldHandled = value;
				MissionScreen missionScreen = base.MissionScreen;
				if (missionScreen == null)
				{
					return;
				}
				missionScreen.SetRadialMenuActiveState(value);
			}
		}

		public MissionGauntletMainAgentEquipmentControllerViewCustom()
		{
			this._missionScreenAsInterface = base.MissionScreen;
			this.EquipHoldHandled = false;
			this.DropHoldHandled = false;
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._gauntletLayer = new GauntletLayer(2, "GauntletLayer", false);
			this._dataSource = new MissionMainAgentEquipmentControllerVM(new Action<EquipmentIndex>(this.OnDropEquipment), new Action<SpawnedItemEntity, EquipmentIndex>(this.OnEquipItem));
			this._gauntletLayer.LoadMovie("MainAgentEquipmentController", this._dataSource);
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.Invalid);
			base.MissionScreen.AddLayer(this._gauntletLayer);
			base.Mission.OnMainAgentChanged += this.OnMainAgentChanged;
		}

		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			base.Mission.OnMainAgentChanged -= this.OnMainAgentChanged;
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			this._gauntletLayer = null;
			this._dataSource.OnFinalize();
			this._dataSource = null;
		}

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			if (this.IsMainAgentAvailable() && base.Mission.IsMainAgentItemInteractionEnabled)
			{
				this.DropWeaponTick(dt);
				this.EquipWeaponTick(dt);
				return;
			}
			this._prevDropKeyDown = false;
			this._prevEquipKeyDown = false;
		}

		public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
		{
			base.OnFocusGained(agent, focusableObject, isInteractable);
			UsableMissionObject usableMissionObject;
			SpawnedItemEntity spawnedItemEntity;
			if ((usableMissionObject = (focusableObject as UsableMissionObject)) != null && (spawnedItemEntity = (usableMissionObject as SpawnedItemEntity)) != null)
			{
				this._isCurrentFocusedItemInteractable = isInteractable;
				if (!spawnedItemEntity.WeaponCopy.IsEmpty)
				{
					Agent main = Agent.Main;
					// Check if Item have a race restriction and prevent picking up objet if needed (Custom)
					ItemObject objecttampon = spawnedItemEntity.WeaponCopy.Item;
					ExtendedItem itemEx = objecttampon.GetExtendedItem();
					IsRaceConditionRespected(main, itemEx);
					if (!_IsRaceConditionRespected && itemEx != null && itemEx.Race_condition != null && itemEx.Race_condition != "" && Config.Instance.EnableRaceRestrictionOnStuff)
					{
						this._isFocusedOnEquipment = false;
					}
					else
					{
						this._isFocusedOnEquipment = true;
					}
					// End of custom code, native end always end with his._isFocusedOnEquipment = true
					this._focusedWeaponItem = spawnedItemEntity;
					this._dataSource.SetCurrentFocusedWeaponEntity(this._focusedWeaponItem);
				}
			}
		}

		public override void OnFocusLost(Agent agent, IFocusable focusableObject)
		{
			base.OnFocusLost(agent, focusableObject);
			this._isCurrentFocusedItemInteractable = false;
			this._isFocusedOnEquipment = false;
			this._focusedWeaponItem = null;
			this._dataSource.SetCurrentFocusedWeaponEntity(this._focusedWeaponItem);
			if (this.EquipHoldHandled)
			{
				this.EquipHoldHandled = false;
				this._equipHoldTime = 0f;
				this._dataSource.OnCancelEquipController();
				Action<bool> onEquipmentEquipInteractionViewToggled = this.OnEquipmentEquipInteractionViewToggled;
				if (onEquipmentEquipInteractionViewToggled != null)
				{
					onEquipmentEquipInteractionViewToggled(false);
				}
				this._equipmentWasInFocusFirstFrameOfEquipDown = false;
			}
		}

		private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
		{
			if (base.Mission.MainAgent == null)
			{
				if (this.EquipHoldHandled)
				{
					this.EquipHoldHandled = false;
					Action<bool> onEquipmentEquipInteractionViewToggled = this.OnEquipmentEquipInteractionViewToggled;
					if (onEquipmentEquipInteractionViewToggled != null)
					{
						onEquipmentEquipInteractionViewToggled(false);
					}
				}
				this._equipHoldTime = 0f;
				this._dataSource.OnCancelEquipController();
				if (this.DropHoldHandled)
				{
					Action<bool> onEquipmentDropInteractionViewToggled = this.OnEquipmentDropInteractionViewToggled;
					if (onEquipmentDropInteractionViewToggled != null)
					{
						onEquipmentDropInteractionViewToggled(false);
					}
					this.DropHoldHandled = false;
				}
				this._dropHoldTime = 0f;
				this._dataSource.OnCancelDropController();
			}
		}

		private void EquipWeaponTick(float dt)
		{
			if (base.MissionScreen.SceneLayer.Input.IsGameKeyDown(13) && !this._prevDropKeyDown && !this.IsDisplayingADialog && this.IsMainAgentAvailable() && !base.MissionScreen.Mission.IsOrderMenuOpen)
			{
				if (!this._firstFrameOfEquipDownHandled)
				{
					this._equipmentWasInFocusFirstFrameOfEquipDown = this._isFocusedOnEquipment;
					this._firstFrameOfEquipDownHandled = true;
				}
				if (this._equipmentWasInFocusFirstFrameOfEquipDown)
				{
					this._equipHoldTime += dt;
					if (this._equipHoldTime > 0.5f && !this.EquipHoldHandled && this._isFocusedOnEquipment && this._isCurrentFocusedItemInteractable)
					{
						this.HandleOpeningHoldEquip();
						this.EquipHoldHandled = true;
					}
				}
				this._prevEquipKeyDown = true;
				return;
			}
			if (this._prevEquipKeyDown && !base.MissionScreen.SceneLayer.Input.IsGameKeyDown(13))
			{
				if (this._equipmentWasInFocusFirstFrameOfEquipDown)
				{
					if (this._equipHoldTime < 0.5f)
					{
						if (this._focusedWeaponItem != null)
						{
							Agent main = Agent.Main;
							if (main != null && main.CanQuickPickUp(this._focusedWeaponItem))
							{
								this.HandleQuickReleaseEquip();
							}
						}
					}
					else
					{
						this.HandleClosingHoldEquip();
					}
				}
				if (this.EquipHoldHandled)
				{
					this.EquipHoldHandled = false;
				}
				this._equipHoldTime = 0f;
				this._firstFrameOfEquipDownHandled = false;
				this._prevEquipKeyDown = false;
			}
		}

		private void DropWeaponTick(float dt)
		{
			if (base.MissionScreen.SceneLayer.Input.IsGameKeyDown(22) && !this._prevEquipKeyDown && !this.IsDisplayingADialog && this.IsMainAgentAvailable() && this.IsMainAgentHasAtLeastOneItem() && !base.MissionScreen.Mission.IsOrderMenuOpen)
			{
				this._dropHoldTime += dt;
				if (this._dropHoldTime > 0.5f && !this.DropHoldHandled)
				{
					this.HandleOpeningHoldDrop();
					this.DropHoldHandled = true;
				}
				this._prevDropKeyDown = true;
				return;
			}
			if (this._prevDropKeyDown && !base.MissionScreen.SceneLayer.Input.IsGameKeyDown(22))
			{
				if (this._dropHoldTime < 0.5f)
				{
					this.HandleQuickReleaseDrop();
				}
				else
				{
					this.HandleClosingHoldDrop();
				}
				this.DropHoldHandled = false;
				this._dropHoldTime = 0f;
				this._prevDropKeyDown = false;
			}
		}

		private void HandleOpeningHoldEquip()
		{
			MissionMainAgentEquipmentControllerVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnEquipControllerToggle(true);
			}
			Action<bool> onEquipmentEquipInteractionViewToggled = this.OnEquipmentEquipInteractionViewToggled;
			if (onEquipmentEquipInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentEquipInteractionViewToggled(true);
		}

		private void HandleClosingHoldEquip()
		{
			MissionMainAgentEquipmentControllerVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnEquipControllerToggle(false);
			}
			Action<bool> onEquipmentEquipInteractionViewToggled = this.OnEquipmentEquipInteractionViewToggled;
			if (onEquipmentEquipInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentEquipInteractionViewToggled(false);
		}

		private void HandleQuickReleaseEquip()
		{
			this.OnEquipItem(this._focusedWeaponItem, EquipmentIndex.None);
		}

		private void HandleOpeningHoldDrop()
		{
			MissionMainAgentEquipmentControllerVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnDropControllerToggle(true);
			}
			Action<bool> onEquipmentDropInteractionViewToggled = this.OnEquipmentDropInteractionViewToggled;
			if (onEquipmentDropInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentDropInteractionViewToggled(true);
		}

		private void HandleClosingHoldDrop()
		{
			MissionMainAgentEquipmentControllerVM dataSource = this._dataSource;
			if (dataSource != null)
			{
				dataSource.OnDropControllerToggle(false);
			}
			Action<bool> onEquipmentDropInteractionViewToggled = this.OnEquipmentDropInteractionViewToggled;
			if (onEquipmentDropInteractionViewToggled == null)
			{
				return;
			}
			onEquipmentDropInteractionViewToggled(false);
		}

		private void HandleQuickReleaseDrop()
		{
			this.OnDropEquipment(EquipmentIndex.None);
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
				GameNetwork.WriteMessage(new DropWeapon(base.Input.IsGameKeyDown(10), indexToDrop));
				GameNetwork.EndModuleEventAsClient();
				return;
			}
			Agent.Main.HandleDropWeapon(base.Input.IsGameKeyDown(10), indexToDrop);
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
			this._gauntletLayer.UIContext.ContextAlpha = 0f;
		}

		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			this._gauntletLayer.UIContext.ContextAlpha = 1f;
		}

		// Custom new fonction
		private void IsRaceConditionRespected(Agent main, ExtendedItem itemEx)
		{
			if (itemEx == null || itemEx.Race_condition == "" )
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
