using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Update a part of the player spawn menu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncPlayerSpawnMenu : PlayerSpawnMenuMessage
	{
		public SyncPlayerSpawnMenu(GlobalOperation operation)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
		}

		public SyncPlayerSpawnMenu(TeamOperation operation, PlayerTeam playerTeam)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			PlayerTeam = playerTeam;
		}

		public SyncPlayerSpawnMenu(FormationOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			PlayerFormation = playerFormation;
		}

		public SyncPlayerSpawnMenu(CharacterOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			AvailableCharacter = availableCharacter;
		}

		public SyncPlayerSpawnMenu()
		{
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Sending " + Operation.ToString() + " to clients";
		}
	}
}