using System;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Action for starting a game.
	/// </summary>
	[Serializable]
	public class StartGameAction : ActionBase
	{
		public GameModeSettings Settings;

		public StartGameAction(GameModeSettings settings)
		{
			Settings = settings;
		}

		public StartGameAction() { }

		public override void Execute() { }
	}
}