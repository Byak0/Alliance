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
		[ConfigProperty(label: "Captured agents", tooltip: "When condition is fulfilled, matching agents will be stored under this temporary variable name within this scripted event.")]
		[VariableOutput(typeof(List<Agent>))]
		public string CapturedAgents = "TriggeringAgents";

		public AgentCountCondition() { }

		public override bool Evaluate(VariableStore context)
		{
			if (Mission.Current == null) return false;
			List<Agent> targets = Who?.Resolve(context) ?? new List<Agent>();
			int targetCount = TargetCount?.Resolve(context) ?? 0;
			bool result = ExactCountOnly ? targets.Count == targetCount : targets.Count >= targetCount;
			if (result && targets.Count > 0)
			{
				context?.Set(CapturedAgents, new List<Agent>(targets));
			}
			return result;
		}
	}
}
