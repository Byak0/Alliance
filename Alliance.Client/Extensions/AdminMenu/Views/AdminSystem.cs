using Alliance.Client.Core.KeyBinder.Models;
using Alliance.Client.Extensions.AdminMenu.ViewModels;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.AdminMenu.Views
{
	[DefaultView]
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
						DefaultInputKey = InputKey.B,
					},
					new BindedKey()
					{
						Id = "key_adm_openmenu",
						Description = "Open/close admin menu.",
						Name = "Open menu",
						DefaultInputKey = InputKey.N,
					},
					new BindedKey()
					{
						Id = "key_adm_teleport",
						Description = "Teleport to cursor.",
						Name = "Teleport",
						DefaultInputKey = InputKey.M,
					},
				}
		};

		GameKey getPlayerKey;
		GameKey openMenuKey;
		GameKey teleportKey;

		GauntletLayer _layerLoaded;
		private bool _isMenuOpen;
		private IGauntletMovie _movie;
		public Agent CurrentHoverAgent { get; private set; }

		public bool IsModoModeActive { get; private set; }

		public override void EarlyStart()
		{
			getPlayerKey = HotKeyManager.GetCategory(adminKeyCategoryId).GetGameKey("key_adm_getplayermouse");
			openMenuKey = HotKeyManager.GetCategory(adminKeyCategoryId).GetGameKey("key_adm_openmenu");
			teleportKey = HotKeyManager.GetCategory(adminKeyCategoryId).GetGameKey("key_adm_teleport");
		}

		private void InitLayer()
		{
			if (_layerLoaded == null)
			{
				AdminInstance.GetInstance().IsVisible = false;
				_layerLoaded ??= new GauntletLayer(2, "AdminSys", false);
				_movie ??= _layerLoaded.LoadMovie("AdminPanel", AdminInstance.GetInstance());
				_layerLoaded.InputRestrictions.SetInputRestrictions();
				_layerLoaded.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MultiplayerHotkeyCategory"));
				MissionScreen.AddLayer(_layerLoaded);
				ScreenManager.TrySetFocus(_layerLoaded);
			}
		}

		public override void OnMissionScreenFinalize()
		{
			if (_layerLoaded != null)
			{
				MissionScreen.RemoveLayer(_layerLoaded);
				_layerLoaded.InputRestrictions.ResetInputRestrictions();
				_layerLoaded = null;
			}

			base.OnMissionScreenFinalize();
		}

		private void StartModoMode()
		{
			InitLayer();
			_layerLoaded.InputRestrictions.SetInputRestrictions();
			IsModoModeActive = true;
		}

		private void StopModoMode()
		{
			_layerLoaded.InputRestrictions.ResetInputRestrictions();
			ScreenManager.TryLoseFocus(_layerLoaded);

			IsModoModeActive = false;
		}

		private void OpenAdminPanel(AdminVM adminVM)
		{
			InitLayer();
			_layerLoaded.InputRestrictions.SetInputRestrictions();
			AdminInstance.GetInstance().IsVisible = true;
			SpriteData spriteData = UIResourceManager.SpriteData;
			TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
			ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
			spriteData.SpriteCategories["ui_mplobby"].Load(resourceContext, uiResourceDepot);
			AdminInstance.SetInstance(adminVM);
			_isMenuOpen = true;
		}

		private void CloseAdminPanel()
		{
			AdminInstance.GetInstance().IsVisible = false;
			_isMenuOpen = false;

			if (!IsModoModeActive)
			{
				// Force to stop modo mode in order to reset cursor control
				StopModoMode();
			}
		}

		public override void OnMissionScreenTick(float dt)
		{
			if (_isMenuOpen)
			{
				if (
					Input.IsKeyPressed(getPlayerKey.KeyboardKey.InputKey)
					|| Input.IsKeyPressed(openMenuKey.KeyboardKey.InputKey)
					|| _layerLoaded.Input.IsKeyPressed(InputKey.RightMouseButton)
					|| _layerLoaded.Input.IsKeyPressed(InputKey.Escape)
					)
				{
					CloseAdminPanel();
				}
			}
			else
			{
				if (GameNetwork.MyPeer.IsAdmin())
				{
					if (IsModoModeActive)
					{
						if ((_layerLoaded.Input.IsKeyPressed(InputKey.LeftMouseButton) || Input.IsKeyPressed(InputKey.LeftMouseButton)) && !_isMenuOpen)
						{
							GetPlayerByMouse();
						}
						if (
							Input.IsKeyPressed(getPlayerKey.KeyboardKey.InputKey)
							|| Input.IsKeyPressed(InputKey.RightMouseButton)
							)
						{
							StopModoMode();
						}
					}
					else
					{
						if (Input.IsKeyPressed(getPlayerKey.KeyboardKey.InputKey) || Input.IsKeyPressed(getPlayerKey.ControllerKey.InputKey))
						{
							StartModoMode();
						}

						if (Input.IsKeyPressed(openMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(openMenuKey.ControllerKey.InputKey))
						{
							AdminInstance.GetInstance().RefreshPlayerList();
							OpenAdminPanel(AdminInstance.GetInstance());
						}
					}

					if (Input.IsKeyPressed(teleportKey.KeyboardKey.InputKey) || Input.IsKeyPressed(teleportKey.ControllerKey.InputKey))
					{
						TeleportToMouse();
					}
				}
			}
		}

		public override void OnMissionTick(float dt)
		{
			base.OnMissionTick(dt);

			if (GameNetwork.MyPeer.IsAdmin())
			{
				if (IsModoModeActive && !_isMenuOpen)
				{
					// Manage border color and 3d text
					MissionScreen.ScreenPointToWorldRay(Input.GetMousePositionRanged(), out var rayBegin, out var rayEnd);
					Agent agent = Mission.Current.RayCastForClosestAgent(rayBegin, rayEnd, out var distance, -1, 0.1f);

					if (agent != null)
					{
						if (CurrentHoverAgent != null && CurrentHoverAgent != agent)
						{
							// Remove contour of old selected agent
							CurrentHoverAgent.AgentVisuals?.GetEntity()?.SetContourColor(null, false);
						}

						CurrentHoverAgent = agent;
						uint color = new Color(1f, 0f, 0f, 1f).ToUnsignedInteger();
						CurrentHoverAgent.AgentVisuals?.GetEntity()?.SetContourColor(color, true);
						string name = CurrentHoverAgent?.MissionPeer?.DisplayedName ?? CurrentHoverAgent.Name;
						Vec3 position = CurrentHoverAgent?.AgentVisuals?.GetGlobalFrame().origin ?? CurrentHoverAgent.Position;
						MBDebug.RenderDebugText3D(position, name, color);
					}
					else if (CurrentHoverAgent != null)
					{
						CurrentHoverAgent.AgentVisuals?.GetEntity()?.SetContourColor(null, false);
						CurrentHoverAgent = null;
					}
				}
				else
				{
					if (CurrentHoverAgent != null)
					{
						CurrentHoverAgent.AgentVisuals?.GetEntity()?.SetContourColor(null, false);
					}
				}
			}
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
