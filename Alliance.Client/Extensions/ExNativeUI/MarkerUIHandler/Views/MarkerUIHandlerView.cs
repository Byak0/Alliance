using Alliance.Client.Extensions.AdminMenu.Views;
using Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.ViewModels;
using System.Linq;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.Views
{
	[OverrideView(typeof(MissionMultiplayerMarkerUIHandler))]
	public class MarkerUIHandlerView : MissionView
	{
		GameKey getPlayerKey;
		AdminSystem adminSystemView;

		public override void EarlyStart()
		{
			getPlayerKey = HotKeyManager.GetCategory("admin_sys").GetGameKey("key_adm_getplayermouse");
		}

		public MarkerUIHandlerView()
		{
		}

		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			_dataSource = new MarkerUIHandlerVM(MissionScreen.CombatCamera);
			_gauntletLayer = new GauntletLayer(1, "GauntletLayer", false);
			_gauntletLayer.LoadMovie("MPMissionMarkers", _dataSource);
			MissionScreen.AddLayer(_gauntletLayer);
			adminSystemView = Mission.GetMissionBehavior<AdminSystem>();
		}

		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			MissionScreen.RemoveLayer(_gauntletLayer);
			_gauntletLayer = null;
			_dataSource.OnFinalize();
			_dataSource = null;
		}

		public override void OnMissionScreenTick(float dt)
		{
			base.OnMissionScreenTick(dt);
			adminSystemView ??= Mission.GetMissionBehavior<AdminSystem>();
			// Update selected user
			if (adminSystemView.CurrentHoverAgent != null)
			{
				if (adminSystemView.CurrentHoverAgent.MissionPeer != null)
				{
					MissionPeer peer = adminSystemView.CurrentHoverAgent.MissionPeer;
					if (_dataSource._currentSelectedUserVMDico.Count == 0)
					{
						// First time that we are selecting user
						_dataSource._teammateDictionary.TryGetValue(peer, out CustomMissionPeerMarkerTargetVM text1);
						if (text1 != null)
						{
							_dataSource._currentSelectedUserVMDico.Add(peer, text1);
							text1.IsSelected = true;
						}
					}
					else
					{
						// Already existing selected user, we need to reset if we are selecting someone else
						if (!_dataSource._currentSelectedUserVMDico.ContainsKey(peer))
						{
							// New player selected
							// Unselect previous one
							_dataSource._currentSelectedUserVMDico.First().Value.IsSelected = false;

							// Replace currentSelectedVM
							_dataSource._currentSelectedUserVMDico.Clear();
							_dataSource._teammateDictionary.TryGetValue(peer, out CustomMissionPeerMarkerTargetVM text1);
							if (text1 != null)
							{
								_dataSource._currentSelectedUserVMDico.Add(peer, text1);
								text1.IsSelected = true;

							}
						}
					}
				}
			}
			else
			{
				// We are not selecting anyone, reseting previous selected user.
				if (_dataSource._currentSelectedUserVMDico.Count != 0)
				{
					_dataSource._currentSelectedUserVMDico.First().Value.IsSelected = false;
					_dataSource._currentSelectedUserVMDico.Clear();
				}
			}

			if ((adminSystemView != null && adminSystemView.IsModoModeActive) || Input.IsGameKeyDown(5))
			{
				_dataSource.IsEnabled = true;
			}
			else
			{
				_dataSource.IsEnabled = false;
			}
			_dataSource.Tick(dt);
		}

		private GauntletLayer _gauntletLayer;

		private MarkerUIHandlerVM _dataSource;
	}
}