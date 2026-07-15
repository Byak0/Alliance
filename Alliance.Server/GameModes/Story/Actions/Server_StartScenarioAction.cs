using Alliance.Common.GameModes.Story.Actions;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a scenario.
	/// </summary>
	[OverrideAction(typeof(StartScenarioAction))]
	public class Server_StartScenarioAction : StartScenarioAction
	{
		public Server_StartScenarioAction() : base() { }

		public override void Execute()
		{
			ScenarioManagerServer.Instance.StartScenario(ScenarioId, ActIndex);
		}
	}
}