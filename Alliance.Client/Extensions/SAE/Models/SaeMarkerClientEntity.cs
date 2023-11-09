using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.SAE.Models
{
    public class SaeMarkerClientEntity
    {
        public int Id { get; }
        public GameEntity VisualArrowEntity { get; }

        public SaeMarkerClientEntity(int id, MatrixFrame position, bool visibility)
        {
            Id = id;
            VisualArrowEntity = InitVisualMarker(position, visibility);
        }

        private static GameEntity InitVisualMarker(MatrixFrame position, bool visibility)
        {
            GameEntity customMarker = GameEntity.Instantiate(Mission.Current.Scene, SaeConstants.VISUAL_STR_POS_OBJ, position);
            customMarker.AddTag(SaeConstants.VISUAL_STR_POS_TAG);
            customMarker.SetVisibilityExcludeParents(visibility);
            customMarker.GetGlobalScale().Normalize();
            customMarker.SetMobility(GameEntity.Mobility.stationary);
            return customMarker;
        }
    }
}
