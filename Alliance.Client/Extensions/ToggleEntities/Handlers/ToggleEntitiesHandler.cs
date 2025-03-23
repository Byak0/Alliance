using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.ToggleEntities.Handlers
{
	public class ToggleEntitiesHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncToggleEntities>(HandleToggleEntitiesVisibility);
			reg.Register<SyncToggleEntitiesLocal>(HandleToggleEntitiesVisibilityLocal);
		}

		public void HandleToggleEntitiesVisibility(SyncToggleEntities message)
		{
			if (Mission.Current?.Scene == null) return;

			foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag(message.EntitiesTag))
			{
				entity.SetVisibilityExcludeParents(message.Show);
			}
		}

		public void HandleToggleEntitiesVisibilityLocal(SyncToggleEntitiesLocal message)
		{
			if (Mission.Current?.Scene == null || message.MissionObjectId == null) return;

			MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
			foreach (GameEntity entity in missionObject.GameEntity.CollectChildrenEntitiesWithTag(message.EntitiesTag))
			{
				entity.SetVisibilityExcludeParents(message.Show);
			}
			if (missionObject.GameEntity.HasTag(message.EntitiesTag))
			{
				missionObject.GameEntity.SetVisibilityExcludeParents(message.Show);
			}
		}
	}
}