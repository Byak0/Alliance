using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Actions
{
	[OverrideAction(typeof(DamageAgentAction))]
	public class Server_DamageAgentAction : DamageAgentAction
	{
		public override ActionTask Execute(VariableStore context)
		{
			if (Mission.Current == null) return ActionTask.CompletedTask;
			int damage = Damage?.Resolve(context) ?? 0;
			List<Agent> targets = Who?.Resolve(context) ?? new List<Agent>();
			foreach (Agent agent in targets)
			{
				CoreUtils.TakeDamage(agent, damage);
			}
			return ActionTask.CompletedTask;
		}
	}
}
