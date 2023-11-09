using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeDeleteMarkerForAllNetworkClientMessage : GameNetworkMessage
    {
        public List<int> ListOfMarkersId { get; set; }
        readonly CompressionInfo.Integer CompressionInfo;

        public SaeDeleteMarkerForAllNetworkClientMessage()
        {
            ListOfMarkersId = new List<int>();
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        public SaeDeleteMarkerForAllNetworkClientMessage(List<int> list)
        {
            ListOfMarkersId = list;
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(ListOfMarkersId.Count, CompressionInfo);
            foreach (int value in ListOfMarkersId)
            {
                WriteIntToPacket(value, CompressionInfo);
            }
        }

        protected override bool OnRead()
        {
            ListOfMarkersId = new List<int>();
            bool bufferReadValid = true;

            int nbElements = ReadIntFromPacket(CompressionInfo, ref bufferReadValid);

            for (int i = 0; i < nbElements; i++)
            {
                ListOfMarkersId.Add(ReadIntFromPacket(CompressionInfo, ref bufferReadValid));
            }

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Request to delete some markers";
        }

    }
}
