using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Action marking the end of a scenario flow.
	/// The concrete runtime implementation decides which post-match transition should be started.
	/// </summary>
	[Serializable]
	public class EndScenarioAction : ActionBase
	{
		public override void Execute() { }
	}
}


