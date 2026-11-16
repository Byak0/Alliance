using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromClient;
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
			if (buildIndex == -1)
			{
				Log($"BuildHandler: Failed to allocate build index for '{message.PrefabName}'.", LogLevel.Error);
				return false;
			}

			GameEntity entity = buildBehavior.BuildPrefab(buildIndex, message.PrefabName, message.PrefabFrame);

			if (entity == null)
			{
				Log($"BuildHandler: Failed to build prefab '{message.PrefabName}'.", LogLevel.Error);
				return false;
			}

			buildBehavior.BroadcastCreation(buildIndex, message.PrefabName, message.PrefabFrame, entity);
			Log($"BuildHandler: {peer.UserName} built '{message.PrefabName}' (#{buildIndex}).", LogLevel.Information);
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

			buildBehavior.BroadcastDelete(message.BuildIndex);
			Log($"BuildHandler: {peer.UserName} removed build #{message.BuildIndex}.", LogLevel.Information);
			return true;
		}
	}
}