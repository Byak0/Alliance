using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Core;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.CvC.Behaviors
{
    public class CvCGameModeBehavior : MissionMultiplayerFlagDomination, IAnalyticsFlagInfo, IMissionBehavior
    {
        public CvCGameModeBehavior(MultiplayerGameType gameType) : base(gameType)
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
            RoundController.OnPreparationEnded += OnPreparationEnded;
            if (WarmupComponent != null)
            {
                WarmupComponent.OnWarmupEnding += OnWarmupEnding;
            }

            RoundController.OnPreRoundEnding += OnRoundEnd;
            RoundController.OnPostRoundEnded += OnPostRoundEnd;
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            Banner banner = new Banner(@object.BannerKey, @object.BackgroundColor1, @object.ForegroundColor1);
            Banner banner2 = new Banner(object2.BannerKey, object2.BackgroundColor2, object2.ForegroundColor2);
            Mission.Teams.Add(BattleSideEnum.Attacker, @object.BackgroundColor1, @object.ForegroundColor1, banner, isPlayerGeneral: false, isPlayerSergeant: true);
            Mission.Teams.Add(BattleSideEnum.Defender, object2.BackgroundColor2, object2.ForegroundColor2, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
        }

        public override void OnRemoveBehavior()
        {
            // Reset  all spawn slots
            for (int i = 0; i < AgentsInfoModel.Instance.Agents.Count; i++)
            {
                AgentsInfoModel.Instance.RemoveAgentInfo(i);
            }

            RoundController.OnRoundStarted -= OnPreparationStart;
            RoundController.OnPreparationEnded -= OnPreparationEnded;
            if (WarmupComponent != null)
            {
                WarmupComponent.OnWarmupEnding -= OnWarmupEnding;
            }
            RoundController.OnPreRoundEnding -= OnRoundEnd;
            RoundController.OnPostRoundEnded -= OnPostRoundEnd;

            GameNetwork.RemoveNetworkHandler(this);
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            //if (oldTeam != null && oldTeam != newTeam && UseGold() && (WarmupComponent == null || !WarmupComponent.IsInWarmup))
            //{
            //    ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), Config.Instance.StartingGold);
            //}
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.UpdateSyncHealthToAllClients(value: true);
        }

        public override void OnAgentRemoved(Agent victim, Agent killer, AgentState agentState, KillingBlow blow)
        {
        }

        private void OnPreparationStart()
        {
            NotificationsComponent.PreparationStarted();
        }

        private void OnPreparationEnded()
        {
            SetStartingGold();
        }

        private void OnWarmupEnding()
        {
            NotificationsComponent.WarmupEnding();
        }

        private void SetStartingGold()
        {
            int goldToGive = Config.Instance.StartingGold;
            List<MissionPeer> commandersAttack = new List<MissionPeer>();
            List<MissionPeer> commandersDefend = new List<MissionPeer>();
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer peer = networkCommunicator?.GetComponent<MissionPeer>();
                if (peer?.Team == Mission.Current.AttackerTeam)
                {
                    commandersAttack.Add(peer);
                }
                else if (peer?.Team == Mission.Current.DefenderTeam)
                {
                    commandersDefend.Add(peer);
                }
            }
            SplitGoldBetweenPlayers(goldToGive, commandersAttack);
            SplitGoldBetweenPlayers(goldToGive, commandersDefend);
        }

        private void SplitGoldBetweenPlayers(int goldToGive, List<MissionPeer> players)
        {
            if (players.Count < 1) return;

            int amountPerPlayer = goldToGive / players.Count;
            foreach (MissionPeer peer in players)
            {
                Log($"Giving {amountPerPlayer}g to {peer.Name}", LogLevel.Debug);
                ChangeCurrentGoldForPeer(peer, amountPerPlayer);
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
            // Reset  all spawn slots
            for (int i = 0; i < AgentsInfoModel.Instance.Agents.Count; i++)
            {
                AgentsInfoModel.Instance.RemoveAgentInfo(i);
            }

            // Go back to Lobby on match end
            if (RoundController.IsMatchEnding)
            {
                GameModeStarter.Instance.StartLobby("Lobby", MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(), MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            }
        }

        public void AddGoldForPeer(MissionPeer peer, int amount)
        {
            ChangeCurrentGoldForPeer(peer, peer.Representative.Gold + amount);
        }

        // Mask parent method and remove gold limit
        public new void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
        {
            if (newAmount >= 0)
            {
                newAmount = MBMath.ClampInt(newAmount, 0, CompressionBasic.RoundGoldAmountCompressionInfo.GetMaximumValue());
            }

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
    }
}