using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.FlagMarker.Targets;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;

namespace Alliance.Client.Extensions.ExNativeUI.ViewModels
{
    public class MarkerUIHandlerVM : ViewModel
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

        private readonly ICommanderInfo _commanderInfo;

        private readonly Dictionary<MissionPeer, MissionPeerMarkerTargetVM> _teammateDictionary;

        private readonly MissionMultiplayerSiegeClient _siegeClient;

        private readonly List<PlayerId> _friendIDs;

        private MBBindingList<MissionFlagMarkerTargetVM> _flagTargets;

        private MBBindingList<MissionPeerMarkerTargetVM> _peerTargets;

        private MBBindingList<MissionSiegeEngineMarkerTargetVM> _siegeEngineTargets;

        private MBBindingList<MissionAlwaysVisibleMarkerTargetVM> _alwaysVisibleTargets;

        private bool _isEnabled;

        [DataSourceProperty]
        public MBBindingList<MissionFlagMarkerTargetVM> FlagTargets
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
        public MBBindingList<MissionPeerMarkerTargetVM> PeerTargets
        {
            get
            {
                return _peerTargets;
            }
            set
            {
                if (value != _peerTargets)
                {
                    _peerTargets = value;
                    OnPropertyChangedWithValue(value, "PeerTargets");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MissionSiegeEngineMarkerTargetVM> SiegeEngineTargets
        {
            get
            {
                return _siegeEngineTargets;
            }
            set
            {
                if (value != _siegeEngineTargets)
                {
                    _siegeEngineTargets = value;
                    OnPropertyChangedWithValue(value, "SiegeEngineTargets");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MissionAlwaysVisibleMarkerTargetVM> AlwaysVisibleTargets
        {
            get
            {
                return _alwaysVisibleTargets;
            }
            set
            {
                if (value != _alwaysVisibleTargets)
                {
                    _alwaysVisibleTargets = value;
                    OnPropertyChangedWithValue(value, "AlwaysVisibleTargets");
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
                    UpdateTargetStates(value);
                }
            }
        }

        public MarkerUIHandlerVM(Camera missionCamera)
        {
            _missionCamera = missionCamera;
            FlagTargets = new MBBindingList<MissionFlagMarkerTargetVM>();
            PeerTargets = new MBBindingList<MissionPeerMarkerTargetVM>();
            SiegeEngineTargets = new MBBindingList<MissionSiegeEngineMarkerTargetVM>();
            AlwaysVisibleTargets = new MBBindingList<MissionAlwaysVisibleMarkerTargetVM>();
            _teammateDictionary = new Dictionary<MissionPeer, MissionPeerMarkerTargetVM>();
            _distanceComparer = new MarkerDistanceComparer();
            _commanderInfo = Mission.Current.GetMissionBehavior<ICommanderInfo>();
            if (_commanderInfo != null)
            {
                _commanderInfo.OnFlagNumberChangedEvent += OnFlagNumberChangedEvent;
                _commanderInfo.OnCapturePointOwnerChangedEvent += OnCapturePointOwnerChangedEvent;
                OnFlagNumberChangedEvent();
                _siegeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerSiegeClient>();
                if (_siegeClient != null)
                {
                    _siegeClient.OnCapturePointRemainingMoraleGainsChangedEvent += OnCapturePointRemainingMoraleGainsChanged;
                }
            }

            MissionPeer.OnTeamChanged += OnTeamChanged;
            _friendIDs = new List<PlayerId>();
            IFriendListService[] friendListServices = PlatformServices.Instance.GetFriendListServices();
            foreach (IFriendListService friendListService in friendListServices)
            {
                _friendIDs.AddRange(friendListService.GetAllFriends());
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            if (_commanderInfo != null)
            {
                _commanderInfo.OnFlagNumberChangedEvent -= OnFlagNumberChangedEvent;
                _commanderInfo.OnCapturePointOwnerChangedEvent -= OnCapturePointOwnerChangedEvent;
                if (_siegeClient != null)
                {
                    _siegeClient.OnCapturePointRemainingMoraleGainsChangedEvent -= OnCapturePointRemainingMoraleGainsChanged;
                }
            }

            MissionPeer.OnTeamChanged -= OnTeamChanged;
        }

        public void Tick(float dt)
        {
            OnRefreshPeerMarkers();
            UpdateAlwaysVisibleTargetScreenPosition();
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

        private void OnCapturePointRemainingMoraleGainsChanged(int[] remainingMoraleGainsArr)
        {
            foreach (MissionFlagMarkerTargetVM flagTarget in FlagTargets)
            {
                int flagIndex = flagTarget.TargetFlag.FlagIndex;
                if (flagIndex >= 0 && flagIndex < remainingMoraleGainsArr.Length)
                {
                    flagTarget.OnRemainingMoraleChanged(remainingMoraleGainsArr[flagIndex]);
                }
            }

            Debug.Print("OnCapturePointRemainingMoraleGainsChanged: " + remainingMoraleGainsArr.Length);
        }

        private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
        {
            if (_commanderInfo != null)
            {
                OnFlagNumberChangedEvent();
            }

            if (!peer.IsMine)
            {
                return;
            }

            SiegeEngineTargets.Clear();
            foreach (GameEntity item in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<SiegeWeapon>())
            {
                SiegeWeapon firstScriptOfType = item.GetFirstScriptOfType<SiegeWeapon>();
                if (newTeam.Side == firstScriptOfType.Side)
                {
                    SiegeEngineTargets.Add(new MissionSiegeEngineMarkerTargetVM(firstScriptOfType));
                }
            }
        }

        private void UpdateTargetScreenPositions()
        {
            PeerTargets.ApplyActionOnAllItems(delegate (MissionPeerMarkerTargetVM pt)
            {
                pt.UpdateScreenPosition(_missionCamera);
            });
            FlagTargets.ApplyActionOnAllItems(delegate (MissionFlagMarkerTargetVM ft)
            {
                ft.UpdateScreenPosition(_missionCamera);
            });
            SiegeEngineTargets.ApplyActionOnAllItems(delegate (MissionSiegeEngineMarkerTargetVM st)
            {
                st.UpdateScreenPosition(_missionCamera);
            });
            PeerTargets.Sort(_distanceComparer);
            FlagTargets.Sort(_distanceComparer);
            SiegeEngineTargets.Sort(_distanceComparer);
        }

        private void UpdateAlwaysVisibleTargetScreenPosition()
        {
            foreach (MissionAlwaysVisibleMarkerTargetVM alwaysVisibleTarget in AlwaysVisibleTargets)
            {
                alwaysVisibleTarget.UpdateScreenPosition(_missionCamera);
            }
        }

        private void OnFlagNumberChangedEvent()
        {
            ResetCapturePointLists();
            InitCapturePoints();
        }

        private void InitCapturePoints()
        {
            if (_commanderInfo != null && Config.Instance.ShowFlagMarkers)
            {
                FlagCapturePoint[] array = _commanderInfo.AllCapturePoints.Where((c) => !c.IsDeactivated).ToArray();
                foreach (FlagCapturePoint flag in array)
                {
                    MissionFlagMarkerTargetVM missionFlagMarkerTargetVM = new MissionFlagMarkerTargetVM(flag);
                    FlagTargets.Add(missionFlagMarkerTargetVM);
                    missionFlagMarkerTargetVM.OnOwnerChanged(_commanderInfo.GetFlagOwner(flag));
                }
            }
        }

        private void ResetCapturePointLists()
        {
            FlagTargets.Clear();
        }

        private void OnCapturePointOwnerChangedEvent(FlagCapturePoint flag, Team team)
        {
            foreach (MissionFlagMarkerTargetVM flagTarget in FlagTargets)
            {
                if (flagTarget.TargetFlag == flag)
                {
                    flagTarget.OnOwnerChanged(team);
                }
            }
        }

        private void OnRefreshPeerMarkers()
        {
            if (GameNetwork.MyPeer == null)
            {
                return;
            }

            BattleSideEnum battleSideEnum = GameNetwork.MyPeer.ControlledAgent?.Team.Side ?? BattleSideEnum.None;
            List<MissionPeerMarkerTargetVM> list = PeerTargets.ToList();
            foreach (MissionPeer missionPeer in VirtualPlayer.Peers<MissionPeer>())
            {
                if (missionPeer?.Team == null || missionPeer.IsMine || missionPeer.Team.Side != battleSideEnum)
                {
                    continue;
                }

                IEnumerable<MissionPeerMarkerTargetVM> source = PeerTargets.Where((t) => t.TargetPeer?.Peer.Id.Equals(missionPeer.Peer.Id) ?? false);
                if (source.Count() > 0)
                {
                    MissionPeerMarkerTargetVM currentMarker = source.First();
                    IEnumerable<MissionAlwaysVisibleMarkerTargetVM> source2 = AlwaysVisibleTargets.Where((t) => t.TargetPeer.Peer.Id.Equals(currentMarker.TargetPeer.Peer.Id));
                    if (BannerlordConfig.EnableDeathIcon && !missionPeer.IsControlledAgentActive)
                    {
                        if (!source2.Any() && source.First().TargetPeer?.ControlledAgent != null)
                        {
                            MissionAlwaysVisibleMarkerTargetVM missionAlwaysVisibleMarkerTargetVM = new MissionAlwaysVisibleMarkerTargetVM(currentMarker.TargetPeer, source.First().WorldPosition, OnRemoveAlwaysVisibleMarker);
                            missionAlwaysVisibleMarkerTargetVM.UpdateScreenPosition(_missionCamera);
                            AlwaysVisibleTargets.Add(missionAlwaysVisibleMarkerTargetVM);
                        }

                        continue;
                    }
                }

                // Only show marker of officers
                if (missionPeer.Peer.IsOfficer())
                {
                    if (!_teammateDictionary.ContainsKey(missionPeer))
                    {
                        MissionPeerMarkerTargetVM missionPeerMarkerTargetVM = new MissionPeerMarkerTargetVM(missionPeer, _friendIDs.Contains(missionPeer.Peer.Id));
                        PeerTargets.Add(missionPeerMarkerTargetVM);
                        _teammateDictionary.Add(missionPeer, missionPeerMarkerTargetVM);
                    }
                    else
                    {
                        list.Remove(_teammateDictionary[missionPeer]);
                    }
                }
            }

            foreach (MissionPeerMarkerTargetVM item in list)
            {
                MissionPeerMarkerTargetVM current;
                if ((current = item) != null)
                {
                    PeerTargets.Remove(current);
                    _teammateDictionary.Remove(current.TargetPeer);
                }
            }
        }

        public void OnRemoveAlwaysVisibleMarker(MissionAlwaysVisibleMarkerTargetVM marker)
        {
            AlwaysVisibleTargets.Remove(marker);
        }

        private void UpdateTargetStates(bool state)
        {
            PeerTargets.ApplyActionOnAllItems(delegate (MissionPeerMarkerTargetVM pt)
            {
                pt.IsEnabled = state;
            });
            FlagTargets.ApplyActionOnAllItems(delegate (MissionFlagMarkerTargetVM ft)
            {
                ft.IsEnabled = state;
            });
            SiegeEngineTargets.ApplyActionOnAllItems(delegate (MissionSiegeEngineMarkerTargetVM st)
            {
                st.IsEnabled = state;
            });
        }
    }
}