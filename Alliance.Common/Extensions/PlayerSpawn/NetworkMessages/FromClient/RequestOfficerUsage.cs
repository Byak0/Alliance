using Alliance.Common.Extensions.PlayerSpawn.Models;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient
{
	/// <summary>
	/// From client : Request to use an officer in the player spawn menu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestOfficerUsage : GameNetworkMessage
	{
		public int TeamIndex { get; set; } = -1;
		public int FormationIndex { get; set; } = -1;
		public int CharacterIndex { get; set; } = -1;
		public int NbPerks { get; private set; } = 0;
		public List<int> SelectedPerks { get; private set; } = new List<int>();
		public string Pitch { get; set; }

		public RequestOfficerUsage(PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter, List<int> selectedPerks, string pitch)
		{
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			CharacterIndex = availableCharacter.Index;
			SelectedPerks = selectedPerks;
			NbPerks = SelectedPerks.Count;
			Pitch = pitch;
		}

		public RequestOfficerUsage()
		{
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(TeamIndex, TeamIndexCompressionInfo);
			WriteIntToPacket(FormationIndex, FormationIndexCompressionInfo);
			WriteIntToPacket(CharacterIndex, CharacterIndexCompressionInfo);
			WriteIntToPacket(NbPerks, CompressionMission.PerkIndexCompressionInfo);
			foreach (int selectedPerk in SelectedPerks)
			{
				WriteIntToPacket(selectedPerk, CompressionMission.PerkIndexCompressionInfo);
			}
			WriteStringToPacket(Pitch);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			TeamIndex = ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
			FormationIndex = ReadIntFromPacket(FormationIndexCompressionInfo, ref bufferReadValid);
			CharacterIndex = ReadIntFromPacket(CharacterIndexCompressionInfo, ref bufferReadValid);
			NbPerks = ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid);
			SelectedPerks = new List<int>();
			for (int i = 0; i < NbPerks; i++)
			{
				SelectedPerks.Add(ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid));
			}
			Pitch = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Requesting to use officer" + TeamIndex + " - " + FormationIndex + " - " + CharacterIndex + " - " + Pitch;
		}
	}
}