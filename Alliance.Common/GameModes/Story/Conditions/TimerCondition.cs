using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if the timer has passed a certain time.
	/// </summary>
	[PhrasePreview("{TypeOfTimer|Once after|Every|Always after} {WaitTime}s")]
	[PhraseTemplate("{TypeOfTimer|Once after|Every|Always after} {WaitTime} seconds.")]
	public class TimerCondition : Condition
	{
		public enum TimerType
		{
			TriggerOnce,
			TriggerAndWaitAgain,
			TriggerAlways
		}

		[ConfigProperty(label: "Wait Time", tooltip: "Time in seconds to wait before the condition is met.")]
		public float WaitTime;
		[ConfigProperty(label: "Type of timer", tooltip: "Specifies the type of timer to use.")]
		public TimerType TypeOfTimer = TimerType.TriggerOnce;

		private bool _triggered;
		private float _lastTriggerTime = 0f;

		public TimerCondition() { }

		public override void Register(WeakGameEntity gameEntity)
		{
			_triggered = false;
			_lastTriggerTime = Mission.Current?.GetMissionTimeInSeconds() ?? 0f;
		}

		public override bool Evaluate(VariableStore context)
		{
			if (_triggered && TypeOfTimer != TimerType.TriggerAndWaitAgain)
			{
				return TypeOfTimer == TimerType.TriggerAlways;
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
