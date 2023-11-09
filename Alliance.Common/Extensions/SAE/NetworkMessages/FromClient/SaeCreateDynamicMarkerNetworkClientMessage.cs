using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromClient
{
    /// <summary>
    /// This message is when the client ask to create a dynamic marker using the pre-marker posed on the map
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SaeCreateDynamicMarkerNetworkClientMessage : GameNetworkMessage
    {
        public List<MatrixFrame> markersPosition;
        readonly CompressionInfo.Integer CompressionInfo;

        public SaeCreateDynamicMarkerNetworkClientMessage()
        {
            markersPosition = new List<MatrixFrame>();
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        public SaeCreateDynamicMarkerNetworkClientMessage(List<MatrixFrame> markersPosition)
        {
            this.markersPosition = markersPosition;
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        protected override void OnWrite()
        {
            /*
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, markersPosition);
                byte[] test = memoryStream.ToArray();
                WriteByteArrayToPacket(test, 0, test.Length);
            }*/



            WriteIntToPacket(markersPosition.Count, CompressionInfo);
            foreach (MatrixFrame value in markersPosition)
            {

                WriteMatrixFrameToPacket(value);
            }
        }

        protected override bool OnRead()
        {

            /*
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = ReadByteArrayFromPacket())
            {
                List<MatrixFrame> myList = (List<MatrixFrame>)formatter.Deserialize(memoryStream);
            }*/




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
            return "Ask server to spawn some dynamic SAE markers";
        }

    }
}
