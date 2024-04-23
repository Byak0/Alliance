using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAudio : GameNetworkMessage
    {
        static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 2000, true);
        static readonly CompressionInfo.Float VolumeCompressionInfo = new CompressionInfo.Float(0f, 1f, 2);

        public int SoundIndex { get; private set; }
        public float Volume { get; private set; }
        public bool Stackable { get; private set; }

        public SyncAudio(int soundIndex, float volume, bool stackable)
        {
            SoundIndex = soundIndex;
            Volume = volume;
            Stackable = stackable;
        }

        public SyncAudio()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
            Volume = ReadFloatFromPacket(VolumeCompressionInfo, ref bufferReadValid);
            Stackable = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
            WriteFloatToPacket(Volume, VolumeCompressionInfo);
            WriteBoolToPacket(Stackable);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize audio id ", SoundIndex);
        }
    }
}