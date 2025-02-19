﻿using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Server.Core.Utils;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for spawning agents.
	/// </summary>
	public class Server_SpawnFormationAction : SpawnFormationAction
	{
		public Server_SpawnFormationAction() : base() { }

		public override void Execute()
		{
			Spawn();
		}

		private async void Spawn()
		{
			Team team = Side == BattleSideEnum.Defender ? Mission.Current.DefenderTeam : Mission.Current.AttackerTeam;
			string cultureId = Side == BattleSideEnum.Defender ? MultiplayerOptions.OptionType.CultureTeam2.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureId);
			Formation formation = null;

			// Spawn the various characters
			foreach (CharacterToSpawn characterToSpawn in Characters)
			{
				BasicCharacterObject character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(characterToSpawn.Character);
				float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(characterToSpawn.Difficulty);

				for (int i = 0; i < characterToSpawn.Number; i++)
				{
					try
					{
						// Calculate random position in the SpawnZone
						Vec3 randomSpawnPosition = CoreUtils.GetRandomPositionWithinRadius(SpawnZone.Position, SpawnZone.Radius);
						MatrixFrame position = new MatrixFrame(Mat3.Identity, randomSpawnPosition);
						Agent agent = await SpawnHelper.SpawnBotAsync(team, culture, character, position, selectedFormation: (int)Formation, botDifficulty: difficulty, healthMultiplier: characterToSpawn.HealthMultiplier);
						formation = agent.Formation;
					}
					catch (Exception ex)
					{
						Log($"Failed to spawn agent: {ex.Message}", LogLevel.Error);
						continue;
					}
				}
			}

			// Set formation order and disposition
			if (formation != null)
			{
				switch (MoveOrder)
				{
					case MoveOrderType.Move:
						// Calculate random position in the target zone
						Vec3 randomTargetPosition = CoreUtils.GetRandomPositionWithinRadius(Direction.Position, Direction.Radius);
						WorldPosition target = randomTargetPosition.ToWorldPosition(Mission.Current.Scene);
						formation.SetMovementOrder(MovementOrder.MovementOrderMove(target));
						break;
					case MoveOrderType.Charge:
						formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
						break;
					case MoveOrderType.Stop:
						formation.SetMovementOrder(MovementOrder.MovementOrderStop);
						break;
					case MoveOrderType.Retreat:
						formation.SetMovementOrder(MovementOrder.MovementOrderRetreat);
						break;
					case MoveOrderType.FallBack:
						formation.SetMovementOrder(MovementOrder.MovementOrderFallBack);
						break;
					case MoveOrderType.Advance:
						formation.SetMovementOrder(MovementOrder.MovementOrderAdvance);
						break;
				}

				switch (Disposition)
				{
					case ArrangementOrder.ArrangementOrderEnum.Line:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLine;
						break;
					case ArrangementOrder.ArrangementOrderEnum.Loose:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderLoose;
						break;
					case ArrangementOrder.ArrangementOrderEnum.ShieldWall:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderShieldWall;
						break;
					case ArrangementOrder.ArrangementOrderEnum.Square:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSquare;
						break;
					case ArrangementOrder.ArrangementOrderEnum.Scatter:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderScatter;
						break;
					case ArrangementOrder.ArrangementOrderEnum.Circle:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderCircle;
						break;
					case ArrangementOrder.ArrangementOrderEnum.Column:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderColumn;
						break;
					case ArrangementOrder.ArrangementOrderEnum.Skein:
						formation.ArrangementOrder = ArrangementOrder.ArrangementOrderSkein;
						break;
				}
			}
		}
	}
}