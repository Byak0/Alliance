using Alliance.Common.GameModes.Story.Actions;

namespace Alliance.Client.GameModes.Story.Actions
{
	/// <summary>
	/// Factory class for creating actions that can be performed during a scenario.
	/// Actions are implemented in either Common, Client, or Server projects for specific behavior.
	/// Instance of this class is set to either Client_ActionFactory or Server_ActionFactory during module initialization.
	/// </summary>
	public class Client_ActionFactory : ActionFactory
	{
		public static void Initialize()
		{
			Instance = new Client_ActionFactory();
		}

		public override ShowResultScreenAction ShowResultScreen()
		{
			return new Client_ShowResultScreenAction();
		}
	}
}