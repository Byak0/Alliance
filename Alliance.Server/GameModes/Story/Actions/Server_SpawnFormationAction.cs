using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
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
			_ = SpawnAsync();
		}

		private async Task SpawnAsync()
		{
			Team team = Side == BattleSideEnum.Defender ? Mission.Current.DefenderTeam : Mission.Current.AttackerTeam;
			string cultureId = Side == BattleSideEnum.Defender ? MultiplayerOptions.OptionType.CultureTeam2.GetStrValue() : MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
			BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureId);
			Formation formation = null;

			// Check if a player control this formation
			MissionPeer playerInCharge = FormationControlModel.Instance.GetControllerOfFormation(Formation, team);
			if (playerInCharge?.ControlledAgent == null)
			{
				int totalFormations = (int)FormationClass.NumberOfRegularFormations;

				// Get list of players in the same team, preferably commanders
				List<NetworkCommunicator> candidates = GameNetwork.NetworkPeers.Where(peer => peer.GetComponent<MissionPeer>()?.Team == team && peer.IsCommander()).ToList();
				if (candidates.Count == 0)
				{
					candidates = GameNetwork.NetworkPeers.Where(peer => peer.GetComponent<MissionPeer>()?.Team == team).ToList();
				}
				// Sort them by their index for consistent distribution
				candidates.OrderByQ(peer => peer.Index);
				// Limit the number of candidates to the number of formations
				int candidatesCount = Math.Min(candidates.Count, totalFormations);

				// Distribute the formation depending on number of players in the team
				if (candidatesCount > 1)
				{
					int baseFormationCountPerPlayer = totalFormations / candidatesCount;
					int remainingFormations = totalFormations % candidatesCount;

					int nbFormationAlreadyAssigned = 0;
					for (int i = 0; i < candidatesCount; i++)
					{
						// If there are remaining formations, assign one more to the first players
						int nbFormationForThisPlayer = baseFormationCountPerPlayer + (i < remainingFormations ? 1 : 0);

						if ((int)Formation < nbFormationAlreadyAssigned + nbFormationForThisPlayer)
						{
							playerInCharge = candidates[i].GetComponent<MissionPeer>();
							break;
						}
						nbFormationAlreadyAssigned += nbFormationForThisPlayer;
					}
				}
				else if (candidatesCount == 1)
				{
					playerInCharge = candidates[0].GetComponent<MissionPeer>();
				}

				if (playerInCharge != null)
				{
					FormationControlModel.Instance.AssignControlToPlayer(playerInCharge, Formation, true);
				}
				else
				{
					Log("No player eligible to control formation " + Formation, LogLevel.Warning);
				}
			}

			// Spawn the various characters
			foreach (CharacterToSpawn characterToSpawn in Characters)
			{
				BasicCharacterObject character = MBObjectManager.Instance.GetObject<BasicCharacterObject>(characterToSpawn.CharacterId);
				float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(characterToSpawn.Difficulty);
				int numberToSpawn = characterToSpawn.IsPercentage ? (int)((characterToSpawn.SpawnCount / 100f) * CoreUtils.CurrentPlayerCount) : characterToSpawn.SpawnCount;

				for (int i = 0; i < numberToSpawn; i++)
				{
					try
					{
						// Calculate random position in the SpawnZone
						Vec3 randomSpawnPosition = CoreUtils.GetRandomPositionWithinRadius(SpawnZone.GlobalPosition, SpawnZone.Radius);
						MatrixFrame position = new MatrixFrame(Mat3.Identity, randomSpawnPosition);
						Agent agent = await SpawnHelper.SpawnBotAsync(team, culture, character, position,
							selectedFormation: (int)Formation, botDifficulty: difficulty, healthMultiplier: characterToSpawn.HealthMultiplier);
						if (agent != null)
						{
							formation = agent.Formation;
						}
					}
					catch (OperationCanceledException)
					{
						Log("Spawn operation was canceled.", LogLevel.Warning);
						return;
					}
					catch (Exception ex)
					{
						Log($"Failed to spawn agent: {ex.Message}", LogLevel.Error);
						return;
					}
				}
			}

			// Only update formation if spawn succeeded and formation is not null.
			if (formation != null)
			{
				switch (MoveOrder)
				{
					case MoveOrderType.Move:
						Vec3 randomTargetPosition = CoreUtils.GetRandomPositionWithinRadius(Direction.GlobalPosition, Direction.Radius);
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