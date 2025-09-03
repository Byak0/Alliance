using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.Zevent;
using Alliance.Server.Core;
using Alliance.Server.Core.Database.Data;
using Alliance.Server.Core.Database.Models;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.Zevent.Behavior
{
	public class ZeventBehavior : MissionNetwork, IMissionBehavior
	{
		public ZeventBehavior() : base()
		{
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			if (Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NAME) return;

			Log("Map Zevent detected, sync gold pile with last known value", LogLevel.Information);

			// Set gold pile to last know value from DB

			AppDbContext dbContext = ServiceLocator.GetService<AppDbContext>();
			ZeventGoldPile moreRecentGoldPile = dbContext.ZeventGoldPiles.OrderByDescending(e => e.InsertDate).FirstOrDefault();

			if (moreRecentGoldPile == null)
			{
				// There is no gold pile in DB yet we need to init it
				moreRecentGoldPile.InsertDate = System.DateTime.Now;
				moreRecentGoldPile.LastUpdateDate = System.DateTime.Now;
				moreRecentGoldPile.GoldAmount = 0;
			}

			// If map is Zevent, we need to sync gold pile
			GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
			if (gameEntity == null)
			{
				Log("There is no gold pile in this map.", LogLevel.Warning);
				return;
			}
			CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();
			goldPileScript.SetVolume(moreRecentGoldPile.GoldAmount / 1000);
			goldPileScript.SetVolumeTarget(moreRecentGoldPile.GoldAmount / 1000);

			ZeventMsg.RequestClientsToUpdateGoldPile(moreRecentGoldPile.GoldAmount, moreRecentGoldPile.GoldAmount);
		}

		protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			base.HandleNewClientAfterSynchronized(networkPeer);
			SyncGoldPileOfConnectingUsers(networkPeer);
		}

		public static void SyncGoldPileOfConnectingUsers(NetworkCommunicator networkPeer)
		{
			if (Mission.Current?.SceneName != ZeventConst.ZEVENT_MAP_NAME) return;

			Log("Player joining on Zevent map, sync gold pile to him", LogLevel.Information);

			// If map is Zevent, we need to sync gold pile
			GameEntity gameEntity = Mission.Current.Scene.GetFirstEntityWithScriptComponent<CS_DynamicPile>();
			if (gameEntity == null)
			{
				Log("There is no gold pile in this map.", LogLevel.Warning);
				return;
			}

			CS_DynamicPile goldPileScript = gameEntity.GetFirstScriptOfType<CS_DynamicPile>();

			int realBaseVolume = (int)(goldPileScript.CurrentVolume * 1000);
			int realTargetVolume = (int)(goldPileScript.VolumeTargetted * 1000);

			ZeventMsg.RequestClientToUpdateGoldPile(realTargetVolume, networkPeer, realBaseVolume);
		}
	}
}
