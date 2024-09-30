using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Audio.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class MusicRequest : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer SoundIndexCompressionInfo = new CompressionInfo.Integer(0, 10000, true);

		public int SoundIndex { get; private set; }

		public MusicRequest(int soundIndex)
		{
			SoundIndex = soundIndex;
		}

		public MusicRequest()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			SoundIndex = ReadIntFromPacket(SoundIndexCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(SoundIndex, SoundIndexCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Request music ", SoundIndex);
		}
	}
}