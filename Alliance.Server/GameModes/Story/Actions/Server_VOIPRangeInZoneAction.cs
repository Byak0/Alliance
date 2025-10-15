using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.Extensions.TroopSpawner.Models;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using System.Collections.Generic;
using System;

namespace Alliance.Server.GameModes.Story.Actions
{
	public class Server_VOIPRangeInZoneAction : VOIPRangeInZoneAction
	{
		// Create a dictionary with agent in zone and previous speakingrange 
		Dictionary<Agent, int> AgentInZone = new Dictionary<Agent, int>();
		public override void Execute()
		{
			MBList<Agent> agents = new MBList<Agent>();

			Mission.Current.GetNearbyAgents(Zone.GlobalPosition.AsVec2, Zone.Radius, agents);
			agents.RemoveAll(agent => !IsValidTarget(agent));
			foreach (Agent agent in agents)
			{
				if (!AgentInZone.ContainsKey(agent))
				{
					AddInAgentWithCustomSpeakingRange(agent);
				}
			}

			foreach (Agent agent in AgentInZone.Keys)
			{
				if (!agents.Contains(agent))
				{
					RemoveInAgentWithCustomSpeakingRange(agent);
				}
			}
		}

		public bool IsValidTarget(Agent agent)
		{
			// Check if the agent is on the correct side
			if (Side != SideType.All && (int)agent.Team.Side != (int)Side) return false;
			// Check if agent is close enough in Z axis
			if (agent.Position.Distance(Zone.GlobalPosition) > Zone.Radius) return false;
			// Other checks
			if (Target == TargetType.All) return true;
			if (Target == TargetType.Bots && agent.IsPlayerControlled) return false;
			if (Target == TargetType.Players && !agent.IsPlayerControlled) return false;
			if (Target == TargetType.Officers && (agent.MissionPeer == null || agent.MissionPeer.GetNetworkPeer().IsOfficer())) return false;
			return true;
		}

		public void AddInAgentWithCustomSpeakingRange(Agent agent)
		{
			int speakingRange = agent.GetSpeakingRange();
			AgentInZone.Add(agent, speakingRange);
			agent.SetSpeakingRange(VOIP_Range, true);
		}

		public void RemoveInAgentWithCustomSpeakingRange(Agent agent)
		{
			if (AgentInZone.TryGetValue(agent, out int speakingRange))
			{
				AgentInZone.Remove(agent);
				agent.SetSpeakingRange(speakingRange, true);
			}
		}
	}
}