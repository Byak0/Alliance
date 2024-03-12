using Alliance.Client.Extensions.BrushPicker.ViewModels;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.BrushPicker.Views
{
#if DEBUG
    [DefaultView]
#endif
    /// <summary>
    /// Utility view for debugging and creating interface. 
    /// Press P to show available brushes.
    /// Press O to show the scene normal grid.
    /// </summary>
    public class BrushPickerView : MissionView
    {
        public bool IsMenuOpen;

        private GauntletLayer _layer;
        private BrushPickerVM _dataSource;
        private bool _initialized;

        public BrushPickerView()
        {
        }

        public override void OnMissionScreenFinalize()
        {
            if (_initialized)
            {
                _layer.InputRestrictions.ResetInputRestrictions();
                MissionScreen.RemoveLayer(_layer);
                _dataSource.OnCloseMenu -= OnCloseMenu;
                _dataSource.OnFinalize();
                _dataSource = null;
                _layer = null;
                _initialized = false;
            }
        }

        private void Init()
        {
            try
            {
                _dataSource = new BrushPickerVM();
                _dataSource.OnCloseMenu += OnCloseMenu;
                _layer = new GauntletLayer(25) { };
                _layer.InputRestrictions.SetInputRestrictions();
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
                _layer.LoadMovie("BrushPicker", _dataSource);
                SpriteData spriteData = UIResourceManager.SpriteData;
                TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
                ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
                // LOAD EVERYTHING
                foreach (KeyValuePair<string, SpriteCategory> sc in spriteData.SpriteCategories)
                {
                    sc.Value.Load(resourceContext, uiResourceDepot);
                }
                MissionScreen.AddLayer(_layer);
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log("Alliance - Error opening BrushPicker :", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        private void OpenMenu()
        {
            if (!_initialized) Init();
            _layer.InputRestrictions.SetInputRestrictions();
            _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
            ScreenManager.TrySetFocus(_layer);
            _dataSource.IsVisible = true;
            IsMenuOpen = true;
        }

        private void CloseMenu()
        {
            _layer.InputRestrictions.ResetInputRestrictions();
            _dataSource.IsVisible = false;
            IsMenuOpen = false;
        }

        public override void OnMissionScreenTick(float dt)
        {
            TickInputs();
        }

        private void TickInputs()
        {
            if (IsMenuOpen)
            {
                if (_layer.Input.IsKeyPressed(InputKey.RightMouseButton) || _layer.Input.IsHotKeyReleased("Exit"))
                {
                    CloseMenu();
                }
            }
            else
            {
                if (Input.IsKeyPressed(InputKey.P))
                {
                    OpenMenu();
                }
                if (Input.IsKeyDown(InputKey.O))
                {
                    ShowNormalGrid();
                }
            }
        }

        private void ShowNormalGrid()
        {
            float gridSpacing = 10f; // 10 meter grid spacing

            for (float x = 1; x <= 50; x += gridSpacing)
            {
                for (float y = 1; y <= 50; y += gridSpacing)
                {
                    Vec2 position = new Vec2(x, y);
                    Vec3 normal = Mission.Scene.GetNormalAt(position);
                    Vec3 start = new Vec3(x, y, Mission.Scene.GetGroundHeightAtPosition(new Vec3(position)));
                    MBDebug.RenderDebugDirectionArrow(new Vec3(position), normal, GetRandomColor((int)(x + y))); // Draw an arrow from start to end
                }
            }
        }

        private uint GetRandomColor(int seed)
        {
            Random random = new Random(seed);
            float r = (float)random.NextDouble();
            float g = (float)random.NextDouble();
            float b = (float)random.NextDouble();
            Color randomColor = new Color(r, g, b);
            return randomColor.ToUnsignedInteger();
        }

        private void OnCloseMenu(object sender, EventArgs e)
        {
            CloseMenu();
        }
    }
}
