using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Update election timer for the player spawn menu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetElectionStatus : GameNetworkMessage
	{
		public bool Enable { get; private set; }
		public int Timer { get; private set; }

		public SetElectionStatus(bool enable, int timer)
		{
			Enable = enable;
			Timer = timer;
		}

		public SetElectionStatus()
		{
		}

		protected override void OnWrite()
		{
			WriteBoolToPacket(Enable);
			if (Enable)
			{
				WriteIntToPacket(Timer, CompressionBasic.WarmupTimeLimitCompressionInfo);
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Enable = ReadBoolFromPacket(ref bufferReadValid);
			if (Enable)
			{
				Timer = ReadIntFromPacket(CompressionBasic.WarmupTimeLimitCompressionInfo, ref bufferReadValid);
			}
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - " + (Enable ? "Election enabled with timer: " + Timer : "Election disabled");
		}
	}
}