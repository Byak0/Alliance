using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer
{
    /// <summary>
    /// NetworkMessage to synchronize FormationControlModel between server and clients.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class FormationControlMessage : GameNetworkMessage
    {
        public NetworkCommunicator Peer { get; private set; }
        public FormationClass Formation { get; private set; }
        public bool Delete { get; private set; }

        public FormationControlMessage() { }

        public FormationControlMessage(NetworkCommunicator peer, FormationClass formation, bool delete = false)
        {
            Peer = peer;
            Formation = formation;
            Delete = delete;
        }

        protected override void OnWrite()
        {
            WriteNetworkPeerReferenceToPacket(Peer);
            WriteIntToPacket((int)Formation, CompressionOrder.FormationClassCompressionInfo);
            WriteBoolToPacket(Delete);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Peer = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
            Formation = (FormationClass)ReadIntFromPacket(CompressionOrder.FormationClassCompressionInfo, ref bufferReadValid);
            Delete = ReadBoolFromPacket(ref bufferReadValid);
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
