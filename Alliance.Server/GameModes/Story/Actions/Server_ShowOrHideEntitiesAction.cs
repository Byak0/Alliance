using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using Alliance.Common.GameModes.Story.Actions;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.GameModes.Story.Actions
{
	public class Server_ShowOrHideEntitiesAction : ShowOrHideEntitiesAction
	{
		private GameEntity _gameEntity = null;

		public override void Register(GameEntity entity = null)
		{
			_gameEntity = entity;
		}

		public override void Execute()
		{
			if (Toggle)
			{
				Visible = !Visible;
			}

			if (ParentEntityOnly && _gameEntity != null)
			{
				foreach (GameEntity gameEntity in _gameEntity.CollectChildrenEntitiesWithTag(Tag))
				{
					gameEntity.SetVisibilityExcludeParents(Visible);
				}
				if (_gameEntity.HasTag(Tag))
				{
					_gameEntity.SetVisibilityExcludeParents(Visible);
				}
				MissionObject missionObject = _gameEntity.GetFirstScriptOfType<MissionObject>();

				if (missionObject == null) return;

				// TODO change to message that target only entity
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncToggleEntitiesLocal(Tag, Visible, missionObject.Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
			else
			{
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
}