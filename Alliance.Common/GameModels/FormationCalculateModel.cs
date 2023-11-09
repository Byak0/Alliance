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
            // Multiplier = (ActiveAgents - MinPlayerValue) / (MinPlayerValue + MaxPlayerValue)
            // Radius = RadiusMin + ( Multiplier * (RadiusMax - RadiusMin) )
            // RequiredAllies = NbAlliesForFormationMin + ( Multiplier * (NbAlliesForFormationMax - NbAlliesForFormationMin) )
            float multiplier = (player.Team.ActiveAgents.Count - Config.Instance.MinPlayer) / (Config.Instance.MinPlayer + Config.Instance.MaxPlayer);
            float radius = Config.Instance.FormRadMin + multiplier * (Config.Instance.FormRadMax - Config.Instance.FormRadMin);
            int requiredAllies = (int)(Config.Instance.NbFormMin + multiplier * (Config.Instance.NbFormMax - Config.Instance.NbFormMin));

            MBList<Agent> target = new() { player };
            MBList<Agent> nearbyAllies = Mission.Current.GetNearbyAllyAgents(player.Position.AsVec2, radius, player.Team, target);
            int nbNearbyAllies = nearbyAllies.Count() - 1;
            if (ownFormationOnly)
            {
                nbNearbyAllies = nearbyAllies.Where(agent => agent.Formation == player.Formation).Count() - 1;
            }

            if (nbNearbyAllies >= requiredAllies)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsInSkirmish(Agent player, bool ownFormationOnly = true)
        {
            // Multiplier = (ActiveAgents - MinPlayerValue) / (MinPlayerValue + MaxPlayerValue)
            // Radius = RadiusMin + ( Multiplier * (RadiusMax - RadiusMin) )
            // RequiredAllies = NbAlliesForSkirmishMin + ( Multiplier * (NbAlliesForSkirmishMax - NbAlliesForSkirmishMin) )
            float multiplier = (player.Team.ActiveAgents.Count - Config.Instance.MinPlayer) / (Config.Instance.MinPlayer + Config.Instance.MaxPlayer);
            float radius = Config.Instance.SkirmRadMin + multiplier * (Config.Instance.SkirmRadMax - Config.Instance.SkirmRadMin);
            int requiredAllies = (int)(Config.Instance.NbSkirmMin + multiplier * (Config.Instance.NbSkirmMax - Config.Instance.NbSkirmMin));

            MBList<Agent> target = new() { player };
            MBList<Agent> nearbyAllies = Mission.Current.GetNearbyAllyAgents(player.Position.AsVec2, radius, player.Team, target);
            int nbNearbyAllies = nearbyAllies.Count() - 1;
            if (ownFormationOnly)
            {
                nbNearbyAllies = nearbyAllies.Where(agent => agent.Formation == player.Formation).Count() - 1;
            }

            if (nbNearbyAllies >= requiredAllies)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}