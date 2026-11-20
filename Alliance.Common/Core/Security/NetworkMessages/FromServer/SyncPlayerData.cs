using Alliance.Common.Core.Security.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Security.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncPlayerData : GameNetworkMessage
	{
		public bool AllData { get; private set; }
		public NetworkCommunicator Player { get; private set; }
		public AL_PlayerData PlayerData { get; private set; }

		public SyncPlayerData() { }

		public SyncPlayerData(AL_PlayerData playerData, NetworkCommunicator player = null, bool allData = false)
		{
			Player = player;
			PlayerData = playerData;
			AllData = allData;
		}

		protected override void OnWrite()
		{
			PlayerSyncService.WritePlayerDataToPacket(PlayerData, Player, AllData);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			(Player, PlayerData, AllData) = PlayerSyncService.ReadPlayerDataFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Administration;
		}

		protected override string OnGetLogFormat()
		{
			return "Update player data";
		}
	}
}
