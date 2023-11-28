using Alliance.Client.Extensions.ExNativeUI.AgentStatus.ViewModels;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;

namespace Alliance.Client.Extensions.ExNativeUI.AgentStatus.Views
{
    [OverrideView(typeof(MissionAgentStatusUIHandler))]
    public class AgentStatusView : MissionGauntletBattleUIBase
    {
        public AgentStatusView()
        {
        }

        public override void OnMissionScreenActivate()
        {
            base.OnMissionScreenActivate();
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnMainAgentWeaponChange();
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            _dataSource = new AgentStatusVM(Mission, MissionScreen.CombatCamera, new Func<float>(MissionScreen.GetCameraToggleProgress));
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
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

        private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.EnableDamageTakenVisuals)
            {
                AgentStatusVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.TakenDamageController.SetIsEnabled(BannerlordConfig.EnableDamageTakenVisuals);
            }
        }

        public override void AfterStart()
        {
            base.AfterStart();
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.InitializeMainAgentPropterties();
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _isInDeployement = Mission.GetMissionBehavior<BattleDeploymentHandler>() != null;
            if (_isInDeployement)
            {
                _deploymentMissionView = Mission.GetMissionBehavior<DeploymentMissionView>();
                if (_deploymentMissionView != null)
                {
                    DeploymentMissionView deploymentMissionView = _deploymentMissionView;
                    deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Combine(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(OnDeploymentFinish));
                }
            }
        }

        private void OnDeploymentFinish()
        {
            _isInDeployement = false;
            DeploymentMissionView deploymentMissionView = _deploymentMissionView;
            deploymentMissionView.OnDeploymentFinish = (OnPlayerDeploymentFinishDelegate)Delegate.Remove(deploymentMissionView.OnDeploymentFinish, new OnPlayerDeploymentFinishDelegate(OnDeploymentFinish));
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            UnregisterInteractionEvents();
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
            CombatLogManager.OnGenerateCombatLog -= OnGenerateCombatLog;
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            AgentStatusVM dataSource = _dataSource;
            if (dataSource != null)
            {
                dataSource.OnFinalize();
            }
            _dataSource = null;
            _missionMainAgentController = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            _dataSource.IsInDeployement = _isInDeployement;
            _dataSource.Tick(dt);
        }

        public override void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
        {
            base.OnFocusGained(mainAgent, focusableObject, isInteractable);
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnFocusGained(mainAgent, focusableObject, isInteractable);
        }

        public override void OnAgentInteraction(Agent userAgent, Agent agent)
        {
            base.OnAgentInteraction(userAgent, agent);
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnAgentInteraction(userAgent, agent);
        }

        public override void OnFocusLost(Agent agent, IFocusable focusableObject)
        {
            base.OnFocusLost(agent, focusableObject);
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnFocusLost(agent, focusableObject);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnAgentDeleted(affectedAgent);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            AgentStatusVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnAgentRemoved(affectedAgent);
        }

        private void OnGenerateCombatLog(CombatLogData logData)
        {
            if (logData.IsVictimAgentMine && logData.TotalDamage > 0 && logData.BodyPartHit != BoneBodyPartType.None)
            {
                AgentStatusVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.OnMainAgentHit(logData.TotalDamage, logData.IsRangedAttack ? 1 : 0);
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
            _missionMainAgentEquipmentControllerView = Mission.GetMissionBehavior<MissionGauntletMainAgentEquipmentControllerView>();
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
            _gauntletLayer.UIContext.ContextAlpha = 0f;
        }

        public override void OnPhotoModeDeactivated()
        {
            base.OnPhotoModeDeactivated();
            _gauntletLayer.UIContext.ContextAlpha = 1f;
        }

        private GauntletLayer _gauntletLayer;

        private AgentStatusVM _dataSource;

        private MissionMainAgentController _missionMainAgentController;

        private MissionGauntletMainAgentEquipmentControllerView _missionMainAgentEquipmentControllerView;

        private DeploymentMissionView _deploymentMissionView;

        private bool _isInDeployement;
    }
}
