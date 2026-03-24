using Alliance.Client.Extensions.ExNativeUI.MainAgentEquipmentController.MissionViews;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views
{
	[OverrideView(typeof(MissionAgentStatusUIHandler))]
	public class AgentStatusView : MissionAgentStatusUIHandler
	{
		public MissionAgentStatusVM DataSource
		{
			get
			{
				return _dataSource;
			}
		}

		public AgentStatusView()
		{
		}

		public override void OnMissionStateActivated()
		{
			base.OnMissionStateActivated();
			_dataSource?.OnMainAgentWeaponChange();
		}

		public override void EarlyStart()
		{
			base.EarlyStart();
			_dataSource = new MissionAgentStatusVM(Mission, MissionScreen.CombatCamera, new Func<float>(MissionScreen.GetCameraToggleProgress));
			_gauntletLayer = new GauntletLayer("MainAgentHUD", ViewOrderPriority);
			_gauntletLayer.LoadMovie("MainAgentHUD", _dataSource);
			MissionScreen.AddLayer(_gauntletLayer);
			_dataSource.TakenDamageController.SetIsEnabled(BannerlordConfig.EnableDamageTakenVisuals);
			RegisterInteractionEvents();
			CombatLogManager.OnGenerateCombatLog += OnGenerateCombatLog;
			ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		}

		protected override void OnCreateView()
		{
			_dataSource.IsAgentStatusAvailable = true;
		}

		protected override void OnDestroyView()
		{
			_dataSource.IsAgentStatusAvailable = false;
		}

		protected override void OnSuspendView()
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, true);
		}

		protected override void OnResumeView()
		{
			ScreenManager.SetSuspendLayer(_gauntletLayer, false);
		}

		private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
		{
			if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableDamageTakenVisuals)
			{
				_dataSource?.TakenDamageController.SetIsEnabled(BannerlordConfig.EnableDamageTakenVisuals);
			}
		}

		public override void AfterStart()
		{
			base.AfterStart();
			_dataSource?.InitializeMainAgentPropterties();
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			_isInDeployment = Mission.Mode == MissionMode.Deployment;
		}

		public override void OnDeploymentFinished()
		{
			base.OnDeploymentFinished();
			_isInDeployment = false;
		}

		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			UnregisterInteractionEvents();
			ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
			CombatLogManager.OnGenerateCombatLog -= OnGenerateCombatLog;
			MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_dataSource?.OnFinalize();
			_dataSource = null;
			_missionMainAgentController = null;
		}

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			_dataSource.IsInDeployement = _isInDeployment;
			_dataSource.Tick(dt);
			_dataSource.InteractionInterface.DisplayInteractionText = !MissionScreen.IsRadialMenuActive && !Mission.IsOrderMenuOpen;
		}

		public override void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
		{
			base.OnFocusGained(mainAgent, focusableObject, isInteractable);
			_dataSource?.OnFocusGained(mainAgent, focusableObject, isInteractable);
		}

		public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
		{
			base.OnAgentInteraction(userAgent, agent, agentBoneIndex);
			_dataSource?.OnAgentInteraction(userAgent, agent, agentBoneIndex);
		}

		public override void OnFocusLost(Agent agent, IFocusable focusableObject)
		{
			base.OnFocusLost(agent, focusableObject);
			_dataSource?.OnFocusLost(agent, focusableObject);
		}

		public override void OnAgentDeleted(Agent affectedAgent)
		{
			_dataSource?.OnAgentDeleted(affectedAgent);
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			_dataSource?.OnAgentRemoved(affectedAgent);
		}

		private void OnGenerateCombatLog(CombatLogData logData)
		{
			if (logData.IsVictimAgentMine && logData.TotalDamage > 0 && logData.BodyPartHit != BoneBodyPartType.None)
			{
				_dataSource?.OnMainAgentHit(logData.TotalDamage, logData.IsRangedAttack ? 1 : 0);
			}
		}

		private void RegisterInteractionEvents()
		{
			_missionMainAgentController = Mission.GetMissionBehavior<MissionMainAgentController>();
			if (_missionMainAgentController != null)
			{
				_missionMainAgentController.InteractionComponent.OnFocusGained += _dataSource.OnSecondaryFocusGained;
				_missionMainAgentController.InteractionComponent.OnFocusLost += _dataSource.OnSecondaryFocusLost;
				_missionMainAgentController.InteractionComponent.OnFocusHealthChanged += _dataSource.InteractionInterface.OnFocusedHealthChanged;
			}
			_missionMainAgentEquipmentControllerView = Mission.GetMissionBehavior<AL_MainAgentEquipmentController>();
			if (_missionMainAgentEquipmentControllerView != null)
			{
				_missionMainAgentEquipmentControllerView.OnEquipmentDropInteractionViewToggled += _dataSource.OnEquipmentInteractionViewToggled;
				_missionMainAgentEquipmentControllerView.OnEquipmentEquipInteractionViewToggled += _dataSource.OnEquipmentInteractionViewToggled;
			}
		}

		private void UnregisterInteractionEvents()
		{
			if (_missionMainAgentController != null)
			{
				_missionMainAgentController.InteractionComponent.OnFocusGained -= _dataSource.OnSecondaryFocusGained;
				_missionMainAgentController.InteractionComponent.OnFocusLost -= _dataSource.OnSecondaryFocusLost;
				_missionMainAgentController.InteractionComponent.OnFocusHealthChanged -= _dataSource.InteractionInterface.OnFocusedHealthChanged;
			}
			if (_missionMainAgentEquipmentControllerView != null)
			{
				_missionMainAgentEquipmentControllerView.OnEquipmentDropInteractionViewToggled -= _dataSource.OnEquipmentInteractionViewToggled;
				_missionMainAgentEquipmentControllerView.OnEquipmentEquipInteractionViewToggled -= _dataSource.OnEquipmentInteractionViewToggled;
			}
		}

		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			if (_gauntletLayer != null)
			{
				_gauntletLayer.UIContext.ContextAlpha = 0f;
			}
			UnregisterInteractionEvents();
		}

		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			if (_gauntletLayer != null)
			{
				_gauntletLayer.UIContext.ContextAlpha = 1f;
			}
			RegisterInteractionEvents();
		}

		private GauntletLayer _gauntletLayer;

		private MissionAgentStatusVM _dataSource;

		private MissionMainAgentController _missionMainAgentController;

		private AL_MainAgentEquipmentController _missionMainAgentEquipmentControllerView;

		private DeploymentMissionView _deploymentMissionView;

		protected bool _isInDeployment;
	}
}
