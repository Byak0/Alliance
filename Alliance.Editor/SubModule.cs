using Alliance.Editor.Extensions.ScenarioMaker.State;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor
{
    public class SubModule : MBSubModuleBase
    {
        public const string ModuleId = "Alliance.Editor";

        protected override void OnSubModuleLoad()
        {
            Log("Alliance.Editor initialized", LogLevel.Debug);
        }

        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
        }

        public void OpenScenarioMakerMenu(Scene scene)
        {
            ScenarioMakerState scenarioMakerState = GameStateManager.Current.CreateState<ScenarioMakerState>(new object[] { scene });
            GameStateManager.Current.PushState(scenarioMakerState, 0);
        }

        protected override void OnApplicationTick(float dt)
        {
            // Test to show custom menu in editor
            //if (Input.IsKeyPressed(InputKey.O))
            //{
            //    SceneView sceneView = MBEditor.GetEditorSceneView();
            //    Scene scene = sceneView?.GetScene();
            //    OpenScenarioMakerMenu(scene);
            //}
        }
    }
}