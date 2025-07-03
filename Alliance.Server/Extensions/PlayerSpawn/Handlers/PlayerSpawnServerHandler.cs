using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerSpawn.Utilities;
using Alliance.Common.Utilities;
using System;
using System.IO;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMessage;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.PlayerSpawn.Handlers
{
	public class PlayerSpawnServerHandler : PlayerSpawnMenuHandlerBase, IHandlerRegister
	{
		private NetworkCommunicator _currentPeerMakingChanges;

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<UpdatePlayerSpawnMenu>(HandleUpdatePlayerSpawnMenu);
		}

		private bool HandleUpdatePlayerSpawnMenu(NetworkCommunicator peer, UpdatePlayerSpawnMenu message)
		{
			// Check if the peer is authorized to update the player spawn menu
			if (!peer.IsAdmin())
			{
				Log($"Alliance - Unauthorized attempt to update player spawn menu by {peer.UserName}", LogLevel.Warning);
				return false;
			}

			if (message.Operation == PlayerSpawnMenuOperation.BeginMenuSync)
			{
				_currentPeerMakingChanges = peer;
			}
			else if (peer != _currentPeerMakingChanges)
			{
				// If the peer is not the one who started the sync, ignore the message
				Log($"Alliance - Ignoring PlayerSpawnMenu update from {peer.UserName} as they are not the current editor.", LogLevel.Warning);
				return false;
			}

			HandlePlayerSpawnMenuOperation(message);
			return true;
		}

		protected override void EndMenuSyncHandler()
		{
			base.EndMenuSyncHandler();

			// Save the updated player spawn menu to file
			string fileName = $"spawn_preset_{DateTime.Now:yyyyMMdd_HHmmss}_{_currentPeerMakingChanges.UserName}.xml";
			string filePath = Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Spawn_Presets", fileName);
			try
			{
				Log($"Alliance - Saving PlayerSpawnMenu to {filePath}");
				SerializeHelper.SaveClassToFile(filePath, _receivedPlayerSpawnMenu);
			}
			catch (Exception ex)
			{
				Log($"Alliance - Failed to save PlayerSpawnMenu to {filePath}: {ex.Message}", LogLevel.Error);
				return;
			}
			finally
			{
				_currentPeerMakingChanges = null;
			}

			// Broadcast the updated player spawn menu to all players
			PlayerSpawnMenuNetworkHelper.SendPlayerSpawnMenuToAll();
		}
	}
}
