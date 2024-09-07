using Alliance.Common.GameModes.Story.Actions;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a scenario.
	/// </summary>
	public class Server_StartScenarioAction : StartScenarioAction
	{
		public Server_StartScenarioAction(string scenarioId, int actIndex) : base(scenarioId, actIndex) { }

		public override void Execute()
		{
			ScenarioManagerServer.Instance.StartScenario(ScenarioId, ActIndex);
		}
	}
}