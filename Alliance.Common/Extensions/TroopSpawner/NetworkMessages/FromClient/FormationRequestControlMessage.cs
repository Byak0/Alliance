using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient
{
    /// <summary>
    /// Request control of formation.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class FormationRequestControlMessage : GameNetworkMessage
    {
        public NetworkCommunicator Peer { get; private set; }
        public FormationClass Formation { get; private set; }

        public FormationRequestControlMessage() { }

        public FormationRequestControlMessage(NetworkCommunicator peer, FormationClass formation)
        {
            Peer = peer;
            Formation = formation;
        }

        protected override void OnWrite()
        {
            WriteNetworkPeerReferenceToPacket(Peer);
            WriteIntToPacket((int)Formation, CompressionMission.FormationClassCompressionInfo);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Peer = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
            Formation = (FormationClass)ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Formations;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync formation control info";
        }
    }
}
