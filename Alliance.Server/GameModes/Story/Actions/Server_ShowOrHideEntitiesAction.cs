using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Server.Extensions.ToggleEntities.Behaviors;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Actions
{
	[OverrideAction(typeof(ShowOrHideEntitiesAction))]
	public class Server_ShowOrHideEntitiesAction : ShowOrHideEntitiesAction
	{
		private WeakGameEntity _gameEntity = WeakGameEntity.Invalid;
		private bool? _currentVisibility;

		public override void Register(WeakGameEntity entity)
		{
			_gameEntity = entity;
		}

		public override ActionTask Execute()
		{
			TriggerContext ctx = ScenarioManager.Instance.CurrentTriggerContext;
			VariableStore globals = ScenarioManager.Instance.Globals;

			string tag = Tag?.Resolve(ctx, globals) ?? "";
			bool defaultVis = DefaultVisibility?.Resolve(ctx, globals) ?? true;

			bool target;
			if (VisibilityType == Visibility.Switch)
			{
				if (!_currentVisibility.HasValue) _currentVisibility = defaultVis;
				_currentVisibility = !_currentVisibility.Value;
				target = _currentVisibility.Value;
			}
			else
			{
				target = VisibilityType == Visibility.Show;
			}

			ToggleEntitiesBehavior toggleBehavior = Mission.Current.GetMissionBehavior<ToggleEntitiesBehavior>();
			if (ParentEntityOnly && _gameEntity != WeakGameEntity.Invalid)
			{
				MissionObject missionObject = _gameEntity.GetFirstScriptOfType<MissionObject>();
				if (missionObject == null)
				{
					Log($"Error in ShowOrHideEntitiesAction - Game entity must have a MissionObject script if ParentEntityOnly is checked", LogLevel.Error);
					return ActionTask.CompletedTask;
				}

				toggleBehavior.SetLocalTagVisibility(missionObject, tag, target);
			}
			else
			{
				toggleBehavior.SetTagVisibility(tag, target);
			}
			return ActionTask.CompletedTask;
		}
	}
}