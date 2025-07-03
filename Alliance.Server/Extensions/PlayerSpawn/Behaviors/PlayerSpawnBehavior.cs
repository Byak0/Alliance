using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Utilities;
using Alliance.Common.Utilities;
using System;
using System.IO;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.PlayerSpawn.Behaviors
{
	public class PlayerSpawnBehavior : MissionNetwork
	{
		protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			// When a new player connects, send the whole player spawn menu.
			PlayerSpawnMenuNetworkHelper.SendPlayerSpawnMenuToPeer(networkPeer);
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			// Initialize the spawn menu
			InitializeSpawnMenu();

			PlayerSpawnMenuNetworkHelper.SendPlayerSpawnMenuToAll();
		}

		private void InitializeSpawnMenu()
		{
			// Load the selected file and update the PlayerSpawnMenu instance
			string fileName = "spawn_preset_nat.xml";
			string filePath = Path.GetFullPath(Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), "Spawn_Presets", fileName));
			try
			{
				if (File.Exists(filePath))
				{
					Log($"Loading PlayerSpawnMenu from {filePath}");
					PlayerSpawnMenu newMenu = SerializeHelper.LoadClassFromFile(filePath, new PlayerSpawnMenu());
					newMenu.RefreshIndices(); // Ensure indices are unique and valid
					PlayerSpawnMenu.Instance = newMenu;
				}
				else
				{
					Log($"Can't load PlayerSpawnMenu, file doesn't exist : {filePath}", LogLevel.Error);
				}
			}
			catch (Exception ex)
			{
				Log($"Failed to load PlayerSpawnMenu from {filePath}: {ex.Message}", LogLevel.Error);
			}
		}
	}
}
