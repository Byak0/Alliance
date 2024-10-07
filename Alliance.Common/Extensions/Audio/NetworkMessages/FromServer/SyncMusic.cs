using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncMusic : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 2000, true);
		static readonly CompressionInfo.Float VolumeCompressionInfo = new CompressionInfo.Float(0f, 1f, 2);

		public int SoundIndex { get; private set; }
		public float Volume { get; private set; }
		public float StartingPoint { get; private set; }

		public SyncMusic(int soundIndex, float volume, float startingPoint)
		{
			SoundIndex = soundIndex;
			Volume = volume;
			StartingPoint = startingPoint;
		}

		public SyncMusic()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
			Volume = ReadFloatFromPacket(VolumeCompressionInfo, ref bufferReadValid);
			StartingPoint = ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
			WriteFloatToPacket(Volume, VolumeCompressionInfo);
			WriteFloatToPacket(StartingPoint, CompressionMatchmaker.MissionTimeCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Synchronize music ", SoundIndex);
		}
	}
}