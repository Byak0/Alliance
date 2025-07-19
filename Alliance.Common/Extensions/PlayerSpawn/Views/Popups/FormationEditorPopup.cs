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
	public class FormationEditorPopup
	{
		private GauntletLayer _layer;
		private FormationEditorVM _dataSource;
		private Action<PlayerFormation> _onMenuClosed;

		public bool IsMenuOpen { get; private set; }

		public void Initialize()
		{
		}

		public void OpenMenu(PlayerFormation formation, Action<PlayerFormation> onMenuClosed)
		{
			_onMenuClosed = onMenuClosed;
			try
			{
				_dataSource = new FormationEditorVM(formation);
				_dataSource.OnCloseMenu += OnCloseMenu;

				_layer = new GauntletLayer(30, "GauntletLayer");
				_layer.InputRestrictions.SetInputRestrictions();
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				_layer.LoadMovie("FormationEditorPopup", _dataSource);

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

		private void OnCloseMenu(object sender, EventArgs e)
		{
			CloseMenu();
		}

		public void CloseMenu()
		{
			try
			{
				_onMenuClosed?.Invoke(_dataSource.Formation);
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