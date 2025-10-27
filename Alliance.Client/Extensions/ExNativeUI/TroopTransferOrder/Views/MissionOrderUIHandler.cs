using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.Views
{
	[OverrideView(typeof(MultiplayerMissionOrderUIHandler))]
	public class MissionOrderUIHandler : GauntletOrderUIHandler
	{
		public override bool IsDeployment
		{
			get
			{
				return false;
			}
		}

		public override bool IsSiegeDeployment
		{
			get
			{
				return false;
			}
		}

		public override bool IsValidForTick
		{
			get
			{
				return this._shouldTick && (!base.MissionScreen.IsRadialMenuActive || this._dataSource.IsToggleOrderShown) && !GameStateManager.Current.ActiveStateDisabledByUser;
			}
		}

		public MissionOrderUIHandler()
		{
			this.ViewOrderPriority = 19;
		}

		public override bool IsReady()
		{
			return true;
		}

		public override void AfterStart()
		{
			base.AfterStart();
			int num;
			MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsPerFormation, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out num);
			this._shouldTick = num > 0;
		}

		public override void OnMissionScreenTick(float dt)
		{
			if (this.IsValidForTick)
			{
				if (!this._isInitialized)
				{
					Team team = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
					if (team != null && (team == base.Mission.AttackerTeam || team == base.Mission.DefenderTeam))
					{
						this.InitializeInADisgustingManner();
					}
				}
				if (!this._isValid)
				{
					Team team2 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
					if (team2 != null && (team2 == base.Mission.AttackerTeam || team2 == base.Mission.DefenderTeam))
					{
						this.ValidateInADisgustingManner();
					}
					return;
				}
				if (this._shouldInitializeFormationInfo)
				{
					Team team3 = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null);
					if (this._dataSource != null && team3 != null)
					{
						this._dataSource.AfterInitialize();
						this._shouldInitializeFormationInfo = false;
					}
				}
			}
			base.OnMissionScreenTick(dt);
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
			this._siegeDeploymentHandler = null;
			ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(this.OnManagedOptionChanged));
			MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
			this._roundComponent = ((missionBehavior != null) ? missionBehavior.RoundComponent : null);
			if (this._roundComponent != null)
			{
				this._roundComponent.OnRoundStarted += this.OnRoundStarted;
				this._roundComponent.OnPreparationEnded += this.OnPreparationEnded;
			}
		}

		private void OnRoundStarted()
		{
			MissionOrderVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.AfterInitialize();
		}

		private void OnPreparationEnded()
		{
			this._shouldInitializeFormationInfo = true;
		}

		private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
		{
			if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.OrderLayoutType)
			{
				if (this._gauntletLayer != null && this._movie != null)
				{
					this._gauntletLayer.ReleaseMovie(this._movie);
					string text = ((BannerlordConfig.OrderType == 0) ? this._barOrderMovieName : this._radialOrderMovieName);
					this._movie = this._gauntletLayer.LoadMovie(text, this._dataSource);
					return;
				}
			}
			else if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.AutoTrackAttackedSettlements)
			{
				MissionOrderVM dataSource = this._dataSource;
				if (dataSource == null)
				{
					return;
				}
				dataSource.OnOrderLayoutTypeChanged();
			}
		}

		public override void OnMissionScreenFinalize()
		{
			this.Clear();
			this._orderTroopPlacer = null;
			MissionPeer.OnTeamChanged -= this.TeamChange;
			ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(this.OnManagedOptionChanged));
			if (this._roundComponent != null)
			{
				this._roundComponent.OnRoundStarted -= this.OnRoundStarted;
				this._roundComponent.OnPreparationEnded -= this.OnPreparationEnded;
			}
			base.OnMissionScreenFinalize();
		}

		protected override void OnTransferFinished()
		{
		}

		protected override void SetLayerEnabled(bool isEnabled)
		{
			if (isEnabled)
			{
				if (this._dataSource == null || this._dataSource.ActiveTargetState == 0)
				{
					this._orderTroopPlacer.SuspendTroopPlacer = false;
				}
				base.MissionScreen.SetOrderFlagVisibility(true);
				Game.Current.EventManager.TriggerEvent<MissionPlayerToggledOrderViewEvent>(new MissionPlayerToggledOrderViewEvent(true));
				return;
			}
			this._orderTroopPlacer.SuspendTroopPlacer = true;
			base.MissionScreen.SetOrderFlagVisibility(false);
			base.MissionScreen.UnregisterRadialMenuObject(this);
			Game.Current.EventManager.TriggerEvent<MissionPlayerToggledOrderViewEvent>(new MissionPlayerToggledOrderViewEvent(false));
		}

		public void InitializeInADisgustingManner()
		{
			base.AfterStart();
			this._orderTroopPlacer = base.Mission.GetMissionBehavior<OrderTroopPlacer>();
			base.MissionScreen.OrderFlag = this._orderTroopPlacer.OrderFlag;
			base.MissionScreen.SetOrderFlagVisibility(false);
			MissionPeer.OnTeamChanged += this.TeamChange;
			this._isInitialized = true;
		}

		public void ValidateInADisgustingManner()
		{
			this._dataSource = new MissionOrderVM(false, true);
			this._dataSource.SetDeploymentParemeters(base.MissionScreen.CombatCamera, this.IsSiegeDeployment ? Enumerable.ToList<DeploymentPoint>(this._siegeDeploymentHandler.PlayerDeploymentPoints) : new List<DeploymentPoint>());
			this._dataSource.SetCallbacks(new MissionOrderCallbacks
			{
				ToggleMissionInputs = new Action<bool>(base.ToggleScreenRotation),
				RefreshVisuals = new MissionOrderCallbacks.OnRefreshVisualsDelegate(this.RefreshVisuals),
				GetVisualOrderExecutionParameters = new MissionOrderCallbacks.GetOrderExecutionParametersDelegate(base.GetVisualOrderExecutionParameters),
				SetSuspendTroopPlacer = new MissionOrderCallbacks.ToggleOrderPositionVisibilityDelegate(this.SetSuspendTroopPlacer),
				OnActivateToggleOrder = new MissionOrderCallbacks.OnToggleActivateOrderStateDelegate(base.OnActivateToggleOrder),
				OnDeactivateToggleOrder = new MissionOrderCallbacks.OnToggleActivateOrderStateDelegate(base.OnDeactivateToggleOrder),
				OnTransferTroopsFinished = new MissionOrderCallbacks.OnTransferTroopsFinishedDelegate(this.OnTransferFinished),
				OnBeforeOrder = new MissionOrderCallbacks.OnBeforeOrderDelegate(base.OnBeforeOrder)
			});
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("ToggleEscapeMenu"));
			this._dataSource.TroopController.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.TroopController.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._dataSource.TroopController.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
			GameKeyContext category = HotKeyManager.GetCategory("MissionOrderHotkeyCategory");
			this._dataSource.SetOrderIndexKey(0, category.GetGameKey(69));
			this._dataSource.SetOrderIndexKey(1, category.GetGameKey(70));
			this._dataSource.SetOrderIndexKey(2, category.GetGameKey(71));
			this._dataSource.SetOrderIndexKey(3, category.GetGameKey(72));
			this._dataSource.SetOrderIndexKey(4, category.GetGameKey(73));
			this._dataSource.SetOrderIndexKey(5, category.GetGameKey(74));
			this._dataSource.SetOrderIndexKey(6, category.GetGameKey(75));
			this._dataSource.SetOrderIndexKey(7, category.GetGameKey(76));
			this._dataSource.SetOrderIndexKey(8, category.GetGameKey(77));
			this._dataSource.SetReturnKey(category.GetGameKey(77));
			this._gauntletLayer = new GauntletLayer(this.ViewOrderPriority, "GauntletLayer", false);
			this._spriteCategory = UIResourceManager.LoadSpriteCategory("ui_order");
			string text = ((BannerlordConfig.OrderType == 0) ? this._barOrderMovieName : this._radialOrderMovieName);
			this._movie = this._gauntletLayer.LoadMovie(text, this._dataSource);
			this._dataSource.InputRestrictions = this._gauntletLayer.InputRestrictions;
			base.MissionScreen.AddLayer(this._gauntletLayer);
			this._dataSource.AfterInitialize();
			this._isValid = true;
		}

		private void RefreshVisuals()
		{
		}

		private void Clear()
		{
			if (this._gauntletLayer != null)
			{
				base.MissionScreen.RemoveLayer(this._gauntletLayer);
			}
			if (this._dataSource != null)
			{
				this._dataSource.OnFinalize();
			}
			this._gauntletLayer = null;
			this._dataSource = null;
			this._movie = null;
			if (this._isValid)
			{
				this._spriteCategory.Unload();
			}
		}

		private void TeamChange(NetworkCommunicator peer, Team previousTeam, Team newTeam)
		{
			if (peer.IsMine)
			{
				this.Clear();
				this._isValid = false;
			}
		}

		private IRoundComponent _roundComponent;

		private bool _isValid;

		private bool _shouldTick;

		private bool _shouldInitializeFormationInfo;
	}
}
