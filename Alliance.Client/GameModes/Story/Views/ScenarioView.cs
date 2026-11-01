using Alliance.Client.GameModes.Story.ViewModels;
using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;
using System.Collections.Generic;

namespace Alliance.Client.GameModes.Story.Views
{
	/// <summary>
	/// Scenario View.
	/// Show scenario, act and objectives informations.
	/// Press tab for details.
	/// </summary>
	public class ScenarioView : MissionView
	{
		// Layers allowed to render above the waiting screen blackout (order must be > 21).
		private static readonly HashSet<string> LayersAboveBlackoutWhitelist = new HashSet<string>
		{
			// Scenario pre-game menus
			"AL_MultiplayerTeamSelection",
			"PlayerSpawnMenu",
			"CharacterEditorPopup",
			"FormationEditorPopup",
			"TeamEditorPopup",
			// Escape menu and its submenus
			"MissionEscapeMenu",
			"MissionOptions",
			"KeyBindingPopup",
			"GameModeMenu",
			"MultiplayerAdminPanel",
			// Inquiry popups (e.g. quit confirmation)
			"QueryManager",
			"ChatLog"
		};

		private const float ActRevealGraceSec = 1f;

		private GauntletLayer _gauntletLayer;
		private ScenarioVM _dataSource;
		private ScenarioClientBehavior _scenarioClientBehavior;
		private CinematicView _cinematicView;

		private bool _showBoard;
		private bool _mouseRequestedWhileBoardActive;
		private bool _isMouseVisible;
		private float _scenarioIntroDuration = 10f;
		private float _scenarioIntroTimeElapsed;
		private bool _scenarioIntroRequested;
		private float _resultDuration = 10f;
		private float _resultTimeElapsed;
		private bool _resultRequested;

		private bool _actRevealPending;
		private float _actRevealTimeElapsed;

		private bool _blackoutVisible;
		private readonly HashSet<ScreenLayer> _hiddenLayers = new HashSet<ScreenLayer>();

		public ScenarioView()
		{
			// Below the team selection (22), spawn menu (26) and editor popups (30) so they
			// render above the waiting screen blackout.
			ViewOrderPriority = 21;
		}

		public ScenarioVM GetDataSource() => _dataSource;

		public override void OnMissionScreenInitialize()
		{
			InitializeLayer();
			Mission.IsFriendlyMission = false;
			GameKeyContext category = HotKeyManager.GetCategory("ScoreboardHotKeyCategory");
			if (!MissionScreen.SceneLayer.Input.IsCategoryRegistered(category))
			{
				MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(category);
			}

			_cinematicView = Mission.GetMissionBehavior<CinematicView>();

			RegisterScenarioEvents();
		}

		private void InitializeLayer()
		{
			_dataSource = new ScenarioVM();
			_gauntletLayer = new GauntletLayer("ScenarioBoard", ViewOrderPriority);
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

			_scenarioClientBehavior = Mission.Current.GetMissionBehavior<ScenarioClientBehavior>();
			_scenarioClientBehavior.State.OnLivesChanged += RefreshLivesDisplay;
		}

		private void UnregisterScenarioEvents()
		{
			MissionPeer.OnTeamChanged -= SetObjectives;
			ScenarioPlayer.Instance.OnActStateAwaitPlayerJoin -= ShowInitialScreen;
			ScenarioPlayer.Instance.OnActStateSpawnParticipants -= ShowSpawnScreen;
			ScenarioPlayer.Instance.OnActStateInProgress -= ShowStartScreen;
			ScenarioPlayer.Instance.OnActStateCompleted -= ShowEndScreen;
			_scenarioClientBehavior.State.OnLivesChanged -= RefreshLivesDisplay;
		}

		public void SetWaitingState(bool visible, bool holdingForCinematic, int ready, int total)
		{
			if (visible != _blackoutVisible)
			{
				_blackoutVisible = visible;
				_dataSource.IsBlackoutVisible = visible;
				if (visible)
				{
					_actRevealPending = false;
					_scenarioIntroRequested = false;
					_dataSource.ShowIntro = false;
					_resultRequested = false;
					_dataSource.ShowResult = false;
					ToggleScoreboard(false);
					HideOtherLayers();
				}
				else
				{
					RestoreLayers();
					RefreshLivesDisplay();
				}
			}
			if (visible)
			{
				_dataSource.IsWaiting = !holdingForCinematic;
				_dataSource.WaitingCounter = ready + " / " + total;
			}
		}

