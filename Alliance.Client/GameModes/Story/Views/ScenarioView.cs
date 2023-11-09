using Alliance.Client.GameModes.Story.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.GameModes.Story.Views
{
    /// <summary>
    /// Scenario View.
    /// Show scenario, act and objectives informations.
    /// Press tab for details.
    /// </summary>
    public class ScenarioView : MissionView
    {
        private GauntletLayer _gauntletLayer;
        private ScenarioVM _dataSource;

        private bool _showBoard;
        private bool _mouseRequestedWhileBoardActive;
        private bool _isMouseVisible;
        private float _scenarioIntroDuration = 10f;
        private float _scenarioIntroTimeElapsed;
        private bool _scenarioIntroRequested;
        private float _resultDuration = 10f;
        private float _resultTimeElapsed;
        private bool _resultRequested;

        public ScenarioView()
        {
            ViewOrderPriority = 25;
        }

        public override void OnMissionScreenInitialize()
        {
            InitializeLayer();
            Mission.IsFriendlyMission = false;
            GameKeyContext category = HotKeyManager.GetCategory("ScoreboardHotKeyCategory");
            if (!MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
            {
                MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
            }

            RegisterScenarioEvents();
        }

        private void InitializeLayer()
        {
            _dataSource = new ScenarioVM();
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("ScenarioBoard", _dataSource);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("Generic"));
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("ScoreboardHotKeyCategory"));
            MissionScreen.AddLayer(_gauntletLayer);
        }

        private void RegisterScenarioEvents()
        {
            MissionPeer.OnTeamChanged += SetObjectives;
            ScenarioPlayer.Instance.OnActStateAwaitPlayerJoin += ShowInitialScreen;
            ScenarioPlayer.Instance.OnActStateSpawnParticipants += ShowSpawnScreen;
            ScenarioPlayer.Instance.OnActStateInProgress += ShowStartScreen;
            ScenarioPlayer.Instance.OnActStateCompleted += ShowEndScreen;
        }

        private void UnregisterScenarioEvents()
        {
            MissionPeer.OnTeamChanged -= SetObjectives;
            ScenarioPlayer.Instance.OnActStateAwaitPlayerJoin -= ShowInitialScreen;
            ScenarioPlayer.Instance.OnActStateSpawnParticipants -= ShowSpawnScreen;
            ScenarioPlayer.Instance.OnActStateInProgress -= ShowStartScreen;
            ScenarioPlayer.Instance.OnActStateCompleted -= ShowEndScreen;
        }

        private void SetObjectives(NetworkCommunicator peer, Team previousTeam, Team newTeam)
        {
            if (peer == null || !peer.IsMine) return;
            _dataSource.SetObjectives(ScenarioPlayer.Instance.CurrentAct, newTeam.Side);

            // TODO : Find a better place for this, added it to fix title/desc not showing when reconnecting
            _dataSource.ActTitle = ScenarioPlayer.Instance.CurrentAct.Name;
            _dataSource.ActDescription = ScenarioPlayer.Instance.CurrentAct.Description;
        }

        public void ShowInitialScreen()
        {
            _dataSource.SetAct(ScenarioPlayer.Instance.CurrentAct);

            _scenarioIntroRequested = true;
            _scenarioIntroTimeElapsed = 0;
            _dataSource.ShowIntro = true;
            Log("Hello I'm InitialScreen", LogLevel.Debug);
        }

        public void ShowSpawnScreen()
        {
            Log("Hello I'm SpawnScreen", LogLevel.Debug);
        }

        public void ShowStartScreen()
        {
            Log("Hello I'm StartScreen", LogLevel.Debug);
        }

        public void ShowEndScreen()
        {
            Log("Hello I'm EndScreen", LogLevel.Debug);
        }

        public void ShowResultScreen(string title, string description, Color color)
        {
            Log("Hello I'm ResultScreen", LogLevel.Debug);
            _resultRequested = true;
            _dataSource.ShowResult = true;
            _dataSource.ResultTitle = title;
            _dataSource.ResultDescription = "";
            _dataSource.ActDescription = description;
            _dataSource.ResultColor = color;
            _resultTimeElapsed = 0f;
        }

        public override void OnMissionScreenFinalize()
        {
            FinalizeLayer();
            base.OnMissionScreenFinalize();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            // Intro screen
            if (_scenarioIntroRequested)
            {
                if (_scenarioIntroTimeElapsed >= _scenarioIntroDuration)
                {
                    _dataSource.ShowIntro = false;
                    _scenarioIntroRequested = false;
                    return;
                }
                _scenarioIntroTimeElapsed += dt;
            }

            // Refresh VM
            _dataSource.RefreshProgress(dt);

            bool isTabPressed = MissionScreen.SceneLayer.Input.IsHotKeyDown("HoldShow") || _gauntletLayer.Input.IsHotKeyDown("HoldShow");

            // Hide result screen if tab is pressed
            if (_resultRequested)
            {
                if (_resultTimeElapsed > _resultDuration)
                {
                    _resultRequested = false;
                    _dataSource.ShowResult = false;
                }
                _resultTimeElapsed += dt;
            }

            // Toggle board depending on hotkeys
            bool activateBoard = isTabPressed && !MissionScreen.IsRadialMenuActive && !Mission.IsOrderMenuOpen;
            ToggleScoreboard(activateBoard);

            // Check to show mouse cursor
            if (_showBoard && (MissionScreen.SceneLayer.Input.IsGameKeyPressed(35) || _gauntletLayer.Input.IsGameKeyPressed(35)))
            {
                _mouseRequestedWhileBoardActive = true;
            }
            bool showCursor = _showBoard && _mouseRequestedWhileBoardActive;
            SetMouseState(showCursor);
        }

        private void ToggleScoreboard(bool isActive)
        {
            if (_showBoard != isActive)
            {
                _showBoard = isActive;
                _dataSource.ShowBoard = _showBoard;
                MissionScreen.SetCameraLockState(_showBoard);
                if (!_showBoard)
                {
                    _mouseRequestedWhileBoardActive = false;
                }
            }
        }

        private void SetMouseState(bool isMouseVisible)
        {
            if (_isMouseVisible != isMouseVisible)
            {
                _isMouseVisible = isMouseVisible;
                if (!_isMouseVisible)
                {
                    _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                }
                else
                {
                    _gauntletLayer.InputRestrictions.SetInputRestrictions(_isMouseVisible, InputUsageMask.Mouse);
                }
                ScenarioVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.SetMouseState(isMouseVisible);
            }
        }

        private void FinalizeLayer()
        {
            _dataSource.OnFinalize();
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _dataSource = null;
            _showBoard = false;
            UnregisterScenarioEvents();
        }
    }
}