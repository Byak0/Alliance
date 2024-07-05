using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class InitFakeArmyMessage : GameNetworkMessage
	{
		public Vec3 PositionToSpawnEmitter { get; set; }
		public int MaxNumberOfTickets { get; set; }
		public int CurrentNumberOfTickets { get; set; }

		public InitFakeArmyMessage() { }
		public InitFakeArmyMessage(Vec3 positionToSpawnEmitter, int maxTickets, int currentTickets)
		{
			PositionToSpawnEmitter = positionToSpawnEmitter;
			MaxNumberOfTickets = maxTickets;
			CurrentNumberOfTickets = currentTickets;
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PositionToSpawnEmitter = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
			MaxNumberOfTickets = ReadIntFromPacket(CompressionBasic.GUIDIntCompressionInfo, ref bufferReadValid);
			CurrentNumberOfTickets = ReadIntFromPacket(CompressionBasic.GUIDIntCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteVec3ToPacket(PositionToSpawnEmitter, CompressionBasic.PositionCompressionInfo);
			WriteIntToPacket(MaxNumberOfTickets, CompressionBasic.GUIDIntCompressionInfo);
			WriteIntToPacket(CurrentNumberOfTickets, CompressionBasic.GUIDIntCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Server request to init FakeArmy";
		}
	}
}
