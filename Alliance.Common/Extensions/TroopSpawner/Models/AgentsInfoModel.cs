using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.TroopSpawner.Models
{
	/// <summary>
	/// Singleton class to store various informations about Agents.
	/// Helps to ensure that each agent uses its own slot to prevent engine from crashing at spawn.
	/// Access it with AgentsInfoModel.Instance.Agents[agent.Index]
	/// </summary>
	public sealed class AgentsInfoModel
	{
		public const float DEFAULT_DIFFICULTY = 1f;
		public const int DEFAULT_LIVES = 1;
		public const int DEFAULT_SPEAKING_RANGE = 30;
		private const int DEFAULT_EXPIRATION_TIMER = 30;

		public ConcurrentDictionary<int, AgentInfo> Agents { get; private set; }
		private const int RESERVED_SLOTS = 500;
		private const int TOTAL_SLOTS = 2000;

		/// <summary>
		/// Return any number of available slots, whether they are consecutive or not.
		/// Use this to define agentBuildData.Index and ensure the agent you are spawning won't crash the engine.
		/// </summary>
		/// <returns>The first slots available, or empty list if no slot available</returns>
		public List<int> GetAvailableSlotIndex(int requiredSlots = 1)
		{
			List<int> availableSlots = new List<int>();

			for (int i = RESERVED_SLOTS; i < Agents.Count; i++)
			{
				if (!Agents.ContainsKey(i) || Agents[i].Agent == null)
				{
					availableSlots.Add(i);
					if (availableSlots.Count == requiredSlots)
					{
						return availableSlots;
					}
				}
			}

			return new List<int>(); // Return an empty list if enough slots are not available
		}

		public int GetAvailableSlotCount()
		{
			int availableSlots = 0;
			for (int i = RESERVED_SLOTS; i < Agents.Count; i++)
			{
				if (Agents[i].Agent == null)
				{
					availableSlots++;
				}
			}
			return availableSlots;
		}

		/// <summary>
		/// Add agent informations to the model.
		/// Use <see cref="GetAvailableSlotIndex"/> to retrieve an available slot before creating the Agent.
		/// </summary>
		/// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
		public void AddAgentInfo(Agent agent, float diff = DEFAULT_DIFFICULTY, int lives = DEFAULT_LIVES, int speakingRange = DEFAULT_SPEAKING_RANGE, bool synchronize = false)
		{
			Agents[agent.Index] = new AgentInfo(agent, diff, lives, speakingRange);
			if (synchronize) SynchronizeAgentValue(agent, AgentDataType.All);
		}

		/// <summary>
		/// Update agent difficulty.
		/// </summary>
		/// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
		public void UpdateAgentDifficulty(Agent agent, float diff = DEFAULT_DIFFICULTY, bool synchronize = false)
		{
			Agents[agent.Index].Difficulty = diff;
			if (synchronize) SynchronizeAgentValue(agent, AgentDataType.Difficulty);
		}

		/// <summary>
		/// Update agent speaking range.
		/// </summary>
		/// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
		public void UpdateAgentSpeakingRange(Agent agent, int speakingRange = DEFAULT_SPEAKING_RANGE, bool synchronize = false)
		{
			Agents[agent.Index].SpeakingRange = speakingRange;
			if (synchronize) SynchronizeAgentValue(agent, AgentDataType.SpeakingRange);
		}

		/// <summary>
		/// Update agent lives.
		/// </summary>
		/// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
		public void UpdateAgentLives(Agent agent, int lives = DEFAULT_LIVES, bool synchronize = false)
		{
			Agents[agent.Index].Lives = lives;
			if (synchronize) SynchronizeAgentValue(agent, AgentDataType.Lives);
		}

		public void SynchronizeAgentValue(Agent agent, AgentDataType dataType)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new AgentsInfoMessage(agent.Index, dataType, Agents[agent.Index]));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public void ClearAllAgentInfos()
		{
			for (int i = 0; i < Agents.Count; i++)
			{
				if (Agents[i].Agent != null)
				{
					MarkAgentInfoAsExpiredWithDelay(Agents[i].Agent.Index);
				}
			}
		}

		public void MarkAgentInfoAsExpiredWithDelay(Agent agent, int delay = DEFAULT_EXPIRATION_TIMER)
		{
			MarkAgentInfoAsExpiredWithDelay(agent.Index, delay);
		}

		public void MarkAgentInfoAsExpiredWithDelay(int agentIndex, int delay = DEFAULT_EXPIRATION_TIMER)
		{
			Agents[agentIndex].ExpirationTimer = delay;
			Log($"Marking agent n.{agentIndex} as expired in {delay}s", LogLevel.Debug);
		}

		public void CheckAndRemoveExpiredAgents()
		{
			for (int i = 0; i < Agents.Count; i++)
			{
				if (Agents[i].ExpirationTimer > 0)
				{
					Agents[i].ExpirationTimer--;
				}
				else if (Agents[i].ExpirationTimer == 0)
				{
					Log($"Removing expired agent n.{i}", LogLevel.Debug);
					Agents[i] = new AgentInfo(null);
				}
			}
		}

		static AgentsInfoModel()
		{
			instance.Agents = new ConcurrentDictionary<int, AgentInfo>();
			for (int i = 0; i < TOTAL_SLOTS; i++)
			{
				instance.Agents[i] = new AgentInfo(null);
			}
		}

		private static readonly AgentsInfoModel instance = new();
		public static AgentsInfoModel Instance { get { return instance; } }
	}

	public class AgentInfo
	{
		public AgentInfo(Agent agent, float diff = AgentsInfoModel.DEFAULT_DIFFICULTY, int lives = AgentsInfoModel.DEFAULT_LIVES, int speakingRange = AgentsInfoModel.DEFAULT_SPEAKING_RANGE, int expirationTimer = -1)
		{
			Agent = agent;
			Difficulty = diff;
			Lives = lives;
			SpeakingRange = speakingRange;
			ExpirationTimer = expirationTimer;
		}

		/// <summary>
		/// Agent reference.
		/// </summary>
		public readonly Agent Agent;

		/// <summary>
		/// Difficulty modifier, impacts the AI behavior and skills.
		/// </summary>
		/// <value>
		/// Between 0.5 (easy) and 2.5 (hardest).
		/// </value>
		public float Difficulty;

		/// <summary>
		/// Number of lives that this agent possess.
		/// </summary>
		public int Lives;

		/// <summary>
		/// Speaking range of the agent, used for VOIP.
		/// </summary>
		public int SpeakingRange;

		/// <summary>
		/// Set this to free the agent slot after the timer expires.
		/// </summary>
		/// <value>
		/// In seconds. -1 means no expiration.
		/// </value>
		public int ExpirationTimer;
	}

	public enum AgentDataType
	{
		None,
		Difficulty,
		Lives,
		SpeakingRange,
		All
	}
}