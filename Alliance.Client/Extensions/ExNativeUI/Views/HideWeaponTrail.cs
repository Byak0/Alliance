using Alliance.Common.Core.Configuration.Models;
using TaleWorlds.Engine.Options;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace Alliance.Client.Extensions.ExNativeUI.Views
{
    class HideWeaponTrail : MissionView
    {
        private bool _isTemporarilyOpenUI;
        private float _previousWeaponTrail = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.TrailAmount);

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);

            if (!_isTemporarilyOpenUI)
            {
                if (ScreenManager.FocusedLayer != MissionScreen.SceneLayer)
                {
                    _isTemporarilyOpenUI = true;
                    BeginTemporarilyOpenUI();
                }
                else if (!Config.Instance.ShowWeaponTrail && NativeOptions.GetConfig(NativeOptions.NativeOptionsType.TrailAmount) != 0)
                {
                    NativeOptions.SetConfig(NativeOptions.NativeOptionsType.TrailAmount, 0);
                }
            }
            else
            {
                if (ScreenManager.FocusedLayer == MissionScreen.SceneLayer)
                {
                    _isTemporarilyOpenUI = false;
                    EndTemporarilyOpenUI();
                }
            }
        }

        public void BeginTemporarilyOpenUI()
        {
            NativeOptions.SetConfig(NativeOptions.NativeOptionsType.TrailAmount, _previousWeaponTrail);
        }

        public void EndTemporarilyOpenUI()
        {
            _previousWeaponTrail = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.TrailAmount);
            if (!Config.Instance.ShowWeaponTrail)
            {
                NativeOptions.SetConfig(NativeOptions.NativeOptionsType.TrailAmount, 0);
            }
        }
    }
}
