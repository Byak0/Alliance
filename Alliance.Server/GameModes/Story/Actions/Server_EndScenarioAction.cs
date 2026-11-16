using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Server.Core;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Server-side implementation for ending a scenario and starting the configured post-match transition.
	/// </summary>
	[OverrideAction(typeof(EndScenarioAction))]
	public class Server_EndScenarioAction : EndScenarioAction
	{
		public override ActionTask Execute(VariableStore context)
		{
			GameModeStarter.Instance.StartPostMatchTransition();
			return ActionTask.CompletedTask;
		}
	}
}


