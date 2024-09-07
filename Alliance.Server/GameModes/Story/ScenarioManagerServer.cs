using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Server.Core;
using Alliance.Server.GameModes.Story.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
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

			if (currentScenario != null && currentScenario.Acts.Count > actIndex)
			{
				Act currentAct = currentScenario.Acts[actIndex];
				StartScenario(currentScenario, currentAct, state);
				Log($"Starting scenario \"{currentScenario?.Name}\" at act {actIndex}", LogLevel.Debug);
			}
			else
			{
				Log($"Failed to start scenario \"{currentScenario?.Name}\" at act {actIndex}", LogLevel.Error);
			}
		}

		public override void StartScenario(Scenario scenario, Act act, ActState state = ActState.Invalid)
		{
			base.StartScenario(scenario, act, state);

			// Sync scenario
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new InitScenarioMessage(scenario.Id, scenario.Acts.IndexOf(act)));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

			// Log the start
			string log = $"Starting scenario \"{scenario.Name}\" - Act \"{act.Name}\"...";
			Log(log, LogLevel.Information);

			// If LoadMap enabled, start a complete new mission with act settings
			if (act.LoadMap)
			{
				GameModeStarter.Instance.StartMission(act.ActSettings);
			}
			// Else, just apply new act settings to the current mission
			else
			{
				GameModeStarter.Instance.ApplyGameModeSettings(act.ActSettings);
				Mission.Current.GetMissionBehavior<ScenarioBehavior>().ResetState();
			}
		}

		public override void StopScenario()
		{
			Mission.Current.GetMissionBehavior<ScenarioBehavior>()?.SpawningBehavior?.SpawningStrategy?.EndSpawnSession();
			base.StopScenario();
		}

		public override void SetActState(ActState newState)
		{
			base.SetActState(newState);

			if (newState == ActState.Completed && CurrentScenario.Acts.Count == CurrentScenario.Acts.IndexOf(CurrentAct) + 1)
			{
				StopScenario();
			}

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

		public override void SetWinner(BattleSideEnum winner)
		{
			base.SetWinner(winner);
		}
	}
}
