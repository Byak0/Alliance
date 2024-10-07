using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Actions;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Actions
{
	public class Server_ShowOrHideEntitiesAction : ShowOrHideEntitiesAction
	{
		public override void Execute()
		{
			if (Toggle)
			{
				Visible = !Visible;
			}
			foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag(Tag))
			{
				entity.SetVisibilityExcludeParents(Visible);
			}

			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncToggleEntities(Tag, Visible));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}