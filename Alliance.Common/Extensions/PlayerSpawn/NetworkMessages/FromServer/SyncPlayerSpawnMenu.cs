using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Update a part of the player spawn menu.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncPlayerSpawnMenu : GameNetworkMessage, IPlayerSpawnMenuMessage
	{
		public PlayerSpawnMenuOperation Operation { get; private set; }
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;
		public PlayerTeam PlayerTeam { get; private set; }
		public PlayerFormation PlayerFormation { get; private set; }
		public AvailableCharacter AvailableCharacter { get; private set; }

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

		protected override void OnWrite()
		{
			WriteIntToPacket((int)Operation, OperationCompressionInfo);

			switch (Operation)
			{
				case PlayerSpawnMenuOperation.AddTeam:
				case PlayerSpawnMenuOperation.UpdateTeam:
				case PlayerSpawnMenuOperation.RemoveTeam:
					WritePlayerTeamToPacket(PlayerTeam);
					break;

				case PlayerSpawnMenuOperation.AddFormation:
				case PlayerSpawnMenuOperation.UpdateFormation:
				case PlayerSpawnMenuOperation.RemoveFormation:
					WriteIntToPacket(TeamIndex, TeamIndexCompressionInfo);
					WritePlayerFormationToPacket(PlayerFormation);
					break;

				case PlayerSpawnMenuOperation.AddCharacter:
				case PlayerSpawnMenuOperation.UpdateCharacter:
				case PlayerSpawnMenuOperation.RemoveCharacter:
					WriteIntToPacket(TeamIndex, TeamIndexCompressionInfo);
					WriteIntToPacket(FormationIndex, FormationIndexCompressionInfo);
					WriteAvailableCharacterToPacket(AvailableCharacter);
					break;

				case PlayerSpawnMenuOperation.BeginMenuSync:
				case PlayerSpawnMenuOperation.EndMenuSync:
				default:
					break;
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			Operation = (PlayerSpawnMenuOperation)ReadIntFromPacket(OperationCompressionInfo, ref bufferReadValid);

			switch (Operation)
			{
				case PlayerSpawnMenuOperation.AddTeam:
				case PlayerSpawnMenuOperation.UpdateTeam:
				case PlayerSpawnMenuOperation.RemoveTeam:
					PlayerTeam = ReadPlayerTeamFromPacket(ref bufferReadValid);
					break;
				case PlayerSpawnMenuOperation.AddFormation:
				case PlayerSpawnMenuOperation.UpdateFormation:
				case PlayerSpawnMenuOperation.RemoveFormation:
					TeamIndex = ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
					PlayerFormation = ReadPlayerFormationFromPacket(ref bufferReadValid);
					break;
				case PlayerSpawnMenuOperation.AddCharacter:
				case PlayerSpawnMenuOperation.UpdateCharacter:
				case PlayerSpawnMenuOperation.RemoveCharacter:
					TeamIndex = ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
					FormationIndex = ReadIntFromPacket(FormationIndexCompressionInfo, ref bufferReadValid);
					AvailableCharacter = ReadAvailableCharacterFromPacket(ref bufferReadValid);
					break;
				case PlayerSpawnMenuOperation.BeginMenuSync:
				case PlayerSpawnMenuOperation.EndMenuSync:
				default:
					break;
			}

			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Sending " + Operation.ToString() + " to clients";
		}
	}
}