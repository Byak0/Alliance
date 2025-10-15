using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Remove a player from a formation.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class RemovePlayerFromFormation : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;

		public RemovePlayerFromFormation(NetworkCommunicator player, PlayerTeam playerTeam, PlayerFormation playerFormation)
		{
			Player = player;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
		}

		public RemovePlayerFromFormation()
		{
		}

		protected override void OnWrite()
		{
			WriteNetworkPeerReferenceToPacket(Player);
			WriteIntToPacket(TeamIndex, TeamIndexCompressionInfo);
			WriteIntToPacket(FormationIndex, FormationIndexCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Player = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
			TeamIndex = ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
			FormationIndex = ReadIntFromPacket(FormationIndexCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - " + Player?.VirtualPlayer?.UserName + " no longer member of formation " + TeamIndex + " - " + FormationIndex;
		}
	}
}