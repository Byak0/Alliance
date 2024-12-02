using Alliance.Client.GameModes.Story;
using Alliance.Common.Extensions.FlagsTracker.Scripts;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.FlagMarker.Targets;

namespace Alliance.Client.Extensions.FlagsTracker.ViewModels
{
    /// <summary>
    /// View model storing capture zones markers informations.
    /// </summary>
    public class CaptureMarkerUIVM : ViewModel
    {
        public class MarkerDistanceComparer : IComparer<MissionMarkerTargetVM>
        {
            public int Compare(MissionMarkerTargetVM x, MissionMarkerTargetVM y)
            {
                return y.Distance.CompareTo(x.Distance);
            }
        }

        private readonly Camera _missionCamera;

        private bool _prevEnabledState;

        private bool _fadeOutTimerStarted;

        private float _fadeOutTimer;

        private MarkerDistanceComparer _distanceComparer;

        private MBBindingList<CaptureZoneVM> _flagTargets;

        private bool _isEnabled;

        [DataSourceProperty]
        public MBBindingList<CaptureZoneVM> FlagTargets
        {
            get
            {
                return _flagTargets;
            }
            set
            {
                if (value != _flagTargets)
                {
                    _flagTargets = value;
                    OnPropertyChangedWithValue(value, "FlagTargets");
                }
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    OnPropertyChangedWithValue(value, "IsEnabled");
                }
            }
        }

        public CaptureMarkerUIVM(Camera missionCamera)
        {
            _missionCamera = missionCamera;
            FlagTargets = new MBBindingList<CaptureZoneVM>();
            _distanceComparer = new MarkerDistanceComparer();
            ScenarioPlayer.Instance.OnActStateSpawnParticipants += InitCaptureZones;
            InitCaptureZones();
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            ScenarioPlayer.Instance.OnActStateSpawnParticipants -= InitCaptureZones;
        }

        public void Tick(float dt)
        {
            if (IsEnabled)
            {
                UpdateTargetScreenPositions();
                _fadeOutTimerStarted = false;
                _fadeOutTimer = 0f;
                _prevEnabledState = IsEnabled;
            }
            else
            {
                if (_prevEnabledState)
                {
                    _fadeOutTimerStarted = true;
                }

                if (_fadeOutTimerStarted)
                {
                    _fadeOutTimer += dt;
                }

                if (_fadeOutTimer < 2f)
                {
                    UpdateTargetScreenPositions();
                }
                else
                {
                    _fadeOutTimerStarted = false;
                }
            }

            _prevEnabledState = IsEnabled;
        }

        private void UpdateTargetScreenPositions()
        {
            FlagTargets.ApplyActionOnAllItems(delegate (CaptureZoneVM ft)
            {
                ft.UpdateScreenPosition(_missionCamera);
                ft.IsEnabled = IsEnabled && ft.Distance < 50;
            });
            FlagTargets.Sort(_distanceComparer);
        }

        public void InitCaptureZones()
        {
            FlagTargets.Clear();
            List<CS_CapturableZone> capturableZones = Mission.Current.MissionObjects.FindAllWithType<CS_CapturableZone>().Where(cz => true).ToList();
            foreach (CS_CapturableZone flag in capturableZones)
            {
                CaptureZoneVM captureZoneVM = new CaptureZoneVM(flag);
                FlagTargets.Add(captureZoneVM);
                flag.OnOwnerChange += OnCapturePointOwnerChangedEvent;
                captureZoneVM.OnOwnerChanged(flag.OwnerTeam);
            }
        }

        private void OnCapturePointOwnerChangedEvent(CS_CapturableZone flag, Team team)
        {
            foreach (CaptureZoneVM flagTarget in FlagTargets)
            {
                if (flagTarget.TargetFlag == flag)
                {
                    flagTarget.OnOwnerChanged(team);
                }
            }
        }
    }
}