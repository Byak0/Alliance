using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Add a player's candidacy for officer.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class AddOfficerCandidacy : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;
		public int CharacterIndex { get; private set; } = -1;
		public string Pitch { get; set; }

		public AddOfficerCandidacy(NetworkCommunicator player, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter, string pitch)
		{
			Player = player;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			CharacterIndex = availableCharacter.Index;
			Pitch = pitch;
		}

		public AddOfficerCandidacy()
		{
		}

		protected override void OnWrite()
		{
			WriteNetworkPeerReferenceToPacket(Player);
			WriteIntToPacket(TeamIndex, CompressionHelper.TeamIndexCompressionInfo);
			WriteIntToPacket(FormationIndex, CompressionHelper.FormationIndexCompressionInfo);
			WriteIntToPacket(CharacterIndex, CompressionHelper.CharacterIndexCompressionInfo);
			WriteStringToPacket(Pitch);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Player = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
			TeamIndex = ReadIntFromPacket(CompressionHelper.TeamIndexCompressionInfo, ref bufferReadValid);
			FormationIndex = ReadIntFromPacket(CompressionHelper.FormationIndexCompressionInfo, ref bufferReadValid);
			CharacterIndex = ReadIntFromPacket(CompressionHelper.CharacterIndexCompressionInfo, ref bufferReadValid);
			Pitch = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - " + Player?.VirtualPlayer?.UserName + " is candidate for officer in " + TeamIndex + " - " + FormationIndex + " - " + CharacterIndex;
		}
	}
}