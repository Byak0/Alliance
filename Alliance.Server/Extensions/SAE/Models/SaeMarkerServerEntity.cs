using Alliance.Common.Core.Configuration.Models;
using Alliance.Server.Extensions.SAE.Behaviors;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE.Models
{
    public class SaeMarkerServerEntity
    {
        public int Id { get; }

        /// <summary>
        /// Entity that contain StrategicArea script.
        /// </summary>
        public GameEntity StrategicArcherPointEntity { get; }

        /// <summary>
        /// Team where the marker is linked to.
        /// </summary>
        public Team MarkerTeam { get; }

        public SaeMarkerServerEntity(MatrixFrame markerPosition, Team team)
        {
            Id = Mission.Current.GetMissionBehavior<SaeBehavior>().GetNextAvailableId();
            StrategicArcherPointEntity = InitStrategicArcherPoint(markerPosition);
            MarkerTeam = team;
        }

        /// <summary>
        /// Init StrategicArcherPointEntity at position send by the player.
        /// </summary>
        /// <param name="markerPosition"></param>
        /// <returns></returns>
        private GameEntity InitStrategicArcherPoint(MatrixFrame markerPosition)
        {
            GameEntity gameEntity = GameEntity.Instantiate(Mission.Current.Scene, SaeConstants.LOGICAL_STR_POS_1000, markerPosition);
            SetStrategicAreaRange(gameEntity.GetFirstScriptOfType<StrategicArea>(), Config.Instance.SAERange);
            gameEntity.SetMobility(GameEntity.Mobility.stationary);
            gameEntity.SetVisibilityExcludeParents(true);
            gameEntity.AddTag(SaeConstants.LOGICAL_STR_POS_TAG);

            return gameEntity;
        }

        private static void SetStrategicAreaRange(StrategicArea strategicArea, float distanceToCheck)
        {
            FieldInfo fieldInfo = typeof(StrategicArea).GetField("_distanceToCheck", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) return;
            fieldInfo.SetValue(strategicArea, distanceToCheck);
        }
    }
}
