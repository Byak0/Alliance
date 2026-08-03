using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.GameModes.Story.Conditions.Condition;

namespace Alliance.Common.GameModes.Story.Functions
{
	/// <summary>Returns true if the agent is inside the zone.</summary>
	[Serializable]
	[PhrasePreview("{Agent} is in {Zone}")]
	[PhraseTemplate("{Agent} is inside {Zone}")]
	public class ZoneContainsAgentFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Zone", tooltip: "Zone to test against.")]
		public ValueSource<Zone> Zone = new LiteralValue<Zone>(new Zone());

		[ConfigProperty(label: "Agent", tooltip: "Agent to test.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public ZoneContainsAgentFunction() { }

		public override object Evaluate(VariableStore context)
		{
			Zone zone = Zone?.Resolve(context);
			Agent agent = Agent?.Resolve(context);
			if (zone == null || agent == null) return false;
			return zone.Contains(agent.Position, context);

		}
	}

	/// <summary>Counts agents inside a zone, optionally filtered by side and target type.</summary>
	[Serializable]
	[PhrasePreview("count of {Target} from {Side} in {Zone}")]
	[PhraseTemplate("count of {Target} from {Side} in {Zone}")]
	public class ZoneAgentCountFunction : Function
	{
		public override Type ReturnType => typeof(int);

		[ConfigProperty(label: "Zone", tooltip: "Zone to count agents in.")]
		public ValueSource<Zone> Zone = new LiteralValue<Zone>(new Zone());

		[ConfigProperty(label: "Side", tooltip: "Which side to include.")]
		public SideType Side = SideType.All;

		[ConfigProperty(label: "Target", tooltip: "Which kind of agents to include.")]
		public TargetType Target = TargetType.All;

		public ZoneAgentCountFunction() { }

		public override object Evaluate(VariableStore context)
		{
			if (Mission.Current == null) return 0;
			Zone zone = Zone?.Resolve(context);
			if (zone == null) return 0;

			int count = 0;
			Vec3 center = zone.ResolveCenter(context);

			MBList<Agent> agents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(center.AsVec2, zone.Shape.BoundingRadius, agents);
			foreach (Agent agent in agents)
			{
				if (agent?.Team == null) continue;
				if (Side != SideType.All && (int)agent.Team.Side != (int)Side) continue;
				if (!zone.Contains(agent.Position, context)) continue;
				if (Target == TargetType.All) { count++; continue; }
				if (Target == TargetType.Bots && agent.IsPlayerControlled) continue;
				if (Target == TargetType.Players && !agent.IsPlayerControlled) continue;
				if (Target == TargetType.Officers && (agent.MissionPeer == null || agent.MissionPeer.GetNetworkPeer().IsOfficer())) continue;
				count++;
			}
			return count;
		}
	}

	/// <summary>Returns true if no agents matching the side/target filter are inside the zone.</summary>
	[Serializable]
	[PhrasePreview("{Zone} has no {Target} from {Side}")]
	[PhraseTemplate("{Zone} has no {Target} from {Side}")]
	public class IsZoneEmptyFunction : Function
	{
		public override Type ReturnType => typeof(bool);

		[ConfigProperty(label: "Zone", tooltip: "Zone to check.")]
		public ValueSource<Zone> Zone = new LiteralValue<Zone>(new Zone());

		[ConfigProperty(label: "Side", tooltip: "Which side to check.")]
		public SideType Side = SideType.All;

		[ConfigProperty(label: "Target", tooltip: "Which kind of agents to check for.")]
		public TargetType Target = TargetType.All;

		public IsZoneEmptyFunction() { }

		public override object Evaluate(VariableStore context)
		{
			if (Mission.Current == null) return true;
			Zone zone = Zone?.Resolve(context);
			if (zone == null) return true;

			Vec3 center = zone.ResolveCenter(context);
			MBList<Agent> agents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(center.AsVec2, zone.Shape.BoundingRadius, agents);
			foreach (Agent agent in agents)
			{
				if (agent?.Team == null) continue;
				if (Side != SideType.All && (int)agent.Team.Side != (int)Side) continue;
				if (!zone.Contains(agent.Position, context)) continue;
				if (Target == TargetType.All) return false;
				if (Target == TargetType.Bots && agent.IsPlayerControlled) continue;
				if (Target == TargetType.Players && !agent.IsPlayerControlled) continue;
				if (Target == TargetType.Officers && (agent.MissionPeer == null || agent.MissionPeer.GetNetworkPeer().IsOfficer())) continue;
				return false;
			}
			return true;
		}
	}

	/// <summary>
	/// Finds the nearest named zone on the current Act to the given agent.
	/// Optionally filter by zone name prefix (e.g. "capture_" to only match zones whose name starts with that prefix).
	/// Returns the first match by distance, or null if no zones match.
	/// </summary>
	[Serializable]
	[PhrasePreview("nearest zone ({Filter}) {?Filtered:with prefix {ZoneNamePrefix} to }{!Filtered:zone to }{Agent}")]
	[PhraseTemplate("nearest zone ({Filter}) {Filtered|to |with prefix}{?Filtered:{ZoneNamePrefix} to}{Agent}")]
	public class NearestZoneToAgentFunction : Function
	{
		public enum ZoneFilter
		{
			EnabledOnly,
			DisabledOnly,
			Any
		}

		public override Type ReturnType => typeof(Zone);

		[ConfigProperty(label: "Agent", tooltip: "Agent to measure distance from.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		[ConfigProperty(label: "Filter by prefix", tooltip: "If enabled, only consider zones whose name starts with the given prefix.")]
		public bool Filtered = false;

		[ConfigProperty(label: "Zone name prefix", tooltip: "Only consider zones whose name starts with this prefix.", dependency: nameof(Filtered))]
		public string ZoneNamePrefix = "";

		[ConfigProperty(label: "Zone filter", tooltip: "Filter zones by enabled/disabled state.")]
		public ZoneFilter Filter = ZoneFilter.EnabledOnly;

		public NearestZoneToAgentFunction() { }

		public override object Evaluate(VariableStore context)
		{
			Agent agent = Agent?.Resolve(context);
			if (agent == null || ScenarioManager.Instance.CurrentAct == null) return null;

			Vec3 agentPos = agent.Position;
			Zone best = null;
			float bestDist = float.MaxValue;

			foreach (NamedZone named in ScenarioManager.Instance.CurrentAct.Zones)
			{
				if (named?.Zone == null) continue;
				if (Filter == ZoneFilter.EnabledOnly && !named.Enabled) continue;
				if (Filter == ZoneFilter.DisabledOnly && named.Enabled) continue;
				if (Filtered && !string.IsNullOrEmpty(ZoneNamePrefix) && !named.Name.StartsWith(ZoneNamePrefix)) continue;
				Vec3 center = named.Zone.ResolveCenter(context);
				float d = agentPos.DistanceSquared(center);
				if (d < bestDist) { bestDist = d; best = named.Zone; }
			}
			return best;
		}
	}

	/// <summary>Returns the distance between the centers of two zones.</summary>
	[Serializable]
	[PhrasePreview("distance from {ZoneA} to {ZoneB}")]
	[PhraseTemplate("distance from {ZoneA} to {ZoneB}")]
	public class DistanceBetweenZonesFunction : Function
	{
		public override Type ReturnType => typeof(float);

		[ConfigProperty(label: "Zone A", tooltip: "First zone.")]
		public ValueSource<Zone> ZoneA = new LiteralValue<Zone>(new Zone());

		[ConfigProperty(label: "Zone B", tooltip: "Second zone.")]
		public ValueSource<Zone> ZoneB = new LiteralValue<Zone>(new Zone());

		public DistanceBetweenZonesFunction() { }

		public override object Evaluate(VariableStore context)
		{
			Zone a = ZoneA?.Resolve(context);
			Zone b = ZoneB?.Resolve(context);
			if (a == null || b == null) return float.MaxValue;
			return a.ResolveCenter(context).Distance(b.ResolveCenter(context));
		}
	}
}
