using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient
{
	/// <summary>
	/// From client : Ask the server to send us the player spawn menu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestPlayerSpawnMenu : GameNetworkMessage
	{
		public RequestPlayerSpawnMenu()
		{
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}


		protected override bool OnRead()
		{
			return true;
		}

		protected override void OnWrite()
		{
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Requesting to receive the player spawn menu";
		}
	}
}