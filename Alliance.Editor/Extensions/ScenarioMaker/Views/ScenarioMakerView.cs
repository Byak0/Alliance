using Alliance.Editor.Extensions.ScenarioMaker.State;
using Alliance.Editor.Extensions.ScenarioMaker.ViewModels;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace Alliance.Editor.Extensions.ScenarioMaker.Views
{
    /// <summary>
    /// Scenario Maker menu.
    /// </summary>
    [GameStateScreen(typeof(ScenarioMakerState))]
    public class ScenarioMakerScreen : ScreenBase, IGameStateListener
    {
        Scene CurrentScene { get; set; }

        public ScenarioMakerScreen(ScenarioMakerState state)
        {
            _state = state;
            CurrentScene = _state.StateScene;
            _state.RegisterListener(this);
        }

        protected override void OnFrameTick(float dt)
        {
            base.OnFrameTick(dt);
            if (Input.IsKeyDown(InputKey.Escape))
            {
                CloseScreen();
            }
        }

        void IGameStateListener.OnActivate()
        {
            base.OnActivate();
            _vm = new ScenarioMakerVM(CurrentScene);
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", true);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
            _gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
            _gauntletLayer.LoadMovie("ScenarioMakerMenu", _vm);
            _gauntletLayer.IsFocusLayer = true;
            AddLayer(_gauntletLayer);
            ScreenManager.TrySetFocus(_gauntletLayer);
        }

        void IGameStateListener.OnDeactivate()
        {
            base.OnDeactivate();
            RemoveLayer(_gauntletLayer);
            _gauntletLayer.IsFocusLayer = false;
            ScreenManager.TryLoseFocus(_gauntletLayer);
        }

        void IGameStateListener.OnFinalize()
        {
            _gauntletLayer = null;
            _vm = null;
        }

        void IGameStateListener.OnInitialize()
        {
            base.OnInitialize();
        }

        private void CloseScreen()
        {
            GameStateManager.Current.PopState(0);
        }

        private GauntletLayer _gauntletLayer;

        private ScenarioMakerVM _vm;

        private ScenarioMakerState _state;
    }
}