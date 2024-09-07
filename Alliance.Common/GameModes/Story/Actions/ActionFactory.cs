using Alliance.Common.GameModes.Story.Conditions;
using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Factory class for creating actions that can be performed during a scenario.
	/// Actions are implemented in either Common, Client, or Server projects for specific behavior.
	/// Instance of this class is set to either Client_ActionFactory or Server_ActionFactory during module initialization.
	/// </summary>
	public abstract class ActionFactory
	{
		private static ActionFactory _instance;
		public static ActionFactory Instance
		{
			get
			{
				if (_instance == null)
					throw new InvalidOperationException("ActionFactory has not been initialized.");
				return _instance;
			}
			protected set => _instance = value;
		}

		public virtual ConditionalAction ConditionalAction(Condition condition, ActionBase actionIfTrue, ActionBase actionIfFalse)
		{
			return new ConditionalAction(condition, actionIfTrue, actionIfFalse);
		}

		public virtual ShowResultScreenAction ShowResultScreen()
		{
			return new ShowResultScreenAction();
		}

		public virtual StartGameAction StartGame(GameModeSettings settings)
		{
			return new StartGameAction(settings);
		}

		public virtual StartScenarioAction StartScenario(string scenarioId, int actIndex)
		{
			return new StartScenarioAction(scenarioId, actIndex);
		}

		// Add more actions here, aswell as in the Client and Server Factories for specific behaviors.
	}
}