#if !SERVER
using Alliance.Common.Core.KeyBinder;
using Alliance.Common.Core.KeyBinder.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.ViewModels;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Views
{
	public class PlayerSpawnMenuView : MissionView, IUseKeyBinder
	{
		private GauntletLayer _layer;
		private PlayerSpawnMenuVM _dataSource;
		private GameKey _menuKey;

		private Action<PlayerSpawnMenu> _onMenuClosed;

		public bool IsMenuOpen { get; private set; }
		public bool Enabled { get; private set; }

		private static readonly string KeyCategoryId = "alliance_player_spawn_cat";

		public BindedKeyCategory BindedKeys => new BindedKeyCategory
		{
			CategoryId = KeyCategoryId,
			Category = "Alliance",
			Keys = new List<BindedKey>
			{
				new BindedKey
				{
					Id = "key_menu",
					Description = "Open the player spawn menu.",
					Name = "Player spawn menu",
					DefaultInputKey = InputKey.P
				}
			}
		};

		public override void OnBehaviorInitialize()
		{
			Dictionary<string, GameKeyContext>.ValueCollection test = HotKeyManager.GetAllCategories();
			_menuKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_menu");
		}

		public override void OnMissionTick(float dt)
		{
			Enabled = true;
			if (Enabled && !IsMenuOpen && CheckOpenMenuKeyPress())
			{
				// Open menu with default instance of PlayerSpawnMenu
				OpenMenu(PlayerSpawnMenu.Instance);
			}
			else if (IsMenuOpen && CheckCloseMenuKeyPress())
			{
				CloseMenu();
			}
		}

		private bool CheckOpenMenuKeyPress()
		{
			// Safely checks if the menu key is pressed. Input can be null in Modding Kit context.
			if (MissionScreen?.SceneLayer?.Input != null)
			{
				return Input.IsKeyPressed(_menuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_menuKey.ControllerKey.InputKey);
			}
			else
			{
				return TaleWorlds.InputSystem.Input.IsKeyPressed(_menuKey.KeyboardKey.InputKey) || TaleWorlds.InputSystem.Input.IsKeyPressed(_menuKey.ControllerKey.InputKey);
			}
		}

		private bool CheckCloseMenuKeyPress()
		{
			// Safely checks if the menu key is pressed. Input can be null in Modding Kit context.
			if (MissionScreen?.SceneLayer?.Input != null)
			{
				return Input.IsKeyPressed(_menuKey.KeyboardKey.InputKey) || _layer.Input.IsKeyPressed(_menuKey.ControllerKey.InputKey) || _layer.Input.IsKeyPressed(InputKey.RightMouseButton) || Input.IsKeyReleased(InputKey.Escape);
			}
			else
			{
				return TaleWorlds.InputSystem.Input.IsKeyPressed(_menuKey.KeyboardKey.InputKey) || TaleWorlds.InputSystem.Input.IsKeyPressed(_menuKey.ControllerKey.InputKey) || TaleWorlds.InputSystem.Input.IsKeyPressed(InputKey.RightMouseButton) || TaleWorlds.InputSystem.Input.IsKeyReleased(InputKey.Escape);
			}
		}

		public void Enable()
		{
			Enabled = true;
		}

		public void Disable()
		{
			Enabled = false;
			if (IsMenuOpen)
			{
				CloseMenu();
			}
		}

		public void OpenMenu(PlayerSpawnMenu playerSpawnMenu, Action<PlayerSpawnMenu> onCloseCallback = null)
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

				_layer = new GauntletLayer(25, "GauntletLayer");
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
				//ScreenManager.AddLayer(_layer);

				_dataSource.IsVisible = true;
				IsMenuOpen = true;
			}
			catch (Exception ex)
			{
				Log($"Error opening menu: {ex.Message}", LogLevel.Error);
			}
		}

		private void CloseMenu()
		{
			try
			{
				if (_layer != null)
				{
					_dataSource.OnFinalize();
					_layer.InputRestrictions.ResetInputRestrictions();
					_dataSource.IsVisible = false;
					ScreenManager.TryLoseFocus(_layer);
					ScreenManager.TopScreen?.RemoveLayer(_layer);
					_onMenuClosed?.Invoke(_dataSource.PlayerSpawnMenu);
					//ScreenManager.RemoveLayer(_layer);
				}
			}
			catch (Exception ex)
			{
				Log($"Error closing menu: {ex.Message}", LogLevel.Error);
			}
			_layer = null;
			_dataSource = null;
			IsMenuOpen = false;
		}
	}
}
#endif