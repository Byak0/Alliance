using Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors;
using Alliance.Common.Extensions.AdvancedCombat.AgentComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	public class AL_MissionAgentHandler : MissionLogic
	{
		public AL_MissionAgentHandler()
		{
			_usablePoints = new Dictionary<string, List<UsableMachine>>();
			_pairedUsablePoints = new Dictionary<string, List<UsableMachine>>();
		}

		public override void EarlyStart()
		{
			CollectUsablePoints();
		}

		private void CollectUsablePoints()
		{
			_usablePoints.Clear();
			foreach (UsableMachine usableMachine in Mission.MissionObjects.FindAllWithType<UsableMachine>())
			{
				if (!usableMachine.IsDeactivated)
				{
					foreach (string key in usableMachine.GameEntity.Tags)
					{
						if (!_usablePoints.ContainsKey(key))
						{
							_usablePoints.Add(key, new List<UsableMachine>());
						}
						_usablePoints[key].Add(usableMachine);
					}
				}
			}
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			foreach (Agent agent in Mission.Agents)
			{
				DefaultAgentComponent component = agent.GetComponent<DefaultAgentComponent>();
				if (component != null)
				{
					component.OnAgentRemoved(affectedAgent);
				}
			}
		}

		public override void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
		{
			if (!atStart)
			{
				foreach (Agent agent in Mission.Agents)
				{
					if (agent.IsHuman)
					{
						agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, agent.CurrentWatchState != Agent.WatchState.Alarmed);
					}
				}
			}
		}

		public void RemoveDeactivatedUsablePlacesFromList()
		{
			Dictionary<string, List<UsableMachine>> dictionary = new Dictionary<string, List<UsableMachine>>();
			foreach (KeyValuePair<string, List<UsableMachine>> keyValuePair in _usablePoints)
			{
				foreach (UsableMachine usableMachine in keyValuePair.Value)
				{
					if (usableMachine.IsDeactivated)
					{
						if (dictionary.ContainsKey(keyValuePair.Key))
						{
							dictionary[keyValuePair.Key].Add(usableMachine);
						}
						else
						{
							dictionary.Add(keyValuePair.Key, new List<UsableMachine>());
							dictionary[keyValuePair.Key].Add(usableMachine);
						}
					}
				}
			}
			foreach (KeyValuePair<string, List<UsableMachine>> keyValuePair2 in dictionary)
			{
				foreach (UsableMachine usableMachine2 in keyValuePair2.Value)
				{
					_usablePoints[keyValuePair2.Key].Remove(usableMachine2);
				}
			}
		}

		public void SimulateAgent(Agent agent)
		{
			if (agent.IsHuman)
			{
				AL_AgentNavigator agentNavigator = agent.GetComponent<DefaultAgentComponent>().AgentNavigator;
				int num = MBRandom.RandomInt(35, 50);
				agent.PreloadForRendering();
				for (int i = 0; i < num; i++)
				{
					if (agentNavigator != null)
					{
						agentNavigator.Tick(0.1f, true);
					}
					if (agent.IsUsingGameObject)
					{
						agent.CurrentlyUsedGameObject.SimulateTick(0.1f);
					}
				}
			}
		}

		public IEnumerable<string> GetAllSpawnTags()
		{
			return Enumerable.Concat<string>(Enumerable.ToList<string>(_usablePoints.Keys), Enumerable.ToList<string>(_pairedUsablePoints.Keys));
		}

		public List<UsableMachine> GetAllUsablePointsWithTag(string tag)
		{
			List<UsableMachine> list = new List<UsableMachine>();
			List<UsableMachine> list2 = new List<UsableMachine>();
			if (_usablePoints.TryGetValue(tag, out list2))
			{
				list.AddRange(list2);
			}
			List<UsableMachine> list3 = new List<UsableMachine>();
			if (_pairedUsablePoints.TryGetValue(tag, out list3))
			{
				list.AddRange(list3);
			}
			return list;
		}

		public static uint GetRandomTournamentTeamColor(int teamIndex)
		{
			return _tournamentTeamColors[teamIndex % _tournamentTeamColors.Length];
		}

		public UsableMachine FindUnusedPointWithTagForAgent(Agent agent, string tag)
		{
			return FindUnusedPointForAgent(agent, _pairedUsablePoints, tag) ?? FindUnusedPointForAgent(agent, _usablePoints, tag);
		}

		private UsableMachine FindUnusedPointForAgent(Agent agent, Dictionary<string, List<UsableMachine>> usableMachinesList, string primaryTag)
		{
			List<UsableMachine> list;
			if (usableMachinesList.TryGetValue(primaryTag, out list) && list.Count > 0)
			{
				int num = MBRandom.RandomInt(0, list.Count);
				for (int i = 0; i < list.Count; i++)
				{
					UsableMachine usableMachine = list[(num + i) % list.Count];
					if (!usableMachine.IsDisabled && !usableMachine.IsDestroyed && usableMachine.IsStandingPointAvailableForAgent(agent))
					{
						return usableMachine;
					}
				}
			}
			return null;
		}

		public List<UsableMachine> FindAllUnusedPoints(Agent agent, string primaryTag)
		{
			List<UsableMachine> list = new List<UsableMachine>();
			List<UsableMachine> list2 = new List<UsableMachine>();
			List<UsableMachine> list3;
			_usablePoints.TryGetValue(primaryTag, out list3);
			List<UsableMachine> list4;
			_pairedUsablePoints.TryGetValue(primaryTag, out list4);
			list4 = ((list4 != null) ? Enumerable.ToList<UsableMachine>(Enumerable.Distinct<UsableMachine>(list4)) : null);
			if (list3 != null && list3.Count > 0)
			{
				list.AddRange(list3);
			}
			if (list4 != null && list4.Count > 0)
			{
				list.AddRange(list4);
			}
			if (list.Count > 0)
			{
				Predicate<StandingPoint> spPredicate = (StandingPoint sp) => (sp.IsInstantUse || (!sp.HasUser && !sp.HasAIMovingTo)) && !sp.IsDisabledForAgent(agent);
				foreach (UsableMachine usableMachine in list)
				{
					List<StandingPoint> standingPoints = usableMachine.StandingPoints;
					//Predicate<StandingPoint> predicate;
					//if ((predicate = spPredicate) == null)
					//{
					//	predicate = (spPredicate = (StandingPoint sp) => (sp.IsInstantUse || (!sp.HasUser && !sp.HasAIMovingTo)) && !sp.IsDisabledForAgent(agent));
					//}
					if (standingPoints.Exists(spPredicate))
					{
						list2.Add(usableMachine);
					}
				}
			}
			return list2;
		}

		public void RemovePropReference(List<GameEntity> props)
		{
			foreach (KeyValuePair<string, List<UsableMachine>> keyValuePair in _usablePoints)
			{
				foreach (GameEntity gameEntity in props)
				{
					if (gameEntity.HasTag(keyValuePair.Key))
					{
						UsableMachine firstScriptOfType = gameEntity.GetFirstScriptOfType<UsableMachine>();
						keyValuePair.Value.Remove(firstScriptOfType);
					}
				}
			}
			foreach (KeyValuePair<string, List<UsableMachine>> keyValuePair2 in _pairedUsablePoints)
			{
				foreach (GameEntity gameEntity2 in props)
				{
					if (gameEntity2.HasTag(keyValuePair2.Key))
					{
						UsableMachine firstScriptOfType2 = gameEntity2.GetFirstScriptOfType<UsableMachine>();
						keyValuePair2.Value.Remove(firstScriptOfType2);
					}
				}
			}
		}

		public void AddPropReference(List<GameEntity> props)
		{
			foreach (KeyValuePair<string, List<UsableMachine>> keyValuePair in _usablePoints)
			{
				foreach (GameEntity gameEntity in props)
				{
					UsableMachine firstScriptOfType = gameEntity.GetFirstScriptOfType<UsableMachine>();
					if (firstScriptOfType != null && gameEntity.HasTag(keyValuePair.Key))
					{
						keyValuePair.Value.Add(firstScriptOfType);
					}
				}
			}
		}

		private const float PassageUsageDeltaTime = 30f;

		private static readonly uint[] _tournamentTeamColors = new uint[]
		{
			4294110933U, 4290269521U, 4291535494U, 4286151096U, 4290286497U, 4291600739U, 4291868275U, 4287285710U, 4283204487U, 4287282028U,
			4290300789U
		};

		private static readonly uint[] _villagerClothColors = new uint[]
		{
			4292860590U, 4291351206U, 4289117081U, 4288460959U, 4287541416U, 4288922566U, 4292654718U, 4289243320U, 4290286483U, 4290288531U,
			4290156159U, 4291136871U, 4289233774U, 4291205980U, 4291735684U, 4292722283U, 4293119406U, 4293911751U, 4294110933U, 4291535494U,
			4289955192U, 4289631650U, 4292133587U, 4288785593U, 4286288275U, 4286222496U, 4287601851U, 4286622134U, 4285898909U, 4285638289U,
			4289830302U, 4287593853U, 4289957781U, 4287071646U, 4284445583U
		};

		private static int _disabledFaceId = -1;

		private readonly Dictionary<string, List<UsableMachine>> _usablePoints;

		private readonly Dictionary<string, List<UsableMachine>> _pairedUsablePoints;
	}
}
