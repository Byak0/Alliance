using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAudioLocalized : GameNetworkMessage
    {
        static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 2000, true);
        static readonly CompressionInfo.Integer MaxHearingDistanceCompressionInfo = new CompressionInfo.Integer(-1, 10000, true);
        static readonly CompressionInfo.Float VolumeCompressionInfo = new CompressionInfo.Float(0f, 1f, 2);

        public int SoundIndex { get; private set; }
        public float Volume { get; private set; }
        public Vec3 SoundOrigin { get; private set; }
        public int MaxHearingDistance { get; private set; }
        public bool Stackable { get; private set; }

        public SyncAudioLocalized(int soundIndex, float volume, Vec3 soundOrigin, int maxHearingDistance, bool stackable)
        {
            SoundIndex = soundIndex;        
            Volume = volume;
            SoundOrigin = soundOrigin;
            MaxHearingDistance = maxHearingDistance;
            Stackable = stackable;
        }

        public SyncAudioLocalized()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
            Volume = ReadFloatFromPacket(VolumeCompressionInfo, ref bufferReadValid);
            SoundOrigin = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
            MaxHearingDistance = ReadIntFromPacket(MaxHearingDistanceCompressionInfo, ref bufferReadValid);
            Stackable = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
            WriteFloatToPacket(Volume, VolumeCompressionInfo);
            WriteVec3ToPacket(SoundOrigin, CompressionBasic.PositionCompressionInfo);
            WriteIntToPacket(MaxHearingDistance, MaxHearingDistanceCompressionInfo);
            WriteBoolToPacket(Stackable);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.General;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize localized audio id ", SoundIndex, " at position: ", SoundOrigin);
        }
    }
}