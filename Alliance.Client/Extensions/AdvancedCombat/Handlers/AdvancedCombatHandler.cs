using Alliance.Client.Extensions.ExNativeUI.KillNotification.Views;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.AdvancedCombat.Handlers
{
	public class AdvancedCombatHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncPersonalKillFeedNotification>(SyncPersonalKillFeedNotification);
		}

		/// <summary>
		/// Server request to display custom damage on client side
		/// </summary>
		/// <param name="message">Message coming from CoreUtils.TakeDamage</param>
		public void SyncPersonalKillFeedNotification(SyncPersonalKillFeedNotification message)
		{
			KillNotificationView _killNotificationView = Mission.Current.GetMissionBehavior<KillNotificationView>();

			_killNotificationView.DisplayPersonalDamage(message);
		}
	}
}
