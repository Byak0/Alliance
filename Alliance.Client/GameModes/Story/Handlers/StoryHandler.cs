using Alliance.Common.Extensions;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using System;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.GameModes.Story.Handlers
{
	public class StoryHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<InitScenarioMessage>(HandleServerEventInitScenarioMessage);
			reg.Register<UpdateScenarioMessage>(HandleServerEventUpdateScenarioMessage);
			reg.Register<ObjectivesProgressMessage>(HandleServerEventObjectivesProgressMessage);
		}

		public void HandleServerEventInitScenarioMessage(InitScenarioMessage message)
		{
			Scenario currentScenario = TryGetScenarioFromId(message.ScenarioId);
			if (currentScenario == null)
			{
				Log($"Failed to start scenario. Scenario id: {message.ScenarioId} not found.", LogLevel.Error);
				return;
			}

			if (currentScenario.Acts.Count <= message.Act)
			{
				Log($"Failed to start scenario \"{currentScenario.Name}\" at act {message.Act}. Act index out of range.", LogLevel.Error);
				return;
			}

			Act currentAct = currentScenario.Acts[message.Act];
			ScenarioPlayer.Instance.StartScenario(currentScenario, currentAct);
		}

		private Scenario TryGetScenarioFromId(string scenarioId)
		{
			try
			{
				return ScenarioManager.Instance.AvailableScenario.Find(scenario => scenario.Id == scenarioId);
			}
			catch (Exception ex)
			{
				Log($"Error occurred when getting scenario with id: {scenarioId}. Exception: {ex.Message}", LogLevel.Error);
				return null;
			}
		}

		public void HandleServerEventUpdateScenarioMessage(UpdateScenarioMessage message)
		{
			try
			{
				Log($"Received UpdateScenarioMessage. State = {message.ScenarioState}", LogLevel.Debug);
				ScenarioClientBehavior scenarioClientBehavior = Mission.Current.GetMissionBehavior<ScenarioClientBehavior>();
				scenarioClientBehavior?.TimerComponent.StartTimerAsClient(message.StateStartTimeInSeconds, message.StateRemainingTime);
				ScenarioPlayer.Instance.SetActState(message.ScenarioState);
			}
			catch (Exception ex)
			{
				Log($"Failed to update scenario state : {message.ScenarioState}", LogLevel.Error);
				Log(ex.Message, LogLevel.Error);
			}
		}

		public void HandleServerEventObjectivesProgressMessage(ObjectivesProgressMessage message)
		{
			ObjectivesBehavior objBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
			if (objBehavior != null)
			{
				objBehavior.TotalAttackerDead = message.AttackersDead;
				objBehavior.TotalDefenderDead = message.DefendersDead;
				objBehavior.StartTimerAsClient(message.TimerStart, message.TimerDuration);
			}
		}
	}
}
