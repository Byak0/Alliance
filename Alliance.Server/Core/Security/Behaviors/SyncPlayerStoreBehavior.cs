using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.Models;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Core.Security.Behaviors
{
	/// <summary>
	/// MissionBehavior used to synchronize player store to clients (permissions, moderation, etc.).
	/// </summary>
	public class SyncPlayerStoreBehavior : MissionNetwork, IMissionBehavior
	{
		public SyncPlayerStoreBehavior() : base()
		{
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
		}

		protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			AL_PlayerData playerData = PlayerStore.Instance.LoadPlayerData(networkPeer);
			SendPlayerStoreToPeer(networkPeer);

			if (playerData != null)
			{
				PlayerSyncService.BroadcastPlayerData(playerData, networkPeer);
			}
		}

		public void SendPlayerStoreToPeer(NetworkCommunicator networkPeer)
		{
			try
			{
				PlayerSyncService.SendPlayerStoreToClient(networkPeer);
				Log($"Alliance - Successfully sent roles to {networkPeer.UserName}", LogLevel.Debug);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Error sending roles to {networkPeer.UserName}", LogLevel.Error);
				Log(ex.Message, LogLevel.Error);
			}
		}
	}
}