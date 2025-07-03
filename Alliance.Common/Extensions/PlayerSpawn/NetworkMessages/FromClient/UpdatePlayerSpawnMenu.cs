using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient
{
	/// <summary>
	/// From client : Update a part of the player spawn menu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class UpdatePlayerSpawnMenu : PlayerSpawnMenuMessage
	{
		public UpdatePlayerSpawnMenu(GlobalOperation operation)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
		}

		public UpdatePlayerSpawnMenu(TeamOperation operation, PlayerTeam playerTeam)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			PlayerTeam = playerTeam;
		}

		public UpdatePlayerSpawnMenu(FormationOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			PlayerFormation = playerFormation;
		}

		public UpdatePlayerSpawnMenu(CharacterOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			AvailableCharacter = availableCharacter;
		}

		public UpdatePlayerSpawnMenu()
		{
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Sending " + Operation.ToString() + " to server";
		}
	}
}