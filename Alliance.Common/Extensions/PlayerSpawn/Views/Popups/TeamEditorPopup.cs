#if !SERVER
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.ViewModels.Popups;
using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Views.Popups
{
	/// <summary>
	/// Popup for formation settings.
	/// </summary>
	public class TeamEditorPopup
	{
		private GauntletLayer _layer;
		private TeamEditorVM _dataSource;
		private Action<PlayerTeam> _onMenuClosed;

		public bool IsMenuOpen { get; private set; }

		public void Initialize()
		{
		}

		public void OpenMenu(PlayerTeam team, Action<PlayerTeam> onMenuClosed)
		{
			_onMenuClosed = onMenuClosed;
			try
			{
				_dataSource = new TeamEditorVM(team);
				_dataSource.OnCloseMenu += (_, _) => CloseMenu();

				_layer = new GauntletLayer(30, "GauntletLayer");
				_layer.InputRestrictions.SetInputRestrictions();
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				_layer.LoadMovie("TeamEditorPopup", _dataSource);

				UIResourceManager.SpriteData.SpriteCategories["ui_mplobby"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_mpintermission"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);
				UIResourceManager.SpriteData.SpriteCategories["ui_order"].Load(UIResourceManager.ResourceContext, UIResourceManager.UIResourceDepot);

				ScreenManager.TopScreen?.AddLayer(_layer);
				ScreenManager.TrySetFocus(_layer);

				IsMenuOpen = true;
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
				_onMenuClosed?.Invoke(_dataSource.Team);
				if (_layer != null)
				{
					_layer.InputRestrictions.ResetInputRestrictions();
					ScreenManager.TryLoseFocus(_layer);
					ScreenManager.TopScreen?.RemoveLayer(_layer);
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