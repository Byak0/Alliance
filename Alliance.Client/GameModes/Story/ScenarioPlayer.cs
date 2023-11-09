using Alliance.Client.GameModes.Story.Scenarios;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using System.Reflection;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.GameModes.Story
{
    /// <summary>
    /// Client version of the ScenarioManager. 
    /// </summary>
    public class ScenarioPlayer : ScenarioManager
    {
        private static readonly ScenarioPlayer instance = new ScenarioPlayer();
        public static ScenarioPlayer Instance { get { return instance; } }

        public override void StartScenario(string scenarioId, int actIndex, ActState state = ActState.Invalid)
        {
            Scenario currentScenario;
            Act currentAct;
            MethodInfo scenarioMethod = typeof(ClientScenarios).GetMethod(scenarioId);

            if (scenarioMethod == null)
            {
                currentScenario = null;
            }
            else
            {
                currentScenario = scenarioMethod.Invoke(null, null) as Scenario;
            }

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
