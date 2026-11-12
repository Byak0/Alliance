using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.Cinematics.Models.Tracks
{
	/// <summary>
	/// Plays a sound event (voice line, SFX, music sting) when crossed.
	/// The audio listener follows the cinematic camera during playback.
	/// </summary>
	[Serializable]
	public class AudioKeyframe : CinematicKeyframe
	{
		[ConfigProperty(label: "Sound", tooltip: "Source = CustomFile: a file from any module's ModuleSounds (pick from the list).\nSource = NativeEvent: an FMOD event (\"event:/...\" path) or a module sound from the native catalogs.")]
		public string SoundName = "";

		[ConfigProperty(label: "Source", tooltip: "Custom file: ModuleSounds asset - exact volume, fades, gapless loop.\nNative event: FMOD event or module sound played through the engine (wonky best effort, no fades, volume control...).")]
		public AudioSoundSource SoundSource = AudioSoundSource.CustomFile;

		[ConfigProperty(label: "Action", tooltip: "Play: start this sound on the lane (replaces the previous one - fades overlap into a crossfade).\nStop: end the lane's current sound with its fade-out.")]
		public AudioActionKind Kind = AudioActionKind.Play;

		[ConfigProperty(label: "Volume", tooltip: "0..2, resolved when the keyframe fires. Exact for custom files. Native events: best-effort through the FMOD \"volume\" parameter - events without that parameter play at their authored loudness.", minValue: 0, maxValue: 2)]
		[SyncToClient]
		public ValueSource<float> Volume = new LiteralValue<float>(1f);

		[ConfigProperty(label: "Fade in (s)", tooltip: "Custom files only: ramp from silence up to the volume over this duration.", minValue: 0, maxValue: 60, dependency: "?Kind=Play&?SoundSource=CustomFile")]
		public float FadeInSec;

		[ConfigProperty(label: "Fade out (s)", tooltip: "Custom files only: ramp down over this duration when replaced by the next Play or ended by a Stop (0 = cut). Also used at cinematic end/skip, as a cut.", minValue: 0, maxValue: 60, dependency: "?Kind=Play&?SoundSource=CustomFile")]
		public float FadeOutSec;

		[ConfigProperty(label: "Loop", tooltip: "Custom files: true gapless loop (plays until replaced, stopped or the cinematic ends).\nNative events: retrigger - the event is re-fired from the top each time it finishes.", dependency: "?Kind=Play")]
		public bool Loop;

		[ConfigProperty(label: "Persist after cinematic", tooltip: "Keep sounding after the cinematic ends or is skipped (e.g. music started during an intro and continuing into gameplay). Custom loops keep looping; native sounds play out their current iteration (no retrigger once the cinematic is over).\nStopped by anything that fully stops the cinematic: another cinematic starting, a stop-by-name, or the mission ending.", dependency: "?Kind=Play")]
		public bool Persist;

		[ConfigProperty(label: "Position", tooltip: "Unset = 2D on the cinematic listener (the camera). Set = 3D, following the target every tick.")]
		public CinematicTarget Position = new CinematicTarget();

		[ConfigProperty(label: "Duck main music (%)", tooltip: "Custom files only: lower the game's main music by this percentage while the lane sounds (ramped both ways).", minValue: 0, maxValue: 100, dependency: "?Kind=Play&?SoundSource=CustomFile")]
		public float DuckMainMusic;

		[ConfigProperty(label: "Duck release (s)", tooltip: "Ramp duration of the ducking, both when engaging and when releasing.", minValue: 0.1f, maxValue: 10, dependency: "?Kind=Play&?SoundSource=CustomFile&?DuckMainMusic>0")]
		public float DuckReleaseSec = 1f;

		public AudioKeyframe() { }

		public AudioKeyframe(float time) : base(time) { }
	}

	[Serializable]
	public class AudioTrack : CinematicTrack
	{
		[ConfigProperty(label: "Keyframes")]
		public List<AudioKeyframe> Keyframes = new List<AudioKeyframe>();

		public AudioTrack() { }

		public override IList GetKeyframes() => Keyframes;
	}
}
