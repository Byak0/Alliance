using Alliance.Common.Extensions.SAE.NetworkMessages.FromClient;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.SAE.Handlers
{
    /// <summary>
    /// This handler will handle message send by a client to the server
    /// So this code will only be executed by server
    /// </summary>
    public class SaeDebugHandler
    {
        public SaeDebugHandler() { }

        public bool OnSaeMessageReceived(NetworkCommunicator peer, SaeDebugNetworkClientMessage message)
        {
            Log("peer.ControlledAgent.Team.DetachmentManager.Detachments.Count = " + peer.ControlledAgent.Team.DetachmentManager.Detachments.Count, LogLevel.Debug);

            peer.ControlledAgent.Team.DetachmentManager.OnAgentRemoved(peer.ControlledAgent);
            peer.ControlledAgent.Team.DetachmentManager.Detachments.ForEach(d =>
            {
                Log("Item1 = " + d.Item1, LogLevel.Debug);
                Log("Item2 = " + d.Item2, LogLevel.Debug);

                if (d.Item1 is StrategicArea)
                {
                    Log("d.Item2.AgentCount = " + d.Item2.AgentCount, LogLevel.Debug);
                    Log("d.Item1.GetDetachmentWeightFromCache() = " + d.Item1.GetDetachmentWeightFromCache(), LogLevel.Debug);
                    Log("d.Item1.GetDetachmentWeight(BattleSideEnum.Defender) = " + d.Item1.GetDetachmentWeight(BattleSideEnum.Defender), LogLevel.Debug);
                    Log("d.Item1.GetDetachmentWeight(BattleSideEnum.Attacker) = " + d.Item1.GetDetachmentWeight(BattleSideEnum.Attacker), LogLevel.Debug);
                    Log("d.Item1.IsEvaluated() (Should be at false) = " + d.Item1.IsEvaluated(), LogLevel.Debug);
                    Log("d.Item2.IsPrecalculated() (Should be at true) = " + d.Item2.IsPrecalculated(), LogLevel.Debug);

                    Log("d.Item2.agentScores.Count = " + d.Item2.agentScores.Count, LogLevel.Debug);

                    //peer.ControlledAgent.Controller = Agent.ControllerType.Player;
                    //peer.ControlledAgent.Formation.OnAgentControllerChanged(peer.ControlledAgent, Agent.ControllerType.None);


                    Log("---- forEach agentScores ----" + d.Item2.agentScores.Count, LogLevel.Debug);
                    d.Item2.agentScores.ForEach(score =>
                    {
                        Log("Agent = " + score.Item1.Controller, LogLevel.Debug);
                        Log("Scores = " + score.Item2.ToString(), LogLevel.Debug);
                    });

                    Log("d.Item2.MovingAgentCount = " + d.Item2.MovingAgentCount, LogLevel.Debug);
                    Log("d.Item2.DefendingAgentCount = " + d.Item2.DefendingAgentCount, LogLevel.Debug);
                    Log("-- Formation INFOS -- = ", LogLevel.Debug);
                    Log("peer.ControlledAgent.Formation.CountOfDetachableNonplayerUnits = " + peer.ControlledAgent.Formation.CountOfDetachableNonplayerUnits, LogLevel.Debug);
                    Log("peer.ControlledAgent.Formation.IsPlayerTroopInFormation = " + peer.ControlledAgent.Formation.IsPlayerTroopInFormation, LogLevel.Debug);
                    Log("peer.ControlledAgent.Formation.HasPlayerControlledTroop = " + peer.ControlledAgent.Formation.HasPlayerControlledTroop, LogLevel.Debug);
                    Log("peer.ControlledAgent.Formation.CountOfUndetachableNonPlayerUnits = " + peer.ControlledAgent.Formation.CountOfUndetachableNonPlayerUnits, LogLevel.Debug);
                    Log("-- ControlledAgent INFOS -- = ", LogLevel.Debug);
                    Log("peer.ControlledAgent.IsPlayerTroop = " + peer.ControlledAgent.IsPlayerTroop, LogLevel.Debug);
                    Log("peer.ControlledAgent.IsPlayerControlled = " + peer.ControlledAgent.IsPlayerControlled, LogLevel.Debug);
                    Log("peer.ControlledAgent.IsMine = " + peer.ControlledAgent.IsMine, LogLevel.Debug);
                    Log("peer.ControlledAgent.Controller = " + peer.ControlledAgent.Controller, LogLevel.Debug);
                    Log("peer.ControlledAgent.MissionPeer = " + peer.ControlledAgent.MissionPeer, LogLevel.Debug);


                }
            });

            return true;
        }
    }
}
