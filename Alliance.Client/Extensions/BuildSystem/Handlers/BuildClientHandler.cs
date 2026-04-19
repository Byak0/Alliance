using Alliance.Common.Extensions;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.BuildSystem.Handlers
{
	/// <summary>
	/// Client-side handler for build system sync messages.
	/// Automatically discovered and registered by ClientAutoHandler.
	/// </summary>
	public class BuildClientHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncPrefabCreation>(HandleSyncPrefabCreation);
			reg.Register<SyncPrefabRemoval>(HandleSyncPrefabRemoval);
		}

		private void HandleSyncPrefabCreation(SyncPrefabCreation message)
		{
			BuildBehavior buildBehavior = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			if (buildBehavior == null)
			{
				Log("BuildClientHandler: BuildBehavior not found in mission.", LogLevel.Error);
				return;
			}

			if (message.HasRootMissionObject)
			{
				// Prefab has MissionObject scripts - entity was already created by native CreateMissionObject handler.
				// Find it using the full MissionObjectId (Id + CreatedAtRuntime)
				buildBehavior.TrackExistingEntity(
					message.BuildIndex, message.PrefabName,
					message.PrefabFrame, message.RootMissionObjectId);
			}
			else
			{
				// Simple prefab without MissionObject scripts - instantiate normally
				buildBehavior.BuildPrefab(message.BuildIndex, message.PrefabName, message.PrefabFrame);
			}
		}

		private void HandleSyncPrefabRemoval(SyncPrefabRemoval message)
		{
			BuildBehavior buildBehavior = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			if (buildBehavior == null)
			{
				Log("BuildClientHandler: BuildBehavior not found in mission.", LogLevel.Error);
				return;
			}

			buildBehavior.RemovePrefab(message.BuildIndex);
		}
	}
}