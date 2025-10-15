using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.PlayerSpawn.Behaviors;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace Alliance.Server.GameModes.Lobby.Behaviors
{
	/// <summary>
	/// Simple spawn behavior for the Lobby.    
	/// </summary>
	public class LobbySpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
	{
		private PlayerSpawnBehavior _playerSpawnBehavior;
		private float _lastSpawnCheck;
		private float _timeBeforeSpawn;
		private static Random _random = new Random();
		private static List<float> _values = new List<float> { 0.5f, 1f, 1.5f, 2f, 2.5f };

		public LobbySpawningBehavior()
		{
		}

		public override void OnTick(float dt)
		{
			if (_playerSpawnBehavior == null || !_playerSpawnBehavior.SpawnInProgress)
			{
				return;
			}

			_lastSpawnCheck += dt;
			if (_lastSpawnCheck >= 1)
			{
				_lastSpawnCheck = 0;
				SpawnAgents();
			}
		}

		public override void Initialize(SpawnComponent spawnComponent)
		{
			base.Initialize(spawnComponent);
			_playerSpawnBehavior = Mission.Current.GetMissionBehavior<PlayerSpawnBehavior>();
			_timeBeforeSpawn = MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue();
		}

		protected override void SpawnAgents()
		{
			// Spawn max 20 players at once
			int playersSpawn = 0;
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				MissionPeer missionPeer = peer.GetComponent<MissionPeer>();

				// If the peer is not a mission peer or already has a controlled agent, skip
				if (missionPeer == null || missionPeer.ControlledAgent != null)
				{
					continue;
				}

				PlayerAssignment playerAssignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(peer);

				// If the player has not chosen his character or can't spawn or its not time to spawn yet, skip
				if (playerAssignment?.Character == null || !playerAssignment.CanSpawn || playerAssignment.TimeBeforeSpawn > 0f)
				{
					continue;
				}

				if (peer.IsSynchronized && (missionPeer.Team == Mission.AttackerTeam || missionPeer.Team == Mission.DefenderTeam))
				{
					BasicCultureObject culture = playerAssignment.Formation.MainCulture;
					BasicCharacterObject basicCharacterObject = playerAssignment.Character.Character;
					MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = playerAssignment.Character.Character.GetHeroClass();
					MPOnSpawnPerkHandler onSpawnPerkHandler = GetOnSpawnPerkHandler(SpawnHelper.GetPerks(mPHeroClassForPeer, playerAssignment.Perks));
					SpawnHelper.SpawnPlayer(peer, onSpawnPerkHandler, basicCharacterObject, customCulture: culture);
					playersSpawn++;
				}
				if (playersSpawn >= 20) break;
			}

			// Spawn bots in random formations from PlayerSpawnMenu
			int nbBotsToSpawnAtt = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
			PlayerTeam teamAttack = PlayerSpawnMenu.Instance.Teams.FirstOrDefault(t => t.TeamSide == BattleSideEnum.Attacker);
			if (teamAttack != null && teamAttack.Formations.Count > 0)
			{
				for (int i = Mission.AttackerTeam.ActiveAgents.Count; i < nbBotsToSpawnAtt; i++)
				{
					PlayerFormation randomFormation = teamAttack.Formations.GetRandomElement();
					BasicCharacterObject troopCharacter = randomFormation.AvailableCharacters.GetRandomElement().Character;

					// Random difficulty between 0.5 and 2.5f
					float difficulty = _values[_random.Next(_values.Count)];
					SpawnHelper.SpawnBot(Mission.AttackerTeam, randomFormation.MainCulture, troopCharacter, botDifficulty: difficulty);
				}
			}

			int nbBotsToSpawnDef = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
			PlayerTeam teamDef = PlayerSpawnMenu.Instance.Teams.FirstOrDefault(t => t.TeamSide == BattleSideEnum.Defender);
			if (teamDef != null && teamDef.Formations?.Count > 0)
			{
				for (int i = Mission.DefenderTeam.ActiveAgents.Count; i < nbBotsToSpawnDef; i++)
				{
					PlayerFormation randomFormation = teamDef.Formations.GetRandomElement();
					BasicCharacterObject troopCharacter = randomFormation.AvailableCharacters.GetRandomElement()?.Character;
					// Skip if no character available
					if (troopCharacter == null) continue;

					// Random difficulty between 0.5 and 2.5f
					float difficulty = _values[_random.Next(_values.Count)];
					SpawnHelper.SpawnBot(Mission.DefenderTeam, randomFormation.MainCulture, troopCharacter, botDifficulty: difficulty);
				}
			}
		}

		protected override bool IsRoundInProgress()
		{
			return Mission.Current.CurrentState == Mission.State.Continuing;
		}

		public override bool AllowEarlyAgentVisualsDespawning(MissionPeer missionPeer)
		{
			return true;
		}

		public bool AllowExternalSpawn()
		{
			return IsRoundInProgress();
		}
	}
}