		private void HideOtherLayers()
		{
			if (_gauntletLayer == null) return;
			foreach (ScreenLayer layer in MissionScreen.Layers)
			{
				if (layer == _gauntletLayer || !(layer is GauntletLayer gauntletLayer)) continue;
				if (LayersAboveBlackoutWhitelist.Contains(layer.Name)) continue;
				Widget root = gauntletLayer.UIContext?.Root;
				if (root == null || !root.IsVisible) continue;
				root.IsVisible = false;
				_hiddenLayers.Add(layer);
			}
		}

		private void RestoreLayers()
		{
			foreach (ScreenLayer layer in _hiddenLayers)
			{
				if (layer.IsFinalized) continue;
				Widget root = (layer as GauntletLayer)?.UIContext?.Root;
				if (root != null) root.IsVisible = true;
			}
			_hiddenLayers.Clear();
		}

		public override void OnPreDisplayMissionTick(float dt)
		{
			base.OnPreDisplayMissionTick(dt);
			if (_blackoutVisible) HideOtherLayers();
		}

		private void SetObjectives(NetworkCommunicator peer, Team previousTeam, Team newTeam)
		{
			if (peer == null || !peer.IsMine || ScenarioPlayer.Instance.CurrentAct == null) return;
			_dataSource.SetObjectives(ScenarioPlayer.Instance.CurrentAct, newTeam.Side);

			// TODO : Find a better place for this, added it to fix title/desc not showing when reconnecting
			_dataSource.ActTitle = ScenarioPlayer.Instance.CurrentAct.Name.LocalizedText;
			_dataSource.ActDescription = ScenarioPlayer.Instance.CurrentAct.Description.LocalizedText;
		}

		public void ShowInitialScreen()
		{
			_dataSource.SetAct(ScenarioPlayer.Instance.CurrentAct);
			RefreshLivesDisplay();
			_actRevealPending = false;
		}

		public void ShowSpawnScreen()
		{
		}

		public void ShowStartScreen()
		{
			RefreshLivesDisplay();
			_actRevealPending = true;
			_actRevealTimeElapsed = 0f;
		}

		public void ShowEndScreen()
		{
		}

		public void ShowResultScreen(string title, string description, Color color)
		{
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

			if (_actRevealPending)
			{
				_actRevealTimeElapsed += dt;
				if (_actRevealTimeElapsed >= ActRevealGraceSec && !IsCinematicPlaying && !_blackoutVisible)
				{
					_actRevealPending = false;
					_scenarioIntroRequested = true;
					_scenarioIntroTimeElapsed = 0f;
					_dataSource.ShowIntro = true;
				}
			}

			// Intro screen
			if (_scenarioIntroRequested)
			{
				if (_scenarioIntroTimeElapsed >= _scenarioIntroDuration)
				{
					_dataSource.ShowIntro = false;
					_scenarioIntroRequested = false;
				}
				else
				{
					_scenarioIntroTimeElapsed += dt;
				}
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
			bool boardAllowed = !_blackoutVisible && !IsCinematicPlaying;
			bool activateBoard = boardAllowed && isTabPressed && !MissionScreen.IsRadialMenuActive && !Mission.IsOrderMenuOpen;
			ToggleScoreboard(activateBoard);

			// Check to show mouse cursor
			if (_showBoard && (MissionScreen.SceneLayer.Input.IsGameKeyPressed(35) || _gauntletLayer.Input.IsGameKeyPressed(35)))
			{
				_mouseRequestedWhileBoardActive = true;
			}
			bool showCursor = _showBoard && _mouseRequestedWhileBoardActive;
			SetMouseState(showCursor);
		}

		private bool IsCinematicPlaying => _cinematicView != null && _cinematicView.IsPlaying;

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

		private void RefreshLivesDisplay()
		{
			_dataSource.ShowLives = ScenarioPlayer.Instance.ActState == ActState.InProgress && !_blackoutVisible;

			_dataSource?.SetLives(
				_scenarioClientBehavior.State.RespawnStrategy,
				_scenarioClientBehavior.State.TeamRemainingLives,
				_scenarioClientBehavior.State.PlayerRemainingLives);
		}

		private void FinalizeLayer()
		{
			RestoreLayers();
			_blackoutVisible = false;
			_cinematicView = null;
			_dataSource.OnFinalize();
			MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_dataSource = null;
			_showBoard = false;
			UnregisterScenarioEvents();
		}
	}
}
