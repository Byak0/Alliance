using Alliance.Common.GameModes.Story.Actions;
using Alliance.Server.Core;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Server-side implementation for ending a scenario and starting the configured post-match transition.
	/// </summary>
	public class Server_EndScenarioAction : EndScenarioAction
	{
		public override void Execute()
		{
			GameModeStarter.Instance.StartPostMatchTransition();
		}
	}
}


