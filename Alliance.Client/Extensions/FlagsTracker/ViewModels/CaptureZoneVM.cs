using Alliance.Common.Extensions.FlagsTracker.Scripts;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.FlagMarker.Targets;

namespace Alliance.Client.Extensions.FlagsTracker.ViewModels
{
    public class CaptureZoneVM : MissionMarkerTargetVM
    {
        public CS_CapturableZone TargetFlag { get; private set; }

        protected override float HeightOffset => 6f;

        private float _flagProgress;

        private bool _isVisible;

        public override Vec3 WorldPosition
        {
            get
            {
                if (TargetFlag != null)
                {
                    return TargetFlag.Position;
                }

                Debug.FailedAssert("No target found!", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Multiplayer\\FlagMarker\\Targets\\MissionFlagMarkerTargetVM.cs", "WorldPosition", 24);
                return Vec3.One;
            }
        }


        [DataSourceProperty]
        public float FlagProgress
        {
            get
            {
                return _flagProgress;
            }
            set
            {
                if (value != _flagProgress)
                {
                    _flagProgress = value;
                    OnPropertyChangedWithValue(value, "FlagProgress");
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

        public CaptureZoneVM(CS_CapturableZone capturableZone) : base(MissionMarkerType.Flag)
        {
            TargetFlag = capturableZone;
            Name = capturableZone.ZoneName;

            OnOwnerChanged(null);
        }

        private Vec3 Vector3Maxamize(Vec3 vector)
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
                vector = Vector3Maxamize(vector);
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
            Distance = (int)(TargetFlag.Position - missionCamera.Position).Length;
            FlagProgress = TargetFlag.GetFlagProgress();
        }

        public void OnOwnerChanged(Team team)
        {
            bool num = team == null || team.TeamIndex == -1;
            uint color = num ? 4284111450u : team.Color;
            uint color2 = num ? uint.MaxValue : team.Color2;
            RefreshColor(color, color2);
        }
    }
}
