using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.PvC.Behaviors
{
    /// <summary>
    /// Complete override of MissionMultiplayerFlagDomination. 
    /// Intended for custom PvC / CvC game modes.
    /// Fix several native limits with flags.
    /// Provides customizable settings for morale and time before flag removal.
    /// Automatically goes back to Lobby on game end.
    /// </summary>
    public class PvCGameModeBehavior : ALMissionMultiplayerFlagDomination
    {
        public PvCGameModeBehavior(MultiplayerGameType gameType) : base(gameType)
        {
            _gameType = gameType;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
        }

        public override void AfterStart()
        {
            base.AfterStart();
        }

        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            base.AddRemoveMessageHandlers(registerer);
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        protected override void OnPreparationEnded()
        {
            SetStartingGold();
        }

        protected override void CheckForPlayersSpawningAsBots()
        {
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                if (networkCommunicator.IsSynchronized)
                {
                    MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();

                    // Removed check on controlled formation to allow control when changing team
                    if (component != null && component.ControlledAgent == null && component.Team != null && component.SpawnCountThisRound > 0)
                    {
                        if (!component.HasSpawnTimerExpired && component.SpawnTimer.Check(Mission.Current.CurrentTime))
                        {
                            component.HasSpawnTimerExpired = true;
                        }
                        if (component.HasSpawnTimerExpired && component.WantsToSpawnAsBot)
                        {
                            Agent followedAgent = component.FollowedAgent;

                            // Check if the followed agent is in a formation controlled by the player
                            if (followedAgent != null && followedAgent.IsActive() && followedAgent.IsAIControlled && followedAgent.Formation != null && followedAgent.Health > 0
                                && FormationControlModel.Instance.GetControllerOfFormation(followedAgent.Formation) == component)
                            {
                                // Update player controlled formation to target
                                component.ControlledFormation = followedAgent.Formation;
                                ReplaceBotWithPlayer(followedAgent, component);
                                component.WantsToSpawnAsBot = false;
                                component.HasSpawnTimerExpired = false;
                            }
                        }
                    }
                }
            }
        }

        public override void ReplaceBotWithPlayer(Agent botAgent, MissionPeer missionPeer)
        {
            if (!GameNetwork.IsClientOrReplay && botAgent != null)
            {
                if (GameNetwork.IsServer)
                {
                    NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
                    if (!networkPeer.IsServerPeer)
                    {
                        GameNetwork.BeginModuleEventAsServer(networkPeer);
                        GameNetwork.WriteMessage(new ReplaceBotWithPlayer(networkPeer, botAgent.Index, botAgent.Health, botAgent.MountAgent?.Health ?? (-1f)));
                        GameNetwork.EndModuleEventAsServer();
                    }
                }

                if (botAgent.Formation != null)
                {
                    botAgent.Formation.PlayerOwner = botAgent;
                }

                botAgent.OwningAgentMissionPeer = null;
                botAgent.MissionPeer = missionPeer;
                botAgent.Formation = missionPeer.ControlledFormation;
                AgentFlag agentFlags = botAgent.GetAgentFlags();
                if (!agentFlags.HasAnyFlag(AgentFlag.CanRide))
                {
                    botAgent.SetAgentFlags(agentFlags | AgentFlag.CanRide);
                }

                // Prevent BotsUnderControlAlive from going under 0 and causing crash
                missionPeer.BotsUnderControlAlive = Math.Max(0, missionPeer.BotsUnderControlAlive - 1);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new BotsControlledChange(missionPeer.GetNetworkPeer(), missionPeer.BotsUnderControlAlive, missionPeer.BotsUnderControlTotal));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

                FormationControlModel.Instance.ReassignControlToAgent(botAgent);
            }
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
    }
}
