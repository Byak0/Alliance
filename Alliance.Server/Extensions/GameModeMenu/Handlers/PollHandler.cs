using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.GameModeMenu.Models;
using Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Battle;
using Alliance.Common.GameModes.BattleRoyale;
using Alliance.Common.GameModes.Captain;
using Alliance.Common.GameModes.CvC;
using Alliance.Common.GameModes.Lobby;
using Alliance.Common.GameModes.PvC;
using Alliance.Common.GameModes.Siege;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Utilities;
using Alliance.Server.Core;
using Alliance.Server.GameModes.Story;
using Alliance.Server.GameModes.Story.Scenarios;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.GameModeMenu.Handlers
{
    public class PollHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<GameModePollRequestMessage>(HandleGameModePollRequest);
            reg.Register<ScenarioPollRequestMessage>(HandleScenarioPollRequest);
        }

        /// <summary>
        /// Handle game mode poll request
        /// </summary>
		public bool HandleGameModePollRequest(NetworkCommunicator peer, GameModePollRequestMessage message)
        {
            // TODO : make use of PollRequest content to start a poll
            bool isInLobby = MultiplayerOptions.OptionType.GameType.GetStrValue() == "Lobby";
            message.GetOption(MultiplayerOptions.OptionType.GameType).GetValue(out string gameType);

            // Check whether the request comes from an admin, or is an authorized game mode launch from Lobby
            if (message.SkipPoll && peer.IsAdmin() || isInLobby && Config.Instance.AuthorizePoll && GameModeMenuConstants.AVAILABLE_GAME_MODES.Contains(gameType))
            {
                message.GetOption(MultiplayerOptions.OptionType.Map).GetValue(out string map);

                // Check if the scene exist on server side
                if (!SceneList.Scenes.Exists(sceneInfo => sceneInfo.Name == map))
                {
                    SendMessageToPeer($"The scene \"{map}\" isn't available on this server", peer);
                    return false;
                }

                message.GetOption(MultiplayerOptions.OptionType.CultureTeam1).GetValue(out string cultureTeam1);
                message.GetOption(MultiplayerOptions.OptionType.CultureTeam2).GetValue(out string cultureTeam2);

                GameModeSettings gameModeSettings = CreateSettingsFromMessage(message);
                GameModeStarter.Instance.StartMission(gameModeSettings);

                string log = $"Starting {gameType} on {map} ({cultureTeam1} VS {cultureTeam2})...";

                Log(log, LogLevel.Information);
                SendMessageToAll(log);

                return true;
            }
            return false;
        }

        private GameModeSettings CreateSettingsFromMessage(GameModePollRequestMessage message)
        {
            GameModeSettings gameModeSettings;

            message.GetOption(MultiplayerOptions.OptionType.GameType).GetValue(out string gameType);

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
                default:
                    Log("Error in PollHandler.CreateSettingsFromMessage : the requested game mode is not handled by the method!", LogLevel.Error);
                    return null;
            }

            gameModeSettings.NativeOptions = message.NativeOptions;
            gameModeSettings.ModOptions = message.ModOptions;

            return gameModeSettings;
        }

        public bool HandleScenarioPollRequest(NetworkCommunicator peer, ScenarioPollRequestMessage message)
        {
            // TODO : make use of PollRequest content to start a poll

            if (message.SkipPoll && peer.IsAdmin())
            {
                Scenario currentScenario;
                Act currentAct;
                MethodInfo scenarioMethod = typeof(ServerScenarios).GetMethod(message.Scenario);

                if (scenarioMethod == null)
                {
                    currentScenario = null;
                }
                else
                {
                    currentScenario = scenarioMethod.Invoke(null, null) as Scenario;
                }

                if (currentScenario != null && currentScenario.Acts.Count > message.Act)
                {
                    currentAct = currentScenario.Acts[message.Act];
                    ScenarioManagerServer.Instance.StartScenario(currentScenario, currentAct);
                }
                else
                {
                    Log($"Failed to start scenario \"{currentScenario?.Name}\" at act {message.Act}", LogLevel.Error);
                }

                return true;
            }
            return false;
        }
    }
}
