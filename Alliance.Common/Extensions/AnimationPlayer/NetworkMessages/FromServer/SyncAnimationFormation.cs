using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAnimationFormation : GameNetworkMessage
    {
        public Team Team { get; set; }

        public int FormationIndex { get; private set; }

        public int ActionIndex { get; private set; }

        public float Speed { get; private set; }

        public SyncAnimationFormation(Formation formation, int actionIndex, float speed)
        {
            Team = formation.Team;
            FormationIndex = formation.Index;
            ActionIndex = actionIndex;
            Speed = speed;
        }

        public SyncAnimationFormation()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            int teamIndex = ReadTeamIndexFromPacket(ref bufferReadValid);
            Team = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(teamIndex);
            FormationIndex = ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
            ActionIndex = ReadIntFromPacket(new CompressionInfo.Integer(-1, 5000, true), ref bufferReadValid);
            Speed = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteTeamIndexToPacket(Team.TeamIndex);
            WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
            WriteIntToPacket(ActionIndex, new CompressionInfo.Integer(-1, 5000, true));
            WriteFloatToPacket(Speed, new CompressionInfo.Float(0f, 5, 0.1f));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.AgentAnimations;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize animation : ", ActionIndex, " of all agents in formation ", FormationIndex, " of team ", Team.Side.ToString());
        }
    }
}