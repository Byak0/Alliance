using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncMusicLocalized : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 2000, true);
		static readonly CompressionInfo.Float VolumeCompressionInfo = new CompressionInfo.Float(0f, 1f, 2);
		static readonly CompressionInfo.Integer MaxHearingDistanceCompressionInfo = new CompressionInfo.Integer(-1, 10000, true);

		public int SoundIndex { get; private set; }
		public float Volume { get; private set; }
		public float StartingPoint { get; private set; }
		public Vec3 Position { get; private set; }
		public int MaxHearingDistance { get; private set; }
		public bool MuteMainMusic { get; private set; }

		public SyncMusicLocalized(int soundIndex, float volume, float startingPoint, Vec3 position, int maxHearingDistance, bool muteMainMusic)
		{
			SoundIndex = soundIndex;
			Volume = volume;
			StartingPoint = startingPoint;
			Position = position;
			MaxHearingDistance = maxHearingDistance;
			MuteMainMusic = muteMainMusic;
		}

		public SyncMusicLocalized()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
			Volume = ReadFloatFromPacket(VolumeCompressionInfo, ref bufferReadValid);
			StartingPoint = ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
			Position = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
			MaxHearingDistance = ReadIntFromPacket(MaxHearingDistanceCompressionInfo, ref bufferReadValid);
			MuteMainMusic = ReadBoolFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
			WriteFloatToPacket(Volume, VolumeCompressionInfo);
			WriteFloatToPacket(StartingPoint, CompressionMatchmaker.MissionTimeCompressionInfo);
			WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
			WriteIntToPacket(MaxHearingDistance, MaxHearingDistanceCompressionInfo);
			WriteBoolToPacket(MuteMainMusic);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Synchronize music ", SoundIndex, " at position ", Position);
		}
	}
}