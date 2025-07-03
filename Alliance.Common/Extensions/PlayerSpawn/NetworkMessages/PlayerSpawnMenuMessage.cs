using Alliance.Common.Core.Configuration.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Core.Configuration.Models.AllianceData;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages
{
	/// <summary>
	/// Abstract base class for send the whole Player Spawn menu. Client and server implement their own versions.
	/// </summary>
	public abstract class PlayerSpawnMenuMessage : GameNetworkMessage
	{
		public static readonly CompressionInfo.Integer OperationCompressionInfo = new CompressionInfo.Integer(0, Enum.GetValues(typeof(PlayerSpawnMenuOperation)).Length, true);
		public static readonly CompressionInfo.Integer TeamIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		public static readonly CompressionInfo.Integer FormationIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		public static readonly CompressionInfo.Integer CharacterIndexCompressionInfo = new CompressionInfo.Integer(0, 32, true);

		public enum PlayerSpawnMenuOperation
		{
			BeginMenuSync,
			EndMenuSync,
			AddTeam,
			UpdateTeam,
			RemoveTeam,
			AddFormation,
			UpdateFormation,
			RemoveFormation,
			AddCharacter,
			UpdateCharacter,
			RemoveCharacter
		}

		public enum GlobalOperation
		{
			BeginMenuSync = PlayerSpawnMenuOperation.BeginMenuSync,
			EndMenuSync = PlayerSpawnMenuOperation.EndMenuSync
		}

		public enum TeamOperation
		{
			AddTeam = PlayerSpawnMenuOperation.AddTeam,
			UpdateTeam = PlayerSpawnMenuOperation.UpdateTeam,
			RemoveTeam = PlayerSpawnMenuOperation.RemoveTeam
		}

		public enum FormationOperation
		{
			AddFormation = PlayerSpawnMenuOperation.AddFormation,
			UpdateFormation = PlayerSpawnMenuOperation.UpdateFormation,
			RemoveFormation = PlayerSpawnMenuOperation.RemoveFormation
		}

		public enum CharacterOperation
		{
			AddCharacter = PlayerSpawnMenuOperation.AddCharacter,
			UpdateCharacter = PlayerSpawnMenuOperation.UpdateCharacter,
			RemoveCharacter = PlayerSpawnMenuOperation.RemoveCharacter
		}

		public PlayerSpawnMenuOperation Operation { get; protected set; }
		public int TeamIndex { get; protected set; } = -1;
		public int FormationIndex { get; protected set; } = -1;
		public PlayerTeam PlayerTeam { get; protected set; }
		public PlayerFormation PlayerFormation { get; protected set; }
		public AvailableCharacter AvailableCharacter { get; protected set; }

		public PlayerSpawnMenuMessage(GlobalOperation operation)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
		}

		public PlayerSpawnMenuMessage(TeamOperation operation, PlayerTeam playerTeam)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			PlayerTeam = playerTeam;
		}

		public PlayerSpawnMenuMessage(FormationOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			PlayerFormation = playerFormation;
		}

		public PlayerSpawnMenuMessage(CharacterOperation operation, PlayerTeam playerTeam, PlayerFormation playerFormation, AvailableCharacter availableCharacter)
		{
			Operation = (PlayerSpawnMenuOperation)operation;
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			AvailableCharacter = availableCharacter;
		}

		public PlayerSpawnMenuMessage()
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

		private static void WritePlayerTeamToPacket(PlayerTeam team)
		{
			WriteIntToPacket(team.Index, TeamIndexCompressionInfo);
			WriteStringToPacket(team.Name);
			WriteIntToPacket((int)team.TeamSide, CompressionMission.TeamSideCompressionInfo);
		}

		private PlayerTeam ReadPlayerTeamFromPacket(ref bool bufferReadValid)
		{
			PlayerTeam team = new PlayerTeam();
			team.Index = ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
			team.Name = ReadStringFromPacket(ref bufferReadValid);
			team.TeamSide = (BattleSideEnum)ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
			return team;
		}

		private static void WritePlayerFormationToPacket(PlayerFormation formation)
		{
			WriteIntToPacket(formation.Index, FormationIndexCompressionInfo);
			WriteStringToPacket(formation.Name);
			WriteObjectReferenceToPacket(formation.MainCulture, CompressionBasic.GUIDCompressionInfo);
			WriteBoolToPacket(formation.Settings.UseMorale);
		}

		private PlayerFormation ReadPlayerFormationFromPacket(ref bool bufferReadValid)
		{
			PlayerFormation formation = new PlayerFormation();
			formation.Index = ReadIntFromPacket(FormationIndexCompressionInfo, ref bufferReadValid);
			formation.Name = ReadStringFromPacket(ref bufferReadValid);
			object cultureObject = ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
			if (cultureObject != null && cultureObject is BasicCultureObject bco)
			{
				formation.MainCultureId = bco.StringId;
			}
			formation.Settings.UseMorale = ReadBoolFromPacket(ref bufferReadValid);
			return formation;
		}

		private static void WriteAvailableCharacterToPacket(AvailableCharacter character)
		{
			WriteIntToPacket(character.Index, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteObjectReferenceToPacket(character.Character, CompressionBasic.GUIDCompressionInfo);
			WriteBoolToPacket(character.Officer);
			WriteIntToPacket(character.SpawnCount, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteBoolToPacket(character.IsPercentage);
			WriteIntToPacket((int)character.Difficulty, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteFloatToPacket(character.HealthMultiplier, CompressionHelper.DefaultFloatValueCompressionInfo);
		}

		private AvailableCharacter ReadAvailableCharacterFromPacket(ref bool bufferReadValid)
		{
			AvailableCharacter character = new AvailableCharacter();
			character.Index = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			character.CharacterId = ((BasicCharacterObject)ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid)).StringId;
			character.Officer = ReadBoolFromPacket(ref bufferReadValid);
			character.SpawnCount = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			character.IsPercentage = ReadBoolFromPacket(ref bufferReadValid);
			character.Difficulty = (Difficulty)ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			character.HealthMultiplier = ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid);
			return character;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}
	}
}
