using Alliance.Common.Core.Utils;
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
		private float _startTime = 0f;

		public TimerObjective(BattleSideEnum side, LocalizedString name, LocalizedString desc, bool instantWin, bool requiredForWin, int duration) :
			base(side, name, desc, instantWin, requiredForWin)
		{
			Duration = duration;
			_remainingTime = duration;
		}

		public TimerObjective() { }

		public override void RegisterForUpdate()
		{
			_startTime = Mission.Current.GetMissionTimeInSeconds();
		}

		public override void UnregisterForUpdate()
		{
			_startTime = Mission.Current.GetMissionTimeInSeconds();
		}

		public override void Reset()
		{
			_startTime = Mission.Current.GetMissionTimeInSeconds();
			Active = true;
		}

		public override bool CheckObjective()
		{
			if (Mission.Current == null) return false;

			_remainingTime = Duration + _startTime - Mission.Current.GetMissionTimeInSeconds();

			if (_remainingTime <= 0)
			{
				return true;
			}
			return false;
		}

		public override string GetProgressAsString()
		{
			if (Mission.Current != null)
				return TimeSpan.FromSeconds(_remainingTime).ToString("mm':'ss");
			else
				return "";
		}
	}
}
