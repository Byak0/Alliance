using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.SoundPlayer.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class SoundRequest : GameNetworkMessage
    {
        public int SoundIndex { get; private set; }
        public int SoundDuration { get; private set; }

        public SoundRequest(int soundIndex, int soundDuration)
        {
            SoundIndex = soundIndex;
            SoundDuration = soundDuration;
        }

        public SoundRequest()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            SoundIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref bufferReadValid);
            SoundDuration = ReadIntFromPacket(new CompressionInfo.Integer(-1, 3600, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(SoundIndex, new CompressionInfo.Integer(0, 10000, true));
            WriteIntToPacket(SoundDuration, new CompressionInfo.Integer(-1, 3600, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Request sound ", SoundIndex, " with duration: ", SoundDuration);
        }
    }
}