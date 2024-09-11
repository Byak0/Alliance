using Alliance.Common.GameModes.Story.Actions;
using Alliance.Server.Core;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a game.
	/// </summary>
	public class Server_StartGameAction : StartGameAction
	{
		public Server_StartGameAction() : base() { }

		public override void Execute()
		{
			GameModeStarter.Instance.StartMission(Settings);
		}
	}
}