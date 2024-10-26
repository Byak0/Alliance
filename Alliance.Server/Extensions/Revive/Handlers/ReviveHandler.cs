using Alliance.Common.Core;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Revive.NetworkMessages.FromClient;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using Alliance.Server.GameModes.PvC.Behaviors;
using NetworkMessages.FromServer;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.Revive.Handlers
{
    public class ReviveHandler : IHandlerRegister
    {
        PvCGameModeBehavior GameMode => Mission.Current.GetMissionBehavior<PvCGameModeBehavior>();
        IBotControllerBehavior GameModeClient => (IBotControllerBehavior)Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
        ISpawnBehavior SpawnBehavior => (ISpawnBehavior)Mission.Current.GetMissionBehavior<SpawnComponent>().SpawningBehavior;
        ISpawnFrameBehavior SpawnFrame => (ISpawnFrameBehavior)Mission.Current.GetMissionBehavior<SpawnComponent>().SpawnFrameBehavior;

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestRescueAgent>(HandleRequestRescueAgent);
        }

        /// <summary>
        /// Handle player request to spawn troops.
        /// </summary>
        public bool HandleRequestRescueAgent(NetworkCommunicator peer, RequestRescueAgent model)
        {
            // Player info
            MissionPeer missionPeer = peer.GetComponent<MissionPeer>();
            NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
            // Troop info
            BasicCharacterObject troopToSpawn = GetTroopToSpawn(model.CharacterToSpawn);
            ExtendedCharacter extendedTroopToSpawn = troopToSpawn.GetExtendedCharacterObject();

            string refuseReason = "";
            if (!CanPlayerRescueAgent(peer, extendedTroopToSpawn, model, ref refuseReason))
            {
                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new ServerMessage(refuseReason, false));
                GameNetwork.EndModuleEventAsServer();
                return false;
            }

            bool agentRescued = false;
            string reportToPlayer = "";
            MatrixFrame spawnPos = model.SpawnPosition;

            // Assign player as sergeant BEFORE spawning to prevent crash OnBotsControlledChange ?
            if (missionPeer.ControlledAgent != null)
            {
                Formation formation = missionPeer.ControlledAgent.Team.GetFormation((FormationClass)model.Formation);
                MissionPeer previousSergeant = formation.PlayerOwner?.MissionPeer;
                if (previousSergeant != missionPeer)
                {
                    // Unassign previous sergeant from this formation to prevent crash 
                    if (previousSergeant != null)
                    {
                        FormationControlModel.Instance.RemoveControlFromPlayer(previousSergeant, (FormationClass)model.Formation, true);
                        previousSergeant.ControlledFormation = null;
                    }
                    FormationControlModel.Instance.AssignControlToPlayer(missionPeer, (FormationClass)model.Formation, true);
                }
            }

            string lackingReason = "";
            // Spawn the required number of bots
            if (!model.SpawnAtExactPosition)
                spawnPos = SpawnFrame.GetClosestSpawnFrame(missionPeer.Team, troopToSpawn.HasMount(), false, spawnPos);
            if (!SpawnHelper.SpawnBot(missionPeer.Team, missionPeer.Culture, troopToSpawn, spawnPos,null, model.Formation, model.Difficulty))
            {
                Log($"Alliance : Can't rescue {model.CharacterToSpawn} (no slot available)", LogLevel.Error);
                lackingReason = $"Can't rescue {model.CharacterToSpawn} (no slot available)";
            }
            else
            {
                agentRescued = true;
            }

            if (missionPeer.ControlledFormation != null)
            {
                // Need to properly calculate bots number to prevent crash
                // Bots who spawned before the player took control are not counted natively
                int botsUnderControlAlive = missionPeer.BotsUnderControlAlive = Math.Max(missionPeer.BotsUnderControlAlive, missionPeer.ControlledFormation.CountOfUnits);
                int botsUnderControlTotal = Math.Max(missionPeer.BotsUnderControlTotal, missionPeer.BotsUnderControlAlive);

                if (GameModeClient != null)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, botsUnderControlAlive, botsUnderControlTotal));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

                    GameModeClient.OnBotsControlledChanged(missionPeer, botsUnderControlAlive, botsUnderControlTotal);

                    Log($"OBCC - {missionPeer.Name} - {(FormationClass)model.Formation} - alive: {botsUnderControlAlive} - total: {botsUnderControlTotal}", LogLevel.Debug);
                }
            }

            reportToPlayer += "You rescued " + troopToSpawn.Name;

            if (agentRescued)
            {
                // Inform players of what just spawned
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SpawnInfoMessage(troopToSpawn, 0, extendedTroopToSpawn.TroopLeft));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
            else
            {
                reportToPlayer = lackingReason;
            }

            // Send report to player who made the spawn request
            GameNetwork.BeginModuleEventAsServer(networkPeer);
            GameNetwork.WriteMessage(new ServerMessage(reportToPlayer, false));
            GameNetwork.EndModuleEventAsServer();

            return true;
        }

        /// <summary>
        /// Check if a player is allowed to rescue.
        /// </summary>
        /// <param name="refuseReason">Explanation in case the player is not allowed to rescue.</param>
        /// <returns>True if allowed, false otherwise</returns>
        private bool CanPlayerRescueAgent(NetworkCommunicator peer, ExtendedCharacter troopToSpawn, RequestRescueAgent model, ref string refuseReason)
        {
            // TODO Implement checks here before rescuing
            if (peer.ControlledAgent == null)
            {
                refuseReason = "You can't rescue since you're DEAD.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Get appropriate BasicCharacterObject from troop name
        /// </summary>
        private static BasicCharacterObject GetTroopToSpawn(string troopName, bool heroVersion = false)
        {
            BasicCharacterObject troopToSpawn = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForCharacter(troopToSpawn);

            if (heroVersion)
            {
                return mPHeroClassForPeer.HeroCharacter;
            }
            else
            {
                return mPHeroClassForPeer.TroopCharacter;
            }
        }
    }
}