using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.Views
{
	[OverrideView(typeof(MultiplayerMissionOrderUIHandler))]
	public class AllianceOrderUIHandler : MissionGauntletMultiplayerOrderUIHandler
	{
		// Override to force tick in all gamemodes
		public override bool IsValidForTick
		{
			get
			{
				return (!base.MissionScreen.IsRadialMenuActive || this._dataSource.IsToggleOrderShown) && !GameStateManager.Current.ActiveStateDisabledByUser;
			}
		}
	}
}
