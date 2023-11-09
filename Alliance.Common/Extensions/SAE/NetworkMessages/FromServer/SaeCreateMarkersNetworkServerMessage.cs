using Alliance.Common.Extensions.SAE.Models;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.SAE.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SaeCreateMarkersNetworkServerMessage : GameNetworkMessage
    {
        public List<SaeMarkerWithIdAndPos> MarkerList { get; private set; }

        readonly CompressionInfo.Integer CompressionInfo;

        public SaeCreateMarkersNetworkServerMessage()
        {
            MarkerList = new List<SaeMarkerWithIdAndPos>();
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        public SaeCreateMarkersNetworkServerMessage(List<SaeMarkerWithIdAndPos> list)
        {
            MarkerList = list;
            CompressionInfo = new CompressionInfo.Integer(0, 20000, true);
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(MarkerList.Count, CompressionInfo);
            MarkerList.ForEach(e => WriteIntToPacket(e.Id, CompressionInfo));

            WriteIntToPacket(MarkerList.Count, CompressionInfo);
            MarkerList.ForEach(e => WriteMatrixFrameToPacket(e.Pos));

        }

        protected override bool OnRead()
        {
            List<int> ListOfMarkersId = new List<int>();
            List<MatrixFrame> ListOfMarkersPosition = new List<MatrixFrame>();
            bool bufferReadValid = true;

            //Fill ListOfMarkersId
            int nbIdElements = ReadIntFromPacket(CompressionInfo, ref bufferReadValid);
            for (int i = 0; i < nbIdElements; i++)
            {
                ListOfMarkersId.Add(ReadIntFromPacket(CompressionInfo, ref bufferReadValid));
            }

            //Fill ListOfMarkersPosition
            int nbPosElements = ReadIntFromPacket(CompressionInfo, ref bufferReadValid);
            for (int i = 0; i < nbPosElements; i++)
            {
                ListOfMarkersPosition.Add(ReadMatrixFrameFromPacket(ref bufferReadValid));
            }

            //Update MarkerList property
            for (int i = 0; i < ListOfMarkersId.Count; i++)
            {
                MarkerList.Add(
                    new SaeMarkerWithIdAndPos(ListOfMarkersId[i], ListOfMarkersPosition[i])
                );
            }

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Formations;
        }

        protected override string OnGetLogFormat()
        {
            return "Send to client all markers from peer's team";
        }

    }
}
