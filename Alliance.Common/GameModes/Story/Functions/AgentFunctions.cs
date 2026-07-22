using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
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

	/// <summary>
	/// Returns the live agent nearest to a reference agent, optionally filtered by side. The reference
	/// agent is read from a variable (typical use: the trigger's captured agent).
	/// </summary>
	[Serializable]
	[PhrasePreview("nearest {Side} agent to {Source}")]
	[PhraseTemplate("nearest {Side} agent to {Source}")]
	public class NearestAgentToFunction : Function
	{
		public override Type ReturnType => typeof(Agent);

		[ConfigProperty(label: "Source agent", tooltip: "Agent to measure distance from.")]
		public ValueSource<Agent> Source = new VariableValue<Agent>();

		[ConfigProperty(label: "Side", tooltip: "Restrict candidates to this side.")]
		public SideType Side = SideType.All;

		public NearestAgentToFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			if (Mission.Current == null || Source == null) return null;
			Agent source = Source.Resolve(ctx, globals);
			if (source == null) return null;

			Agent best = null;
			float bestDist = float.MaxValue;
			foreach (Agent candidate in Mission.Current.Agents)
			{
				if (candidate == source || candidate?.Team == null) continue;
				if (!MatchesSide(candidate, source)) continue;
				float d = candidate.Position.DistanceSquared(source.Position);
				if (d < bestDist) { bestDist = d; best = candidate; }
			}
			return best;
		}

		private bool MatchesSide(Agent candidate, Agent source)
		{
			if (Side == SideType.All) return true;
			if (Side == SideType.Defender) return candidate.Team.Side == BattleSideEnum.Defender;
			if (Side == SideType.Attacker) return candidate.Team.Side == BattleSideEnum.Attacker;
			return true;
		}
	}

	/// <summary>Returns the number of players currently in the mission.</summary>
	[Serializable]
	[PhrasePreview("player count")]
	[PhraseTemplate("number of players")]
	public class GetPlayerCountFunction : Function
	{
		public override Type ReturnType => typeof(int);

		public GetPlayerCountFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			if (Mission.Current == null) return 0;
			int count = 0;
			foreach (Agent agent in Mission.Current.Agents)
			{
				if (agent.IsPlayerControlled) count++;
			}
			return count;
		}
	}

	/// <summary>Returns the display name of an agent (player name if available, otherwise agent name).</summary>
	[Serializable]
	[PhrasePreview("name of {Agent}")]
	[PhraseTemplate("name of {Agent}")]
	public class GetPlayerNameFromAgentFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Agent", tooltip: "Agent to get the name of.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public GetPlayerNameFromAgentFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Agent agent = Agent?.Resolve(ctx, globals);
			if (agent == null) return "";
			return agent.MissionPeer?.Name ?? agent.Name;
		}
	}

	/// <summary>Returns the unique player ID of an agent (empty string for bots).</summary>
	[Serializable]
	[PhrasePreview("ID of {Agent}")]
	[PhraseTemplate("ID of {Agent}")]
	public class GetPlayerIdFromAgentFunction : Function
	{
		public override Type ReturnType => typeof(string);

		[ConfigProperty(label: "Agent", tooltip: "Agent to get the player ID of.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public GetPlayerIdFromAgentFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Agent agent = Agent?.Resolve(ctx, globals);
			if (agent?.MissionPeer == null) return "";
			return agent.MissionPeer.GetNetworkPeer().VirtualPlayer.Id.ToString();
		}
	}

	/// <summary>Returns the straight-line distance between two agents.</summary>
	[Serializable]
	[PhrasePreview("distance from {Source} to {Target}")]
	[PhraseTemplate("distance from {Source} to {Target}")]
	public class DistanceBetweenAgentsFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Source", tooltip: "Reference agent.")]
		public ValueSource<Agent> Source = new VariableValue<Agent>();
		[ConfigProperty(label: "Target", tooltip: "Target agent.")]
		public ValueSource<Agent> Target = new VariableValue<Agent>();

		public DistanceBetweenAgentsFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Agent src = Source?.Resolve(ctx, globals);
			Agent tgt = Target?.Resolve(ctx, globals);
			if (src == null || tgt == null) return float.MaxValue;
			return src.Position.Distance(tgt.Position);
		}
	}

	/// <summary>Returns true if the agent is alive and active.</summary>
	[Serializable]
	[PhrasePreview("{Agent} is alive")]
	[PhraseTemplate("{Agent} is alive")]
	public class IsAgentAliveFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Agent", tooltip: "Agent to check.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public IsAgentAliveFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Agent agent = Agent?.Resolve(ctx, globals);
			return agent != null && agent.IsActive();
		}
	}

	/// <summary>Returns the remaining health of an agent as a float.</summary>
	[Serializable]
	[PhrasePreview("health of {Agent}")]
	[PhraseTemplate("health of {Agent}")]
	public class GetAgentHealthFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Agent", tooltip: "Agent to get health of.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public GetAgentHealthFunction() { }

		public override object Evaluate(TriggerContext ctx, VariableStore globals)
		{
			Agent agent = Agent?.Resolve(ctx, globals);
			return agent?.Health ?? 0f;
		}
	}
}
