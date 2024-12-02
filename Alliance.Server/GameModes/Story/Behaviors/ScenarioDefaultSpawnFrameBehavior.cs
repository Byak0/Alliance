using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.GameModes.Story.Behaviors
{
	/// <summary>
	/// This class is called natively when spawning if no location was specified.
	/// It will return default locations for each team based on current act spawn logic.
	/// </summary>
	public class ScenarioDefaultSpawnFrameBehavior : SpawnFrameBehaviorBase, ISpawnFrameBehavior
	{
		private const string DefaultAttackerTag = "attacker";
		private const string DefaultDefenderTag = "defender";
		private const string StartingTag = "starting";
		private const string ExcludeMountedTag = "exclude_mounted";

		// Lists of spawn points and zones for each team
		private IEnumerable<GameEntity>[] _spawnPointsByTeam;
		private IEnumerable<GameEntity>[] _spawnZonesByTeam;

		public ScenarioDefaultSpawnFrameBehavior()
		{
		}

		public override void Initialize()
		{
			SpawnPoints = Mission.Current.Scene.FindEntitiesWithTag("spawnpoint");

			// Initialize spawn points and zones
			_spawnPointsByTeam = new IEnumerable<GameEntity>[2];
			_spawnZonesByTeam = new IEnumerable<GameEntity>[2];

			if (ScenarioManagerServer.Instance.CurrentAct?.SpawnLogic == null) return;

			string customAttackerTag = ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.DefaultSpawnTags[(int)BattleSideEnum.Attacker];
			string customDefenderTag = ScenarioManagerServer.Instance.CurrentAct.SpawnLogic.DefaultSpawnTags[(int)BattleSideEnum.Defender];

			// Fall back to default tags if custom tags are empty
			if (string.IsNullOrEmpty(customAttackerTag))
				customAttackerTag = DefaultAttackerTag;

			if (string.IsNullOrEmpty(customDefenderTag))
				customDefenderTag = DefaultDefenderTag;

			// Set attacker and defender spawn points
			SetSpawnPointsForTeam(customAttackerTag, BattleSideEnum.Attacker);
			SetSpawnPointsForTeam(customDefenderTag, BattleSideEnum.Defender);

			// Set attacker and defender spawn zones
			_spawnZonesByTeam[1] = GetSpawnZonesForTeam(BattleSideEnum.Attacker);
			_spawnZonesByTeam[0] = GetSpawnZonesForTeam(BattleSideEnum.Defender);
		}

		private void SetSpawnPointsForTeam(string teamTag, BattleSideEnum teamSide)
		{
			List<GameEntity> teamSpawnPoints = SpawnPoints.Where(sp => sp.HasTag(teamTag)).ToList();

			_spawnPointsByTeam[(int)teamSide] = teamSpawnPoints.Count > 0 ? teamSpawnPoints : SpawnPoints;
		}

		private IEnumerable<GameEntity> GetSpawnZonesForTeam(BattleSideEnum teamSide)
		{
			return _spawnPointsByTeam[(int)teamSide]
				.Select(spawnPoint => spawnPoint.Parent)
				.Where(spawnZone => spawnZone != null)
				.Distinct()
				.ToList();
		}

		public override MatrixFrame GetSpawnFrame(Team team, bool hasMount, bool isInitialSpawn)
		{
			GameEntity bestZone = GetBestZone(team, isInitialSpawn);

			List<GameEntity> spawnPoints = bestZone != null
				? _spawnPointsByTeam[(int)team.Side].Where(sp => sp.Parent == bestZone).ToList()
				: _spawnPointsByTeam[(int)team.Side].ToList();

			return GetBestSpawnPoint(spawnPoints, hasMount);
		}

		private GameEntity GetBestZone(Team team, bool isInitialSpawn)
		{
			// Check if there are any spawn zones for the given team
			if (!_spawnZonesByTeam[(int)team.Side].Any())
			{
				return null;
			}

			// Handle initial spawn
			if (isInitialSpawn)
			{
				GameEntity initialSpawnZone = _spawnZonesByTeam[(int)team.Side]
					.SingleOrDefault(sz => sz.HasTag(StartingTag) && sz.IsVisibleIncludeParents());

				if (initialSpawnZone != null)
				{
					return initialSpawnZone;
				}
			}

			// Get a list of spawn zones excluding the initial spawn zones
			List<GameEntity> spawnZones = _spawnZonesByTeam[(int)team.Side]
				.Where(sz => !sz.HasTag(StartingTag) && sz.IsVisibleIncludeParents())
				.ToList();

			if (!spawnZones.Any())
			{
				return null;
			}

			float[] spawnZoneScores = new float[spawnZones.Count];

			// Calculate the score for each spawn zone. We try to spawn the agent close to its team members
			foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
			{
				MissionPeer missionPeer = networkCommunicator.GetComponent<MissionPeer>();

				if (missionPeer?.Team != null && missionPeer.Team.Side != BattleSideEnum.None && missionPeer.ControlledAgent != null && missionPeer.ControlledAgent.IsActive())
				{
					for (int i = 0; i < spawnZones.Count; i++)
					{
						Vec3 spawnZonePosition = spawnZones[i].GlobalPosition;
						float distance = missionPeer.ControlledAgent.Position.Distance(spawnZonePosition);
						float influence = 1f / (0.0001f + distance);

						if (missionPeer.Team != team)
						{
							spawnZoneScores[i] -= influence;
						}
						else
						{
							spawnZoneScores[i] += influence * 1.5f;
						}
					}
				}
			}

			// Get the spawn zone with the highest score
			int bestZoneIndex = -1;
			for (int i = 0; i < spawnZoneScores.Length; i++)
			{
				if (bestZoneIndex < 0 || spawnZoneScores[i] > spawnZoneScores[bestZoneIndex])
				{
					bestZoneIndex = i;
				}
			}

			// Return the best spawn zone
			return spawnZones[bestZoneIndex];
		}

		private MatrixFrame GetBestSpawnPoint(List<GameEntity> spawnPointList, bool hasMount)
		{
			float bestSpawnScore = float.MinValue;
			int bestSpawnIndex = -1;
			int nbAgentsAtBest = 0;
			MBList<Agent> agents = (MBList<Agent>)Mission.Current.Agents;
			for (int i = 0; i < spawnPointList.Count; i++)
			{
				if (!spawnPointList[i].IsVisibleIncludeParents())
				{
					continue;
				}

				float spawnScore = MBRandom.RandomFloat * 0.2f;
				int nbAgents = 0;
				foreach (Agent agent in Mission.Current.GetNearbyAgents(spawnPointList[i].GlobalPosition.AsVec2, 2f, agents))
				{
					nbAgents++;
					float lengthSquared = (agent.Position - spawnPointList[i].GlobalPosition).LengthSquared;
					if (lengthSquared < 4f)
					{
						float length = MathF.Sqrt(lengthSquared);
						spawnScore -= (2f - length) * 5f;
					}
				}
				if (hasMount && spawnPointList[i].HasTag(ExcludeMountedTag))
				{
					spawnScore -= 100f;
				}
				if (spawnScore > bestSpawnScore)
				{
					nbAgentsAtBest = nbAgents;
					bestSpawnScore = spawnScore;
					bestSpawnIndex = i;
				}
			}
			MatrixFrame globalFrame = spawnPointList[bestSpawnIndex].GetGlobalFrame();

			// If there are too many agents at the best spawn point, slightly move the spawn point to the side
			if (nbAgentsAtBest > 50)
			{
				Log("Too much agents already spawned at this position, spawning slightly to the side.", LogLevel.Warning);
				globalFrame.Advance(MBRandom.RandomFloatRanged(-10f, 10f));
				globalFrame.Strafe(MBRandom.RandomFloatRanged(-10f, 10f));
			}

			globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();

			return globalFrame;
		}

		// Returns the closest spawn frame from a specified position
		public MatrixFrame GetClosestSpawnFrame(Team team, bool hasMount, bool isInitialSpawn, MatrixFrame spawnPos)
		{
			GameEntity bestZone = GetBestZone(team, isInitialSpawn);

			List<GameEntity> spawnPoints = bestZone != null
				? _spawnPointsByTeam[(int)team.Side].Where(sp => sp.Parent == bestZone).ToList()
				: _spawnPointsByTeam[(int)team.Side].ToList();

			// Call the new method that calculates the best spawn frame based on proximity
			return GetBestClosestSpawnFrame(spawnPoints, hasMount, spawnPos);
		}

		// Calculates the spawn frame that is closest to the specified spawn position
		private MatrixFrame GetBestClosestSpawnFrame(List<GameEntity> spawnPoints, bool hasMount, MatrixFrame spawnPos)
		{
			float bestSpawnScore = float.MinValue;
			int bestSpawnIndex = -1;
			int nbAgentsAtBest = 0;
			MBList<Agent> agents = (MBList<Agent>)Mission.Current.Agents;

			for (int i = 0; i < spawnPoints.Count; i++)
			{
				if (!spawnPoints[i].IsVisibleIncludeParents())
				{
					continue;
				}

				float spawnScore = 0;

				// The farther away the spawn point is from the desired position, the lower it scores
				spawnScore -= Math.Abs((spawnPos.origin - spawnPoints[i].GlobalPosition).Length);

				// The more agents are already close to the spawn point, the lower it scores
				int nbAgents = Mission.Current.GetNearbyAgents(spawnPoints[i].GlobalPosition.AsVec2, 5f, agents).Count;
				//spawnScore -= nbAgents;
				// Test to spawn one by one
				spawnScore -= nbAgents * 5;

				// If the agent has a mount and the spawn point has an "exclude_mounted" tag, it scores very low
				if (hasMount && spawnPoints[i].HasTag(ExcludeMountedTag))
				{
					spawnScore -= 100f;
				}

				// Keep track of the spawn point with the highest score
				if (spawnScore > bestSpawnScore)
				{
					nbAgentsAtBest = nbAgents;
					bestSpawnScore = spawnScore;
					bestSpawnIndex = i;
				}
			}

			MatrixFrame globalFrame = spawnPoints[bestSpawnIndex].GetGlobalFrame();

			// If there are too many agents at the best spawn point, slightly move the spawn point to the side
			if (nbAgentsAtBest > 50)
			{
				Log("Too much agents already spawned at this position, spawning slightly to the side.", LogLevel.Warning);
				globalFrame.Advance(MBRandom.RandomFloatRanged(-10f, 10f));
				globalFrame.Strafe(MBRandom.RandomFloatRanged(-10f, 10f));
			}

			globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();

			return globalFrame;
		}
	}
}
