using Alliance.Common.Extensions.Audio;
using TaleWorlds.Engine.Options;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;

namespace Alliance.Client.Extensions.Audio.Views
{
    [DefaultView]
    public class AudioView : MissionView
    {
        private float _lastTick;

        public AudioView()
        {
        }

        public override void AfterStart()
        {
            AudioPlayer.Instance.SetVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
            NativeOptions.OnNativeOptionChanged += OnNativeOptionChanged;
        }

        public override void OnRemoveBehavior()
        {
            NativeOptions.OnNativeOptionChanged -= OnNativeOptionChanged;
        }

        private void OnNativeOptionChanged(NativeOptions.NativeOptionsType changedNativeOptionsType)
        {
            if (changedNativeOptionsType == NativeOptions.NativeOptionsType.MasterVolume)
            {
                AudioPlayer.Instance.SetVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
            }
            else if (changedNativeOptionsType == NativeOptions.NativeOptionsType.SoundVolume)
            {
                AudioPlayer.Instance.SetVolume(NativeOptions.GetConfig(NativeOptions.NativeOptionsType.MasterVolume) * NativeOptions.GetConfig(NativeOptions.NativeOptionsType.SoundVolume));
            }
        }

        public override void OnMissionScreenTick(float dt)
        {
            _lastTick += dt;
            if(_lastTick < 0.2f)
            {
                return;
            }
            _lastTick = 0f;
            AudioPlayer.Instance.CleanSounds();
            AudioPlayer.Instance.UpdateSoundPositions();
        }
    }
}
