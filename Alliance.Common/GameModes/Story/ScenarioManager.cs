using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using static Alliance.Common.GameModes.Story.Utilities.ScenarioData;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story
{
	/// <summary>
	/// Base class for managing a Scenario and Act, including its current state and objectives.
	/// </summary>
	public class ScenarioManager
	{
		public static readonly string SCENARIO_FOLDER_NAME = "Scenarios";

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

		/// <summary>
		/// Global variables for the current scenario. Initialized from Scenario.Variables on scenario start.		
		/// </summary>
		public VariableStore Globals { get; protected set; } = new VariableStore();

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
			InitGlobals();
			ActionBase.AssignActionIds(act, CurrentActIndex);
			OnStartScenario?.Invoke();
		}

		/// <summary>
		/// Initializes global variables from the current scenario definition.
		/// Parses each ScenarioVariable.DefaultValue according to its VariableType.
		/// </summary>
		protected virtual void InitGlobals()
		{
			Globals = new VariableStore();
			if (CurrentScenario?.Variables == null) return;
			foreach (ScenarioVariable sv in CurrentScenario.Variables)
			{
				if (string.IsNullOrWhiteSpace(sv.Name)) continue;
				object val = ParseDefaultValue(sv.DefaultValue, sv.Type);
				Globals.Set(sv.Name, val);
			}
		}

		/// <summary>
		/// Parses a default value string according to VariableType.
		/// </summary>
		private static object ParseDefaultValue(string raw, VariableType type)
		{
			switch (type)
			{
				case VariableType.Int:
					if (int.TryParse(raw, out int i)) return i;
					return 0;
				case VariableType.Float:
					if (float.TryParse(raw, out float f)) return f;
					return 0f;
				case VariableType.Bool:
					if (bool.TryParse(raw, out bool b)) return b;
					return false;
				case VariableType.String:
					return raw ?? "";
				default:
					return raw;
			}
		}

		public virtual void StopScenario()
		{
			UnregisterObjectives();
			OnStopScenario?.Invoke();
			ActState = ActState.Invalid;
			CurrentWinner = BattleSideEnum.None;
			Globals?.Reset();
		}

		/// <summary>
		/// Sets the current state of the act.
		/// </summary>
		public virtual void SetActState(ActState newState)
		{
			Log($"==================================================", LogLevel.Debug);
			Log($"{CurrentScenario?.Name?.LocalizedText} - {CurrentActIndex + 1} - {CurrentAct?.Name?.LocalizedText} - {newState}", LogLevel.Debug);
			Log($"==================================================", LogLevel.Debug);

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
					CurrentAct.VictoryLogic.Execute();
					OnActStateDisplayResults?.Invoke();
					break;
				case ActState.Completed:
					OnActStateCompleted?.Invoke();
					break;
			}
		}

		public virtual void OnMissionTick(float dt) { }

		/// <summary>
		/// Sets the winner of the current act.
		/// </summary>
		public virtual void SetWinner(BattleSideEnum winner)
		{
			UnregisterObjectives();
			CurrentWinner = winner;
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

			foreach (BattleSideEnum side in Enum.GetValues(typeof(BattleSideEnum)))
			{
				if (CheckObjectivesForSide(side))
				{
					Log($"Side {side} has completed all objectives.", LogLevel.Debug);
					return true;
				}
			}

			return false;
		}

		private bool CheckObjectivesForSide(BattleSideEnum side)
		{
			List<Objective> objectives = CurrentAct.Objectives.FindAll(o => o.Side == side && o.Active);

			if (objectives.Count == 0)
			{
				Log($"No objectives found for side {side}.", LogLevel.Debug);
				return false;
			}

			VariableStore context = Globals;
			bool sideWin = true;

			foreach (Objective objective in objectives)
			{
				bool objectiveCompleted = objective.Check(context);
				LogObjectiveProgress(objective, objectiveCompleted);

				if (objectiveCompleted)
				{
					if (objective.InstantActWin)
					{
						SetWinner(objective.Side);
						return true;
					}
				}

				if (!objective.Optional)
				{
					sideWin &= objectiveCompleted;
				}
			}

			if (sideWin)
			{
				SetWinner(side);
				return true;
			}

			return false;
		}

		public virtual void LogObjectiveProgress(Objective objective, bool objectiveCompleted)
		{
			string logMessage = objectiveCompleted
				? $"{objective.Name.LocalizedText} ({objective.Side}) completed"
				: $"{objective.Name.LocalizedText} ({objective.Side})";

			Log(logMessage, LogLevel.Debug);
		}

		public void RefreshAvailableScenarios()
		{
			List<ModuleInfo> selectedModules = TaleWorlds.Engine.Utilities.GetModulesNames().Select(ModuleHelper.GetModuleInfo).ToList();

			List<Scenario> scenarios = new List<Scenario>();

			// Check each multiplayer module for scenarios
			foreach (ModuleInfo module in selectedModules)
			{
				string scenarioPath = Path.Combine(ModuleHelper.GetModuleFullPath(module.Id), SCENARIO_FOLDER_NAME);
				if (Directory.Exists(scenarioPath))
				{
					try
					{
						var moduleScenarios = ScenarioSerializer.DeserializeAllScenarios(scenarioPath);
						scenarios.AddRange(moduleScenarios);
					}
					catch (Exception ex)
					{
						Log($"Failed to deserialize scenarios from module {module.Name}: {ex.Message}", LogLevel.Error);
						continue;
					}
				}
			}

			AvailableScenario = new List<Scenario>();
			AvailableScenario.AddRange(scenarios);
		}
	}
}