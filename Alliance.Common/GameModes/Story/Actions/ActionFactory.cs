using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Factory class for creating actions that can be performed during a scenario.
	/// Actions are implemented in either Common, Client, or Server projects for specific behavior.
	/// Instance of this class is set to either Client_ActionFactory or Server_ActionFactory during module initialization.
	/// </summary>
	public class ActionFactory
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

		public static void Initialize()
		{
			Instance = new ActionFactory();
		}

		public virtual ConditionalAction ConditionalAction()
		{
			return new ConditionalAction();
		}

		public virtual ShowResultScreenAction ShowResultScreenAction()
		{
			return new ShowResultScreenAction();
		}

		public virtual StartGameAction StartGameAction()
		{
			return new StartGameAction();
		}

		public virtual StartScenarioAction StartScenarioAction()
		{
			return new StartScenarioAction();
		}

		public virtual SpawnAgentAction SpawnAgentAction()
		{
			return new SpawnAgentAction();
		}

		public virtual DamageAgentInZoneAction DamageAgentInZoneAction()
		{
			return new DamageAgentInZoneAction();
		}

		// Add more actions here, aswell as in the Client and Server Factories for specific behaviors.
	}
}