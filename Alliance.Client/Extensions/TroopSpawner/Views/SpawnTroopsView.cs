using Alliance.Client.Core.KeyBinder.Models;
using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Client.Extensions.TroopSpawner.Utilities;
using Alliance.Client.Extensions.TroopSpawner.ViewModels;
using Alliance.Common.Core.Security.Extension;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.TroopSpawner.Views
{
    public class SpawnTroopsView : MissionView, IUseKeyBinder
    {
        public bool IsMenuOpen;
        private static string KeyCategoryId = "alliance_spawn_cat";
        BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory
        {
            CategoryId = KeyCategoryId,
            Category = "Alliance",
            Keys = new List<BindedKey>
            {
                new BindedKey
                {
                    Id = "key_menu",
                    Description = "Open the recruitment menu (only in commander mode).",
                    Name = "Recruitment menu",
                    DefaultInputKey = InputKey.C
                },
                new BindedKey
                {
                    Id = "key_recruit",
                    Description = "Recruit soldiers (only in commander mode).",
                    Name = "Recruit",
                    DefaultInputKey = InputKey.V
                },
                new BindedKey
                {
                    Id = "key_spawn",
                    Description = "Spawn soldiers at cursor location (admin only).",
                    Name = "Admin only - Spawn",
                    DefaultInputKey = InputKey.B
                },
                new BindedKey
                {
                    Id = "key_siege_spawn",
                    Description = "Shift + this to spawn the thing (dev only).",
                    Name = "Dev only - Spawn the thing",
                    DefaultInputKey = InputKey.N
                }
            }
        };

        private GauntletLayer _layer;
        private SpawnTroopsVM _dataSource;
        private GameKey menuKey;
        private GameKey recruitKey;
        private GameKey spawnKey;
        private GameKey siegeSpawnKey;
        private float _lastSpawnCommand;
        private bool _initialized;

        public SpawnTroopsView()
        {
        }

        public override void EarlyStart()
        {
            menuKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_menu");
            recruitKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_recruit");
            spawnKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_spawn");
            siegeSpawnKey = HotKeyManager.GetCategory(KeyCategoryId).GetGameKey("key_siege_spawn");
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
                _dataSource = new SpawnTroopsVM();
                _dataSource.OnCloseMenu += OnCloseMenu;
                _layer = new GauntletLayer(25) { };
                _layer.InputRestrictions.SetInputRestrictions();
                _layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
                _layer.LoadMovie("SpawnMenu", _dataSource);
                SpriteData spriteData = UIResourceManager.SpriteData;
                TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
                ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
                spriteData.SpriteCategories["ui_mplobby"].Load(resourceContext, uiResourceDepot);
                spriteData.SpriteCategories["ui_order"].Load(resourceContext, uiResourceDepot);
                MissionScreen.AddLayer(_layer);
                _initialized = true;
            }
            catch (Exception ex)
            {
                Log("Alliance - Error opening spawn menu :", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
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
            MissionPeer peer = GameNetwork.MyPeer.GetComponent<MissionPeer>();

            // Only show recruitment menu to admins & commanders
            if (peer?.Team == null || !GameNetwork.MyPeer.IsCommander() && !GameNetwork.MyPeer.IsAdmin())
            {
                return;
            }

            TickInputs();
        }

        private void TickInputs()
        {
            if (IsMenuOpen)
            {
                if (Input.IsKeyPressed(menuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(menuKey.ControllerKey.InputKey) || _layer.Input.IsKeyPressed(InputKey.RightMouseButton) || _layer.Input.IsHotKeyReleased("Exit"))
                {
                    CloseMenu();
                }
            }
            else
            {
                if (Input.IsKeyPressed(menuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(menuKey.ControllerKey.InputKey))
                {
                    OpenMenu();
                }
                if (Input.IsKeyPressed(recruitKey.KeyboardKey.InputKey) || Input.IsKeyPressed(recruitKey.ControllerKey.InputKey))
                {
                    SpawnRequestHelper.RequestSpawnTroop();
                }
                if (Input.IsKeyDown(spawnKey.KeyboardKey.InputKey) || Input.IsKeyDown(spawnKey.ControllerKey.InputKey))
                {
                    // Spawn every 0.15sec max
                    if (Input.IsKeyDown(InputKey.LeftShift) || Mission.CurrentTime > _lastSpawnCommand + 0.15f)
                    {
                        _lastSpawnCommand = Mission.CurrentTime;
                        SpawnTroop();
                    }
                }
                if (Input.IsKeyDown(InputKey.LeftShift) && Input.IsKeyPressed(siegeSpawnKey.KeyboardKey.InputKey) || Input.IsKeyPressed(siegeSpawnKey.ControllerKey.InputKey))
                {
                    SpawnTheThing();
                }
            }
        }

        // Admin command - Spawn troop at exact location
        private void SpawnTroop()
        {
            bool validTargetArea = MissionScreen.GetProjectedMousePositionOnGround(out var groundPos, out _, BodyFlags.BodyOwnerFlora, true);
            if (!validTargetArea)
            {
                Log("Invalid spawn target area", LogLevel.Error);
                return;
            }
            if (SpawnTroopsModel.Instance.SelectedTroop?.StringId == null)
            {
                Log("No troop selected for spawn", LogLevel.Error);
                return;
            }

            SpawnRequestHelper.AdminRequestSpawnTroop(groundPos);
        }

        // Dev command - Spawn the thing at exact location
        private void SpawnTheThing()
        {
            if (!GameNetwork.MyPeer.IsDev()) return;

            bool validTargetArea = MissionScreen.GetProjectedMousePositionOnGround(out var groundPos, out _, BodyFlags.BodyOwnerFlora, true);
            if (!validTargetArea)
            {
                Log("Invalid spawn target area", LogLevel.Error);
                return;
            }

            SpawnRequestHelper.RequestSpawnTheThing(groundPos);
        }

        private void OnCloseMenu(object sender, EventArgs e)
        {
            CloseMenu();
        }
    }
}