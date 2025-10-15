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
		public static new void Initialize()
		{
			Instance = new Server_ActionFactory();
		}

		public override StartGameAction StartGameAction()
		{
			return new Server_StartGameAction();
		}

		public override StartScenarioAction StartScenarioAction()
		{
			return new Server_StartScenarioAction();
		}

		public override SpawnAgentAction SpawnAgentAction()
		{
			return new Server_SpawnAgentAction();
		}

		public override SpawnFormationAction SpawnFormationAction()
		{
			return new Server_SpawnFormationAction();
		}

		public override DamageAgentInZoneAction DamageAgentInZoneAction()
		{
			return new Server_DamageAgentInZoneAction();
		}

		public override MortalityStateZoneAction MortalityStateZoneAction()
		{
			return new Server_MortalityStateZoneAction();
		}

		public override ShowOrHideEntitiesAction ShowOrHideEntitiesAction()
		{
			return new Server_ShowOrHideEntitiesAction();
		}

		public override TeleportAgentAction TeleportAgentAction()
		{
			return new Server_TeleportAgentAction();
		}

		public override VOIPRangeInZoneAction VOIPRangeInZoneAction()
		{
			return new Server_VOIPRangeInZoneAction();
		}

		public override ZEventAction ZEventAction()
		{
			return new Server_ZEventAction();
		}
	}
}