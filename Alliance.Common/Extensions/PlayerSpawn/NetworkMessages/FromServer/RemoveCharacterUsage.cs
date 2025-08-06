using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Clear a player's character usage in the PlayerSpawnMenu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class RemoveCharacterUsage : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }

		public RemoveCharacterUsage(NetworkCommunicator player)
		{
			Player = player;
		}

		public RemoveCharacterUsage()
		{
		}

		protected override void OnWrite()
		{
			WriteNetworkPeerReferenceToPacket(Player);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Player = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Clearing " + Player?.VirtualPlayer?.UserName + "'s reserved character";
		}
	}
}