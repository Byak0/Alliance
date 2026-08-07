using Alliance.Common.Extensions;
using Alliance.Common.Extensions.BuildSystem;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.BuildSystem.Handlers
{
	/// <summary>
	/// Client-side handler for build system sync messages (entity create/move/delete).
	/// </summary>
	public class BuildClientHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncPrefabCreation>(HandleSyncPrefabCreation);
			reg.Register<SyncEntityMove>(HandleSyncEntityMove);
			reg.Register<SyncEntityDelete>(HandleSyncEntityDelete);
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
				buildBehavior.TrackExistingEntity(
					message.BuildIndex, message.PrefabName,
					message.PrefabFrame, message.RootMissionObjectId);
			}
			else
			{
				buildBehavior.BuildPrefab(message.BuildIndex, message.PrefabName, message.PrefabFrame);
			}
		}

		private void HandleSyncEntityMove(SyncEntityMove message)
		{
			WeakGameEntity entity = ResolveEntity(message.IsBuildTracked, message.BuildIndex, message.RefId);
			if (entity.IsValid)
			{
				entity.SetGlobalFrame(message.Frame);
				entity.SetFrameChanged();
			}
		}

		private void HandleSyncEntityDelete(SyncEntityDelete message)
		{
			if (message.IsBuildTracked)
			{
				BuildBehavior buildBehavior = Mission.Current?.GetMissionBehavior<BuildBehavior>();
				buildBehavior?.RemovePrefab(message.BuildIndex);
				return;
			}

			WeakGameEntity entity = EntityMarkerIndex.Resolve(message.RefId);
			if (!entity.IsValid) return;
			entity.SetVisibilityExcludeParents(false);
			entity.RemoveAllChildren();
			entity.Remove(0);
		}

		private static WeakGameEntity ResolveEntity(bool isBuildTracked, int buildIndex, string refId)
		{
			if (isBuildTracked)
			{
				BuildBehavior build = Mission.Current?.GetMissionBehavior<BuildBehavior>();
				if (build != null && build.BuiltEntities.TryGetValue(buildIndex, out var entry) && entry.Entity != null)
					return entry.Entity.WeakEntity;
				return WeakGameEntity.Invalid;
			}
			return EntityMarkerIndex.Resolve(refId);
		}
	}
}
