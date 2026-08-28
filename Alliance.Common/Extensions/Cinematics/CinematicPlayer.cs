using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Extensions.Cinematics
{
	/// <summary>
	/// Cinematic clock + sampler. Advances playback time and emits side effects
	/// (camera, overlay, subtitles, keyframe crossings) to an ICinematicPlaybackSink.
	/// Pure data - no engine camera code - so it runs identically on client, server and editor preview.
	/// </summary>
	public class CinematicPlayer
	{
		private const float MaxStep = 0.1f; // clamp dt to avoid skipping keyframes after a hitch

		private readonly Cinematic _cinematic;
		private readonly ICinematicPlaybackSink _sink;
		private readonly ICinematicBindings _bindings;
		private readonly HashSet<CinematicKeyframe> _fired = new HashSet<CinematicKeyframe>();

		private float _currentTime;
		private float _lastTime = -1f;
		private bool _playing;

		public bool IsPlaying => _playing;
		public float CurrentTime => _currentTime;
		public float Duration => _cinematic?.GetDuration() ?? 0f;
		public Cinematic Cinematic => _cinematic;

		public CinematicPlayer(Cinematic cinematic, ICinematicPlaybackSink sink = null, ICinematicBindings bindings = null)
		{
			_cinematic = cinematic;
			_sink = sink;
			_bindings = bindings;
		}

		public void Start()
		{
			_currentTime = 0f;
			_lastTime = -1f;
			_fired.Clear();
			_playing = _cinematic != null && Duration > 0f;
		}

		public void Stop()
		{
			if (!_playing) return;
			_playing = false;
			_sink?.OnFinished();
		}

		public void Seek(float time)
		{
			_currentTime = Math.Max(0f, time);
			_lastTime = _currentTime;
			_fired.Clear();
		}

		/// <summary>Re-samples every continuous track (camera, screen overlay, subtitles) at the current
		/// time without advancing playback and without firing discrete events — used when seeking/scrubbing
		/// while paused in the editor.</summary>
		public void SampleOnce()
		{
			if (!_playing || _cinematic == null) return;
			SampleScreen(_currentTime, Duration);
			SampleCamera(_currentTime);
			SampleSubtitle(_currentTime);
		}

		public void Tick(float dt)
		{
			if (!_playing || _cinematic == null) return;
			if (dt > MaxStep) dt = MaxStep;
			if (dt <= 0f) return;

			float prevTime = _currentTime;
			_currentTime += dt;
			float duration = Duration;
			bool wrapped = false;

			if (duration > 0f && _currentTime >= duration)
			{
				if (_cinematic.Loop)
				{
					_currentTime -= duration;
					prevTime = -1f;
					_fired.Clear();
					wrapped = true;
				}
				else
				{
					_currentTime = duration;
				}
			}

			if (_sink != null && _sink.RequiresVisualSampling)
			{
				SampleScreen(_currentTime, duration);
				SampleCamera(_currentTime);
				SampleSubtitle(_currentTime);
			}

			FireCrossings(prevTime, _currentTime);

			if (!wrapped)
			{
				bool finished = duration <= 0f || (!_cinematic.Loop && _currentTime >= duration);
				if (finished)
				{
					_playing = false;
					_sink?.OnFinished();
				}
			}
		}

		private float ComputeFadeAlpha(float time, float duration)
		{
			float alpha = 0f;
			if (_cinematic.FadeInSec > 0f && time < _cinematic.FadeInSec)
				alpha = 1f - (time / _cinematic.FadeInSec);

			if (duration > 0f && _cinematic.FadeOutSec > 0f)
			{
				float start = duration - _cinematic.FadeOutSec;
				if (time > start)
					alpha = Math.Max(alpha, (time - start) / _cinematic.FadeOutSec);
			}

			if (alpha < 0f) alpha = 0f;
			if (alpha > 1f) alpha = 1f;
			return alpha;
		}

		/// <summary>Drives the letterbox + fade overlay. Uses a OverlayTrack if present, otherwise the
		/// Cinematic.FadeInSec/FadeOutSec convenience (no letterbox).</summary>
		private void SampleScreen(float time, float duration)
		{
			if (_sink == null) return;
			OverlayTrack track = _cinematic.FindTrack<OverlayTrack>();
			if (track != null && track.Keyframes != null && track.Keyframes.Count > 0
				&& KeyframeEvaluator.Bracket(track.Keyframes, time,
					out OverlayKeyframe prev, out OverlayKeyframe next, out _, out _, out float localT))
			{
				float curveT = KeyframeEvaluator.CurveT(next.Interpolation, localT);
				float letterbox = prev == next ? prev.Letterbox : prev.Letterbox + (next.Letterbox - prev.Letterbox) * curveT;
				float fade = prev == next ? prev.FadeAlpha : prev.FadeAlpha + (next.FadeAlpha - prev.FadeAlpha) * curveT;
				_sink.OnScreen(letterbox, fade);
			}
			else if (track == null && (_cinematic.FadeInSec > 0f || _cinematic.FadeOutSec > 0f))
			{
				_sink.OnScreen(0f, ComputeFadeAlpha(time, duration));
			}
		}

		private void SampleCamera(float time)
		{
			CameraTrack track = _cinematic.FindTrack<CameraTrack>();
			if (track == null || track.Keyframes == null || track.Keyframes.Count == 0) return;

			if (!KeyframeEvaluator.Bracket(track.Keyframes, time,
				out CameraKeyframe prev, out CameraKeyframe next,
				out CameraKeyframe before, out CameraKeyframe after, out float localT))
			{
				return;
			}

			CameraState state = KeyframeEvaluator.EvaluateCamera(prev, next, before, after, localT);
			state = ApplyKeyframeTarget(state, prev, next, localT);
			state = ApplyLookAt(state, time);
			_sink?.OnCameraState(state);
		}

		private CameraState ApplyKeyframeTarget(CameraState state, CameraKeyframe prev, CameraKeyframe next, float localT)
		{
			if (prev.LookAt == LookAtMode.None && next.LookAt == LookAtMode.None) return state;
			Vec3? prevTarget = ResolveKeyframeTarget(prev);
			Vec3? nextTarget = ResolveKeyframeTarget(next);
			if (prevTarget == null && nextTarget == null) return state;

			float curveT = KeyframeEvaluator.CurveT(next.Interpolation, localT);
			MatrixFrame? prevLook = prevTarget.HasValue ? CameraMath.LookAtFrame(state.Frame.origin, prevTarget.Value) : (MatrixFrame?)null;
			MatrixFrame? nextLook = nextTarget.HasValue ? CameraMath.LookAtFrame(state.Frame.origin, nextTarget.Value) : (MatrixFrame?)null;

			Mat3 rot;
			if (prevLook != null && nextLook != null)
				rot = Mat3.Lerp(prevLook.Value.rotation, nextLook.Value.rotation, curveT);
			else if (prevLook != null)
				rot = Mat3.Lerp(prevLook.Value.rotation, state.Frame.rotation, curveT);
			else
				rot = Mat3.Lerp(state.Frame.rotation, nextLook.Value.rotation, curveT);

			state.Frame = new MatrixFrame(rot, state.Frame.origin);
			return state;
		}

		private Vec3? ResolveKeyframeTarget(CameraKeyframe kf)
		{
			switch (kf.LookAt)
			{
				case LookAtMode.MainAgent:
					return _bindings?.ResolveRolePosition("MainAgent");
				case LookAtMode.Entity:
					{
						WeakGameEntity e = kf.LookAtEntity?.Resolve(null) ?? WeakGameEntity.Invalid;
						return e.IsValid ? e.GetGlobalFrame().origin : (Vec3?)null;
					}
				case LookAtMode.Position:
					return kf.LookAtPosition?.ToFrame().origin;
				default:
					return null;
			}
		}

		private CameraState ApplyLookAt(CameraState state, float time)
		{
			LookAtTrack track = _cinematic.FindTrack<LookAtTrack>();
			if (track == null || track.Keyframes == null || track.Keyframes.Count == 0) return state;

			if (!KeyframeEvaluator.Bracket(track.Keyframes, time,
				out LookAtKeyframe prev, out LookAtKeyframe next,
				out _, out _, out float localT))
			{
				return state;
			}

			Vec3? targetPrev = ResolveTarget(prev);
			Vec3? targetNext = ResolveTarget(next);
			if (!targetPrev.HasValue && !targetNext.HasValue) return state;

			Vec3 target;
			if (targetPrev.HasValue && targetNext.HasValue)
				target = Vec3.Lerp(targetPrev.Value, targetNext.Value, KeyframeEvaluator.Smoothstep(localT));
			else
				target = (targetNext.HasValue ? targetNext : targetPrev).Value;

			Vec3 dir = target - state.Frame.origin;
			if (dir.LengthSquared < 1e-6f) return state;
			// Bannerlord cameras look along -u, so build the look-at with that convention (CameraMath.LookAtFrame).
			state.Frame = CameraMath.LookAtFrame(state.Frame.origin, target);
			return state;
		}

		private Vec3? ResolveTarget(LookAtKeyframe kf)
		{
			if (kf == null) return null;
			if (kf.UsePosition) return kf.TargetPosition?.ToFrame().origin;
			if (!string.IsNullOrEmpty(kf.TargetRole)) return _bindings?.ResolveRolePosition(kf.TargetRole);
			return null;
		}

		// Reused sample buffers - hosts must not hold references to these lists (they are refilled every tick).
		private readonly List<SubtitleState> _activeSubtitles = new List<SubtitleState>();

		private void SampleSubtitle(float time)
		{
			if (_sink == null) return;
			_activeSubtitles.Clear();

			foreach (var track in _cinematic.Tracks ?? Enumerable.Empty<CinematicTrack>())
			{
				if (track is not SubtitleTrack || !track.Enabled || track.Muted) continue;
				var st = (SubtitleTrack)track;
				if (st.Keyframes == null) continue;

				foreach (var kf in st.Keyframes)
				{
					float elapsed = time - kf.Time;
					if (elapsed < 0 || elapsed > kf.Duration) continue;

					float fade = kf.FadeSec > 0f ? kf.FadeSec : 0.01f;
					float alpha;
					if (elapsed < fade)
						alpha = elapsed / fade;
					else if (elapsed > kf.Duration - fade)
						alpha = Math.Max(0f, (kf.Duration - elapsed) / fade);
					else
						alpha = 1f;

					_activeSubtitles.Add(new SubtitleState
					{
						Source = kf,
						Text = kf.Text?.GetText() ?? "",
						Alpha = alpha,
						FontSize = kf.FontSize,
						FontColor = HexColor.Normalize(kf.FontColor),
						Font = string.IsNullOrEmpty(kf.Font) ? "Galahad" : kf.Font,
						HAlign = kf.HPosition switch
						{
							SubtitleHPosition.Left => TextHorizontalAlignment.Left,
							SubtitleHPosition.Center => TextHorizontalAlignment.Center,
							SubtitleHPosition.Right => TextHorizontalAlignment.Right,
							_ => TextHorizontalAlignment.Center
						},
						VAlign = kf.VPosition switch
						{
							SubtitleVPosition.Top => VerticalAlignment.Top,
							SubtitleVPosition.Center => VerticalAlignment.Center,
							SubtitleVPosition.Bottom => VerticalAlignment.Bottom,
							_ => VerticalAlignment.Bottom
						}
					});
				}
			}

			_sink.OnSubtitles(_activeSubtitles);
		}

		private void FireCrossings(float prevTime, float currentTime)
		{
			if (_sink == null) return;
			List<CinematicTrack> tracks = _cinematic.Tracks;
			if (tracks == null) return;

			bool visuals = _sink.RequiresVisualSampling;
			foreach (CinematicTrack track in tracks)
			{
				if (track == null || !track.Enabled || track.Muted) continue;
				if (!visuals && track is not EventTrack) continue;

				switch (track)
				{
					case EventTrack et:
						Fire(et.Keyframes, prevTime, currentTime, kf => _sink.OnEventActions(kf.Actions));
						break;
				case AudioTrack at:
					Fire(at.Keyframes, prevTime, currentTime, kf => _sink.OnAudio(kf.SoundEvent, kf.Volume, kf.Loop));
					break;
				case EntityVisibilityTrack vt:
						Fire(vt.Keyframes, prevTime, currentTime, kf => _sink.OnEntityVisibility(kf.Entity, kf.Visible));
						break;
					case AgentAnimationTrack mt:
						Fire(mt.Keyframes, prevTime, currentTime, kf => _sink.OnAgentAnimation(kf.TargetRole, kf.ActionName, kf.FacialAnimation, kf.Loop));
						break;
				}
			}
		}

		private void Fire<T>(IList<T> keyframes, float prevTime, float currentTime, Action<T> handler) where T : CinematicKeyframe
		{
			if (keyframes == null) return;
			foreach (T kf in keyframes)
			{
				if (kf == null || _fired.Contains(kf)) continue;
				if (kf.Time > prevTime && kf.Time <= currentTime)
				{
					_fired.Add(kf);
					handler(kf);
				}
			}
		}
	}
}
