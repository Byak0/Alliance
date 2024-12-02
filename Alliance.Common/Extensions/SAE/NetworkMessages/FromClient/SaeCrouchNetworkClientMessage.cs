using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeCrouchNetworkClientMessage : GameNetworkMessage
    {
        public int TeamIndex { get; private set; }
        public int FormationIndex { get; private set; }
        public bool CrouchMode { get; private set; }

        public SaeCrouchNetworkClientMessage() { }

        public SaeCrouchNetworkClientMessage(Formation formation, bool crouchMode)
        {
            TeamIndex = formation.Team.TeamIndex;
            FormationIndex = formation.Index;
            CrouchMode = crouchMode;
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            TeamIndex = ReadTeamIndexFromPacket(ref bufferReadValid);
            FormationIndex = ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
            CrouchMode = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteTeamIndexToPacket(TeamIndex);
            WriteIntToPacket(FormationIndex, CompressionMission.FormationClassCompressionInfo);
            WriteBoolToPacket(CrouchMode);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Formations;
        }

        protected override string OnGetLogFormat()
        {
            return "Order units to crouch";
        }
    }
}
