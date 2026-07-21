using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Play a sound.
	/// </summary>
	[Serializable]
	[PhrasePreview("Play {SoundName}")]
	[PhraseTemplate(
		"Play {SoundName} as a {SoundType|local sound|local music|main music} at volume {Volume}{?SoundType!=MainMusic: and position {SoundZone}}",
		"While we hear the sound, {PauseMainMusicWhilePlaying|leave main music playing|mute main music}")]
	public class PlaySoundAction : ActionBase
	{
		public enum SoundCategory
		{
			AudioLocal,
			MusicLocal,
			MainMusic
		}

		[ConfigProperty(label: "Sound Name", tooltip: "You can use either a file name from Alliance/ModuleSounds or a native event. Examples: \n- LOTR/OST/Flaming Red Hair.wav\n- event:/music/musicians/aserai/01", dataType: AllianceData.DataTypes.Sounds)]
		public string SoundName;
		[ConfigProperty(label: "Category", tooltip: "Type of sound to play.\n- AudioLocal : generic localized sounds (explosions, sound effects, etc.)\n- MusicLocal : localized music (taverns)\n- MainMusic : main music theme (not localized)")]
		public SoundCategory SoundType;
		[ConfigProperty(label: "Mute Main Music", tooltip: "Mute the current main music while this sound is playing (for temporary/localized musics). Doesn't work with native sounds.")]
		public bool PauseMainMusicWhilePlaying;
		[ConfigProperty(label: "Volume", tooltip: "Volume level. Default is 1.")]
		public ValueSource<float> Volume = new LiteralValue<float>(1f);
		[ConfigProperty(label: "Position", tooltip: "Position of the sound. Radius is the hearing range. Can't be used with Main Music.")]
		public ValueSource<Zone> SoundZone = new LiteralValue<Zone>(new Zone());

		public PlaySoundAction() { }

		public override ActionTask Execute()
		{
			if (!GameNetwork.IsServer) return ActionTask.CompletedTask;
			float volume = Volume?.Resolve(ScenarioManager.Instance.CurrentTriggerContext, ScenarioManager.Instance.Globals) ?? 1f;

			switch (SoundType)
			{
				case SoundCategory.AudioLocal:
					PlayLocalizedSound(volume);
					break;
				case SoundCategory.MusicLocal:
					PlayLocalizedMusic(volume);
					break;
				case SoundCategory.MainMusic:
					PlayMainMusic();
					break;
			}
			return ActionTask.CompletedTask;
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

		private void PlayLocalizedMusic(float volume)
		{
			if (string.IsNullOrEmpty(SoundName) || Mission.Current == null || SoundZone == null) return;

			Zone zone = SoundZone.Resolve(ScenarioManager.Instance.CurrentTriggerContext, ScenarioManager.Instance.Globals);
			if (zone == null) return;
			Vec3 center = zone.ResolveCenter(ScenarioManager.Instance.CurrentTriggerContext, ScenarioManager.Instance.Globals);

			if (IsNativeSound(SoundName))
			{
				NativeAudioPlayer.Instance.PlaySoundLocalized(SoundName, center, synchronize: true);
			}
			else
			{
				AudioPlayer.Instance.PlayLocalizedMusic(SoundName, volume, center, (int)zone.Radius, Mission.Current.GetMissionTimeInSeconds(), PauseMainMusicWhilePlaying, synchronize: true);
			}
		}

		private void PlayLocalizedSound(float volume)
		{
			if (string.IsNullOrEmpty(SoundName)) return;

			Zone zone = SoundZone.Resolve(ScenarioManager.Instance.CurrentTriggerContext, ScenarioManager.Instance.Globals);
			if (zone == null) return;
			Vec3 center = zone.ResolveCenter(ScenarioManager.Instance.CurrentTriggerContext, ScenarioManager.Instance.Globals);

			if (IsNativeSound(SoundName))
			{
				NativeAudioPlayer.Instance.PlaySoundLocalized(SoundName, center, synchronize: true);
			}
			else
			{
				AudioPlayer.Instance.Play(SoundName, volume, true, (int)zone.Radius, center, synchronize: true);
			}
		}

		private bool IsNativeSound(string soundName)
		{
			// Check if it's a native sound (e.g., a sound with a native event ID)
			return soundName.StartsWith("event:/");
		}
	}
}
