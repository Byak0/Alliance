using Alliance.Common.Core.Security;
using Alliance.Common.Core.Security.NetworkMessages.FromServer;
using Alliance.Common.Extensions;
using System;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Client.Core.Security.Handlers
{
	public class PlayerDataHandler : IHandlerRegister
	{
		public void Register(NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncPlayerData>(HandleSyncPlayerData);
		}

		public void HandleSyncPlayerData(SyncPlayerData message)
		{
			try
			{
				PlayerService.ApplyPlayerDataUpdate(message.PlayerData, message.Player, message.AllData);
				Log($"{message.PlayerData.Name} - {message.PlayerData.Id} - {message.PlayerData.Sudo} - {message.PlayerData.WarningCount}");
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to udpate player data for {message.Player?.UserName}", LogLevel.Error);
				Log(ex.ToString(), LogLevel.Error);
			}
		}
	}
}
