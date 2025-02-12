using Alliance.Common.Core.Configuration.Models;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModels
{
	public static class FormationCalculateModel
	{
		public static bool IsInFormation(Agent player, bool ownFormationOnly = true)
		{
			if (player == null || player.Formation == null)
			{
				return false;
			}

			switch (player.Formation.FormationIndex)
			{
				case TaleWorlds.Core.FormationClass.Cavalry:
					return IsCavalryInFormation(player, false);
				case TaleWorlds.Core.FormationClass.Ranged:
					return IsRangedInFormation(player, false);
				case TaleWorlds.Core.FormationClass.Infantry:
					return IsInfantryInFormation(player, false);
				default:
					return IsInfantryInFormation(player, false);
			}
		}

		public static bool IsInSkirmish(Agent player, bool ownFormationOnly = true)
		{
			if (player == null || player.Formation == null)
			{
				return false;
			}

			switch (player.Formation.FormationIndex)
			{
				case TaleWorlds.Core.FormationClass.Cavalry:
					return IsCavalryInSkirmish(player, false);
				case TaleWorlds.Core.FormationClass.Ranged:
					return IsRangedInSkirmish(player, false);
				case TaleWorlds.Core.FormationClass.Infantry:
					return IsInfantryInSkirmish(player, false);
				default:
					return IsInfantryInSkirmish(player, false);
			}
		}

		private static bool IsCavalryInFormation(Agent player, bool ownFormationOnly = true)
		{
			return IsInFormation(player, ownFormationOnly, 2f);
		}

		private static bool IsInfantryInFormation(Agent player, bool ownFormationOnly = true)
		{
			return IsInFormation(player, ownFormationOnly, 1.0f);
		}

		private static bool IsRangedInFormation(Agent player, bool ownFormationOnly = true)
		{
			return IsInFormation(player, ownFormationOnly, 1.7f);
		}


		private static bool IsCavalryInSkirmish(Agent player, bool ownFormationOnly = true)
		{
			return IsInSkirmish(player, ownFormationOnly, 1.5f);
		}

		private static bool IsInfantryInSkirmish(Agent player, bool ownFormationOnly = true)
		{
			return IsInSkirmish(player, ownFormationOnly, 1.0f);
		}

		private static bool IsRangedInSkirmish(Agent player, bool ownFormationOnly = true)
		{
			return IsInSkirmish(player, ownFormationOnly, 1.2f);
		}

		private static bool IsInFormation(Agent player, bool ownFormationOnly, float radiusMultiplier)
		{
			float multiplier = ((float)player.Team.ActiveAgents.Count - Config.Instance.MinPlayer) / (Config.Instance.MinPlayer + Config.Instance.MaxPlayer);
			float radius = (Config.Instance.FormRadMin + multiplier * (Config.Instance.FormRadMax - Config.Instance.FormRadMin)) * radiusMultiplier;
			// More there are alive players, more the requiredAllies value will tends to NbFormMax
			int requiredAllies = (int)(Config.Instance.NbFormMin + (multiplier * (Config.Instance.NbFormMax - Config.Instance.NbFormMin)));

			MBList<Agent> target = new() { player };
			MBList<Agent> nearbyAllies = Mission.Current.GetNearbyAllyAgents(player.Position.AsVec2, radius, player.Team, target);
			int nbNearbyAllies = nearbyAllies.Count() - 1;
			if (ownFormationOnly)
			{
				nbNearbyAllies = nearbyAllies.Where(agent => agent.Formation == player.Formation).Count() - 1;
			}

			return nbNearbyAllies >= requiredAllies;
		}

		private static bool IsInSkirmish(Agent player, bool ownFormationOnly, float radiusMultiplier)
		{
			float multiplier = ((float)player.Team.ActiveAgents.Count - Config.Instance.MinPlayer) / (Config.Instance.MinPlayer + Config.Instance.MaxPlayer);
			float radius = (Config.Instance.SkirmRadMin + multiplier * (Config.Instance.SkirmRadMax - Config.Instance.SkirmRadMin)) * radiusMultiplier;
			// More there are alive players, more the requiredAllies value will tends to NbSkirmMax
			int requiredAllies = (int)(Config.Instance.NbSkirmMin + multiplier * (Config.Instance.NbSkirmMax - Config.Instance.NbSkirmMin));

			MBList<Agent> target = new() { player };
			MBList<Agent> nearbyAllies = Mission.Current.GetNearbyAllyAgents(player.Position.AsVec2, radius, player.Team, target);
			int nbNearbyAllies = nearbyAllies.Count() - 1;
			if (ownFormationOnly)
			{
				nbNearbyAllies = nearbyAllies.Where(agent => agent.Formation == player.Formation).Count() - 1;
			}

			return nbNearbyAllies >= requiredAllies;
		}
	}
}