using Alliance.Common.Extensions.Revive.Models;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.FlagMarker.Targets;

namespace Alliance.Client.Extensions.Revive.ViewModels
{
    public class WoundedMarkerTargetVM : MissionMarkerTargetVM
    {
        public GameEntity WoundedAgentEntity { get; private set; }

        public BattleSideEnum Side { get; private set; }
        //public int Formation { get; private set; }

        protected override float HeightOffset => 4f;

        private float _rezProgress;

        private bool _isVisible;

        public override Vec3 WorldPosition
        {
            get
            {
                if (WoundedAgentEntity != null)
                {
                    return WoundedAgentEntity.GlobalPosition;
                }
                return Vec3.One;
            }
        }


        [DataSourceProperty]
        public float RezProgress
        {
            get
            {
                return _rezProgress;
            }
            set
            {
                if (value != _rezProgress)
                {
                    _rezProgress = value;
                    OnPropertyChangedWithValue(value, "RezProgress");
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }

        public WoundedMarkerTargetVM(WoundedAgentInfos woundedAgentInfos) : base(MissionMarkerType.Peer)
        {
            WoundedAgentEntity = woundedAgentInfos.WoundedAgentEntity;
            Name = woundedAgentInfos.Name;
            Color = Colors.White.ToString();
            Color2 = Colors.White.ToString();
        }

        private Vec3 Vector3Maximize(Vec3 vector)
        {
            Vec3 vec = vector;
            float num = 0f;
            num = vector.x > num ? vector.x : num;
            num = vector.y > num ? vector.y : num;
            num = vector.z > num ? vector.z : num;
            return vec / num;
        }

        public override void UpdateScreenPosition(Camera missionCamera)
        {
            Vec3 worldPoint = WorldPosition;
            worldPoint.z += HeightOffset - 3f;
            Vec3 vector = missionCamera.WorldPointToViewPortPoint(ref worldPoint);
            vector.y = 1f - vector.y;
            if (vector.z < 0f)
            {
                vector.x = 1f - vector.x;
                vector.y = 1f - vector.y;
                vector.z = 0f;
                vector = Vector3Maximize(vector);
            }

            if (float.IsPositiveInfinity(vector.x))
            {
                vector.x = 1f;
            }
            else if (float.IsNegativeInfinity(vector.x))
            {
                vector.x = 0f;
            }

            if (float.IsPositiveInfinity(vector.y))
            {
                vector.y = 1f;
            }
            else if (float.IsNegativeInfinity(vector.y))
            {
                vector.y = 0f;
            }

            vector.x = MathF.Clamp(vector.x, 0f, 1f) * Screen.RealScreenResolutionWidth;
            vector.y = MathF.Clamp(vector.y, 0f, 1f) * Screen.RealScreenResolutionHeight;
            ScreenPosition = new Vec2(vector.x, vector.y);
            Distance = (int)(WorldPosition - missionCamera.Position).Length;

            // TODO : implement rez progress
            //RezProgress = TargetFlag.GetFlagProgress();
        }
    }
}
