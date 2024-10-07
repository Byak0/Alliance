using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class NativeSoundRequest : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 10000, true);
		static readonly CompressionInfo.Integer SoundDurationCompressionInfo = new CompressionInfo.Integer(-1, 3600, true);

		public int SoundIndex { get; private set; }
		public int SoundDuration { get; private set; }

		public NativeSoundRequest(int soundIndex, int soundDuration)
		{
			SoundIndex = soundIndex;
			SoundDuration = soundDuration;
		}

		public NativeSoundRequest()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
			SoundDuration = ReadIntFromPacket(SoundDurationCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
			WriteIntToPacket(SoundDuration, SoundDurationCompressionInfo);
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