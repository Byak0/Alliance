using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeCrouchNetworkClientMessage : GameNetworkMessage
    {
        public Team Team { get; private set; }
        public int FormationIndex { get; private set; }
        public bool CrouchMode { get; private set; }

        public SaeCrouchNetworkClientMessage() { }

        public SaeCrouchNetworkClientMessage(Formation formation, bool crouchMode)
        {
            Team = formation.Team;
            FormationIndex = formation.Index;
            CrouchMode = crouchMode;
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Team = ReadTeamReferenceFromPacket(ref bufferReadValid);
            FormationIndex = ReadIntFromPacket(CompressionOrder.FormationClassCompressionInfo, ref bufferReadValid);
            CrouchMode = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteTeamReferenceToPacket(Team);
            WriteIntToPacket(FormationIndex, CompressionOrder.FormationClassCompressionInfo);
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
