using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Models
{
    /// <summary>
    /// Represent a team with his list of markers.
    /// </summary>
    public class SaeMarkerTeam
    {
        public Team Team { get; }
        public List<SaeMarkerServerEntity> SaeMarkers { get; }

        public SaeMarkerTeam(Team team)
        {
            Team = team;
            SaeMarkers = new List<SaeMarkerServerEntity>();
        }
    }
}
