using Alliance.Client.Extensions.FormationEnforcer.ViewModels;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.TwoDimension;

namespace Alliance.Client.Extensions.FormationEnforcer.Views
{
    public class FormationStatusView : MissionView
    {
        // Timer Manager
        float _lastFormationCheck = 0;

        private GauntletLayer _layer;
        private FormationStatusVM _dataSource;

        public FormationStatusView()
        {
        }

        public override void EarlyStart()
        {
            ViewOrderPriority = 20;
            _dataSource = new FormationStatusVM();
            _layer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _layer.LoadMovie("FormationStatusHUD", _dataSource);
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiResourceDepot = UIResourceManager.UIResourceDepot;
            spriteData.SpriteCategories["ui_mplobby"].Load(resourceContext, uiResourceDepot);
            MissionScreen.AddLayer(_layer);
        }

        public override void AfterStart()
        {
            //if(PvCRepresentative.Main != null) PvCRepresentative.Main.OnStateChange += OnStateChange;
        }

        public override void OnMissionScreenFinalize()
        {
            //if (PvCRepresentative.Main != null) PvCRepresentative.Main.OnStateChange -= OnStateChange;
            MissionScreen.RemoveLayer(_layer);
            _layer = null;
            _dataSource?.OnFinalize();
            _dataSource = null;
        }

        public override void OnMissionScreenTick(float dt)
        {
            // Tick every 0.5second
            _lastFormationCheck += dt;
            if (_lastFormationCheck >= 0.25f)
            {
                _lastFormationCheck = 0;
                if (FormationComponent.Main != null) _dataSource.FormationStatusState = FormationComponent.Main.State;
                _dataSource.ShowFormationStatus = Agent.Main?.Team?.ActiveAgents.Count > Config.Instance.MinPlayerForm;
            }
        }

        //private void OnStateChange(object sender, EventArgs e)
        //{
        //    _dataSource.FormationStatusState = PvCRepresentative.Main.State;            
        //}
    }
}