using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.GameModes.Story
{
	/// <summary>
	/// Client version of the ScenarioManager. 
	/// </summary>
	public class ScenarioPlayer : ScenarioManager
	{
		public static void Initialize()
		{
			Instance = new ScenarioPlayer();
			Instance.RefreshAvailableScenarios();
		}

		public override void StartScenario(string scenarioId, int actIndex, ActState state = ActState.Invalid)
		{
			Scenario currentScenario;
			Act currentAct;
			currentScenario = AvailableScenario.Find(scenario => scenario.Id == scenarioId);

			if (currentScenario != null && currentScenario.Acts.Count > actIndex)
			{
				currentAct = currentScenario.Acts[actIndex];
				StartScenario(currentScenario, currentAct, state);
				Log($"Starting scenario \"{currentScenario?.Name}\" at act {actIndex}", LogLevel.Information);
			}
			else
			{
				Log($"Failed to start scenario \"{currentScenario?.Name}\" at act {actIndex}", LogLevel.Error);
			}
		}
	}
}
