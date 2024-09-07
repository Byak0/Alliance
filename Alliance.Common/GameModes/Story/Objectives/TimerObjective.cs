using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Objectives
{
	public class TimerObjective : ObjectiveBase
	{
		public float Duration;

		private float _remainingTime;
		private ObjectivesBehavior _objectivesBehavior;

		public TimerObjective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool requiredForWin, int duration) :
			base(side, name, desc, instantWin, requiredForWin)
		{
			Duration = duration;
			_remainingTime = duration;
		}

		public TimerObjective() { }

		public override void RegisterForUpdate()
		{
			_objectivesBehavior = Mission.Current?.GetMissionBehavior<ObjectivesBehavior>();
			if (GameNetwork.IsServer) _objectivesBehavior?.StartTimerAsServer(Duration);
		}

		public override void UnregisterForUpdate()
		{
			_objectivesBehavior = null;
		}

		public override void Reset()
		{
			Active = true;
			_objectivesBehavior = Mission.Current?.GetMissionBehavior<ObjectivesBehavior>();
			if (GameNetwork.IsServer) _objectivesBehavior?.StartTimerAsServer(Duration);
		}

		public override bool CheckObjective()
		{
			if (Mission.Current == null || _objectivesBehavior == null) return false;
			_remainingTime = _objectivesBehavior.GetRemainingTime(false);
			return _objectivesBehavior.HasTimerElapsed();
		}

		public override string GetProgressAsString()
		{
			if (_objectivesBehavior != null && _objectivesBehavior.IsTimerRunning)
				return TimeSpan.FromSeconds(_remainingTime).ToString("mm':'ss");
			else
				return "";
		}
	}
}
