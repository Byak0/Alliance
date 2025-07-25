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
		public int SyncId { get; private set; } = -1;
		public int? TotalMessageCount { get; private set; } // Only used in EndMenuSync operation to indicate how many messages were sent in total.
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;
		public PlayerTeam PlayerTeam { get; private set; }
		public PlayerFormation PlayerFormation { get; private set; }
		public AvailableCharacter AvailableCharacter { get; private set; }

		public SyncPlayerSpawnMenu(GlobalOperation operation, int syncId, int? totalMessageCount = null)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			SyncId = syncId;
			TotalMessageCount = totalMessageCount;
		}

		public SyncPlayerSpawnMenu(TeamOperation operation, PlayerTeam playerTeam, int syncId)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			PlayerTeam = playerTeam;
			SyncId = syncId;
		}

		public SyncPlayerSpawnMenu(FormationOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation, int syncId)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			PlayerFormation = playerFormation;
			SyncId = syncId;
		}

		public SyncPlayerSpawnMenu(CharacterOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter, int syncId)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			AvailableCharacter = availableCharacter;
			SyncId = syncId;
		}

		public SyncPlayerSpawnMenu()
		{
		}

		protected override void OnWrite()
		{
			WriteIntToPacket((int)Operation, OperationCompressionInfo);
			WriteIntToPacket(SyncId, SyncIdCompressionInfo);

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

				case PlayerSpawnMenuOperation.EndMenuSync:
					WriteIntToPacket(TotalMessageCount.Value, TotalMessageCountCompressionInfo);
					break;

				case PlayerSpawnMenuOperation.BeginMenuSync:
				default:
					break;
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			Operation = (PlayerSpawnMenuOperation)ReadIntFromPacket(OperationCompressionInfo, ref bufferReadValid);
			SyncId = ReadIntFromPacket(SyncIdCompressionInfo, ref bufferReadValid);

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

				case PlayerSpawnMenuOperation.EndMenuSync:
					TotalMessageCount = ReadIntFromPacket(TotalMessageCountCompressionInfo, ref bufferReadValid);
					break;

				case PlayerSpawnMenuOperation.BeginMenuSync:
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
			return "Alliance - PlayerSpawnMenu - Sending " + Operation.ToString() + " to clients (syncId: " + SyncId + ")";
		}
	}
}