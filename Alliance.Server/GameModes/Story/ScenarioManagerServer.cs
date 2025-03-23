using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Server.Core;
using Alliance.Server.GameModes.Story.Behaviors;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.GameModes.Story
{
	/// <summary>
	/// Server version of the ScenarioManager.
	/// </summary>
	public class ScenarioManagerServer : ScenarioManager
	{
		public static void Initialize()
		{
			Instance = new ScenarioManagerServer();
			Instance.RefreshAvailableScenarios();
		}

		public override void StartScenario(string scenarioId, int actIndex, ActState state = ActState.Invalid)
		{
			Scenario currentScenario = AvailableScenario.Find(scenario => scenario.Id == scenarioId);
			// Use current scenario if none provided
			currentScenario ??= CurrentScenario;

			if (currentScenario != null && currentScenario.Acts.Count > actIndex)
			{
				Act currentAct = currentScenario.Acts[actIndex];
				StartScenario(currentScenario, currentAct, state);
				Log($"Starting scenario \"{currentScenario?.Name.LocalizedText}\" at act {actIndex + 1}", LogLevel.Debug);
			}
			else
			{
				Log($"Failed to start scenario \"{currentScenario?.Name.LocalizedText}\" at act {actIndex + 1}", LogLevel.Error);
			}
		}

		public override void StartScenario(Scenario scenario, Act act, ActState state = ActState.Invalid)
		{
			base.StartScenario(scenario, act, state);

			// If LoadMap enabled or current map is different from act map, start a complete new mission with act settings
			if (act.LoadMap || (act.MapID != null && OptionType.Map.GetValueText() != act.MapID))
			{
				// If currently in a Scenario, prevent it from changing state
				Mission.Current.GetMissionBehavior<ScenarioBehavior>()?.StopScenario();

				// Log the start
				string log2 = $"Starting scenario \"{scenario.Name.LocalizedText}\" - Act \"{act.Name.LocalizedText}\" on map {act.MapID}...";
				Log(log2, LogLevel.Information);
				SendMessageToAll(log2);
				GameModeStarter.Instance.StartMission(act.ActSettings);
				return;
			}

			// Log the start
			string log = $"Starting scenario \"{scenario.Name.LocalizedText}\" - Act \"{act.Name.LocalizedText}\"...";
			Log(log, LogLevel.Information);
			SendMessageToAll(log);

			// Else, just apply new act settings to the current mission
			GameModeStarter.Instance.ApplyGameModeSettings(act.ActSettings);
			Mission.Current.GetMissionBehavior<ScenarioBehavior>().ResetState();

			// Sync scenario
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new InitScenarioMessage(scenario.Id, scenario.Acts.IndexOf(act)));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		public override void StopScenario()
		{
			Mission.Current.GetMissionBehavior<ScenarioBehavior>()?.SpawningBehavior?.SpawningStrategy?.EndSpawnSession();
			base.StopScenario();
		}

		public override void SetActState(ActState newState)
		{
			base.SetActState(newState);

			SyncActState(newState);
		}

		/// <summary>
		/// Send scenario state to every players.
		/// </summary>
		public void SyncActState(ActState newState)
		{
			float stateRemainingTime = (float)(Mission.Current?.GetMissionBehavior<ScenarioBehavior>()?.TimerComponent.GetRemainingTime(false));
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new UpdateScenarioMessage(newState, MissionTime.Now.NumberOfTicks, MathF.Ceiling(stateRemainingTime)));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
