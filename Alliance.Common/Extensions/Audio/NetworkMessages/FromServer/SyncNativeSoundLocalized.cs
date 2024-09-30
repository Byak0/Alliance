using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncNativeSoundLocalized : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 10000, true);
		static readonly CompressionInfo.Integer SoundDurationCompressionInfo = new CompressionInfo.Integer(-1, 3600, true);

		public int SoundIndex { get; private set; }
		public int SoundDuration { get; private set; }
		public Vec3 Position { get; private set; }

		public SyncNativeSoundLocalized(int soundIndex, int soundDuration, Vec3 position)
		{
			SoundIndex = soundIndex;
			SoundDuration = soundDuration;
			Position = position;
		}

		public SyncNativeSoundLocalized()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
			SoundDuration = ReadIntFromPacket(SoundDurationCompressionInfo, ref bufferReadValid);
			Position = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
			WriteIntToPacket(SoundDuration, SoundDurationCompressionInfo);
			WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Synchronize sound ", SoundIndex, " with duration: ", SoundDuration, " at ", Position);
		}
	}
}