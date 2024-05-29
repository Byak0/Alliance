using Alliance.Client.Patch.Behaviors;
using Alliance.Common.Core.Configuration.Models;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace Alliance.Client.Extensions.ExNativeUI.HUDExtension.ViewModels
{
    public class PvCInfoVM : ViewModel
    {
        private readonly MissionRepresentativeBase _missionRepresentative;

        private readonly MissionMultiplayerGameModeBaseClient _gameMode;

        private readonly MissionMultiplayerSiegeClient _siegeClient;

        private readonly MissionScoreboardComponent _missionScoreboardComponent;

        private const float InitialArmyStrength = 1f;

        private int _attackerTeamInitialMemberCount;

        private int _defenderTeamInitialMemberCount;

        private Team _allyTeam;

        private Team _enemyTeam;

        private ICommanderInfo _commanderInfo;

        private bool _areMoraleEventsRegistered;

        private MBBindingList<PvCCapturePointVM> _allyControlPoints;

        private MBBindingList<PvCCapturePointVM> _neutralControlPoints;

        private MBBindingList<PvCCapturePointVM> _enemyControlPoints;

        private int _allyMoraleIncreaseLevel;

        private int _enemyMoraleIncreaseLevel;

        private int _allyMoralePercentage;

        private int _enemyMoralePercentage;

        private int _allyMemberCount;

        private int _enemyMemberCount;

        private PowerLevelComparer _powerLevelComparer;

        private bool _showTacticalInfo;

        private bool _usePowerComparer;

        private bool _useMoraleComparer;

        private bool _areMoralesIndependent;

        private bool _showControlPointStatus;

        private string _allyTeamColor;

        private string _allyTeamColorSecondary;

        private string _enemyTeamColor;

        private string _enemyTeamColorSecondary;

        [DataSourceProperty]
        public MBBindingList<PvCCapturePointVM> AllyControlPoints
        {
            get
            {
                return _allyControlPoints;
            }
            set
            {
                if (value != _allyControlPoints)
                {
                    _allyControlPoints = value;
                    OnPropertyChangedWithValue(value, "AllyControlPoints");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<PvCCapturePointVM> NeutralControlPoints
        {
            get
            {
                return _neutralControlPoints;
            }
            set
            {
                if (value != _neutralControlPoints)
                {
                    _neutralControlPoints = value;
                    OnPropertyChangedWithValue(value, "NeutralControlPoints");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<PvCCapturePointVM> EnemyControlPoints
        {
            get
            {
                return _enemyControlPoints;
            }
            set
            {
                if (value != _enemyControlPoints)
                {
                    _enemyControlPoints = value;
                    OnPropertyChangedWithValue(value, "EnemyControlPoints");
                }
            }
        }

        [DataSourceProperty]
        public string AllyTeamColor
        {
            get
            {
                return _allyTeamColor;
            }
            set
            {
                if (value != _allyTeamColor)
                {
                    _allyTeamColor = value;
                    OnPropertyChangedWithValue(value, "AllyTeamColor");
                }
            }
        }

        [DataSourceProperty]
        public string AllyTeamColorSecondary
        {
            get
            {
                return _allyTeamColorSecondary;
            }
            set
            {
                if (value != _allyTeamColorSecondary)
                {
                    _allyTeamColorSecondary = value;
                    OnPropertyChangedWithValue(value, "AllyTeamColorSecondary");
                }
            }
        }

        [DataSourceProperty]
        public string EnemyTeamColor
        {
            get
            {
                return _enemyTeamColor;
            }
            set
            {
                if (value != _enemyTeamColor)
                {
                    _enemyTeamColor = value;
                    OnPropertyChangedWithValue(value, "EnemyTeamColor");
                }
            }
        }

        [DataSourceProperty]
        public string EnemyTeamColorSecondary
        {
            get
            {
                return _enemyTeamColorSecondary;
            }
            set
            {
                if (value != _enemyTeamColorSecondary)
                {
                    _enemyTeamColorSecondary = value;
                    OnPropertyChangedWithValue(value, "EnemyTeamColorSecondary");
                }
            }
        }

        [DataSourceProperty]
        public int AllyMoraleIncreaseLevel
        {
            get
            {
                return _allyMoraleIncreaseLevel;
            }
            set
            {
                if (value != _allyMoraleIncreaseLevel)
                {
                    _allyMoraleIncreaseLevel = value;
                    OnPropertyChangedWithValue(value, "AllyMoraleIncreaseLevel");
                }
            }
        }

        [DataSourceProperty]
        public int EnemyMoraleIncreaseLevel
        {
            get
            {
                return _enemyMoraleIncreaseLevel;
            }
            set
            {
                if (value != _enemyMoraleIncreaseLevel)
                {
                    _enemyMoraleIncreaseLevel = value;
                    OnPropertyChangedWithValue(value, "EnemyMoraleIncreaseLevel");
                }
            }
        }

        [DataSourceProperty]
        public int AllyMoralePercentage
        {
            get
            {
                return _allyMoralePercentage;
            }
            set
            {
                if (value != _allyMoralePercentage)
                {
                    _allyMoralePercentage = value;
                    OnPropertyChangedWithValue(value, "AllyMoralePercentage");
                }
            }
        }

        [DataSourceProperty]
        public int EnemyMoralePercentage
        {
            get
            {
                return _enemyMoralePercentage;
            }
            set
            {
                if (value != _enemyMoralePercentage)
                {
                    _enemyMoralePercentage = value;
                    OnPropertyChangedWithValue(value, "EnemyMoralePercentage");
                }
            }
        }

        [DataSourceProperty]
        public int AllyMemberCount
        {
            get
            {
                return _allyMemberCount;
            }
            set
            {
                if (value != _allyMemberCount)
                {
                    _allyMemberCount = value;
                    OnPropertyChangedWithValue(value, "AllyMemberCount");
                }
            }
        }

        [DataSourceProperty]
        public int EnemyMemberCount
        {
            get
            {
                return _enemyMemberCount;
            }
            set
            {
                if (value != _enemyMemberCount)
                {
                    _enemyMemberCount = value;
                    OnPropertyChangedWithValue(value, "EnemyMemberCount");
                }
            }
        }

        [DataSourceProperty]
        public PowerLevelComparer PowerLevelComparer
        {
            get
            {
                return _powerLevelComparer;
            }
            set
            {
                if (value != _powerLevelComparer)
                {
                    _powerLevelComparer = value;
                    OnPropertyChangedWithValue(value, "PowerLevelComparer");
                }
            }
        }

        [DataSourceProperty]
        public bool UsePowerComparer
        {
            get
            {
                return _usePowerComparer;
            }
            set
            {
                if (value != _usePowerComparer)
                {
                    _usePowerComparer = value;
                    OnPropertyChangedWithValue(value, "UsePowerComparer");
                }
            }
        }

        [DataSourceProperty]
        public bool UseMoraleComparer
        {
            get
            {
                return _useMoraleComparer;
            }
            set
            {
                if (value != _useMoraleComparer)
                {
                    _useMoraleComparer = value;
                    OnPropertyChangedWithValue(value, "UseMoraleComparer");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowTacticalInfo
        {
            get
            {
                return _showTacticalInfo;
            }
            set
            {
                if (value != _showTacticalInfo)
                {
                    _showTacticalInfo = value;
                    OnPropertyChangedWithValue(value, "ShowTacticalInfo");
                }
            }
        }

        [DataSourceProperty]
        public bool AreMoralesIndependent
        {
            get
            {
                return _areMoralesIndependent;
            }
            set
            {
                if (value != _areMoralesIndependent)
                {
                    _areMoralesIndependent = value;
                    OnPropertyChangedWithValue(value, "AreMoralesIndependent");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowControlPointStatus
        {
            get
            {
                return _showControlPointStatus;
            }
            set
            {
                if (value != _showControlPointStatus)
                {
                    _showControlPointStatus = value;
                    OnPropertyChangedWithValue(value, "ShowControlPointStatus");
                }
            }
        }

        public PvCInfoVM(MissionRepresentativeBase missionRepresentative)
        {
            _missionRepresentative = missionRepresentative;
            AllyControlPoints = new MBBindingList<PvCCapturePointVM>();
            NeutralControlPoints = new MBBindingList<PvCCapturePointVM>();
            EnemyControlPoints = new MBBindingList<PvCCapturePointVM>();
            _gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            _missionScoreboardComponent = Mission.Current.GetMissionBehavior<MissionScoreboardComponent>();
            _commanderInfo = Mission.Current.GetMissionBehavior<ICommanderInfo>();
            ShowTacticalInfo = true;
            if (_gameMode != null)
            {
                UpdateWarmupDependentFlags(_gameMode.IsInWarmup);
                UsePowerComparer = _gameMode.GameType == MultiplayerGameType.Battle && _gameMode.ScoreboardComponent != null;
                if (UsePowerComparer)
                {
                    PowerLevelComparer = new PowerLevelComparer(1.0, 1.0);
                }

                if (UseMoraleComparer)
                {
                    RegisterMoraleEvents();
                }
            }

            _siegeClient = Mission.Current.GetMissionBehavior<MissionMultiplayerSiegeClient>();
            if (_siegeClient != null)
            {
                _siegeClient.OnCapturePointRemainingMoraleGainsChangedEvent += OnCapturePointRemainingMoraleGainsChanged;
            }

            Mission.Current.OnMissionReset += OnMissionReset;
            AllianceAgentVisualSpawnComponent missionBehavior = Mission.Current.GetMissionBehavior<AllianceAgentVisualSpawnComponent>();
            missionBehavior.OnMyAgentSpawnedFromVisual += OnPreparationEnded;
            missionBehavior.OnMyAgentVisualSpawned += OnRoundStarted;
            OnTeamChanged();
        }

        private void OnRoundStarted()
        {
            OnTeamChanged();
            if (UsePowerComparer)
            {
                _attackerTeamInitialMemberCount = _missionScoreboardComponent.Sides[1].Players.Count();
                _defenderTeamInitialMemberCount = _missionScoreboardComponent.Sides[0].Players.Count();
            }
        }

        private void RegisterMoraleEvents()
        {
            if (!_areMoraleEventsRegistered)
            {
                _commanderInfo.OnMoraleChangedEvent += OnUpdateMorale;
                _commanderInfo.OnFlagNumberChangedEvent += OnNumberOfCapturePointsChanged;
                _commanderInfo.OnCapturePointOwnerChangedEvent += OnCapturePointOwnerChanged;
                AreMoralesIndependent = _commanderInfo.AreMoralesIndependent;
                ResetCapturePointLists();
                InitCapturePoints();
                _areMoraleEventsRegistered = true;
            }
        }

        private void OnPreparationEnded()
        {
            ShowTacticalInfo = true;
            OnTeamChanged();
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            if (_commanderInfo != null)
            {
                _commanderInfo.OnMoraleChangedEvent -= OnUpdateMorale;
                _commanderInfo.OnFlagNumberChangedEvent -= InitCapturePoints;
            }

            Mission.Current.OnMissionReset -= OnMissionReset;
            AllianceAgentVisualSpawnComponent missionBehavior = Mission.Current.GetMissionBehavior<AllianceAgentVisualSpawnComponent>();
            missionBehavior.OnMyAgentSpawnedFromVisual -= OnPreparationEnded;
            missionBehavior.OnMyAgentVisualSpawned -= OnRoundStarted;
            if (_siegeClient != null)
            {
                _siegeClient.OnCapturePointRemainingMoraleGainsChangedEvent -= OnCapturePointRemainingMoraleGainsChanged;
            }
        }

        public void UpdateWarmupDependentFlags(bool isInWarmup)
        {
            UseMoraleComparer = !isInWarmup && _gameMode.IsGameModeTactical && _commanderInfo != null && Config.Instance.ShowFlagMarkers;
            ShowControlPointStatus = !isInWarmup && Config.Instance.ShowFlagMarkers;
            if (!isInWarmup && UseMoraleComparer)
            {
                RegisterMoraleEvents();
            }
        }

        public void OnUpdateMorale(BattleSideEnum side, float morale)
        {
            if (_allyTeam != null && _allyTeam.Side == side)
            {
                AllyMoralePercentage = MathF.Round(MathF.Abs(morale * 100f));
            }
            else if (_enemyTeam != null && _enemyTeam.Side == side)
            {
                EnemyMoralePercentage = MathF.Round(MathF.Abs(morale * 100f));
            }
        }

        private void OnMissionReset(object sender, PropertyChangedEventArgs e)
        {
            if (UseMoraleComparer)
            {
                AllyMoralePercentage = 50;
                EnemyMoralePercentage = 50;
            }

            if (UsePowerComparer)
            {
                PowerLevelComparer.Update(1.0, 1.0, 1.0, 1.0);
            }
        }

        internal void Tick(float dt)
        {
            foreach (PvCCapturePointVM allyControlPoint in AllyControlPoints)
            {
                allyControlPoint.Refresh(0f, 0f, 0f);
            }

            foreach (PvCCapturePointVM enemyControlPoint in EnemyControlPoints)
            {
                enemyControlPoint.Refresh(0f, 0f, 0f);
            }

            foreach (PvCCapturePointVM neutralControlPoint in NeutralControlPoints)
            {
                neutralControlPoint.Refresh(0f, 0f, 0f);
            }

            if (_allyTeam != null && UsePowerComparer)
            {
                int count = Mission.Current.AttackerTeam.ActiveAgents.Count;
                int count2 = Mission.Current.DefenderTeam.ActiveAgents.Count;
                AllyMemberCount = _allyTeam.Side == BattleSideEnum.Attacker ? count : count2;
                EnemyMemberCount = _allyTeam.Side == BattleSideEnum.Attacker ? count2 : count;
                int num = _allyTeam.Side == BattleSideEnum.Attacker ? _attackerTeamInitialMemberCount : _defenderTeamInitialMemberCount;
                Team allyTeam = _allyTeam;
                int num2 = allyTeam != null && allyTeam.Side == BattleSideEnum.Attacker ? _defenderTeamInitialMemberCount : _attackerTeamInitialMemberCount;
                if (num2 == 0 && num == 0)
                {
                    PowerLevelComparer.Update(1.0, 1.0, 1.0, 1.0);
                }
                else
                {
                    PowerLevelComparer.Update(EnemyMemberCount, AllyMemberCount, num2, num);
                }
            }
        }

        private void OnCapturePointOwnerChanged(FlagCapturePoint target, Team newOwnerTeam)
        {
            PvCCapturePointVM capturePointVM = FindCapturePointInLists(target);
            if (capturePointVM != null)
            {
                RemoveFlagFromLists(capturePointVM);
                HandleAddNewCapturePoint(capturePointVM);
                capturePointVM.OnOwnerChanged(newOwnerTeam);
            }
        }

        private void OnCapturePointRemainingMoraleGainsChanged(int[] remainingMoraleArr)
        {
            foreach (PvCCapturePointVM allyControlPoint in AllyControlPoints)
            {
                int flagIndex = allyControlPoint.Target.FlagIndex;
                if (flagIndex >= 0 && remainingMoraleArr.Length > flagIndex)
                {
                    allyControlPoint.OnRemainingMoraleChanged(remainingMoraleArr[flagIndex]);
                }
            }

            foreach (PvCCapturePointVM enemyControlPoint in EnemyControlPoints)
            {
                int flagIndex2 = enemyControlPoint.Target.FlagIndex;
                if (flagIndex2 >= 0 && remainingMoraleArr.Length > flagIndex2)
                {
                    enemyControlPoint.OnRemainingMoraleChanged(remainingMoraleArr[flagIndex2]);
                }
            }

            foreach (PvCCapturePointVM neutralControlPoint in NeutralControlPoints)
            {
                int flagIndex3 = neutralControlPoint.Target.FlagIndex;
                if (flagIndex3 >= 0 && remainingMoraleArr.Length > flagIndex3)
                {
                    neutralControlPoint.OnRemainingMoraleChanged(remainingMoraleArr[flagIndex3]);
                }
            }
        }

        private void OnNumberOfCapturePointsChanged()
        {
            ResetCapturePointLists();
            InitCapturePoints();
        }

        private void InitCapturePoints()
        {
            if (_commanderInfo != null && GameNetwork.MyPeer?.GetComponent<MissionPeer>()?.Team != null)
            {
                FlagCapturePoint[] array = _commanderInfo.AllCapturePoints.Where((c) => !c.IsDeactivated).ToArray();
                foreach (FlagCapturePoint flagCapturePoint in array)
                {
                    PvCCapturePointVM capturePointVM = new PvCCapturePointVM(flagCapturePoint, (TargetIconType)(17 + flagCapturePoint.FlagIndex));
                    HandleAddNewCapturePoint(capturePointVM);
                }

                RefreshMoraleIncreaseLevels();
            }
        }

        private void HandleAddNewCapturePoint(PvCCapturePointVM capturePointVM)
        {
            RemoveFlagFromLists(capturePointVM);
            if (_allyTeam != null)
            {
                Team team = _commanderInfo.GetFlagOwner(capturePointVM.Target);
                if (team != null && (team.Side == BattleSideEnum.None || team.Side == BattleSideEnum.NumSides))
                {
                    team = null;
                }

                capturePointVM.OnOwnerChanged(team);
                bool isDeactivated = capturePointVM.Target.IsDeactivated;
                if ((team == null || team.TeamIndex == -1) && !isDeactivated)
                {
                    int index = MathF.Min(NeutralControlPoints.Count, capturePointVM.Target.FlagIndex);
                    NeutralControlPoints.Insert(index, capturePointVM);
                }
                else if (_allyTeam == team)
                {
                    int index2 = MathF.Min(AllyControlPoints.Count, capturePointVM.Target.FlagIndex);
                    AllyControlPoints.Insert(index2, capturePointVM);
                }
                else if (_allyTeam != team)
                {
                    int index3 = MathF.Min(EnemyControlPoints.Count, capturePointVM.Target.FlagIndex);
                    EnemyControlPoints.Insert(index3, capturePointVM);
                }
                else if (team.Side != BattleSideEnum.None)
                {
                    Debug.FailedAssert("Incorrect flag team state", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Multiplayer\\HUDExtensions\\CommanderInfoVM.cs", "HandleAddNewCapturePoint", 317);
                }

                RefreshMoraleIncreaseLevels();
            }
        }

        private void RefreshMoraleIncreaseLevels()
        {
            AllyMoraleIncreaseLevel = MathF.Max(0, AllyControlPoints.Count - EnemyControlPoints.Count);
            EnemyMoraleIncreaseLevel = MathF.Max(0, EnemyControlPoints.Count - AllyControlPoints.Count);
        }

        private void RemoveFlagFromLists(PvCCapturePointVM capturePoint)
        {
            if (AllyControlPoints.Contains(capturePoint))
            {
                AllyControlPoints.Remove(capturePoint);
            }
            else if (NeutralControlPoints.Contains(capturePoint))
            {
                NeutralControlPoints.Remove(capturePoint);
            }
            else if (EnemyControlPoints.Contains(capturePoint))
            {
                EnemyControlPoints.Remove(capturePoint);
            }
        }

        public void OnTeamChanged()
        {
            if (!GameNetwork.IsMyPeerReady || !ShowTacticalInfo)
            {
                return;
            }

            MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
            _allyTeam = component.Team;
            if (_allyTeam != null)
            {
                IEnumerable<Team> source = Mission.Current.Teams.Where((t) => t.IsEnemyOf(_allyTeam));
                _enemyTeam = source.FirstOrDefault();
                if (_allyTeam.Side == BattleSideEnum.None)
                {
                    _allyTeam = Mission.Current.AttackerTeam;
                    return;
                }

                ResetCapturePointLists();
                InitCapturePoints();
            }
        }

        private void ResetCapturePointLists()
        {
            AllyControlPoints.Clear();
            NeutralControlPoints.Clear();
            EnemyControlPoints.Clear();
        }

        private PvCCapturePointVM FindCapturePointInLists(FlagCapturePoint target)
        {
            PvCCapturePointVM capturePointVM = AllyControlPoints.SingleOrDefault((c) => c.Target == target);
            if (capturePointVM != null)
            {
                return capturePointVM;
            }

            PvCCapturePointVM capturePointVM2 = EnemyControlPoints.SingleOrDefault((c) => c.Target == target);
            if (capturePointVM2 != null)
            {
                return capturePointVM2;
            }

            PvCCapturePointVM capturePointVM3 = NeutralControlPoints.SingleOrDefault((c) => c.Target == target);
            if (capturePointVM3 != null)
            {
                return capturePointVM3;
            }

            return null;
        }

        public void RefreshColors(string allyTeamColor, string allyTeamColorSecondary, string enemyTeamColor, string enemyTeamColorSecondary)
        {
            AllyTeamColor = allyTeamColor;
            AllyTeamColorSecondary = allyTeamColorSecondary;
            EnemyTeamColor = enemyTeamColor;
            EnemyTeamColorSecondary = enemyTeamColorSecondary;
            if (UsePowerComparer)
            {
                PowerLevelComparer.SetColors(EnemyTeamColor, AllyTeamColor);
            }
        }
    }
}