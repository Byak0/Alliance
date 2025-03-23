using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Base class for conditions.
	/// Conditions are used in Scenarios or CS_ConditionalAction to determine if an action should be executed.
	/// </summary>
	public abstract class Condition
	{
		public enum TargetType
		{
			All,
			Bots,
			Players,
			Officers
		}

		public enum SideType
		{
			All = -1,
			Defender = BattleSideEnum.Defender,
			Attacker = BattleSideEnum.Attacker
		}

		public enum MoveOrderType
		{
			Charge,
			Move,
			Retreat,
			Stop,
			Advance,
			FallBack
		}

		public abstract bool Evaluate(ScenarioManager context);
		public virtual void Register(GameEntity entity = null) { }
		public virtual void Unregister() { }
	}
}
