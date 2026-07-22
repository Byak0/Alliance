using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.GameModes.Story.Actions
{
	[OverrideAction(typeof(SetAgentMortalityAction))]
	public class Server_SetAgentMortalityAction : SetAgentMortalityAction
	{
		public override ActionTask Execute()
		{
			if (Mission.Current == null) return ActionTask.CompletedTask;
			TriggerContext ctx = ScenarioManager.Instance.CurrentTriggerContext;
			VariableStore globals = ScenarioManager.Instance.Globals;
			MortalityState state = State?.Resolve(ctx, globals) ?? MortalityState.Invulnerable;
			List<Agent> targets = Who?.Resolve(ctx, globals) ?? new List<Agent>();
			foreach (Agent agent in targets)
			{
				agent.SetMortalityState(state);
			}
			return ActionTask.CompletedTask;
		}
	}
}