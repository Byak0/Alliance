namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Base class for conditions.
	/// Conditions are used in Scenarios or CS_ConditionalAction to determine if an action should be executed.
	/// </summary>
	public abstract class Condition
	{
		public abstract bool Evaluate(ScenarioManager context);
	}
}
