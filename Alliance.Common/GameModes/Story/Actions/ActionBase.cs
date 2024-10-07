using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Base class for Actions that can be performed during a scenario.
	/// Can be implemented in either Common, Client, or Server projects for specific behavior.
	/// Actions are created by the ActionFactory.
	/// </summary>
	[Serializable]
	public abstract class ActionBase
	{
		public virtual void Execute() { }
	}
}