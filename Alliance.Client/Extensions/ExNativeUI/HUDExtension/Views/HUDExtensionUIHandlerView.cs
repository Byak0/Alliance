using Alliance.Client.Extensions.ExNativeUI.HUDExtension.ViewModels;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.TwoDimension;

namespace Alliance.Client.Extensions.ExNativeUI.HUDExtension.Views
{
    //[OverrideView(typeof(MissionMultiplayerHUDExtensionUIHandler))]
    public class HUDExtensionUIHandlerView : MissionView
    {
        public HUDExtensionUIHandlerView()
        {
            ViewOrderPriority = 2;
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            _mpMissionCategory = spriteData.SpriteCategories["ui_mpmission"];
            _mpMissionCategory.Load(resourceContext, uiresourceDepot);
            _dataSource = new HUDExtensionVM(Mission);
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("HUDExtension", _dataSource);
            MissionScreen.AddLayer(_gauntletLayer);
            MissionScreen.OnSpectateAgentFocusIn += _dataSource.OnSpectatedAgentFocusIn;
            MissionScreen.OnSpectateAgentFocusOut += _dataSource.OnSpectatedAgentFocusOut;
            Game.Current.EventManager.RegisterEvent(new Action<MissionPlayerToggledOrderViewEvent>(OnMissionPlayerToggledOrderViewEvent));
            _lobbyComponent = Mission.GetMissionBehavior<MissionLobbyComponent>();
            _lobbyComponent.OnPostMatchEnded += OnPostMatchEnded;
        }

        public override void OnMissionScreenFinalize()
        {
            _lobbyComponent.OnPostMatchEnded -= OnPostMatchEnded;
            MissionScreen.OnSpectateAgentFocusIn -= _dataSource.OnSpectatedAgentFocusIn;
            MissionScreen.OnSpectateAgentFocusOut -= _dataSource.OnSpectatedAgentFocusOut;
            MissionScreen.RemoveLayer(_gauntletLayer);
            SpriteCategory mpMissionCategory = _mpMissionCategory;
            if (mpMissionCategory != null)
            {
                mpMissionCategory.Unload();
            }
            _dataSource.OnFinalize();
            _dataSource = null;
            _gauntletLayer = null;
            Game.Current.EventManager.UnregisterEvent(new Action<MissionPlayerToggledOrderViewEvent>(OnMissionPlayerToggledOrderViewEvent));
            base.OnMissionScreenFinalize();
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            _dataSource.Tick(dt);
        }

        private void OnMissionPlayerToggledOrderViewEvent(MissionPlayerToggledOrderViewEvent eventObj)
        {
            _dataSource.IsOrderActive = eventObj.IsOrderEnabled;
        }

        private void OnPostMatchEnded()
        {
            _dataSource.ShowHud = false;
        }

        private HUDExtensionVM _dataSource;

        private GauntletLayer _gauntletLayer;

        private SpriteCategory _mpMissionCategory;

        private MissionLobbyComponent _lobbyComponent;
    }
}