using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Actions
{
	[OverrideAction(typeof(SetVoipRangeAction))]
	public class Server_SetVoipRangeAction : SetVoipRangeAction
	{
		// Create a dictionary with modified agents and their original speaking range
		Dictionary<Agent, int> AgentsWithCustomRange = new Dictionary<Agent, int>();

		public override ActionTask Execute()
		{
			if (Mission.Current == null) return ActionTask.CompletedTask;
			TriggerContext ctx = ScenarioManager.Instance.CurrentTriggerContext;
			VariableStore globals = ScenarioManager.Instance.Globals;
			int voipRange = VOIP_Range?.Resolve(ctx, globals) ?? 0;
			List<Agent> targets = Who?.Resolve(ctx, globals) ?? new List<Agent>();
			// Set the speaking range for all agents in the targets list
			foreach (Agent agent in targets)
			{
				if (!AgentsWithCustomRange.ContainsKey(agent))
				{
					AddInAgentWithCustomSpeakingRange(agent, voipRange);
				}
			}
			// Restore their original speaking range for all agents that are not in the targets list
			foreach (Agent agent in AgentsWithCustomRange.Keys)
			{
				if (!targets.Contains(agent))
				{
					RemoveInAgentWithCustomSpeakingRange(agent);
				}
			}
			return ActionTask.CompletedTask;
		}

		public void AddInAgentWithCustomSpeakingRange(Agent agent, int voipRange)
		{
			int speakingRange = agent.GetSpeakingRange();
			AgentsWithCustomRange.Add(agent, speakingRange);
			agent.SetSpeakingRange(voipRange, true);
		}

		public void RemoveInAgentWithCustomSpeakingRange(Agent agent)
		{
			if (AgentsWithCustomRange.TryGetValue(agent, out int speakingRange))
			{
				AgentsWithCustomRange.Remove(agent);
				agent.SetSpeakingRange(speakingRange, true);
			}
		}
	}
}