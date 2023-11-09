using Alliance.Client.Extensions.FlagsTracker.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.FlagsTracker.Views
{
    /// <summary>
    /// View showing capture zones markers in UI.
    /// </summary>
    public class CaptureMarkerUIView : MissionView
    {
        public CaptureMarkerUIView()
        {
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _dataSource = new CaptureMarkerUIVM(MissionScreen.CombatCamera);
            _gauntletLayer = new GauntletLayer(1, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("MPCaptureMarkers", _dataSource);
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

        private CaptureMarkerUIVM _dataSource;
    }
}