using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>
	/// Returns the first agent of a list (or null when empty). Bridges a multi-agent source
	/// (<c>ValueSource&lt;List&lt;Agent&gt;&gt;</c>) onto a single-agent slot
	/// (<c>ValueSource&lt;Agent&gt;</c>).
	/// </summary>
	[Serializable]
	[PhrasePreview("first of {List}")]
	[PhraseTemplate("first agent of {List}")]
	public class FirstAgentOfFunction : Function
	{
		public override Type ReturnType => typeof(Agent);

		[ConfigProperty(label: "List", tooltip: "Source list of agents (e.g. result of an 'agents in zone' function).")]
		public ValueSource<List<Agent>> List = new VariableValue<List<Agent>>();

		public FirstAgentOfFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			List<Agent> agents = List?.Resolve(ctx, globals);
			return agents != null && agents.Count > 0 ? (object)agents[0] : null;
		}
	}

	/// <summary>Returns the number of items in a list of agents.</summary>
	[Serializable]
	[PhrasePreview("count of {List}")]
	[PhraseTemplate("count of {List}")]
	public class CountOfFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "List", tooltip: "List of agents to count.")]
		public ValueSource<List<Agent>> List = new VariableValue<List<Agent>>();

		public CountOfFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			List<Agent> agents = List?.Resolve(ctx, globals);
			return agents?.Count ?? 0;
		}
	}
}
