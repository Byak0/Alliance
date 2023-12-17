using Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.ExNativeUI.MarkerUIHandler.Views
{
    [OverrideView(typeof(MissionMultiplayerMarkerUIHandler))]
    public class MarkerUIHandlerView : MissionView
    {
        public MarkerUIHandlerView()
        {
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _dataSource = new MarkerUIHandlerVM(MissionScreen.CombatCamera);
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("MPMissionMarkers", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            MissionScreen.RemoveLayer(_gauntletLayer);
            _gauntletLayer = null;
            _dataSource.OnFinalize();
            _dataSource = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (Input.IsGameKeyDown(5))
            {
                _dataSource.IsEnabled = true;
            }
            else
            {
                _dataSource.IsEnabled = false;
            }
            _dataSource.Tick(dt);
        }

        private GauntletLayer _gauntletLayer;

        private MarkerUIHandlerVM _dataSource;
    }
}