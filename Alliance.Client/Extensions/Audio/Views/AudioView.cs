using Alliance.Common.Extensions.Audio;
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

		public override void OnMissionScreenTick(float dt)
		{
			_lastTick += dt;
			if (_lastTick < 0.1f)
			{
				return;
			}
			_lastTick = 0f;
			AudioPlayer.Instance.CleanSounds();
			AudioPlayer.Instance.TickAudio();
		}
	}
}
