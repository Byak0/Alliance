using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Update which character a player is using.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class AddCharacterUsage : GameNetworkMessage
	{
		public NetworkCommunicator Player { get; private set; }
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;
		public int CharacterIndex { get; private set; } = -1;

		public AddCharacterUsage(NetworkCommunicator player, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter)
		{
			Player = player;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			CharacterIndex = availableCharacter.Index;
		}

		public AddCharacterUsage()
		{
		}

		protected override void OnWrite()
		{
			WriteNetworkPeerReferenceToPacket(Player);
			WriteIntToPacket(TeamIndex, CompressionHelper.TeamIndexCompressionInfo);
			WriteIntToPacket(FormationIndex, CompressionHelper.FormationIndexCompressionInfo);
			WriteIntToPacket(CharacterIndex, CompressionHelper.CharacterIndexCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Player = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
			TeamIndex = ReadIntFromPacket(CompressionHelper.TeamIndexCompressionInfo, ref bufferReadValid);
			FormationIndex = ReadIntFromPacket(CompressionHelper.FormationIndexCompressionInfo, ref bufferReadValid);
			CharacterIndex = ReadIntFromPacket(CompressionHelper.CharacterIndexCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - " + Player?.VirtualPlayer?.UserName + " reserved character " + TeamIndex + " - " + FormationIndex + " - " + CharacterIndex;
		}
	}
}