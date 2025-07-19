#if !SERVER
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Views
{
	public class PlayerSpawnMenuView : MissionView
	{
		private GauntletLayer _layer;
		private PlayerSpawnMenuVM _dataSource;
		private bool _recentlyClosed;

		private Action<PlayerSpawnMenu> _onMenuClosed;

		public bool IsMenuOpen { get; private set; }

		public override void OnBehaviorInitialize()
		{
			// todo wip , join team
			if (GameNetwork.MyPeer != null && GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team != null)
			{
				PlayerSpawnMenu.Instance.SelectTeam();
			}

			MissionPeer.OnTeamChanged += OnPlayerChangeTeam;
			PlayerSpawnMenu.OnSpawnStatusChanged += OnSpawnStatusChanged;
		}

		public override void OnRemoveBehavior()
		{
			MissionPeer.OnTeamChanged -= OnPlayerChangeTeam;
			PlayerSpawnMenu.OnSpawnStatusChanged -= OnSpawnStatusChanged;
		}

		// TODO : improve team change logic / team selection ?
		private void OnPlayerChangeTeam(NetworkCommunicator peer, Team previousTeam, Team newTeam)
		{
			if (peer.IsMine)
			{
				PlayerSpawnMenu.Instance.SelectTeam(PlayerSpawnMenu.Instance.Teams.Find(team => team.TeamSide == newTeam.Side));
				// Refresh the player spawn menu when the player changes team
				_dataSource?.GenerateMenu();
			}
		}

		private void OnSpawnStatusChanged(bool spawnEnabled)
		{
			if (spawnEnabled && GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team != null
					&& GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team != Mission.SpectatorTeam
					&& Agent.Main == null)
			{
				if (!IsMenuOpen)
				{
					if (PlayerSpawnMenu.Instance.MyAssignment.Team == null)
					{
						PlayerSpawnMenu.Instance.SelectTeam();
					}
					OpenMenu(PlayerSpawnMenu.Instance);
				}
			}
		}

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			base.OnAgentBuild(agent, banner);

			if (agent.MissionPeer != null && agent.MissionPeer.IsMine && IsMenuOpen)
			{
				// If the agent is created, close the menu if it was open
				CloseMenu();
			}
		}

		// todo improve this (user experience)
		public override void OnMissionTick(float dt)
		{
			// In-game logic (in multiplayer context)
			if (GameNetwork.IsClient)
			{
				if (GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team != null
					&& GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team != Mission.SpectatorTeam
					&& Agent.Main == null)
				{
					// If the player has no agent and no character assigned, open the spawn menu
					if (!IsMenuOpen && !_recentlyClosed)
					{
						if (PlayerSpawnMenu.Instance.MyAssignment.Team == null)
						{
							PlayerSpawnMenu.Instance.SelectTeam();
						}
						OpenMenu(PlayerSpawnMenu.Instance);
					}
				}
			}

			if (IsMenuOpen && CheckCloseMenuKeyPress())
			{
				CloseMenu();
			}

			if (PlayerSpawnMenu.Instance == null) return;

			RefreshVM(dt);
		}

		private void RefreshVM(float dt)
		{
			if (!GameNetwork.IsClient) return;

			// Update Election status
			if (PlayerSpawnMenu.Instance.ElectionInProgress && PlayerSpawnMenu.Instance.TimeBeforeOfficerElection > 0f)
			{
				PlayerSpawnMenu.Instance.TimeBeforeOfficerElection -= dt;
				if (_dataSource != null)
				{
					_dataSource.TimeBeforeOfficerElection = (float)Math.Round(PlayerSpawnMenu.Instance.TimeBeforeOfficerElection, 0);
				}
			}

			// Update Spawn status
			if (PlayerSpawnMenu.Instance.MyAssignment.CanSpawn && PlayerSpawnMenu.Instance.MyAssignment.TimeBeforeSpawn > 0f && PlayerSpawnMenu.Instance.MyAssignment.Character != null)
			{
				PlayerSpawnMenu.Instance.MyAssignment.TimeBeforeSpawn = Math.Max(PlayerSpawnMenu.Instance.MyAssignment.TimeBeforeSpawn - dt, 0f);
				if (_dataSource != null)
				{
					_dataSource.TimeBeforeSpawn = (float)Math.Round(PlayerSpawnMenu.Instance.MyAssignment.TimeBeforeSpawn, 1);
				}
			}
		}

		private bool CheckCloseMenuKeyPress()
		{
			// Safely checks if the menu key is pressed. Input can be null in Modding Kit context.
			if (MissionScreen?.SceneLayer?.Input != null)
			{
				return Input.IsKeyReleased(InputKey.Escape);
			}
			else
			{
				return TaleWorlds.InputSystem.Input.IsKeyReleased(InputKey.Escape);
			}
		}

		public void OpenMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback = null, bool editMode = false)
		{
			if (IsMenuOpen)
			{
				Log($"Menu is already opened", LogLevel.Error);
				return;
			}
			try
			{
				Characters.Instance.TryRefreshCharacters();
				_dataSource = new PlayerSpawnMenuVM(playerSpawnMenu);
				_onMenuClosed += onCloseCallback;
				_dataSource.OnCloseMenu += CloseMenu;

				_layer = new GauntletLayer(26, "GauntletLayer");
				_layer.InputRestrictions.SetInputRestrictions();
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				_layer.LoadMovie("PlayerSpawnMenu", _dataSource);

				UIResourceManager.SpriteData.SpriteCategories["ui_mplobby"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_mpintermission"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_order"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_kingdom"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_facegen"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_conversation"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);

				ScreenManager.TopScreen?.AddLayer(_layer);
				ScreenManager.TrySetFocus(_layer);

				_dataSource.EditMode = editMode;
				IsMenuOpen = true;
				_recentlyClosed = false;
			}
			catch (Exception ex)
			{
				Log($"Error opening menu: {ex.Message}", LogLevel.Error);
			}
		}

		public void CloseMenu()
		{
			try
			{
				if (_layer != null)
				{
					_dataSource.OnCloseMenu -= CloseMenu;
					_dataSource.OnFinalize();
					_layer.InputRestrictions.ResetInputRestrictions();
					ScreenManager.TryLoseFocus(_layer);
					ScreenManager.TopScreen?.RemoveLayer(_layer);
					_onMenuClosed?.Invoke(_dataSource.PlayerSpawnMenu);
				}
			}
			catch (Exception ex)
			{
				Log($"Error closing menu: {ex.Message}", LogLevel.Error);
			}
			_layer = null;
			_dataSource = null;
			IsMenuOpen = false;
			_recentlyClosed = true;
		}
	}
}
#endif