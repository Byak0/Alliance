using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>
	/// Returns all agents currently matching a zone filter (side/target).
	/// Its <see cref="Zone"/> parameter is itself a <c>ValueSource&lt;Zone&gt;</c>, so it can be a literal, a variable, or another function call.
	/// </summary>
	[Serializable]
	[PhrasePreview("{Target} from {Side} {?Anywhere:anywhere}{?!Anywhere:in {Zone}}")]
	[PhraseTemplate("{Target} from {Side}{Anywhere| in| anywhere} {?!Anywhere:{Zone}}")]
	public class AgentsInZoneFunction : Function
	{
		public override Type ReturnType => typeof(List<Agent>);

		[ConfigProperty(label: "Anywhere", tooltip: "If true, ignore the zone and include all agents.")]
		public bool Anywhere = false;

		[ConfigProperty(label: "Zone", tooltip: "Zone to pick agents from.")]
		public ValueSource<Zone> Zone = new LiteralValue<Zone>(new Zone());

		[ConfigProperty(label: "Side", tooltip: "Which side to include.")]
		public SideType Side = SideType.All;

		[ConfigProperty(label: "Target", tooltip: "Which kind of agents to include (all, bots, players, officers).")]
		public TargetType Target = TargetType.All;

		public AgentsInZoneFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			List<Agent> result = new List<Agent>();
			if (Mission.Current == null) return result;

			if (Anywhere)
			{
				foreach (Agent agent in Mission.Current.Agents)
				{
					if (Matches(agent, null, ctx, globals)) result.Add(agent);
				}
				return result;
			}

			if (Zone == null) return result;
			Zone zone = Zone.Resolve(ctx, globals);
			if (zone == null) return result;
			Vec3 center = zone.ResolveCenter(ctx, globals);

			MBList<Agent> agents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(center.AsVec2, zone.Shape.BoundingRadius, agents);
			foreach (Agent agent in agents)
			{
				if (Matches(agent, zone, ctx, globals)) result.Add(agent);
			}
			return result;
		}

		private bool Matches(Agent agent, Zone zone, TriggerContext ctx, VariableStore globals)
		{
			if (agent?.Team == null) return false;
			if (Side != SideType.All && (int)agent.Team.Side != (int)Side) return false;
			if (zone != null && !zone.Contains(agent.Position, ctx, globals)) return false;
			if (Target == TargetType.All) return true;
			if (Target == TargetType.Bots && agent.IsPlayerControlled) return false;
			if (Target == TargetType.Players && !agent.IsPlayerControlled) return false;
			if (Target == TargetType.Officers && (agent.MissionPeer == null || agent.MissionPeer.GetNetworkPeer().IsOfficer())) return false;
			return true;
		}
	}
}
