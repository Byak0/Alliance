using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeCreateMarkerNetworkClientMessage : GameNetworkMessage
    {
        public List<MatrixFrame> markersPosition;
        readonly CompressionInfo.Integer CompressionInfo;

        public SaeCreateMarkerNetworkClientMessage()
        {
            markersPosition = new List<MatrixFrame>();
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        public SaeCreateMarkerNetworkClientMessage(List<MatrixFrame> spawnPositions)
        {
            markersPosition = spawnPositions;
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(markersPosition.Count, CompressionInfo);
            foreach (MatrixFrame value in markersPosition)
            {
                WriteMatrixFrameToPacket(value);
            }
        }

        protected override bool OnRead()
        {
            markersPosition = new List<MatrixFrame>();
            bool bufferReadValid = true;

            int nbElements = ReadIntFromPacket(CompressionInfo, ref bufferReadValid);

            for (int i = 0; i < nbElements; i++)
            {
                markersPosition.Add(ReadMatrixFrameFromPacket(ref bufferReadValid));
            }

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Formations;
        }
        protected override string OnGetLogFormat()
        {
            return "Ask server to spawn a some SAE markers";
        }

    }
}
