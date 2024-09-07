using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Story.Actions;

namespace Alliance.Server.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a game.
	/// </summary>
	public class Server_StartGameAction : StartGameAction
	{
		public Server_StartGameAction(GameModeSettings settings) : base(settings) { }

		public override void Execute()
		{
			// TODO : Server implementation
		}
	}
}