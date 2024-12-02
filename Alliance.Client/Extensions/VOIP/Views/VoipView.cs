using Alliance.Client.Extensions.VOIP.ViewModels;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.VOIP.Views
{
    /// <summary>
    /// Custom view for VOIP.
    /// </summary>
    [DefaultView]
    public class VoipView : MissionView
    {
        public VoipView()
        {
            ViewOrderPriority = 60;
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _dataSource = new VoipVM(Mission);
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("VoiceChat", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
        }

        public override void OnMissionScreenFinalize()
        {
            MissionScreen.RemoveLayer(_gauntletLayer);
            _dataSource.OnFinalize();
            _dataSource = null;
            _gauntletLayer = null;
            base.OnMissionScreenFinalize();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            _dataSource.OnTick(dt);
        }

        private VoipVM _dataSource;

        private GauntletLayer _gauntletLayer;
    }
}
