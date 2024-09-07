using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Story.Actions;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Factory class for creating actions that can be performed during a scenario.
	/// Actions are implemented in either Common, Client, or Server projects for specific behavior.
	/// Instance of this class is set to either Client_ActionFactory or Server_ActionFactory during module initialization.
	/// </summary>
	public class Server_ActionFactory : ActionFactory
	{
		public static void Initialize()
		{
			Instance = new Server_ActionFactory();
		}

		public override StartGameAction StartGame(GameModeSettings settings)
		{
			return new Server_StartGameAction(settings);
		}

		public override StartScenarioAction StartScenario(string scenarioId, int actIndex)
		{
			return new Server_StartScenarioAction(scenarioId, actIndex);
		}
	}
}