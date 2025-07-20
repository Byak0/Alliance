using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Set a formation's officer.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetFormationOfficer : GameNetworkMessage
	{
		public NetworkCommunicator NewOfficer { get; private set; }
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;

		public SetFormationOfficer(NetworkCommunicator newOfficer, PlayerTeam playerTeam, PlayerFormation playerFormation)
		{
			NewOfficer = newOfficer;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
		}

		public SetFormationOfficer()
		{
		}

		protected override void OnWrite()
		{
			WriteNetworkPeerReferenceToPacket(NewOfficer);
			WriteIntToPacket(TeamIndex, TeamIndexCompressionInfo);
			WriteIntToPacket(FormationIndex, FormationIndexCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			NewOfficer = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
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
			return "Alliance - PlayerSpawnMenu - " + NewOfficer?.VirtualPlayer?.UserName + " is the new officer of " + TeamIndex + " - " + FormationIndex;
		}
	}
}