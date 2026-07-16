using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story
{
	/// <summary>
	/// Resolves a set of agents at runtime. Actions depend on a source instead of hardcoding how agents
	/// are picked, so one action covers all cases: a captured trigger variable (single Agent or
	/// List&lt;Agent&gt;) or a live zone query, filtered or not.
	/// </summary>
	[Serializable]
	[XmlInclude(typeof(AgentsFromVariable))]
	[XmlInclude(typeof(AgentsInZone))]
	public abstract class AgentSource
	{
		public abstract List<Agent> Resolve(TriggerContext context);
	}

	/// <summary>Agents held by a trigger variable (a single Agent or a List&lt;Agent&gt;).</summary>
	[Serializable]
	[PhrasePreview("{VariableName}")]
	[PhraseTemplate("{VariableName}")]
	public class AgentsFromVariable : AgentSource
	{
		[ConfigProperty(label: "Variable", tooltip: "Trigger variable holding the agent(s): a single captured agent or a captured list.")]
		[VariableRef(typeof(Agent))]
		public string VariableName = "";

		public AgentsFromVariable() { }

		public override List<Agent> Resolve(TriggerContext context)
		{
			List<Agent> result = new List<Agent>();
			if (context == null || string.IsNullOrEmpty(VariableName)) return result;

			object value = context.Get<object>(VariableName);
			switch (value)
			{
				case null:
					break;
				case Agent single:
					result.Add(single);
					break;
				case IEnumerable list when !(value is string):
					foreach (object item in list)
					{
						if (item is Agent agent) result.Add(agent);
					}
					break;
			}
			return result;
		}
	}

	/// <summary>All agents currently in a zone, optionally filtered by side/target.</summary>
	[Serializable]
	[PhrasePreview("{Target} from {Side} in {?Everywhere:zone|everywhere}")]
	[PhraseTemplate("{Target} from {Side} {Everywhere|in |everywhere} {?!Everywhere:{Zone}}")]
	public class AgentsInZone : AgentSource
	{
		[ConfigProperty(label: "Everywhere", tooltip: "If true, the zone is ignored and all agents are included.")]
		public bool Everywhere = false;
		[ConfigProperty(label: "Zone", tooltip: "Zone to pick agents from.")]
		public SerializableZone Zone;
		[ConfigProperty(label: "Side", tooltip: "Which side to include.")]
		public SideType Side = SideType.All;
		[ConfigProperty(label: "Target", tooltip: "Which kind of agents to include (all, bots, players, officers).")]
		public TargetType Target = TargetType.All;

		public AgentsInZone() { }

		public override List<Agent> Resolve(TriggerContext context)
		{
			List<Agent> result = new List<Agent>();
			if (Zone == null || Mission.Current == null) return result;

			MBList<Agent> agents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Zone.GlobalPosition.AsVec2, Zone.Radius, agents);
			foreach (Agent agent in agents)
			{
				if (Matches(agent)) result.Add(agent);
			}
			return result;
		}

		private bool Matches(Agent agent)
		{
			if (agent?.Team == null) return false;
			if (Side != SideType.All && (int)agent.Team.Side != (int)Side) return false;
			if (agent.Position.Distance(Zone.GlobalPosition) > Zone.Radius) return false;
			if (Target == TargetType.All) return true;
			if (Target == TargetType.Bots && agent.IsPlayerControlled) return false;
			if (Target == TargetType.Players && !agent.IsPlayerControlled) return false;
			if (Target == TargetType.Officers && (agent.MissionPeer == null || agent.MissionPeer.GetNetworkPeer().IsOfficer())) return false;
			return true;
		}
	}
}
