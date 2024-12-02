using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.VOIP.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class ALSendVoiceRecord : GameNetworkMessage
    {
        public byte[] Buffer { get; private set; }

        public int BufferLength { get; private set; }

        public bool IsAnnouncement { get; private set; }

        public ALSendVoiceRecord()
        {
        }

        public ALSendVoiceRecord(byte[] buffer, int bufferLength, bool isAnnouncement = false)
        {
            Buffer = buffer;
            BufferLength = bufferLength;
            IsAnnouncement = isAnnouncement;
        }

        protected override void OnWrite()
        {
            WriteByteArrayToPacket(Buffer, 0, BufferLength);
            WriteBoolToPacket(IsAnnouncement);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Buffer = new byte[1440];
            BufferLength = ReadByteArrayFromPacket(Buffer, 0, 1440, ref bufferReadValid);
            IsAnnouncement = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return string.Empty;
        }
    }
}
