using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromClient;
using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.BuildSystem.Handlers
{
	/// <summary>
	/// Server-side handler for build system requests.
	/// Automatically discovered and registered by ServerAutoHandler.
	/// </summary>
	public class BuildHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestPrefabCreation>(HandleRequestPrefabCreation);
			reg.Register<RequestPrefabRemoval>(HandleRequestPrefabRemoval);
		}

		private bool HandleRequestPrefabCreation(NetworkCommunicator peer, RequestPrefabCreation message)
		{
			if (!peer.IsAdmin())
			{
				Log($"BuildHandler: {peer.UserName} is not admin. Build request denied.", LogLevel.Warning);
				return false;
			}

			BuildBehavior buildBehavior = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			if (buildBehavior == null)
			{
				Log("BuildHandler: BuildBehavior not found in mission.", LogLevel.Error);
				return false;
			}

			int buildIndex = buildBehavior.AllocateBuildIndex();
			GameEntity entity = buildBehavior.BuildPrefab(buildIndex, message.PrefabName, message.PrefabFrame);

			if (entity == null)
			{
				Log($"BuildHandler: Failed to build prefab '{message.PrefabName}'.", LogLevel.Error);
				return false;
			}

			// Use the full MissionObjectId (Id + CreatedAtRuntime) so the client
			// can unambiguously find the runtime entity created by CreateMissionObject,
			// instead of accidentally matching a pre-placed scene object with the same int Id.
			GameNetwork.BeginBroadcastModuleEvent();

			if (BuildBehavior.TryGetRootMissionObjectId(entity, out MissionObjectId moId))
			{
				GameNetwork.WriteMessage(new SyncPrefabCreation(buildIndex, message.PrefabName, message.PrefabFrame, moId));
				Log($"BuildHandler: {peer.UserName} built '{message.PrefabName}' (#{buildIndex}, MO={moId.Id}).", LogLevel.Information);
			}
			else
			{
				GameNetwork.WriteMessage(new SyncPrefabCreation(buildIndex, message.PrefabName, message.PrefabFrame));
				Log($"BuildHandler: {peer.UserName} built '{message.PrefabName}' (#{buildIndex}, no MO).", LogLevel.Information);
			}

			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			return true;
		}

		private bool HandleRequestPrefabRemoval(NetworkCommunicator peer, RequestPrefabRemoval message)
		{
			if (!peer.IsAdmin())
			{
				Log($"BuildHandler: {peer.UserName} is not admin. Removal request denied.", LogLevel.Warning);
				return false;
			}

			BuildBehavior buildBehavior = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			if (buildBehavior == null)
			{
				Log("BuildHandler: BuildBehavior not found in mission.", LogLevel.Error);
				return false;
			}

			if (!buildBehavior.RemovePrefab(message.BuildIndex))
			{
				return false;
			}

			// Broadcast removal to all clients
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncPrefabRemoval(message.BuildIndex));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);

			Log($"BuildHandler: {peer.UserName} removed build #{message.BuildIndex}.", LogLevel.Information);
			return true;
		}
	}
}