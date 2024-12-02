using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Battle;
using Alliance.Common.GameModes.BattleRoyale;
using Alliance.Common.GameModes.Captain;
using Alliance.Common.GameModes.CvC;
using Alliance.Common.GameModes.Lobby;
using Alliance.Common.GameModes.PvC;
using Alliance.Common.GameModes.Siege;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Server.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

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

			GameModeSettings gameModeSettings;

			string gameType = Settings.TWOptions.GameType;
			string map = Settings.TWOptions.Map;
			string cultureTeam1 = Settings.TWOptions[MultiplayerOptions.OptionType.CultureTeam1].ToString();
			string cultureTeam2 = Settings.TWOptions[MultiplayerOptions.OptionType.CultureTeam2].ToString();

			switch (gameType)
			{
				case "CaptainX": gameModeSettings = new CaptainGameModeSettings(); break;
				case "BattleX": gameModeSettings = new BattleGameModeSettings(); break;
				case "SiegeX": gameModeSettings = new SiegeGameModeSettings(); break;
				case "Scenario": gameModeSettings = new ScenarioGameModeSettings(); break;
				case "PvC": gameModeSettings = new PvCGameModeSettings(); break;
				case "CvC": gameModeSettings = new CvCGameModeSettings(); break;
				case "BattleRoyale": gameModeSettings = new BRGameModeSettings(); break;
				case "Lobby": gameModeSettings = new LobbyGameModeSettings(); break;
				default: gameModeSettings = new LobbyGameModeSettings(); break;
			}

			gameModeSettings.TWOptions = Settings.TWOptions;
			gameModeSettings.ModOptions = Settings.ModOptions;

			GameModeStarter.Instance.StartMission(gameModeSettings);

			string log = $"Starting {gameType} on {map} ({cultureTeam1} VS {cultureTeam2})...";

			Log(log, LogLevel.Information);
			SendMessageToAll(log);
		}
	}
}