using Alliance.Client.Extensions.GameModeMenu.ViewModels;
using System;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace Alliance.Client.GameModes.Story.Views
{
    /// <summary>
    /// View handling transitions for scenario.
    /// Fade in/out on loading and act switching.
    /// </summary>
    public class ScenarioTransitionView : MissionView
    {
        private GauntletLayer _layer;
        private GameModeMenuVM _dataSource;

        public ScenarioTransitionView()
        {
        }

        public bool IsMenuOpen;

        public override void EarlyStart()
        {
        }

        public override void OnMissionScreenFinalize()
        {
            if (IsMenuOpen)
            {
                _layer.InputRestrictions.ResetInputRestrictions();
                MissionScreen.RemoveLayer(_layer);
                _dataSource.OnCloseMenu -= OnCloseMenu;
                _dataSource.OnFinalize();
                _dataSource = null;
                _layer = null;
                IsMenuOpen = false;
            }
        }

        public override void OnMissionScreenTick(float dt)
        {
            if (IsMenuOpen)
            {
                if (_layer.Input.IsKeyPressed(InputKey.RightMouseButton) || _layer.Input.IsHotKeyReleased("Exit"))
                {
                    CloseMenu();
                }
            }
        }

        public void OpenMenu()
        {
            try
            {
                _dataSource = new GameModeMenuVM();
                _dataSource.OnCloseMenu += OnCloseMenu;
                _layer = new GauntletLayer(25) { };
                _layer.InputRestrictions.SetInputRestrictions();
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
                _layer.LoadMovie("GameModeRequestMenu", _dataSource);
                SpriteData spriteData = UIResourceManager.SpriteData;
                TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
                ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
                spriteData.SpriteCategories["ui_mplobby"].Load(resourceContext, uiResourceDepot);
                MissionScreen.AddLayer(_layer);
                ScreenManager.TrySetFocus(_layer);
                IsMenuOpen = true;
            }
            catch (Exception ex)
            {
                InformationManager.DisplayMessage(new InformationMessage("PvC - Error opening GameMode menu :", Colors.Red));
                InformationManager.DisplayMessage(new InformationMessage(ex.Message, Colors.Red));
            }
        }

        public void CloseMenu()
        {
            _layer.InputRestrictions.ResetInputRestrictions();
            MissionScreen.RemoveLayer(_layer);
            _dataSource.OnCloseMenu -= OnCloseMenu;
            _dataSource.OnFinalize();
            _dataSource = null;
            _layer = null;
            IsMenuOpen = false;
        }

        private void OnCloseMenu(object sender, EventArgs e)
        {
            CloseMenu();
        }
    }
}