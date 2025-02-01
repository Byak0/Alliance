using Alliance.Common.Core.Security.Extension;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if agents entered a zone.
	/// </summary>
	public class AgentEnteredZoneCondition : Condition
	{
		public SerializableZone Zone;
		public SideType Side = SideType.All;
		public TargetType Target = TargetType.All;
		[ScenarioEditor(label: "Target count", tooltip: "How many matching agents must be present in the zone.")]
		public int TargetCount = 1;
		[ScenarioEditor(label: "Exact count only", tooltip: "If enabled, the condition will only trigger if there is the EXACT number of matching agents in the zone (not more, not less).")]
		public bool ExactCountOnly = false;

		public AgentEnteredZoneCondition() { }

		public override bool Evaluate(ScenarioManager context)
		{
			if (Zone == null) return false;
			MBList<Agent> agents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Zone.Position.AsVec2, Zone.Radius, agents);
			agents.RemoveAll(agent => !IsValidTarget(agent));
			return ExactCountOnly ? agents.Count == TargetCount : agents.Count >= TargetCount;
		}

		public bool IsValidTarget(Agent agent)
		{
			// Check if the agent is on the correct side
			if (Side != SideType.All && (int)agent.Team.Side != (int)Side) return false;
			// Check if agent is close enough in Z axis
			if (agent.Position.Distance(Zone.Position) > Zone.Radius) return false;
			// Other checks
			if (Target == TargetType.All) return true;
			if (Target == TargetType.Bots && agent.IsPlayerControlled) return false;
			if (Target == TargetType.Players && !agent.IsPlayerControlled) return false;
			if (Target == TargetType.Officers && (agent.MissionPeer == null || agent.MissionPeer.GetNetworkPeer().IsOfficer())) return false;
			return true;
		}
	}
}
