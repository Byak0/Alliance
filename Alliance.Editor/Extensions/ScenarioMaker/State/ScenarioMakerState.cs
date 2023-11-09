using TaleWorlds.Core;
using TaleWorlds.Engine;

namespace Alliance.Editor.Extensions.ScenarioMaker.State
{
    public class ScenarioMakerState : GameState
    {
        public override bool IsMenuState
        {
            get
            {
                return true;
            }
        }

        public Scene StateScene { get; private set; }

        public ScenarioMakerState()
        {
        }

        public ScenarioMakerState(Scene scene)
        {
            StateScene = scene;
        }
    }
}