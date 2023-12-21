using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.TroopSpawner.Models
{
    /// <summary>
    /// Singleton class to store various informations about Agents.
    /// Helps to ensure that each agent uses its own slot to prevent engine from crashing at spawn.
    /// Access it with AgentsInfoModel.Instance.Agents[agent.Index]
    /// </summary>
    public sealed class AgentsInfoModel
    {
        public Dictionary<int, AgentInfo> Agents { get => agents; private set => agents = value; }

        private Dictionary<int, AgentInfo> agents;

        public class AgentInfo
        {
            public AgentInfo(Agent agent, float diff, int exp, int lives)
            {
                Agent = agent;
                Difficulty = diff;
                Experience = exp;
                Lives = lives;
            }

            /// <summary>
            /// Agent reference.
            /// </summary>
            public readonly Agent Agent;

            /// <summary>
            /// Difficulty modifier, impacts the AI behavior and skills.
            /// </summary>
            /// <value>
            /// Between 0.5 (easy) and 2.5 (hardest). Default value is 1f.
            /// </value>
            public float Difficulty;

            /// <summary>
            /// Experience level, might have an use later.
            /// </summary>
            public int Experience;

            /// <summary>
            /// Number of lives that this agent possess.
            /// </summary>
            public int Lives;
        }

        /// <summary>
        /// Return any number of available slots, whether they are consecutive or not.
        /// Use this to define agentBuildData.Index and ensure the agent you are spawning won't crash the engine.
        /// </summary>
        /// <returns>The first slots available, or empty list if no slot available</returns>
        public List<int> GetAvailableSlotIndex(int requiredSlots = 1)
        {
            List<int> availableSlots = new List<int>();

            for (int i = 500; i < Agents.Count; i++)
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

        /// <summary>
        /// Add agent informations to the model.
        /// Use <see cref="GetAvailableSlotIndex"/> to retrieve an available slot before creating the Agent.
        /// </summary>
        /// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
        public void AddAgentInfo(Agent agent, float diff = 1f, int exp = 0, int lives = 0, bool synchronize = false)
        {
            Agents[agent.Index] = new AgentInfo(agent, diff, exp, lives);
            if (synchronize)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new AgentsInfoMessage(agent.Index, DataType.All, diff, exp, lives));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        /// <summary>
        /// Update agent difficulty.
        /// </summary>
        /// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
        public void UpdateAgentDifficulty(Agent agent, float diff = 1f, bool synchronize = false)
        {
            Agents[agent.Index].Difficulty = diff;
            if (synchronize)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new AgentsInfoMessage(agent.Index, DataType.Difficulty, difficulty: diff));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        /// <summary>
        /// Update agent experience.
        /// </summary>
        /// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
        public void UpdateAgentExperience(Agent agent, int exp = 0, bool synchronize = false)
        {
            Agents[agent.Index].Experience = exp;
            if (synchronize)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new AgentsInfoMessage(agent.Index, DataType.Experience, experience: exp));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        /// <summary>
        /// Update agent lives.
        /// </summary>
        /// <param name="synchronize">Set this to true if you want to synchronize with all clients</param>
        public void UpdateAgentLives(Agent agent, int lives = 0, bool synchronize = false)
        {
            Agents[agent.Index].Lives = lives;
            if (synchronize)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new AgentsInfoMessage(agent.Index, DataType.Lives, lives: lives));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public void RemoveAgentInfo(Agent agent)
        {
            Agents[agent.Index] = new AgentInfo(null, 1f, 0, 0);
        }

        public void RemoveAgentInfo(int agentIndex)
        {
            Agents[agentIndex] = new AgentInfo(null, 1f, 0, 0);
        }

        static AgentsInfoModel()
        {
            instance.Agents = new Dictionary<int, AgentInfo>();
            for (int i = 0; i < 2045; i++)
            {
                instance.Agents[i] = new AgentInfo(null, 1f, 0, 0);
            }
        }

        private static readonly AgentsInfoModel instance = new();
        public static AgentsInfoModel Instance { get { return instance; } }
    }
}