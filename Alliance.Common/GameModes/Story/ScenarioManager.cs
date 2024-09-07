using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story
{
	/// <summary>
	/// Base class for managing a Scenario and Act, including its current state and objectives.
	/// </summary>
	public class ScenarioManager
	{
		private static ScenarioManager _instance;
		public static ScenarioManager Instance
		{
			get
			{
				if (_instance == null)
					throw new InvalidOperationException("ScenarioManager has not been initialized.");
				return _instance;
			}
			set => _instance = value;
		}

		public event Action OnStartScenario;
		public event Action OnStopScenario;

		public event Action OnActStateAwaitPlayerJoin;
		public event Action OnActStateSpawnParticipants;
		public event Action OnActStateInProgress;
		public event Action OnActStateDisplayResults;
		public event Action OnActStateCompleted;

		public List<Scenario> AvailableScenario { get; protected set; }

		// Current scenario, act, act state and winner
		public Scenario CurrentScenario { get; protected set; }
		public Act CurrentAct { get; protected set; }
		public int CurrentActIndex => CurrentScenario.Acts.IndexOf(CurrentAct);
		public ActState ActState { get; protected set; }
		public BattleSideEnum CurrentWinner { get; protected set; }

		public virtual void StartScenario(string scenarioId, int actIndex, ActState state = ActState.Invalid) { }

		/// <summary>
		/// Initializes and starts a new scenario with the specified act and state.
		/// </summary>
		public virtual void StartScenario(Scenario scenario, Act act, ActState state = ActState.Invalid)
		{
			CurrentScenario = scenario;
			CurrentAct = act;
			ActState = state;
			CurrentWinner = BattleSideEnum.None;
			OnStartScenario?.Invoke();
		}

		public virtual void StopScenario()
		{
			UnregisterObjectives();
			CurrentScenario = null;
			CurrentAct = null;
			ActState = ActState.Invalid;
			CurrentWinner = BattleSideEnum.None;
			OnStopScenario?.Invoke();
		}

		/// <summary>
		/// Sets the current state of the act.
		/// </summary>
		public virtual void SetActState(ActState newState)
		{
			ActState = newState;
			switch (newState)
			{
				case ActState.Invalid:
					break;
				case ActState.AwaitingPlayerJoin:
					OnActStateAwaitPlayerJoin?.Invoke();
					break;
				case ActState.SpawningParticipants:
					OnActStateSpawnParticipants?.Invoke();
					break;
				case ActState.InProgress:
					OnActStateInProgress?.Invoke();
					break;
				case ActState.DisplayingResults:
					OnActStateDisplayResults?.Invoke();
					break;
				case ActState.Completed:
					CurrentAct.VictoryLogic.OnActCompleted(CurrentWinner);
					OnActStateCompleted?.Invoke();
					break;
			}
		}

		/// <summary>
		/// Sets the winner of the current act.
		/// </summary>
		public virtual void SetWinner(BattleSideEnum winner)
		{
			CurrentWinner = winner;
			CurrentAct.VictoryLogic.OnDisplayResults(winner);
		}

		/// <summary>
		/// Registers all objectives in the current act.
		/// </summary>
		public virtual void RegisterObjectives()
		{
			CurrentAct.RegisterObjectives();
		}

		/// <summary>
		/// Unregisters all objectives in the current act.
		/// </summary>
		public virtual void UnregisterObjectives()
		{
			CurrentAct?.UnregisterObjectives();
		}

		/// <summary>
		/// Checks all objectives for the current act. 
		/// If any objective is completed, the objective is deactivated.
		/// If any side has completed all of its objectives, it's considered as the winner.
		/// </summary>
		public virtual bool CheckObjectives()
		{
			if (CurrentWinner != BattleSideEnum.None)
			{
				return true;
			}

			bool[] objectivesCompletedBySide = new bool[(int)BattleSideEnum.NumSides + 1];

			foreach (ObjectiveBase objective in CurrentAct.Objectives)
			{
				if (!objective.Active) continue;

				bool objectiveCompleted = objective.CheckObjective();
				LogObjectiveProgress(objective, objectiveCompleted);

				if (objective.RequiredForActWin)
				{
					objectivesCompletedBySide[(int)objective.Side] &= objectiveCompleted;
				}

				if (objectiveCompleted)
				{
					objective.Active = false;

					if (objective.InstantActWin)
					{
						UnregisterObjectives();
						SetWinner(objective.Side);
						return true;
					}
				}
			}

			for (int i = 0; i < objectivesCompletedBySide.Length; i++)
			{
				if (objectivesCompletedBySide[i])
				{
					UnregisterObjectives();
					SetWinner((BattleSideEnum)i);
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Logs the progress of the given objective.
		/// </summary>
		public virtual void LogObjectiveProgress(ObjectiveBase objective, bool objectiveCompleted)
		{
			string logMessage = objectiveCompleted
				? $"{objective.Name} ({objective.Side}) completed : {objective.GetProgressAsString()}"
				: $"{objective.Name} ({objective.Side}) : {objective.GetProgressAsString()}";

			ConsoleColor logColor = objectiveCompleted ? ConsoleColor.Green : ConsoleColor.Cyan;

			Log(logMessage, LogLevel.Debug);
		}

		public void RefreshAvailableScenarios()
		{
			AvailableScenario = new List<Scenario>();
			AvailableScenario.AddRange(ScenarioSerializer.DeserializeAllScenarios(Path.Combine(ModuleHelper.GetModuleFullPath("Alliance"), "Scenarios")));
		}
	}
}