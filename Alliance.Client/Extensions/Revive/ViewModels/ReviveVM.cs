using Alliance.Common.Extensions.Revive.Models;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.FlagMarker.Targets;

namespace Alliance.Client.Extensions.Revive.ViewModels
{
    /// <summary>
    /// View Model storing wounded allies infos.
    /// </summary>
    public class ReviveVM : ViewModel
    {
        private readonly Camera _missionCamera;
        private bool _prevEnabledState;
        private bool _fadeOutTimerStarted;
        private float _fadeOutTimer;
        private MarkerDistanceComparer _distanceComparer;
        private MBBindingList<WoundedMarkerTargetVM> _woundedAgents;
        private AgentInteractionInterfaceVM _agentInteractionVM;
        private bool _isEnabled;

        public class MarkerDistanceComparer : IComparer<MissionMarkerTargetVM>
        {
            public int Compare(MissionMarkerTargetVM x, MissionMarkerTargetVM y)
            {
                return y.Distance.CompareTo(x.Distance);
            }
        }

        [DataSourceProperty]
        public AgentInteractionInterfaceVM InteractionInterface
        {
            get
            {
                return _agentInteractionVM;
            }
            set
            {
                bool flag = value != _agentInteractionVM;
                if (flag)
                {
                    _agentInteractionVM = value;
                    OnPropertyChangedWithValue(value, "InteractionInterface");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<WoundedMarkerTargetVM> WoundedAgents
        {
            get
            {
                return _woundedAgents;
            }
            set
            {
                if (value != _woundedAgents)
                {
                    _woundedAgents = value;
                    OnPropertyChangedWithValue(value, "WoundedAgents");
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

        public ReviveVM(Camera missionCamera)
        {
            _missionCamera = missionCamera;
            WoundedAgents = new MBBindingList<WoundedMarkerTargetVM>();
            _distanceComparer = new MarkerDistanceComparer();
            InteractionInterface = new AgentInteractionInterfaceVM(Mission.Current);
            InitWoundedAgents();
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
            WoundedAgents.ApplyActionOnAllItems(delegate (WoundedMarkerTargetVM ft)
            {
                ft.UpdateScreenPosition(_missionCamera);
                ft.IsEnabled = IsEnabled && ft.Distance < 10;
            });
            WoundedAgents.Sort(_distanceComparer);
        }

        public void InitWoundedAgents()
        {
            WoundedAgents.Clear();
            foreach (GameEntity woundedEntity in Mission.Current.Scene.FindEntitiesWithTag(ReviveTags.WoundedTag))
            {
                WoundedAgentInfos woundedAgentInfos = new WoundedAgentInfos(woundedEntity);
                WoundedMarkerTargetVM woundedMarkerVM = new WoundedMarkerTargetVM(woundedAgentInfos);
                WoundedAgents.Add(woundedMarkerVM);
            }
        }

        public void OnNewWounded(WoundedAgentInfos woundedAgentInfos)
        {
            WoundedAgents.Add(new WoundedMarkerTargetVM(woundedAgentInfos));
        }
    }
}