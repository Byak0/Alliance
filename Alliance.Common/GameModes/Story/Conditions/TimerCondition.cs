using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if the timer has passed a certain time.
	/// </summary>
	public class TimerCondition : Condition
	{
		[ScenarioEditor(label: "Wait Time", tooltip: "Time in seconds to wait before the condition is met.")]
		public float WaitTime;
		[ScenarioEditor(label: "Repeat", tooltip: "If true, it will trigger regularly, using WaitTime as an interval.")]
		public bool Repeat;

		private bool _triggered;
		private float _lastTriggerTime = 0f;

		public TimerCondition() { }

		public override void Register(GameEntity gameEntity = null)
		{
			_triggered = false;
			_lastTriggerTime = Mission.Current?.GetMissionTimeInSeconds() ?? 0f;
		}

		public override bool Evaluate(ScenarioManager context)
		{
			if (_triggered && !Repeat)
			{
				return false;
			}
			if (Mission.Current.GetMissionTimeInSeconds() >= WaitTime + _lastTriggerTime)
			{
				_lastTriggerTime = Mission.Current.GetMissionTimeInSeconds();
				_triggered = true;
				return true;
			}
			return false;
		}
	}
}
