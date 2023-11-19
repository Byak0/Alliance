using Alliance.Client.Core.KeyBinder.Models;
using Alliance.Client.Extensions.AdminMenu.ViewModels;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.AdminMenu.Views
{
    public class AdminSystem : MissionView, IUseKeyBinder
    {
        private static string adminKeyCategoryId = "admin_sys";
        BindedKeyCategory IUseKeyBinder.BindedKeys => new BindedKeyCategory()
        {
            CategoryId = adminKeyCategoryId,
            Category = "Alliance - Admin System",
            Keys = new List<BindedKey>()
                {
                    new BindedKey()
                    {
                        Id = "key_adm_getplayermouse",
                        Description = "Target a player and open menu with his informations.",
                        Name = "Get player by mouse",
                        DefaultInputKey = InputKey.Comma,
                    },
                    new BindedKey()
                    {
                        Id = "key_adm_openmenu",
                        Description = "Open/close admin menu.",
                        Name = "Open menu",
                        DefaultInputKey = InputKey.K,
                    },
                    new BindedKey()
                    {
                        Id = "key_adm_teleport",
                        Description = "Teleport to cursor.",
                        Name = "Teleport",
                        DefaultInputKey = InputKey.NumpadMultiply,
                    },
                }
        };

        GameKey getPlayerKey;
        GameKey openMenuKey;
        GameKey teleportKey;

        GauntletLayer _layerLoaded;
        private AdminVM _adminVM;
        private bool _isMenuOpen;

        public override void EarlyStart()
        {
            getPlayerKey = HotKeyManager.GetCategory(adminKeyCategoryId).GetGameKey("key_adm_getplayermouse");
            openMenuKey = HotKeyManager.GetCategory(adminKeyCategoryId).GetGameKey("key_adm_openmenu");
            teleportKey = HotKeyManager.GetCategory(adminKeyCategoryId).GetGameKey("key_adm_teleport");
        }

        private void OpenAdminPanel(AdminVM adminVM)
        {
            if (_layerLoaded != null)
                MissionScreen.RemoveLayer(_layerLoaded);

            _layerLoaded = new GauntletLayer(2);
            IGauntletMovie movie = _layerLoaded.LoadMovie("AdminPanel", adminVM);
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
            spriteData.SpriteCategories["ui_mplobby"].Load(resourceContext, uiResourceDepot);
            _layerLoaded.IsFocusLayer = true;
            _layerLoaded.InputRestrictions.SetInputRestrictions();
            _layerLoaded.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
            ScreenManager.TrySetFocus(_layerLoaded);
            MissionScreen.AddLayer(_layerLoaded);
            AdminInstance.SetInstance(adminVM);
            _isMenuOpen = true;
        }

        private void CloseAdminPanel()
        {
            MissionScreen.RemoveLayer(_layerLoaded);
            _isMenuOpen = false;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);

            if (_isMenuOpen)
            {
                if (_layerLoaded.Input.IsKeyPressed(getPlayerKey.KeyboardKey.InputKey) || _layerLoaded.Input.IsKeyPressed(openMenuKey.KeyboardKey.InputKey) || _layerLoaded.Input.IsKeyPressed(InputKey.RightMouseButton) || _layerLoaded.Input.IsKeyPressed(InputKey.Escape) || Input.IsKeyPressed(InputKey.LeftMouseButton))
                {
                    CloseAdminPanel();
                }
            }
            else
            {
                if (GameNetwork.MyPeer.IsAdmin())
                {
                    if (Input.IsKeyPressed(teleportKey.KeyboardKey.InputKey) || Input.IsKeyPressed(teleportKey.ControllerKey.InputKey))
                    {
                        TeleportToMouse();
                    }
                    else if (Input.IsKeyPressed(getPlayerKey.KeyboardKey.InputKey) || Input.IsKeyPressed(getPlayerKey.ControllerKey.InputKey))
                    {
                        GetPlayerByMouse();
                    }
                    else if (Input.IsKeyPressed(openMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(openMenuKey.ControllerKey.InputKey))
                    {
                        AdminInstance.GetInstance().RefreshPlayerList();
                        OpenAdminPanel(AdminInstance.GetInstance());
                    }
                }
            }
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenFinalize();
        }

        private void TeleportToMouse()
        {
            MissionScreen.ScreenPointToWorldRay(Input.GetMousePositionRanged(), out var rayBegin, out var rayEnd);
            bool validPosition = MissionScreen.GetProjectedMousePositionOnGround(out Vec3 groundPosition, out Vec3 groundNormal, TaleWorlds.Engine.BodyFlags.None, false);
            if (validPosition)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new TeleportRequest(groundPosition));
                GameNetwork.EndModuleEventAsClient();
                Log($"Requesting teleport to {groundPosition}", LogLevel.Debug);
            }
            else
            {
                Log($"Invalid teleport position", LogLevel.Information);
            }
        }

        private void GetPlayerByMouse()
        {
            MissionScreen.ScreenPointToWorldRay(Input.GetMousePositionRanged(), out var rayBegin, out var rayEnd);

            Agent agent = Mission.Current.RayCastForClosestAgent(rayBegin, rayEnd, out var distance, -1, 0.1f);

            if (agent != null)
            {
                GetTargetInformation(agent);
                return;
            }

            Log($"[AdminPanel] No agent found.", LogLevel.Information);
        }

        private void GetTargetInformation(Agent agent)
        {
            Log($"[AdminPanel] Target : {(agent.MissionPeer?.Name ?? agent.Name)}", LogLevel.Information);
            AdminVM adminVM = AdminInstance.GetInstance();
            adminVM.RefreshPlayerList();
            NetworkPeerVM peerVM = new NetworkPeerVM()
            {
                Username = agent.MissionPeer?.Name ?? agent.Name,
                AgentIndex = agent.Index,
                PeerId = agent.MissionPeer?.Peer?.Id.ToString()
            };
            adminVM.SelectTarget(agent);
            OpenAdminPanel(adminVM);
        }
    }
}
