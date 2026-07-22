using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Actions
{
	[OverrideAction(typeof(DamageAgentAction))]
	public class Server_DamageAgentAction : DamageAgentAction
	{
		public override ActionTask Execute()
		{
			if (Mission.Current == null) return ActionTask.CompletedTask;
			TriggerContext ctx = ScenarioManager.Instance.CurrentTriggerContext;
			VariableStore globals = ScenarioManager.Instance.Globals;
			int damage = Damage?.Resolve(ctx, globals) ?? 0;
			List<Agent> targets = Who?.Resolve(ctx, globals) ?? new List<Agent>();
			foreach (Agent agent in targets)
			{
				CoreUtils.TakeDamage(agent, damage);
			}
			return ActionTask.CompletedTask;
		}
	}
}
