using Alliance.Common.Extensions;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using Alliance.Common.Extensions.Cinematics;
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
			reg.Register<SyncObjectiveProgressMessage>(HandleSyncObjectiveProgress);
			reg.Register<SyncScenarioLivesMessage>(HandleServerEventSyncScenarioLivesMessage);
			reg.Register<ExecuteActionMessage>(HandleExecuteActionMessage);
			reg.Register<PlayCinematicMessage>(HandlePlayCinematicMessage);
			reg.Register<StopCinematicMessage>(HandleStopCinematicMessage);
			reg.Register<SetCinematicTimeMessage>(HandleSetCinematicTimeMessage);
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
				Log($"Failed to start scenario \"{currentScenario.Name.LocalizedText}\" at act {message.Act}. Act index out of range.", LogLevel.Error);
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

		public void HandleSyncObjectiveProgress(SyncObjectiveProgressMessage message)
		{
			var view = Mission.Current.GetMissionBehavior<Views.ScenarioView>();
			if (view != null)
			{
				view.GetDataSource()?.SetSyncedProgress(message.Objectives);
			}
		}

		public void HandleServerEventSyncScenarioLivesMessage(SyncScenarioLivesMessage message)
		{
			ScenarioClientBehavior scenarioClientBehavior = Mission.Current.GetMissionBehavior<ScenarioClientBehavior>();
			scenarioClientBehavior?.UpdateLives(message.RespawnStrategy, message.TeamRemainingLives, message.PlayerRemainingLives);
		}

		public void HandleExecuteActionMessage(ExecuteActionMessage message)
		{
			try
			{
				var action = ActionBase.FindById(message.ScopeId, message.ActionId);
				if (action == null)
				{
					Log($"ExecuteActionMessage: no action with scope {message.ScopeId}, id {message.ActionId}", LogLevel.Warning);
					return;
				}
				action.InjectSyncData(message.Data);
				action.ExecuteClient();
			}
			catch (Exception ex)
			{
				Log($"Failed to execute action: {ex.Message}", LogLevel.Error);
			}
		}

		public void HandlePlayCinematicMessage(PlayCinematicMessage message)
		{
			try
			{
				Cinematic cinematic = message.UseActionRef
					? TryGetCinematicFromAction(message.ScopeId, message.ActionId)
					: ScenarioManager.Instance.CurrentScenario?.Cinematics?.Find(c => c != null && c.Id == message.CinematicId);
				if (cinematic == null)
				{
					Log($"PlayCinematicMessage: cinematic not found ({(message.UseActionRef ? $"scope={message.ScopeId}, action={message.ActionId}" : $"'{message.CinematicId}'")}).", LogLevel.Warning);
					return;
				}

				CinematicView view = Mission.Current?.GetMissionBehavior<CinematicView>();
				view?.PlayCinematic(cinematic, message.StartTimeInSeconds, message.IsSkippable, message.UseViewerOrigin);
			}
			catch (Exception ex)
			{
				Log($"Failed to play cinematic: {ex.Message}", LogLevel.Error);
			}
		}

		public void HandleStopCinematicMessage(StopCinematicMessage message)
		{
			try
			{
				CinematicView view = Mission.Current?.GetMissionBehavior<CinematicView>();
				if (view == null) return;
				// Empty id = wildcard (e.g. scenario abort); otherwise only stop the named cinematic.
				if (string.IsNullOrEmpty(message.CinematicId) || view.PlayingCinematicId == message.CinematicId)
					view.StopCinematic();
			}
			catch (Exception ex)
			{
				Log($"Failed to stop cinematic: {ex.Message}", LogLevel.Error);
			}
		}

		public void HandleSetCinematicTimeMessage(SetCinematicTimeMessage message)
		{
			try
			{
				CinematicView view = Mission.Current?.GetMissionBehavior<CinematicView>();
				if (view == null) return;
				if (view.PlayingCinematicId == message.CinematicId)
					view.Seek(message.TimeInSeconds);
			}
			catch (Exception ex)
			{
				Log($"Failed to set cinematic time: {ex.Message}", LogLevel.Error);
			}
		}

		/// <summary>Resolves the inline cinematic of an entity- or act-scoped PlayCinematicAction
		/// through the (ScopeId, ActionId) action registry.</summary>
		private Cinematic TryGetCinematicFromAction(int scopeId, int actionId)
		{
			try
			{
				if (ActionBase.FindById(scopeId, actionId) is PlayCinematicAction action && action.Cinematic != null)
					return action.Cinematic;
				return null;
			}
			catch (Exception ex)
			{
				Log($"Error getting cinematic from action (scope={scopeId}, id={actionId}): {ex.Message}", LogLevel.Error);
				return null;
			}
		}
	}
}
