using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.GameModes.Story.Actions
{
	[OverrideAction(typeof(SetAgentMortalityAction))]
	public class Server_SetAgentMortalityAction : SetAgentMortalityAction
	{
		public override ActionTask Execute(VariableStore context)
		{
			if (Mission.Current == null) return ActionTask.CompletedTask;
			MortalityState state = State?.Resolve(context) ?? MortalityState.Invulnerable;
			List<Agent> targets = Who?.Resolve(context) ?? new List<Agent>();
			foreach (Agent agent in targets)
			{
				agent.SetMortalityState(state);
			}
			return ActionTask.CompletedTask;
		}
	}
}