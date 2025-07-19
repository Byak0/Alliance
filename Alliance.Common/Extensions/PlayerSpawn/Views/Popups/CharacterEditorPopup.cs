#if !SERVER
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.ViewModels.Popups;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.ScreenSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerSpawn.Views.Popups
{
	/// <summary>
	/// Popup for selecting a character when editing the player spawn menu.
	/// </summary>
	public class CharacterEditorPopup
	{
		private GauntletLayer _layer;
		private CharacterEditorVM _dataSource;
		private Action<AvailableCharacter> _onMenuClosed;

		public bool IsMenuOpen { get; private set; }

		public void Initialize()
		{
		}

		public void OpenMenu(AvailableCharacter availableCharacter, BasicCultureObject culture = null, Action<AvailableCharacter> onMenuClosed = null)
		{
			if (IsMenuOpen)
			{
				CloseMenu();
			}
			_onMenuClosed = onMenuClosed;
			try
			{
				_dataSource = new CharacterEditorVM(availableCharacter, culture);
				_dataSource.OnCloseMenu += OnCloseMenu;

				_layer = new GauntletLayer(30, "GauntletLayer");
				_layer.InputRestrictions.SetInputRestrictions();
				_layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				_layer.LoadMovie("CharacterEditorPopup", _dataSource);

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
				if (_layer != null)
				{
					_onMenuClosed?.Invoke(_dataSource?.AvailableCharacter);
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