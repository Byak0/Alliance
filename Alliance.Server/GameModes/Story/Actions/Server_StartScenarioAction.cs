using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Utilities;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a scenario.
	/// </summary>
	[OverrideAction(typeof(StartScenarioAction))]
	public class Server_StartScenarioAction : StartScenarioAction
	{
		public Server_StartScenarioAction() : base() { }

		public override ActionTask Execute(VariableStore context)
		{
			ScenarioManagerServer.Instance.StartScenario(ScenarioId, ActIndex);
			return ActionTask.CompletedTask;
		}
	}
}