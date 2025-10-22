using Alliance.Common.GameModes.Story.Actions;
using Alliance.Server.Extensions.ToggleEntities.Behaviors;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Actions
{
	public class Server_ShowOrHideEntitiesAction : ShowOrHideEntitiesAction
	{
		private WeakGameEntity _gameEntity = WeakGameEntity.Invalid;

		public override void Register(WeakGameEntity entity)
		{
			_gameEntity = entity;
		}

		public override void Execute()
		{
			if (Toggle)
			{
				Visible = !Visible;
			}

			ToggleEntitiesBehavior toggleBehavior = Mission.Current.GetMissionBehavior<ToggleEntitiesBehavior>();
			if (ParentEntityOnly && _gameEntity != WeakGameEntity.Invalid)
			{
				// TODO: Add support for any entity. For now, we rely on MissionObjectId to sync with clients.
				MissionObject missionObject = _gameEntity.GetFirstScriptOfType<MissionObject>();
				if (missionObject == null)
				{
					Log($"Error in ShowOrHideEntitiesAction - Game entity must have a MissionObject script if ParentEntityOnly is checked", LogLevel.Error);
					return;
				}

				toggleBehavior.SetLocalTagVisibility(missionObject, Tag, Visible);
			}
			else
			{
				toggleBehavior.SetTagVisibility(Tag, Visible);
			}
		}
	}
}