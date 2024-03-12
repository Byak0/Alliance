using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModels;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Server.GameModes.Story.Behaviors.SpawningStrategy;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Extensions.CustomScripts.Scripts.CS_RefillAmmo;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.GameModes.Story.Behaviors
{
    /// <summary>
    /// Server-side base behavior for scenario game mode.
    /// Responsible for updating scenario state.
    /// </summary>
    public class ScenarioBehavior : MissionMultiplayerGameModeBase, IMissionBehavior
    {
        public Scenario Scenario => ScenarioManagerServer.Instance.CurrentScenario;
        public Act Act => ScenarioManagerServer.Instance.CurrentAct;
        public ActState State => ScenarioManagerServer.Instance.ActState;

        public override bool IsGameModeHidingAllAgentVisuals => true;
        public override bool IsGameModeUsingOpposingTeams => true;

        public ScenarioSpawningBehavior SpawningBehavior { get; set; }
        public ObjectivesBehavior ObjectivesBehavior { get; set; }
        public ScenarioClientBehavior ClientBehavior { get; set; }

        private float _minimumStateDuration = 5f;
        private float _stateDuration;
        private float _checkStateDelay = 1f;

        public bool EnableStateChange { get; private set; }

        private float _checkStateDt;

        public ScenarioBehavior()
        {
        }

        public override void AfterStart()
        {
            BasicCultureObject attacker = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject defender = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            Banner banner = new Banner(attacker.BannerKey, attacker.BackgroundColor1, attacker.ForegroundColor1);
            Banner banner2 = new Banner(defender.BannerKey, defender.BackgroundColor1, defender.ForegroundColor1);
            Mission.Teams.Add(BattleSideEnum.Attacker, attacker.BackgroundColor1, attacker.ForegroundColor1, banner, isPlayerGeneral: false, isPlayerSergeant: true);
            Mission.Teams.Add(BattleSideEnum.Defender, defender.BackgroundColor1, defender.ForegroundColor1, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
            SpawningBehavior = (ScenarioSpawningBehavior)SpawnComponent.SpawningBehavior;
            ObjectivesBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
            ClientBehavior = Mission.Current.GetMissionBehavior<ScenarioClientBehavior>();

            ChangeState(ActState.AwaitingPlayerJoin);
            EnableStateChange = true;
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            string scenarioId = Scenario.Id;
            int actId = Scenario.Acts.IndexOf(Act);

            GameNetwork.BeginModuleEventAsServer(networkPeer);
            GameNetwork.WriteMessage(new InitScenarioMessage(scenarioId, actId));
            GameNetwork.EndModuleEventAsServer();

            float stateRemainingTime = TimerComponent.GetRemainingTime(false);
            GameNetwork.BeginModuleEventAsServer(networkPeer);
            GameNetwork.WriteMessage(new UpdateScenarioMessage(State, MissionTime.Now.NumberOfTicks, MathF.Ceiling(stateRemainingTime)));
            GameNetwork.EndModuleEventAsServer();

            long timerStart = 0;
            int timerDuration = 0;
            if (State == ActState.InProgress && ObjectivesBehavior.MissionTimer != null)
            {
                timerStart = ObjectivesBehavior.MissionTimer.GetStartTime().NumberOfTicks;
                timerDuration = (int)ObjectivesBehavior.MissionTimer.GetTimerDuration();
            }
            GameNetwork.BeginModuleEventAsServer(networkPeer);
            GameNetwork.WriteMessage(new ObjectivesProgressMessage(
                ObjectivesBehavior.TotalAttackerDead,
                ObjectivesBehavior.TotalDefenderDead,
                timerStart,
                timerDuration));
            GameNetwork.EndModuleEventAsServer();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (EnableStateChange)
            {
                CheckScenarioState(dt);
            }
        }

        private void CheckScenarioState(float dt)
        {
            _checkStateDt += dt;
            _stateDuration += dt;
            if (_checkStateDt < _checkStateDelay) return;
            _checkStateDt = 0;
            if (_stateDuration < _minimumStateDuration) return;
            switch (State)
            {
                case ActState.Invalid:
                    ChangeState(ActState.AwaitingPlayerJoin);
                    break;
                case ActState.AwaitingPlayerJoin:
                    if (EnoughPlayersJoined())
                    {
                        ChangeState(ActState.SpawningParticipants);
                        StartSpawn();
                    }
                    break;
                case ActState.SpawningParticipants:
                    if (InitialSpawnFinished())
                    {
                        ChangeState(ActState.InProgress);
                        StartAct();
                        SyncObjectives();
                    }
                    break;
                case ActState.InProgress:
                    if (CheckObjectives())
                    {
                        ChangeState(ActState.DisplayingResults);
                        DisplayResults();
                    }
                    break;
                case ActState.DisplayingResults:
                    if (CanEndAct())
                    {
                        ChangeState(ActState.Completed);
                        EndAct();
                    }
                    break;
                case ActState.Completed:
                    EnableStateChange = false;
                    break;
            }
        }

        private bool CanEndAct()
        {
            return _stateDuration > 15f;
        }

        private void ChangeState(ActState newState)
        {
            ScenarioManagerServer.Instance.SetActState(newState);
            _stateDuration = 0;
        }

        private void SyncObjectives()
        {
            long timerStart = 0;
            int timerDuration = 0;
            if (State == ActState.InProgress && ObjectivesBehavior.MissionTimer != null)
            {
                timerStart = ObjectivesBehavior.MissionTimer.GetStartTime().NumberOfTicks;
                timerDuration = (int)ObjectivesBehavior.MissionTimer.GetTimerDuration();
            }
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ObjectivesProgressMessage(
                ObjectivesBehavior.TotalAttackerDead,
                ObjectivesBehavior.TotalDefenderDead,
                timerStart,
                timerDuration));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        private bool EnoughPlayersJoined()
        {
            int minPlayersForStart = (int)MathF.Clamp((float)Math.Round(GameNetwork.NetworkPeers.Count / 1.1), 1, Math.Max(GameNetwork.NetworkPeers.Count - 1, 1));
            int playersReady = 0;
            foreach (ICommunicator peer in GameNetwork.NetworkPeers)
            {
                if (peer.IsSynchronized) playersReady++;
            }
            string log = "Waiting for players to load... (" + playersReady + "/" + minPlayersForStart + ")";
            Log(log);
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage(log));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            return playersReady >= minPlayersForStart;
        }

        private bool InitialSpawnFinished()
        {
            //return !SpawningBehavior.AreAgentsSpawning() || SpawningBehavior.SpawningStrategy.SpawningTimer > MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            // TODO check if this fix the act starting before spawn ended
            return !SpawningBehavior.AreAgentsSpawning() || SpawningBehavior.SpawningStrategy.SpawningTimer > 10f + MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
        }

        private bool CheckObjectives()
        {
            return ObjectivesBehavior.ObjectiveIsOver;
        }

        private void StartSpawn()
        {
            Log("Alliance - Starting spawn...", LogLevel.Debug);
            TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
        }

        private void StartAct()
        {
            Log("Alliance - Starting act...", LogLevel.Debug);
            TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
        }

        private void DisplayResults()
        {
            Log("Alliance - Displaying results...", LogLevel.Debug);
        }

        private void EndAct()
        {
            Log("Alliance - Ended act.", LogLevel.Debug);
        }

        public void ResetState()
        {
            EnableStateChange = true;
            _checkStateDt = 0;
            ObjectivesBehavior.Reset();
            //SpawningBehavior.SetSpawningStrategy(new SpawningStrategyBase()); // Todo : define strategy in act ?
        }

        public override void OnRemoveBehavior()
        {
            ScenarioManagerServer.Instance.CurrentAct.UnregisterObjectives();
            GameNetwork.RemoveNetworkHandler(this);
        }

        public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), Config.Instance.StartingGold);
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            agent.UpdateSyncHealthToAllClients(value: true);

            // Set ammo of players to 0 when respawning
            if (SpawningBehavior.SpawningStrategy is SpawningStrategyBase baseSpawningStrategy && agent?.MissionPeer != null)
            {
                // Check if player is respawning
                if (baseSpawningStrategy.PlayerUsedLives.ContainsKey(agent.MissionPeer) && baseSpawningStrategy.PlayerUsedLives[agent.MissionPeer] > 1)
                {
                    SetAmmoOfAgent(agent, 0);
                }
            }
        }

        public void SetAmmoOfAgent(Agent agent, short ammoCount)
        {
            if (agent == null || agent.Equipment == null) return;

            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
            {
                if (!agent.Equipment[equipmentIndex].IsEmpty)
                {
                    if (Enum.IsDefined(typeof(AmmoClass), (int)agent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass) && agent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass != (WeaponClass)AmmoClass.All)
                    {
                        if (agent.Equipment[equipmentIndex].Amount > ammoCount)
                        {
                            agent.SetWeaponAmountInSlot(equipmentIndex, ammoCount, true);
                        }
                    }
                }
            }
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
                            ScenarioRepresentative ScenarioRepresentative3;
                            if (num > 0 && (ScenarioRepresentative3 = missionPeer?.Representative as ScenarioRepresentative) != null)
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
                    ScenarioRepresentative player = killer.MissionPeer.Representative as ScenarioRepresentative;
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
                    ScenarioRepresentative commander = (ScenarioRepresentative)(killer.Formation?.PlayerOwner?.MissionPeer?.Representative);
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

        private void ReportBonusGoldFromKill(Agent victim, ScenarioRepresentative commander, int goldGainFromKillDataAndUpdateFlags)
        {
            string victimName = victim.MissionPeer?.DisplayedName != null ? victim.MissionPeer.DisplayedName : victim.Name;
            string report = "You killed " + victimName + " for " + goldGainFromKillDataAndUpdateFlags + " golds ("
                + (goldGainFromKillDataAndUpdateFlags - Config.Instance.GoldPerKill) + " bonus) !";
            GameNetwork.BeginModuleEventAsServer(commander.MissionPeer.GetNetworkPeer());
            GameNetwork.WriteMessage(new ServerMessage(report, false));
            GameNetwork.EndModuleEventAsServer();
        }

        public void AddGoldForPeer(MissionPeer peer, int amount)
        {
            ChangeCurrentGoldForPeer(peer, peer.Representative.Gold + amount);
        }

        // Mask parent method and remove gold limit
        public new void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
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

        public bool UseGold()
        {
            return Config.Instance.UseTroopCost;
        }

        public override MultiplayerGameType GetMissionType()
        {
            return MultiplayerGameType.Captain;
        }
    }
}