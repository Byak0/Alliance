using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if a certain number of agents are present.
	/// </summary>
	[PhrasePreview("{Who} are {ExactCountOnly|at least |}{TargetCount} ({CapturedAgents})")]
	[PhraseTemplate(
		"{Who} are {ExactCountOnly|at least|exactly} {TargetCount}",
		"If condition is fulfilled, they are stored as {CapturedAgents}")]
	public class AgentCountCondition : Condition
	{
		public ValueSource<List<Agent>> Who = new VariableValue<List<Agent>>();
		public ValueSource<int> TargetCount = new LiteralValue<int>(1);		
		public bool ExactCountOnly = false;
		[ConfigProperty(label: "Captured agents", tooltip: "When condition is fulfilled, matching agents will be stored under this variable name for later use.")]
		[VariableOutput(typeof(List<Agent>))]
		public string CapturedAgents = "TriggeringAgents";

		public AgentCountCondition() { }

		public override bool Evaluate(ScenarioManager context)
		{
			if (Mission.Current == null) return false;
			TriggerContext ctx = ScenarioManager.Instance.CurrentTriggerContext;
			VariableStore globals = ScenarioManager.Instance.Globals;
			List<Agent> targets = Who?.Resolve(ctx, globals) ?? new List<Agent>();
			int targetCount = TargetCount?.Resolve(ctx, globals) ?? 0;
			bool result = ExactCountOnly ? targets.Count == targetCount : targets.Count >= targetCount;
			if (result && targets.Count > 0)
			{
				ctx?.Set(CapturedAgents, new List<Agent>(targets));
			}
			return result;
		}
	}
}
