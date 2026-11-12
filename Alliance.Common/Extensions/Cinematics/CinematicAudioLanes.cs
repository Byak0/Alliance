#if !SERVER
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Client-side runtime of the cinematic AudioTracks: one lane per AudioTrack, at most one sounding sound per lane.
	/// Orchestrates the lane (play/stop/fade/spatialization, etc.).
	/// Engine sounds (FMOD events, module_sounds entries) go through <see cref="NativeAudioPlayer"/>.
	/// Custom ModuleSounds files through <see cref="AudioPlayer"/>.
	/// Everything is played locally by client, based on the shared cinematic clock.
	/// </summary>
	public class CinematicAudioLanes
	{
		private const int DefaultHearingDistance = 100;
		/// <summary>Backward playhead jump above which a loop wrap is assumed (lane state is then
		/// re-derived from the timeline, like a seek).</summary>
		private const float WrapDetectionSec = 0.5f;

		/// <summary>One sounding (or fading-out) sound of a lane.</summary>
		private class ActiveSound
		{
			public AudioKeyframe Kf;
			/// <summary>Volume resolved from the keyframe's ValueSource at fire time (0..2).</summary>
			public float BaseVolume;
			public SoundEvent Native;
			public CachedSound Custom;
			public bool Is3D;
			/// <summary>Seconds since the current fade started; envelope direction given by FadingOut.</summary>
			public float FadeElapsed;
			/// <summary>0 = no ramp (full volume / instant cut).</summary>
			public float FadeDuration;
			public bool FadingOut;
			public bool Done;
			/// <summary>Native retrigger guard: re-fire only after the event was observed playing,
			/// so slow-loading events are not recreated every tick.</summary>
			public bool NativeSeenPlaying;
			/// <summary>Volume envelope from the fade ramps (0..1), excluding distance/base volume.</summary>
			public float FadeFactor => FadeDuration <= 0f ? 1f
				: FadingOut ? 1f - Math.Min(1f, FadeElapsed / FadeDuration)
				: Math.Min(1f, FadeElapsed / FadeDuration);
		}

		private readonly Dictionary<AudioTrack, List<ActiveSound>> _lanes = new Dictionary<AudioTrack, List<ActiveSound>>();
		/// <summary>Sounds flagged as "Persistent" that were released from their lane when the cinematic ended:
		/// they keep sounding, but stay tracked so any full stop (next cinematic, stop-by-name, mission end) kills them.</summary>
		private readonly List<ActiveSound> _persisted = new List<ActiveSound>();

		private Cinematic _cinematic;
		private ICinematicBindings _bindings;
		private Func<Scene> _sceneProvider;
		private Func<MatrixFrame?> _listenerProvider;
		private float _lastTimelineTime = -1f;

		/// <summary>Wire the manager to the current playback. Cuts any previous lane state.</summary>
		public void Begin(Cinematic cinematic, ICinematicBindings bindings, Func<Scene> sceneProvider, Func<MatrixFrame?> listenerProvider)
		{
			CutAll();
			_lanes.Clear();
			AudioPlayer.Instance.ResetDuck();
			_cinematic = cinematic;
			_bindings = bindings;
			_sceneProvider = sceneProvider;
			_listenerProvider = listenerProvider;
			_lastTimelineTime = -1f;
		}

		/// <summary>Apply an audio keyframe crossing (Play starts/replaces the lane's sound, Stop ends it).
		/// A null lane (keyframe detached from its track) still plays on an anonymous lane.</summary>
		public void OnKeyframe(AudioTrack lane, AudioKeyframe kf)
		{
			if (kf == null) return;
			List<ActiveSound> sounds = GetLane(lane);

			if (kf.Kind == AudioActionKind.Stop)
			{
				TransitionOut(sounds);
				return;
			}

			TransitionOut(sounds);

			ActiveSound sound = new ActiveSound
			{
				Kf = kf,
				BaseVolume = TryResolveVolume(kf),
				Is3D = kf.Position != null && kf.Position.Type != CinematicTargetType.None,
				FadeDuration = Math.Max(0f, kf.FadeInSec)
			};
			if (StartSound(sound)) sounds.Add(sound);
		}

		/// <summary>Ends the lane's current sound: custom files ramp down over their own keyframe's
		/// FadeOutSec (0 = cut), native events cut (FMOD events have no managed fades).</summary>
		private void TransitionOut(List<ActiveSound> sounds)
		{
			for (int i = 0; i < sounds.Count; i++)
			{
				ActiveSound sound = sounds[i];
				if (sound == null || sound.FadingOut || sound.Done) continue;
				if (sound.Custom != null && sound.Kf != null && sound.Kf.FadeOutSec > 0f)
				{
					sound.FadingOut = true;
					sound.FadeElapsed = 0f;
					sound.FadeDuration = sound.Kf.FadeOutSec;
				}
				else
				{
					Kill(sound);
					sound.Done = true;
				}
			}
		}

		/// <summary>Advance every lane: fade ramps, 3D follows, native loop retriggers, completion
		/// cleanup and main-music ducking. timelineTime is the player's current time (wrap detection).</summary>
		public void Tick(float dt, float timelineTime)
		{
			if (_cinematic == null) return;

			if (_lastTimelineTime >= 0f && timelineTime + WrapDetectionSec < _lastTimelineTime)
			{
				SyncToTime(timelineTime);
			}
			_lastTimelineTime = timelineTime;

			float duckFraction = 0f;
			float duckRelease = 1f;
			MatrixFrame? listener = ListenerFrame();

			foreach (KeyValuePair<AudioTrack, List<ActiveSound>> lane in _lanes)
			{
				for (int i = 0; i < lane.Value.Count; i++)
				{
					ActiveSound sound = lane.Value[i];
					if (sound == null || sound.Done) continue;

					if (sound.Custom != null) TickCustom(sound, dt, listener, ref duckFraction, ref duckRelease);
					else if (sound.Native != null) TickNative(sound);
				}
				lane.Value.RemoveAll(s => s == null || s.Done);
			}

			AudioPlayer.Instance.DuckMainMusic(duckFraction, duckRelease);
			AudioPlayer.Instance.TickDuck(dt);
		}

		private void TickCustom(ActiveSound sound, float dt, MatrixFrame? listener, ref float duckFraction, ref float duckRelease)
		{
			sound.FadeElapsed += dt;

			if (sound.FadingOut)
			{
				if (sound.FadeElapsed >= sound.FadeDuration || sound.Custom.IsComplete)
				{
					Kill(sound);
					sound.Done = true;
					return;
				}
			}
			else if (sound.Custom.IsComplete)
			{
				Kill(sound);
				sound.Done = true;
				return;
			}

			MatrixFrame? target = sound.Is3D ? ResolveTarget(sound.Kf) : null;
			AudioPlayer.Instance.UpdateHandledSound(sound.Custom, sound.BaseVolume, sound.FadeFactor, listener, target);

			if (!sound.FadingOut && sound.Kf.DuckMainMusic > 0f)
			{
				float fraction = Math.Min(1f, sound.Kf.DuckMainMusic / 100f);
				if (fraction > duckFraction)
				{
					duckFraction = fraction;
					duckRelease = sound.Kf.DuckReleaseSec > 0f ? sound.Kf.DuckReleaseSec : 1f;
				}
			}
		}

		private void TickNative(ActiveSound sound)
		{
			if(sound.Native != null && sound.Native.IsPlaying()) sound.NativeSeenPlaying = true;

			if (sound.Kf.Loop && (sound.Native == null || (sound.NativeSeenPlaying && !sound.Native.IsPlaying())))
			{
				// Loop = retrigger: re-fire the event from the top once it finished.
				NativeAudioPlayer.Instance.StopHandled(sound.Native);
				sound.Native = null;
				if (!StartSound(sound)) sound.Done = true;
				return;
			}

			if (sound.Is3D)
			{
				NativeAudioPlayer.Instance.UpdateHandledSound(sound.Native, ResolveTarget(sound.Kf)?.origin);
			}
		}

		/// <summary>Seek/late-join catch-up: each lane derives its sounding sound from the timeline
		/// position - a still-sounding loop restarts at the elapsed offset (custom files support
		/// mid-file entry; native loops restart from the top), past one-shots never replay.</summary>
		public void SyncToTime(float time)
		{
			if (_cinematic == null) return;
			CutAll();
			_lastTimelineTime = time;

			foreach (AudioTrack lane in (_cinematic.Tracks ?? Enumerable.Empty<CinematicTrack>()).OfType<AudioTrack>())
			{
				if (lane == null || !lane.Enabled || lane.Muted) continue;
				List<ActiveSound> sounds = GetLane(lane);
				AudioKeyframe last = null;
				foreach (AudioKeyframe kf in lane.Keyframes ?? Enumerable.Empty<AudioKeyframe>())
				{
					if (kf == null) continue;
					if (kf.Time <= time) last = kf;
				}
				if (last == null || last.Kind != AudioActionKind.Play) continue;

				float elapsed = time - last.Time;
				float? duration = last.SoundSource == AudioSoundSource.CustomFile
					? AudioPlayer.Instance.GetSoundDuration(last.SoundName)
					: null;

				bool sounding;
				float offset = 0f;
				if (duration.HasValue)
				{
					// Custom: exact extents known - loops are state (restart at the elapsed offset, mid-file), one-shots still sound only inside their extent.
					sounding = last.Loop || elapsed < duration.Value;
					if (duration.Value > 0f) offset = last.Loop ? elapsed % duration.Value : elapsed;
				}
				else
				{
					// Native: loops are state (restart from the top), one-shots are events (a past fire never replays).
					sounding = last.SoundSource == AudioSoundSource.NativeEvent && last.Loop;
				}
				if (!sounding) continue;

				ActiveSound sound = new ActiveSound
				{
					Kf = last,
					BaseVolume = TryResolveVolume(last),
					Is3D = last.Position != null && last.Position.Type != CinematicTargetType.None,
					FadeDuration = Math.Max(0f, last.FadeInSec)
				};
				// Resume mid-fade when the playhead lands inside the fade-in window.
				sound.FadeElapsed = Math.Min(sound.FadeDuration, Math.Max(0f, elapsed));
				if (StartSound(sound, offset)) sounds.Add(sound);
			}
		}

		/// <summary>Stop the managed sounds at cinematic end. persistSounding: keyframes flagged
		/// Persist are released instead of cut - they keep sounding (music continuing into
		/// gameplay) until any full stop. Either way the ducked main music is restored.</summary>
		public void StopAll(bool persistSounding)
		{
			if (persistSounding)
			{
				foreach (List<ActiveSound> sounds in _lanes.Values)
				{
					for (int i = 0; i < sounds.Count; i++)
					{
						ActiveSound sound = sounds[i];
						if (sound == null || sound.Done) continue;
						if (sound.Kf != null && sound.Kf.Persist)
						{
							_persisted.Add(sound);
							sound.Done = true; // detached from the lane without killing it
						}
					}
					sounds.RemoveAll(s => s == null || s.Done);
				}
			}
			else CutAll();
			AudioPlayer.Instance.ResetDuck();
		}

		/// <summary>Immediate stop of every managed sound (no ramps), including persisted ones.</summary>
		public void CutAll()
		{
			foreach (List<ActiveSound> sounds in _lanes.Values)
			{
				for (int i = 0; i < sounds.Count; i++)
				{
					ActiveSound sound = sounds[i];
					if (sound == null || sound.Done) continue;
					Kill(sound);
					sound.Done = true;
				}
				sounds.RemoveAll(s => s == null || s.Done);
			}
			for (int i = 0; i < _persisted.Count; i++) Kill(_persisted[i]);
			_persisted.Clear();
		}

		private bool StartSound(ActiveSound sound, float startTime = 0f)
		{
			if (sound.Kf.SoundSource == AudioSoundSource.NativeEvent)
			{
				Vec3? origin = sound.Is3D ? ResolveTarget(sound.Kf)?.origin : null;
				sound.Native = NativeAudioPlayer.Instance.PlayHandled(sound.Kf.SoundName, sound.BaseVolume, origin, _sceneProvider?.Invoke());
				return sound.Native != null;
			}

			Vec3? origin3D = sound.Is3D ? ResolveTarget(sound.Kf)?.origin : null;
			sound.Custom = AudioPlayer.Instance.PlayHandled(sound.Kf.SoundName, sound.BaseVolume, sound.Kf.Loop,
				startTime, origin3D, DefaultHearingDistance, ListenerFrame());
			return sound.Custom != null;
		}

		private void Kill(ActiveSound sound)
		{
			if (sound.Native != null)
			{
				NativeAudioPlayer.Instance.StopHandled(sound.Native);
				sound.Native = null;
			}
			if (sound.Custom != null)
			{
				AudioPlayer.Instance.RemoveSound(sound.Custom);
				sound.Custom = null;
			}
		}

		private MatrixFrame? ResolveTarget(AudioKeyframe kf)
		{
			if (kf?.Position == null || kf.Position.Type == CinematicTargetType.None) return null;
			try { return _bindings?.ResolveTargetFrame(kf.Position); }
			catch { return null; }
		}

		private MatrixFrame? ListenerFrame()
		{
			try { return _listenerProvider?.Invoke(); }
			catch { return null; }
		}

		private static float TryResolveVolume(AudioKeyframe kf)
		{
			try { return kf.Volume?.Resolve(null) ?? 1f; }
			catch { return 1f; }
		}

		private List<ActiveSound> GetLane(AudioTrack lane)
		{
			if (!_lanes.TryGetValue(lane, out List<ActiveSound> sounds))
			{
				sounds = new List<ActiveSound>();
				_lanes[lane] = sounds;
			}
			return sounds;
		}
	}
}
#endif
