using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Play a sound.
	/// </summary>
	[Serializable]
	public class PlaySoundAction : ActionBase
	{
		public enum SoundCategory
		{
			AudioLocal,
			MusicLocal,
			MainMusic
		}

		[ScenarioEditor(label: "Sound Name", tooltip: "You can use either a file name from Alliance/ModuleSounds or a native event. Examples: \n- LOTR/OST/Flaming Red Hair.wav\n- event:/music/musicians/aserai/01", dataType: ScenarioData.DataTypes.Sounds)]
		public string SoundName;
		[ScenarioEditor(label: "Category", tooltip: "Type of sound to play.\n- AudioLocal : generic localized sounds (explosions, sound effects, etc.)\n- MusicLocal : localized music (taverns)\n- MainMusic : main music theme (not localized)")]
		public SoundCategory SoundType;
		[ScenarioEditor(label: "Mute Main Music", tooltip: "Mute the current main music while this sound is playing (for temporary/localized musics). Doesn't work with native sounds.")]
		public bool PauseMainMusicWhilePlaying;
		[ScenarioEditor(label: "Volume", tooltip: "Volume level. Default is 1.")]
		public float Volume = 1f;
		[ScenarioEditor(label: "Position", tooltip: "Position of the sound. Radius is the hearing range. Can't be used with Main Music.")]
		public SerializableZone SoundZone;

		public PlaySoundAction() { }

		public override void Execute()
		{
			if (!GameNetwork.IsServer) return;

			switch (SoundType)
			{
				case SoundCategory.AudioLocal:
					PlayLocalizedSound();
					break;
				case SoundCategory.MusicLocal:
					PlayLocalizedMusic();
					break;
				case SoundCategory.MainMusic:
					PlayMainMusic();
					break;
			}
		}

		private void PlayMainMusic()
		{
			if (string.IsNullOrEmpty(SoundName)) return;

			if (IsNativeSound(SoundName))
			{
				NativeAudioPlayer.Instance.PlaySound(SoundName, synchronize: true);
			}
			else
			{
				AudioPlayer.Instance.PlayMainMusic(SoundName, Mission.Current.GetMissionTimeInSeconds(), synchronize: true);
			}
		}

		private void PlayLocalizedMusic()
		{
			if (string.IsNullOrEmpty(SoundName) || Mission.Current == null || SoundZone == null) return;

			if (IsNativeSound(SoundName))
			{
				NativeAudioPlayer.Instance.PlaySoundLocalized(SoundName, SoundZone.GlobalPosition, synchronize: true);
			}
			else
			{
				AudioPlayer.Instance.PlayLocalizedMusic(SoundName, Volume, SoundZone.GlobalPosition, (int)SoundZone.Radius, Mission.Current.GetMissionTimeInSeconds(), PauseMainMusicWhilePlaying, synchronize: true);
			}
		}

		private void PlayLocalizedSound()
		{
			if (string.IsNullOrEmpty(SoundName)) return;

			if (IsNativeSound(SoundName))
			{
				NativeAudioPlayer.Instance.PlaySoundLocalized(SoundName, SoundZone.GlobalPosition, synchronize: true);
			}
			else
			{
				AudioPlayer.Instance.Play(SoundName, Volume, true, (int)SoundZone.Radius, SoundZone.GlobalPosition, synchronize: true);
			}
		}

		private bool IsNativeSound(string soundName)
		{
			// Check if it's a native sound (e.g., a sound with a native event ID)
			return soundName.StartsWith("event:/");
		}
	}
}