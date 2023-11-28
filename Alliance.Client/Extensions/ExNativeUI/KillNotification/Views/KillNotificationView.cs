using Alliance.Common.Core.Configuration.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.KillFeed;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.ExNativeUI.KillNotification.Views
{
    [OverrideView(typeof(MissionMultiplayerKillNotificationUIHandler))]
    public class KillNotificationView : MissionView
    {
        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            ViewOrderPriority = 2;
            _isGeneralFeedEnabled = _doesGameModeAllowGeneralFeed && BannerlordConfig.ReportCasualtiesType < 2;
            _isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
            _dataSource = new MPKillFeedVM();
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("MultiplayerKillFeed", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
            CombatLogManager.OnGenerateCombatLog += OnCombatLogManagerOnPrintCombatLog;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnOptionChange));
        }

        private void OnOptionChange(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.ReportCasualtiesType)
            {
                _isGeneralFeedEnabled = _doesGameModeAllowGeneralFeed && BannerlordConfig.ReportCasualtiesType < 2;
                return;
            }
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.ReportPersonalDamage)
            {
                _isPersonalFeedEnabled = BannerlordConfig.ReportPersonalDamage;
            }
        }

        public override void AfterStart()
        {
            base.AfterStart();
            //_tdmClient = Mission.GetMissionBehavior<MissionMultiplayerTeamDeathmatchClient>();
            //if (_tdmClient != null)
            //{
            //    _tdmClient.OnGoldGainEvent += OnGoldGain;
            //}
            //_siegeClient = Mission.GetMissionBehavior<MissionMultiplayerSiegeClient>();
            //if (_siegeClient != null)
            //{
            //    _siegeClient.OnGoldGainEvent += OnGoldGain;
            //}
            //_flagDominationClient = Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
            //if (_flagDominationClient != null)
            //{
            //    _flagDominationClient.OnGoldGainEvent += OnGoldGain;
            //}
            _duelClient = Mission.GetMissionBehavior<MissionMultiplayerGameModeDuelClient>();
            if (_duelClient != null)
            {
                _doesGameModeAllowGeneralFeed = false;
            }
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            CombatLogManager.OnGenerateCombatLog -= OnCombatLogManagerOnPrintCombatLog;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnOptionChange));
            //if (_tdmClient != null)
            //{
            //    _tdmClient.OnGoldGainEvent -= OnGoldGain;
            //}
            //if (_siegeClient != null)
            //{
            //    _siegeClient.OnGoldGainEvent -= OnGoldGain;
            //}
            //if (_flagDominationClient != null)
            //{
            //    _flagDominationClient.OnGoldGainEvent -= OnGoldGain;
            //}
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _dataSource.OnFinalize();
            _dataSource = null;
        }

        //private void OnGoldGain(GoldGain goldGainMessage)
        //{
        //    if (_isPersonalFeedEnabled)
        //    {
        //        foreach (KeyValuePair<ushort, int> keyValuePair in goldGainMessage.GoldChangeEventList)
        //        {
        //            _dataSource.PersonalCasualty.OnGoldChange(keyValuePair.Value, (GoldGainFlags)keyValuePair.Key);
        //        }
        //    }
        //}

        private void OnCombatLogManagerOnPrintCombatLog(CombatLogData logData)
        {
            if (_isPersonalFeedEnabled && (logData.IsAttackerAgentMine || logData.IsAttackerAgentRiderAgentMine) && logData.TotalDamage > 0 && !logData.IsVictimAgentSameAsAttackerAgent)
            {
                _dataSource.OnPersonalDamage(logData.TotalDamage, logData.IsFatalDamage, logData.IsVictimAgentMount, logData.IsFriendlyFire, logData.BodyPartHit == BoneBodyPartType.Head, logData.VictimAgentName);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
            // Check if kill feed is enabled
            if (!Config.Instance.KillFeedEnabled || !_isGeneralFeedEnabled || GameNetwork.IsDedicatedServer || affectorAgent == null || !affectedAgent.IsHuman || agentState != AgentState.Killed && agentState != AgentState.Unconscious)
            {
                return;
            }
            _dataSource.OnAgentRemoved(affectedAgent, affectorAgent, _isPersonalFeedEnabled);
        }

        public KillNotificationView()
        {
        }

        private MPKillFeedVM _dataSource;

        private GauntletLayer _gauntletLayer;

        private MissionMultiplayerTeamDeathmatchClient _tdmClient;

        private MissionMultiplayerSiegeClient _siegeClient;

        private MissionMultiplayerGameModeDuelClient _duelClient;

        private MissionMultiplayerGameModeFlagDominationClient _flagDominationClient;

        private bool _isGeneralFeedEnabled;

        private bool _doesGameModeAllowGeneralFeed = true;

        private bool _isPersonalFeedEnabled;
    }
}