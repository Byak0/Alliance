using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModels;
using Alliance.Common.GameModes.PvC.Behaviors;
using Alliance.Server.Core;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.PvC.Behaviors
{
    public class PvCGameModeBehavior : MissionMultiplayerFlagDomination, IAnalyticsFlagInfo, IMissionBehavior
    {
        private bool _goldGivenThisRound;

        public PvCGameModeBehavior(MissionLobbyComponent.MultiplayerGameType gameType) : base(gameType)
        {
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            // Remove flag removal mechanic in case there is no flags to prevent crash
            FieldInfo pointRemovalTimeInSeconds = typeof(MissionMultiplayerFlagDomination).GetField("_pointRemovalTimeInSeconds", BindingFlags.Instance | BindingFlags.NonPublic);
            if (AllCapturePoints.Count == 0)
            {
                pointRemovalTimeInSeconds.SetValue(this, 4000f);
            }
            else
            {
                pointRemovalTimeInSeconds.SetValue(this, Config.Instance.TimeBeforeFlagRemoval);
            }

            FieldInfo moraleMultiplierForEachFlag = typeof(MissionMultiplayerFlagDomination).GetField("_moraleMultiplierForEachFlag", BindingFlags.Instance | BindingFlags.NonPublic);
            moraleMultiplierForEachFlag.SetValue(this, Config.Instance.MoraleMultiplierForFlag);

            FieldInfo moraleMultiplierOnLastFlag = typeof(MissionMultiplayerFlagDomination).GetField("_moraleMultiplierOnLastFlag", BindingFlags.Instance | BindingFlags.NonPublic);
            moraleMultiplierOnLastFlag.SetValue(this, Config.Instance.MoraleMultiplierForLastFlag);
        }

        public override void AfterStart()
        {
            RoundController.OnRoundStarted += OnPreparationStart;
            MissionPeer.OnPreTeamChanged += OnPreTeamChanged;
            RoundController.OnPreparationEnded += OnPreparationEnded;
            RoundController.OnPreRoundEnding += OnRoundEnd;
            RoundController.OnPostRoundEnded += OnPostRoundEnd;
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            Banner banner = new Banner(@object.BannerKey, @object.BackgroundColor1, @object.ForegroundColor1);
            Banner banner2 = new Banner(object2.BannerKey, object2.BackgroundColor2, object2.ForegroundColor2);
            Mission.Teams.Add(BattleSideEnum.Attacker, @object.BackgroundColor1, @object.ForegroundColor1, banner, isPlayerGeneral: false, isPlayerSergeant: true);
            Mission.Teams.Add(BattleSideEnum.Defender, object2.BackgroundColor2, object2.ForegroundColor2, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
        }

        // Override to prevent call to WarmupComponent (we removed it duh)
        public override void OnRemoveBehavior()
        {
            // Reset  all spawn slots
            for (int i = 0; i < AgentsInfoModel.Instance.Agents.Count; i++)
            {
                AgentsInfoModel.Instance.RemoveAgentInfo(i);
            }
            RoundController.OnRoundStarted -= OnPreparationStart;
            MissionPeer.OnPreTeamChanged -= OnPreTeamChanged;
            RoundController.OnPreparationEnded -= OnPreparationEnded;
            //WarmupComponent.OnWarmupEnding -= this.OnWarmupEnding;
            RoundController.OnPreRoundEnding -= OnRoundEnd;
            RoundController.OnPostRoundEnded -= OnPostRoundEnd;
            GameNetwork.RemoveNetworkHandler(this);
            //base.OnRemoveBehavior();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (RoundController.CurrentRoundState == MultiplayerRoundState.InProgress)
            {
                if (!_goldGivenThisRound && TimerComponent.GetRemainingTime(false) < MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue() - 5f)
                {
                    SetStartingGold();
                    _goldGivenThisRound = true;
                }
            }
        }

        // Override to prevent FlagDominationMissionRepresentative being added to peer
        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
        }

        public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), Config.Instance.StartingGold);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.UpdateSyncHealthToAllClients(value: true);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            // Free spawn slot of victim
            SpawnHelper.RemoveBot(affectedAgent);
        }

        public override void OnAgentRemoved(Agent victim, Agent killer, AgentState agentState, KillingBlow blow)
        {
            float goldMultiplier = 1f;

            // Adjust gold multiplier based on victim formation state
            if (victim?.Team != null)
            {
                if (FormationCalculateModel.IsInFormation(victim))
                {
                    goldMultiplier = 1f;
                }
                else if (FormationCalculateModel.IsInSkirmish(victim))
                {
                    goldMultiplier = 4f;
                }
                else
                {
                    goldMultiplier = 10f;
                }
            }

            if (UseGold() && killer != null && victim != null && victim.IsHuman && blow.DamageType != DamageTypes.Invalid && (agentState == AgentState.Unconscious || agentState == AgentState.Killed))
            {
                if (!victim.IsHuman || !RoundController.IsRoundInProgress || blow.DamageType == DamageTypes.Invalid || agentState != AgentState.Unconscious && agentState != AgentState.Killed)
                {
                    return;
                }

                bool teamKill = killer.Team != null && victim.Team != null && killer.Team.Side == victim.Team.Side;

                if (victim.MissionPeer?.Team != null && !teamKill)
                {
                    // Gold gain for ally death
                    IEnumerable<(MissionPeer, int)> enumerable = MPPerkObject.GetPerkHandler(victim.MissionPeer)?.GetTeamGoldRewardsOnDeath();
                    if (enumerable != null)
                    {
                        foreach (var (missionPeer, num) in enumerable)
                        {
                            PvCRepresentative PvCRepresentative3;
                            if (num > 0 && (PvCRepresentative3 = missionPeer?.Representative as PvCRepresentative) != null)
                            {
                                int goldGainsFromAllyDeathReward = Config.Instance.GoldPerAllyDead;
                                if (goldGainsFromAllyDeathReward > 0)
                                {
                                    AddGoldForPeer(missionPeer, goldGainsFromAllyDeathReward);
                                }
                            }
                        }
                    }
                }
                MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter2 = MultiplayerClassDivisions.GetMPHeroClassForCharacter(victim.Character);
                if (killer?.MissionPeer != null && killer.Team != victim.Team)
                {
                    // Gold gain for a kill
                    PvCRepresentative player = killer.MissionPeer.Representative as PvCRepresentative;
                    int goldGainFromKillDataAndUpdateFlags = (int)(goldMultiplier * Config.Instance.GoldPerKill);
                    AddGoldForPeer(killer.MissionPeer, goldGainFromKillDataAndUpdateFlags);

                    // Send report to commander if bonus gold obtained
                    if (goldGainFromKillDataAndUpdateFlags > 0 && goldMultiplier > 1 && player.IsCommander)
                    {
                        ReportBonusGoldFromKill(victim, player, goldGainFromKillDataAndUpdateFlags);
                    }
                }
                else if (killer.Team != victim.Team)
                {
                    // Gold gain for the commander when a bot kill an enemy
                    PvCRepresentative commander = (PvCRepresentative)(killer.Formation?.PlayerOwner?.MissionPeer?.Representative);
                    if (commander != null)
                    {
                        int goldGainFromKillDataAndUpdateFlags = (int)(goldMultiplier * Config.Instance.GoldPerKill);
                        AddGoldForPeer(commander.MissionPeer, goldGainFromKillDataAndUpdateFlags);

                        // Send report to commander if bonus gold obtained
                        if (goldGainFromKillDataAndUpdateFlags > 0 && goldMultiplier > 1)
                        {
                            ReportBonusGoldFromKill(victim, commander, goldGainFromKillDataAndUpdateFlags);
                        }
                    }
                }

                List<Agent.Hitter> list = victim.HitterList.Where((hitter) => hitter.HitterPeer != killer.MissionPeer).ToList();
                if (list.Count > 0)
                {
                    Agent.Hitter hitter2 = TaleWorlds.Core.Extensions.MaxBy(list, (hitter) => hitter.Damage);
                    if (hitter2.Damage >= 35f)
                    {
                        // Gold gain for an assist if damage >= 35                        
                        int goldGainFromKillDataAndUpdateFlags2 = (int)(goldMultiplier * Config.Instance.GoldPerAssist);
                        AddGoldForPeer(hitter2.HitterPeer, goldGainFromKillDataAndUpdateFlags2);
                    }
                }
            }
        }

        private void ReportBonusGoldFromKill(Agent victim, PvCRepresentative commander, int goldGainFromKillDataAndUpdateFlags)
        {
            string victimName = victim.MissionPeer?.DisplayedName != null ? victim.MissionPeer.DisplayedName : victim.Name;
            string report = "You killed " + victimName + " for " + goldGainFromKillDataAndUpdateFlags + " golds ("
                + (goldGainFromKillDataAndUpdateFlags - Config.Instance.GoldPerKill) + " bonus) !";
            GameNetwork.BeginModuleEventAsServer(commander.MissionPeer.GetNetworkPeer());
            GameNetwork.WriteMessage(new ServerMessage(report, false));
            GameNetwork.EndModuleEventAsServer();
        }

        private void OnPreparationStart()
        {
            NotificationsComponent.PreparationStarted();
            _goldGivenThisRound = false;
        }

        private void OnPreTeamChanged(NetworkCommunicator peer, Team currentTeam, Team newTeam)
        {
        }

        private void OnPreparationEnded()
        {
            if (!UseGold() || RoundController.IsMatchEnding || RoundController.RoundCount <= 0)
            {
                return;
            }
        }

        private void SetStartingGold()
        {
            int playerTeamValue = GetTeamArmyValue((BattleSideEnum)Config.Instance.CommanderSide == BattleSideEnum.Attacker ? BattleSideEnum.Defender : BattleSideEnum.Attacker);
            List<MissionPeer> commanders = new List<MissionPeer>();
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer peer = networkCommunicator?.GetComponent<MissionPeer>();
                if (peer != null)
                {
                    if (peer.Team != null && peer.Team.Side == (BattleSideEnum)Config.Instance.CommanderSide)
                    {
                        commanders.Add(peer);
                    }
                    else
                    {
                        ChangeCurrentGoldForPeer(peer, Config.Instance.StartingGold);
                    }
                }
            }
            foreach (MissionPeer peer in commanders)
            {
                ChangeCurrentGoldForPeer(peer, Config.Instance.StartingGold + playerTeamValue / commanders.Count);
            }
        }

        public int GetTeamArmyValue(BattleSideEnum side)
        {
            int value = 0;
            int agentsCount;

            if (side == Mission.Current.AttackerTeam.Side)
            {
                agentsCount = Mission.Current.AttackerTeam.ActiveAgents.Count;
                foreach (Agent agent in Mission.Current.AttackerTeam.ActiveAgents)
                {
                    value += SpawnHelper.GetTroopCost(agent.Character);
                }
            }
            else
            {
                agentsCount = Mission.Current.DefenderTeam.ActiveAgents.Count;
                foreach (Agent agent in Mission.Current.DefenderTeam.ActiveAgents)
                {
                    value += SpawnHelper.GetTroopCost(agent.Character);
                }
            }

            Log($"Value of {side}'s army : {value} * {Config.Instance.GoldMultiplier} ({agentsCount} agents)", LogLevel.Debug);

            value = (int)(value * Config.Instance.GoldMultiplier);

            return value;
        }

        private void OnRoundEnd()
        {
            foreach (FlagCapturePoint allCapturePoint in AllCapturePoints)
            {
                if (!allCapturePoint.IsDeactivated)
                {
                    allCapturePoint.SetMoveNone();
                }
            }

            RoundEndReason roundEndReason = RoundEndReason.Invalid;
            bool flag = RoundController.RemainingRoundTime <= 0f && !CheckIfOvertime();
            int num = -1;
            for (int i = 0; i < 2; i++)
            {
                int num2 = i * 2 - 1;
                if (flag && num2 * MoraleRounded > 0f || !flag && num2 * MoraleRounded >= 1f)
                {
                    num = i;
                    break;
                }
            }

            CaptureTheFlagCaptureResultEnum captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.NotCaptured;
            if (num >= 0)
            {
                captureTheFlagCaptureResultEnum = num == 0 ? CaptureTheFlagCaptureResultEnum.DefendersWin : CaptureTheFlagCaptureResultEnum.AttackersWin;
                RoundController.RoundWinner = num != 0 ? BattleSideEnum.Attacker : BattleSideEnum.Defender;
                roundEndReason = flag ? RoundEndReason.RoundTimeEnded : RoundEndReason.GameModeSpecificEnded;
            }
            else
            {
                bool flag2 = Mission.AttackerTeam.ActiveAgents.Count > 0;
                bool flag3 = Mission.DefenderTeam.ActiveAgents.Count > 0;
                if (flag2 && flag3)
                {
                    if (MoraleRounded > 0f)
                    {
                        captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.AttackersWin;
                        RoundController.RoundWinner = BattleSideEnum.Attacker;
                    }
                    else if (MoraleRounded < 0f)
                    {
                        captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.DefendersWin;
                        RoundController.RoundWinner = BattleSideEnum.Defender;
                    }
                    else
                    {
                        captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.Draw;
                        RoundController.RoundWinner = BattleSideEnum.None;
                    }

                    roundEndReason = RoundEndReason.RoundTimeEnded;
                }
                else if (flag2)
                {
                    captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.AttackersWin;
                    RoundController.RoundWinner = BattleSideEnum.Attacker;
                    roundEndReason = RoundEndReason.SideDepleted;
                }
                else if (flag3)
                {
                    captureTheFlagCaptureResultEnum = CaptureTheFlagCaptureResultEnum.DefendersWin;
                    RoundController.RoundWinner = BattleSideEnum.Defender;
                    roundEndReason = RoundEndReason.SideDepleted;
                }
                else
                {
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
                        {
                            string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
                            if (component.Team.Side != BattleSideEnum.Attacker)
                            {
                                strValue = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
                            }

                            if (GetCurrentGoldForPeer(component) >= MultiplayerClassDivisions.GetMinimumTroopCost(MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue)))
                            {
                                RoundController.RoundWinner = component.Team.Side;
                                roundEndReason = RoundEndReason.SideDepleted;
                                captureTheFlagCaptureResultEnum = component.Team.Side != BattleSideEnum.Attacker ? CaptureTheFlagCaptureResultEnum.DefendersWin : CaptureTheFlagCaptureResultEnum.AttackersWin;
                                break;
                            }
                        }
                    }
                }
            }

            if (captureTheFlagCaptureResultEnum != CaptureTheFlagCaptureResultEnum.NotCaptured)
            {
                RoundController.RoundEndReason = roundEndReason;
                HandleRoundEnd(captureTheFlagCaptureResultEnum);
            }
        }

        private void HandleRoundEnd(CaptureTheFlagCaptureResultEnum roundResult)
        {
            AgentVictoryLogic missionBehavior = Mission.GetMissionBehavior<AgentVictoryLogic>();
            if (missionBehavior == null)
            {
                Debug.FailedAssert("Agent victory logic should not be null after someone just won/lost!", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\MissionNetworkLogics\\MultiplayerGameModeLogics\\ServerGameModeLogics\\MissionMultiplayerFlagDomination.cs", "HandleRoundEnd", 761);
                return;
            }

            switch (roundResult)
            {
                case CaptureTheFlagCaptureResultEnum.AttackersWin:
                    missionBehavior.SetTimersOfVictoryReactionsOnBattleEnd(BattleSideEnum.Attacker);
                    break;
                case CaptureTheFlagCaptureResultEnum.DefendersWin:
                    missionBehavior.SetTimersOfVictoryReactionsOnBattleEnd(BattleSideEnum.Defender);
                    break;
            }
        }

        private void OnPostRoundEnd()
        {
            if (RoundController.IsMatchEnding)
            {
                GameModeStarter.Instance.StartLobby("Lobby", "empire", "vlandia");
            }

            // TODO : check if necessary. MB OnPreparationEnded is enough.
            /*if (!UseGold() || RoundController.IsMatchEnding || RoundController.RoundCount <= 0) 
            { 
                return; 
            }
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                if(component != null) ChangeCurrentGoldForPeer(component, Config.Instance.StartingGoldAmount);
            }*/
        }

        public void AddGoldForPeer(MissionPeer peer, int amount)
        {
            ChangeCurrentGoldForPeer(peer, peer.Representative.Gold + amount);
        }

        // Mask parent method and remove gold limit
        public new void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
        {
            try
            {
                newAmount = MBMath.ClampInt(newAmount, 0, CompressionBasic.RoundGoldAmountCompressionInfo.GetMaximumValue());

                if (peer.Peer.Communicator.IsConnectionActive)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new SyncGoldsForSkirmish(peer.Peer, newAmount));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                }

                if (GameModeBaseClient != null)
                {
                    GameModeBaseClient.OnGoldAmountChangedForRepresentative(peer.Representative, newAmount);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("PvC : ERROR giving gold to " + peer, 0, Debug.DebugColor.Red);
                Debug.Print(ex.ToString(), 0, Debug.DebugColor.Red);
            }
        }
    }
}