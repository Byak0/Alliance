using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.Compass;

namespace Alliance.Client.Extensions.ExNativeUI.ViewModels
{
    public class PvCCapturePointVM : CompassTargetVM
    {
        public readonly FlagCapturePoint Target;

        private float _flagProgress;

        private int _remainingRemovalTime = -1;

        private bool _isKeepFlag;

        private bool _isSpawnAffectorFlag;

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
        public bool IsSpawnAffectorFlag
        {
            get
            {
                return _isSpawnAffectorFlag;
            }
            set
            {
                if (value != _isSpawnAffectorFlag)
                {
                    _isSpawnAffectorFlag = value;
                    OnPropertyChangedWithValue(value, "IsSpawnAffectorFlag");
                }
            }
        }

        [DataSourceProperty]
        public bool IsKeepFlag
        {
            get
            {
                return _isKeepFlag;
            }
            set
            {
                if (value != _isKeepFlag)
                {
                    _isKeepFlag = value;
                    OnPropertyChangedWithValue(value, "IsKeepFlag");
                }
            }
        }

        [DataSourceProperty]
        public int RemainingRemovalTime
        {
            get
            {
                return _remainingRemovalTime;
            }
            set
            {
                if (value != _remainingRemovalTime)
                {
                    _remainingRemovalTime = value;
                    OnPropertyChangedWithValue(value, "RemainingRemovalTime");
                }
            }
        }

        public PvCCapturePointVM(FlagCapturePoint target, TargetIconType iconType)
            : base(iconType, 0u, 0u, null, isAttacker: false, isAlly: false)
        {
            Target = target;
            string[] tags = Target.GameEntity.Tags;
            foreach (string text in tags)
            {
                if (text.StartsWith("enable_") || text.StartsWith("disable_"))
                {
                    IsSpawnAffectorFlag = true;
                }
            }

            if (Target.GameEntity.HasTag("keep_capture_point"))
            {
                IsKeepFlag = true;
            }

            ResetFlag();
        }

        public override void Refresh(float circleX, float x, float distance)
        {
            base.Refresh(circleX, x, distance);
            FlagProgress = Target.GetFlagProgress();
        }

        public void OnOwnerChanged(Team newTeam)
        {
            uint color = (uint)((int?)newTeam?.Color ?? -10855846);
            uint color2 = (uint)((int?)newTeam?.Color2 ?? -1);
            RefreshColor(color, color2);
        }

        public void ResetFlag()
        {
            OnOwnerChanged(null);
        }

        public void OnRemainingMoraleChanged(int remainingMorale)
        {
            if (RemainingRemovalTime != remainingMorale && remainingMorale != 90)
            {
                RemainingRemovalTime = (int)(remainingMorale / 1f);
            }
        }
    }
}