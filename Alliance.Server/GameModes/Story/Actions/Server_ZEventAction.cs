using Alliance.Common.Core.Security.Extension;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Server.Extensions.Zevent;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Actions
{
	public class Server_ZEventAction : ZEventAction
	{
		public override void Execute()
		{
			if (Zone == null) return;

			switch (Action)
			{
				case ActionType.Tutut:
					DoTheTutut();
					break;
				case ActionType.LabyrintheCompleted:
					LabyrintheCompleted();
					break;
				case ActionType.LabyrintheAbandonned:
					LabyrintheAbandonned();
					break;
				default:
					break;
			}
		}

		private void LabyrintheCompleted()
		{
			List<Agent> agents = GetNearbyPlayers();
			if (agents == null || agents.Count == 0) return;
			if (agents.Count == 1)
			{
				SendMessageToAll($"INCROYABLE ! {agents[0].Name} vient de finir le labyrinthe ! BRAVO !");
			}
			else
			{
				// Concatenates all players names
				string names = "";
				for (int i = 0; i < agents.Count; i++)
				{
					names += agents[i].Name;
					if (i < agents.Count - 2)
						names += ", ";
					else if (i == agents.Count - 2)
						names += " et ";
				}
				SendMessageToAll($"INCROYABLE ! {names} viennent de finir le labyrinthe ! BRAVO à vous !");
			}
		}

		private void LabyrintheAbandonned()
		{
			List<Agent> agents = GetNearbyPlayers();
			if (agents == null || agents.Count == 0) return;

			List<string> randomForfeitPhrases = new()
			{
				"Ce n'est qu'une pause avant d'y retourner !",
				"Petit détour par la taverne et on recommence !",
				"Le labyrinthe garde ses mystères pour une autre fois !",
				"Chaque détour est une aventure en soi !",
				"Un repos bien mérité, le labyrinthe sera toujours là !",
				"On a croisé quelqu'un qui l'avait finit, y'a rien au bout !",
				"Même les héros ont besoin de souffler parfois !",
				"Ce n'était qu'un essai, le prochain sera le bon !",
				"Finalement on avait mieux à faire..."
			};

			string phrase = randomForfeitPhrases[MBRandom.RandomInt(randomForfeitPhrases.Count)];

			if (agents.Count == 1)
			{
				SendMessageToAll($"{agents[0].Name} a quitté le labyrinthe. {phrase}");
			}
			else
			{
				// Concatène les noms
				string names = "";
				for (int i = 0; i < agents.Count; i++)
				{
					names += agents[i].Name;
					if (i < agents.Count - 2)
						names += ", ";
					else if (i == agents.Count - 2)
						names += " et ";
				}
				SendMessageToAll($"{names} ont quitté le labyrinthe. {phrase}");
			}
		}

		private void DoTheTutut()
		{
			List<Agent> agents = GetNearbyPlayers();
			if (agents == null || agents.Count == 0) return;
			if (agents.Count == 1)
			{
				SendMessageToAll($"{agents[0].Name} vient de finir le parcours et le cor du Zevent retentit !");
			}
			else
			{
				// Concatenates all players names
				string names = "";
				for (int i = 0; i < agents.Count; i++)
				{
					names += agents[i].Name;
					if (i < agents.Count - 2)
						names += ", ";
					else if (i == agents.Count - 2)
						names += " et ";
				}
				SendMessageToAll($"{names} viennent de finir le parcours et le cor du Zevent retentit !");
			}

			RefreshZeventGoldPileAsync();
		}

		private async void RefreshZeventGoldPileAsync()
		{
			await ZeventService.Instance.RefreshZeventGoldPileAsync();
		}

		public List<Agent> GetNearbyPlayers()
		{
			if (Zone == null) return null;

			MBList<Agent> agents = new MBList<Agent>();
			Mission.Current.GetNearbyAgents(Zone.GlobalPosition.AsVec2, Zone.Radius * 2f, agents);
			Log($"{Zone.Position} - {Zone.GlobalPosition} - {Zone.Radius} - {Zone.LocalEntity.GlobalPosition}", LogLevel.Debug);
			Log($"NB agents : {agents.Count}", LogLevel.Debug);
			agents.RemoveAll(agent => !IsValidTarget(agent));
			return agents;
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
	}
}