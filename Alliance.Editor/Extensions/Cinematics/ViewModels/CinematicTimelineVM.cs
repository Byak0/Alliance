using Alliance.Editor.GameModes.Story.ViewModels;
using Alliance.Editor.GameModes.Story.Views;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.Extensions.Cinematics;
using Alliance.Common.Extensions.Cinematics.Models;
using Alliance.Common.Core.Utils;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Engine;
using Alliance.Common.Extensions.Cinematics.Models.Tracks;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using TaleWorlds.Library;

namespace Alliance.Editor.Extensions.Cinematics.ViewModels
{
	/// <summary>
	/// View-model for the WPF cinematic timeline editor. The single, dedicated UI for authoring a <see cref="Cinematic"/>: 
	/// Feature play/stop, a draggable camera-keyframe timeline lane, and an inspector for the selected keyframe.
	/// </summary>
	public class CinematicTimelineVM : INotifyPropertyChanged
	{
		/// <summary>Horizontal padding (px) kept at each end of the lane so keyframes at t=0 / t=duration
		/// aren't half-clipped and stay easy to grab. The scrubber above uses the same inset to align.</summary>
		public const float TimelinePadding = 14f;

		/// <summary>Width over which keyframe time is mapped (lane width minus the two paddings).</summary>
		public float UsableWidth => Math.Max(1f, TimelineWidth - 2f * TimelinePadding);

		// -- Marker color palette (per track type) ------------------------------------------
		private static SolidColorBrush Rgb(byte r, byte g, byte b)
		{
			var brush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
			brush.Freeze();
			return brush;
		}
		internal static readonly SolidColorBrush SelectedFill = Rgb(0xFF, 0xFF, 0xFF);
		internal static readonly SolidColorBrush NormalStroke = Rgb(0x1B, 0x1F, 0x25);
		internal static readonly SolidColorBrush DefaultFill = Rgb(0x6B, 0x74, 0x80);
		internal static readonly SolidColorBrush CameraFill = Rgb(0xC8, 0x82, 0x1F);

		private static readonly Dictionary<Type, SolidColorBrush> TrackFills = new Dictionary<Type, SolidColorBrush>
		{
			{ typeof(CameraTrack), CameraFill },
			{ typeof(OverlayTrack), Rgb(0xAB, 0x47, 0xBC) },
			{ typeof(AudioTrack), Rgb(0x66, 0xBB, 0x6A) },
			{ typeof(SubtitleTrack), Rgb(0x26, 0xC6, 0xDA) },
			{ typeof(EventTrack), Rgb(0xEF, 0x53, 0x50) },
			{ typeof(EntityTrack), Rgb(0x42, 0xA5, 0xF5) },
			{ typeof(AgentActionTrack), Rgb(0x26, 0xA6, 0x9A) },
			{ typeof(LookAtTrack), Rgb(0xFF, 0xEE, 0x58) },
		};

		internal static SolidColorBrush BrushForTrack(CinematicTrack track)
		{
			if (track != null && TrackFills.TryGetValue(track.GetType(), out SolidColorBrush b)) return b;
			return DefaultFill;
		}

		private float _timelineWidth = 760f;
		/// <summary>Current rendered width of the timeline lane (px). Updated on resize so keyframes and the
		/// playhead scale with the window, matching the scrubber above.</summary>
		public float TimelineWidth
		{
			get => _timelineWidth;
			set
			{
				if (Math.Abs(_timelineWidth - value) > 0.5f)
				{
					_timelineWidth = value;
					RecomputeKeyframePositions();
				}
			}
		}

		private readonly Cinematic _cinematic;
		/// <summary>The scenario the cinematic belongs to (null for standalone inline copies) - gives
		/// the timeline's value-source popups their variable context.</summary>
		public Scenario Scenario { get; }
		private readonly Action<Cinematic> _onClosed;
		private CameraKeyframeVM _selected;
		private float _currentTime;
		private string _title;

		public ObservableCollection<CameraKeyframeVM> Keyframes { get; } = new ObservableCollection<CameraKeyframeVM>();
		public ObservableCollection<TrackLaneVM> OtherTracks { get; } = new ObservableCollection<TrackLaneVM>();

		/// <summary>Available track type names for the "Add Track" picker.</summary>
		public string[] AvailableTrackTypes { get; } =
		{
			"Overlay",
			"LookAt",
			"Event", "Entity", "Agent", "Audio", "Subtitle"
		};

		private string _selectedTrackType = "Overlay";
		public string SelectedTrackType { get => _selectedTrackType; set { _selectedTrackType = value; OnPropertyChanged(); } }

		public CameraKeyframeVM SelectedKeyframe
		{
			get => _selected;
			set
			{
				if (_selected != value)
				{
					if (_selected != null) _selected.IsSelected = false;
					_selected = value;
					if (_selected != null) _selected.IsSelected = true;
					_selectedGeneric = null; // mutually exclusive
					ClearGenericMarkerHighlights();
					OnPropertyChanged();
					OnPropertyChanged(nameof(HasSelection));
					OnPropertyChanged(nameof(NoSelection));
					OnPropertyChanged(nameof(HasGenericSelection));
					CommandManager.InvalidateRequerySuggested();
				}
			}
		}

		public bool HasSelection => SelectedKeyframe != null;
		public bool NoSelection => SelectedKeyframe == null && _selectedGeneric == null;

		private GenericKeyframeVM _selectedGeneric;
		public GenericKeyframeVM SelectedGenericKeyframe
		{
			get => _selectedGeneric;
			set
			{
				if (_selected != null) _selected.IsSelected = false;
				_selectedGeneric = value;
				if (value != null) { _selected = null; value.OnTimeChanged = () => RecomputeKeyframePositions(); }
				OnPropertyChanged();
				OnPropertyChanged(nameof(HasGenericSelection));
				OnPropertyChanged(nameof(HasSelection));
				OnPropertyChanged(nameof(NoSelection));
				CommandManager.InvalidateRequerySuggested();
			}
		}
		public bool HasGenericSelection => _selectedGeneric != null;

		private void ClearGenericMarkerHighlights()
		{
			foreach (var lane in OtherTracks)
				foreach (var m in lane.Markers)
					m.IsSelected = false;
		}

		private bool _isPlaying;     // preview is alive (playing or paused)
		private bool _isPaused;      // preview is paused
		private bool _playbackUpdating; // set while the playback callback drives CurrentTime (don't seek)

		public bool IsPlaying { get => _isPlaying; set { _isPlaying = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseLabel)); CommandManager.InvalidateRequerySuggested(); } }
		public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PlayPauseLabel)); CommandManager.InvalidateRequerySuggested(); } }

		/// <summary>Dynamic label for the play/pause toggle button.</summary>
		public string PlayPauseLabel
		{
			get
			{
				if (!_isPlaying) return "? Play";
				return _isPaused ? "? Resume" : "? Pause";
			}
		}

		public float CurrentTime
		{
			get => _currentTime;
			set
			{
				_currentTime = Math.Max(0f, value);
				PlayheadX = TimelinePadding + (_currentTime / EffectiveDuration) * UsableWidth;
				OnPropertyChanged();
				// When the user scrubs (not the playback callback) while not actively playing, seek the preview.
				if (!_playbackUpdating && (!IsPlaying || IsPaused))
				{
					EditorToolsManager.SeekPreview(_currentTime);
					if (IsPaused) EditorToolsManager.SamplePreview();
				}
			}
		}

		public float EffectiveDuration => Math.Max(_cinematic.GetDuration(), 0.001f);

		private float _playheadX;
		public float PlayheadX { get => _playheadX; set { _playheadX = value; OnPropertyChanged(); } }

		public string Title { get => _title; set { _title = value; OnPropertyChanged(); } }

		public string Name
		{
			get => _cinematic?.Name ?? "";
			set
			{
				if (_cinematic != null)
				{
					_cinematic.Name = value;
					Title = "Cinematic Editor" + (!string.IsNullOrEmpty(value) ? " - " + value : "");
					OnPropertyChanged();
				}
			}
		}

		public float DurationSec
		{
			get => _cinematic.DurationSec;
			set { _cinematic.DurationSec = Math.Max(0f, value); RecomputeKeyframePositions(); OnPropertyChanged(nameof(EffectiveDuration)); OnPropertyChanged(); }
		}
		public bool Loop { get => _cinematic.Loop; set { _cinematic.Loop = value; OnPropertyChanged(); } }
		public bool IsSkippable { get => _cinematic.IsSkippable; set { _cinematic.IsSkippable = value; OnPropertyChanged(); } }
		public AgentBehaviorMode AgentBehavior { get => _cinematic.AgentBehavior; set { _cinematic.AgentBehavior = value; OnPropertyChanged(); } }
		public Array AgentBehaviorOptions => Enum.GetValues(typeof(AgentBehaviorMode));
		public InvulnerabilityMode Invulnerability { get => _cinematic.Invulnerability; set { _cinematic.Invulnerability = value; OnPropertyChanged(); } }
		public Array InvulnerabilityOptions => Enum.GetValues(typeof(InvulnerabilityMode));

		public ICommand PlayPauseCommand { get; }
		public ICommand StopCommand { get; }
		public ICommand AddKeyframeCommand { get; }
		public ICommand AddTrackCommand { get; }
		public ICommand DeleteGenericKeyframeCommand { get; }
		public ICommand CopyCinematicCommand { get; }
		public ICommand PasteCinematicCommand { get; }
		public ICommand CopyKeyframeCommand { get; }
		public ICommand PasteKeyframeCommand { get; }

		private static object _keyframeClipboard;
		private static Type _keyframeClipboardType;
		public bool CanPasteKeyframe => _keyframeClipboard != null && (
			(_selected != null && _keyframeClipboardType == _selected.Keyframe.GetType()) ||
			(_selectedGeneric != null && _keyframeClipboardType == _selectedGeneric.Model.GetType()));
		public bool CanPasteCinematic => _cinematicClipboard != null;
		private static object _cinematicClipboard;

		public CinematicTimelineVM(Cinematic cinematic, Action<Cinematic> onClosed, Scenario scenario = null)
		{
			_cinematic = cinematic;
			_onClosed = onClosed;
			Scenario = scenario;
			EditorToolsManager.ActiveEditingCinematic = cinematic;
			Title = "Cinematic Editor" + (!string.IsNullOrEmpty(cinematic?.Name) ? " - " + cinematic.Name : "");
			RebuildKeyframes();
			RebuildTracks();

			PlayPauseCommand = new RelayCommand(_ => PlayPause(), _ => Keyframes.Count > 0);
			StopCommand = new RelayCommand(_ => Stop(), _ => IsPlaying);
			AddKeyframeCommand = new RelayCommand(_ => AddKeyframe());
			AddTrackCommand = new RelayCommand(_ => AddTrack());
			DeleteGenericKeyframeCommand = new RelayCommand(_ => DeleteGenericKeyframe(), _ => _selectedGeneric != null);
			CopyCinematicCommand = new RelayCommand(_ => CopyCinematic());
			PasteCinematicCommand = new RelayCommand(_ => PasteCinematic(), _ => CanPasteCinematic);
			CopyKeyframeCommand = new RelayCommand(_ => CopySelectedKeyframe(), _ => _selectedGeneric != null || _selected != null);
			PasteKeyframeCommand = new RelayCommand(_ => PasteIntoSelectedKeyframe(), _ => CanPasteKeyframe);
		}

		private void CopyCinematic()
		{
			_cinematicClipboard = ObjectEditorViewModel.DeepCloneObject(_cinematic);
			OnPropertyChanged(nameof(CanPasteCinematic));
		}

		private void PasteCinematic()
		{
			if (_cinematicClipboard == null) return;
			ObjectEditorViewModel.CopyObjectState(_cinematicClipboard, _cinematic);
			RebuildKeyframes();
			RebuildTracks();
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(DurationSec));
			OnPropertyChanged(nameof(EffectiveDuration));
		}

		private void CopySelectedKeyframe()
		{
			object src = _selected != null ? (object)_selected.Keyframe : _selectedGeneric?.Model;
			if (src == null) return;
			_keyframeClipboardType = src.GetType();
			_keyframeClipboard = ObjectEditorViewModel.DeepCloneObject(src);
			OnPropertyChanged(nameof(CanPasteKeyframe));
		}

		private void PasteIntoSelectedKeyframe()
		{
			if (_keyframeClipboard == null) return;
			object target = _selected != null ? (object)_selected.Keyframe : _selectedGeneric?.Model;
			if (target == null || target.GetType() != _keyframeClipboardType) return;
			float savedTime = ((CinematicKeyframe)target).Time;
			ObjectEditorViewModel.CopyObjectState(_keyframeClipboard, target);
			((CinematicKeyframe)target).Time = savedTime;
			if (_selected != null) _selected.RefreshAll();
			if (_selectedGeneric != null)
			{
				var newVm = GenericKeyframeVM.Create((CinematicKeyframe)target, this);
				SelectedGenericKeyframe = newVm;
			}
			RecomputeKeyframePositions();
		}

		public CameraTrack CameraTrack => _cinematic.FindTrack<CameraTrack>();

		/// <summary>Single Play / Pause / Resume toggle.</summary>
		public void PlayPause()
		{
			if (!IsPlaying)
			{
				// Editor preview runs without a started scenario - seed the globals store so value
				// slots (subtitle text args, target variables...) resolve with authored defaults.
				if (Scenario != null) ScenarioManager.Instance.LoadGlobals(Scenario);
				EditorToolsManager.PlayPreview(_cinematic, t =>
				{
					_playbackUpdating = true;
					CurrentTime = t;
					_playbackUpdating = false;
				}, OnPreviewFinished);
				IsPlaying = EditorToolsManager.IsPreviewing;
				IsPaused = false;
			}
			else if (IsPaused)
			{
				EditorToolsManager.ResumePreview();
				IsPaused = false;
			}
			else
			{
				EditorToolsManager.PausePreview();
				IsPaused = true;
			}
		}

		private void OnPreviewFinished()
		{
			IsPaused = false;
			IsPlaying = false;
			CurrentTime = 0f;
		}

		public void Stop()
		{
			EditorToolsManager.StopPreview();
			// OnPreviewFinished (fired by StopPreview) resets IsPlaying/IsPaused/CurrentTime.
		}

		public void AddKeyframe()
		{
			CameraTrack track = EnsureCameraTrack();
			// Insert at the playhead/cursor position so keyframes land where you scrubbed.
			float t = Math.Max(0f, _currentTime);
			if (Keyframes.Any(k => Math.Abs(k.Time - t) < 0.05f)) t = (Keyframes.Max(k => k.Time)) + 1f;
			var kf = new CameraKeyframe(t) { Fov = 60f };
			MatrixFrame? frame = EditorToolsManager.CaptureEditorCameraFrame();
			if (frame.HasValue) kf.Frame.CopyFrom(FrameValue.FromFrame(frame.Value));
			track.Keyframes.Add(kf);
			RebuildKeyframes();
			SelectedKeyframe = Keyframes.FirstOrDefault(vm => ReferenceEquals(vm.Keyframe, kf));
		}

		/// <summary>Captures the current editor view into the given keyframe.</summary>
		public void CaptureInto(CameraKeyframeVM vm)
		{
			if (vm == null) return;
			MatrixFrame? frame = EditorToolsManager.CaptureEditorCameraFrame();
			if (frame.HasValue)
			{
				vm.Keyframe.Frame.CopyFrom(FrameValue.FromFrame(frame.Value));
				vm.RefreshAll();
			}
		}

		public void DeleteKeyframe(CameraKeyframeVM vm)
		{
			if (vm == null) return;
			if (ReferenceEquals(vm, _selected)) SelectedKeyframe = null;
			CameraTrack track = CameraTrack;
			if (track != null)
			{
				track.Keyframes.Remove(vm.Keyframe);
				RebuildKeyframes();
			}
		}

		/// <summary>Called when the window is closing (title-bar ?): stops the preview and notifies the host.</summary>
		public void OnExternalClose()
		{
			Stop();
			EditorToolsManager.ActiveEditingCinematic = null;
			_onClosed?.Invoke(_cinematic);
		}

		private CameraTrack EnsureCameraTrack()
		{
			CameraTrack track = CameraTrack;
			if (track == null)
			{
				track = new CameraTrack { Name = "Camera" };
				_cinematic.Tracks.Add(track);
			}
			return track;
		}

		private void RebuildKeyframes()
		{
			Keyframes.Clear();
			CameraTrack track = CameraTrack;
			if (track == null) return;
			track.Keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
			foreach (var kf in track.Keyframes) Keyframes.Add(new CameraKeyframeVM(kf, this));
			RecomputeKeyframePositions();
			OnPropertyChanged(nameof(EffectiveDuration));
		}

		public void RecomputeKeyframePositions()
		{
			CameraTrack camTrack = CameraTrack;
			if (camTrack != null)
				camTrack.Keyframes.Sort((a, b) => a.Time.CompareTo(b.Time));
			foreach (var lane in OtherTracks)
			{
				var kfs = lane.Track?.GetKeyframes();
				if (kfs != null && kfs.Count > 1)
				{
					var sorted = kfs.Cast<CinematicKeyframe>().OrderBy(x => x.Time).ToList();
					for (int i = 0; i < sorted.Count; i++) kfs[i] = sorted[i];
				}
			}

			float dur = EffectiveDuration;
			OnPropertyChanged(nameof(EffectiveDuration));
			foreach (var vm in Keyframes) vm.RecomputeX(dur);
			foreach (var lane in OtherTracks) lane.RecomputePositions(dur, UsableWidth, TimelinePadding);
			float ct = Math.Min(_currentTime, dur);
			PlayheadX = TimelinePadding + (ct / dur) * UsableWidth;
		}

		// -- Other-track management ----------------------------------------------------------

		private void RebuildTracks()
		{
			OtherTracks.Clear();
			foreach (var track in _cinematic.Tracks ?? Enumerable.Empty<CinematicTrack>())
			{
				if (track is CameraTrack) continue;
				OtherTracks.Add(new TrackLaneVM(track, this));
			}
			RecomputeKeyframePositions();
		}

		private static readonly HashSet<Type> UniqueTrackTypes = new HashSet<Type>
		{
			typeof(OverlayTrack), typeof(LookAtTrack)
		};

		public void AddTrack()
		{
			CinematicTrack track = CreateTrack(SelectedTrackType);
			if (track == null) return;
			if (UniqueTrackTypes.Contains(track.GetType()) && _cinematic.Tracks.Any(t => t != null && t.GetType() == track.GetType()))
				return;
			track.Name = SelectedTrackType;
			_cinematic.Tracks.Add(track);
			RebuildTracks();
		}

		public void DeleteTrack(TrackLaneVM lane)
		{
			if (lane?.Track == null) return;
			_cinematic.Tracks.Remove(lane.Track);
			RebuildTracks();
		}

		public void DeleteGenericKeyframe()
		{
			if (_selectedGeneric?.Model == null) return;
			CinematicKeyframe model = _selectedGeneric.Model;
			foreach (var lane in OtherTracks)
			{
				var kfs = lane.Track?.GetKeyframes();
				if (kfs != null && kfs.Contains(model))
				{
					kfs.Remove(model);
					lane.RebuildMarkers();
					break;
				}
			}
			SelectedGenericKeyframe = null;
			RecomputeKeyframePositions();
		}

		private static CinematicTrack CreateTrack(string name) => name switch
		{
			"Overlay" => new OverlayTrack(),
			"LookAt" => new LookAtTrack(),
			"Event" => new EventTrack(),
			"Entity" => new EntityTrack(),
			"Agent" => new AgentActionTrack(),
			"Audio" => new AudioTrack(),
			"Subtitle" => new SubtitleTrack(),
			_ => null
		};
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>One keyframe in the timeline lane + the inspector.</summary>
	public class CameraKeyframeVM : INotifyPropertyChanged
	{
		private const float Rad2Deg = 180f / (float)Math.PI;
		private const float Deg2Rad = (float)Math.PI / 180f;

		private float _x;
		private bool _isSelected;
		private float _time;
		public CameraKeyframe Keyframe { get; }
		private readonly CinematicTimelineVM _parent;

		public CameraKeyframeVM(CameraKeyframe keyframe, CinematicTimelineVM parent)
		{
			Keyframe = keyframe;
			_parent = parent;
			_time = keyframe.Time;
			SelectCommand = new RelayCommand(_ => _parent.SelectedKeyframe = this);
			CaptureCommand = new RelayCommand(_ => _parent.CaptureInto(this));
			DeleteCommand = new RelayCommand(_ => _parent.DeleteKeyframe(this));
			EditFrameTargetAgentCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditAgent(_parent, Keyframe.FrameTarget.AgentVariable,
					value => { Keyframe.FrameTarget.AgentVariable = value; OnPropertyChanged(nameof(FrameTargetAgentDisplay)); }));
			EditFrameTargetEntityCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditEntity(_parent, Keyframe.FrameTarget.Entity,
					value => { Keyframe.FrameTarget.Entity = value; OnPropertyChanged(nameof(FrameTargetEntityDisplay)); }));
		}

		private void RefreshTargetVisibilities()
		{
			OnPropertyChanged(nameof(IsFrameAbsolute));
			OnPropertyChanged(nameof(IsFrameRelative));
			OnPropertyChanged(nameof(FrameTargetIsAgent));
			OnPropertyChanged(nameof(FrameTargetIsEntity));
			OnPropertyChanged(nameof(FrameTargetAgentDisplay));
			OnPropertyChanged(nameof(FrameTargetEntityDisplay));
		}

		public float Time
		{
			get => _time;
			set
			{
				float v = Math.Max(0f, value);
				if (Math.Abs(_time - v) > 1e-5f)
				{
					_time = v;
					Keyframe.Time = _time;
					_parent?.RecomputeKeyframePositions();
					OnPropertyChanged();
				}
			}
		}

		public float Fov { get => Keyframe.Fov; set { Keyframe.Fov = value; OnPropertyChanged(); } }
		public float NearPlane { get => Keyframe.NearPlane; set { Keyframe.NearPlane = value; OnPropertyChanged(); } }
		public float FarPlane { get => Keyframe.FarPlane; set { Keyframe.FarPlane = value; OnPropertyChanged(); } }
		public float PositionX { get => Keyframe.Frame.Px; set { Keyframe.Frame.Px = value; OnPropertyChanged(); } }
		public float PositionY { get => Keyframe.Frame.Py; set { Keyframe.Frame.Py = value; OnPropertyChanged(); } }
		public float PositionZ { get => Keyframe.Frame.Pz; set { Keyframe.Frame.Pz = value; OnPropertyChanged(); } }
		public float RotationXDeg { get => Keyframe.Frame.Rx * Rad2Deg; set { Keyframe.Frame.Rx = value * Deg2Rad; OnPropertyChanged(); } }
		public float RotationYDeg { get => Keyframe.Frame.Ry * Rad2Deg; set { Keyframe.Frame.Ry = value * Deg2Rad; OnPropertyChanged(); } }
		public float RotationZDeg { get => Keyframe.Frame.Rz * Rad2Deg; set { Keyframe.Frame.Rz = value * Deg2Rad; OnPropertyChanged(); } }

		public Interpolation Interpolation { get => Keyframe.Interpolation; set { Keyframe.Interpolation = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCatmullRom)); } }
		public float Tension { get => Keyframe.Tension; set { Keyframe.Tension = value; OnPropertyChanged(); } }
		public bool IsCatmullRom => Keyframe.Interpolation == Interpolation.CatmullRom;
		public Array InterpolationOptions => Enum.GetValues(typeof(Interpolation));

		// --- Frame mode (absolute world frame vs target-relative frame) ---
		public CameraFrameMode FrameMode
		{
			get => Keyframe.FrameMode;
			set
			{
				Keyframe.FrameMode = value;
				// Coerce stale target types (None/Position are not offered in relative mode).
				if (value == CameraFrameMode.Relative
					&& (Keyframe.FrameTarget.Type == CinematicTargetType.None || Keyframe.FrameTarget.Type == CinematicTargetType.Position))
				{
					Keyframe.FrameTarget.Type = CinematicTargetType.ViewerCamera;
				}
				OnPropertyChanged();
				RefreshTargetVisibilities();
			}
		}
		public Array FrameModeOptions => Enum.GetValues(typeof(CameraFrameMode));
		public bool IsFrameAbsolute => Keyframe.FrameMode == CameraFrameMode.Absolute;
		public bool IsFrameRelative => Keyframe.FrameMode == CameraFrameMode.Relative;

		public TargetTrackMode TrackMode { get => Keyframe.TrackMode; set { Keyframe.TrackMode = value; OnPropertyChanged(); } }
		public Array TrackModeOptions => Enum.GetValues(typeof(TargetTrackMode));

		public CinematicTargetType FrameTargetType
		{
			get => Keyframe.FrameTarget.Type;
			set { Keyframe.FrameTarget.Type = value; OnPropertyChanged(); RefreshTargetVisibilities(); }
		}
		/// <summary>Relative frame targets: None and Position are meaningless here (a static position
		/// is what FrameMode = Absolute already does).</summary>
		public CinematicTargetType[] FrameTargetTypeOptions { get; } =
			{ CinematicTargetType.ViewerAgent, CinematicTargetType.ViewerCamera, CinematicTargetType.SpecificAgent, CinematicTargetType.SpecificEntity };
		public bool FrameTargetIsAgent => IsFrameRelative && Keyframe.FrameTarget.Type == CinematicTargetType.SpecificAgent;
		public bool FrameTargetIsEntity => IsFrameRelative && Keyframe.FrameTarget.Type == CinematicTargetType.SpecificEntity;
		public string FrameTargetAgentDisplay => CinematicTargetEditing.DescribeAgent(Keyframe.FrameTarget.AgentVariable);
		public string FrameTargetEntityDisplay => CinematicTargetEditing.DescribeEntity(Keyframe.FrameTarget.Entity);
		public ICommand EditFrameTargetAgentCommand { get; }
		public ICommand EditFrameTargetEntityCommand { get; }
		public float OffsetX { get => Keyframe.FrameOffset.Px; set { Keyframe.FrameOffset.Px = value; OnPropertyChanged(); } }
		public float OffsetY { get => Keyframe.FrameOffset.Py; set { Keyframe.FrameOffset.Py = value; OnPropertyChanged(); } }
		public float OffsetZ { get => Keyframe.FrameOffset.Pz; set { Keyframe.FrameOffset.Pz = value; OnPropertyChanged(); } }

		public Array TargetTypeOptions => Enum.GetValues(typeof(CinematicTargetType));

		public float X { get => _x; set { _x = value; OnPropertyChanged(); } }
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(MarkerFill));
				OnPropertyChanged(nameof(MarkerStroke));
			}
		}
		public SolidColorBrush MarkerFill => _isSelected ? CinematicTimelineVM.SelectedFill : CinematicTimelineVM.CameraFill;
		public SolidColorBrush MarkerStroke => CinematicTimelineVM.NormalStroke;

		public ICommand SelectCommand { get; }
		public ICommand CaptureCommand { get; }
		public ICommand DeleteCommand { get; }

		public void RecomputeX(float duration) => X = CinematicTimelineVM.TimelinePadding + (Time / duration) * _parent.UsableWidth;

		public void RefreshAll()
		{
			OnPropertyChanged(nameof(Time));
			OnPropertyChanged(nameof(Fov));
			OnPropertyChanged(nameof(NearPlane));
			OnPropertyChanged(nameof(FarPlane));
			OnPropertyChanged(nameof(PositionX));
			OnPropertyChanged(nameof(PositionY));
			OnPropertyChanged(nameof(PositionZ));
			OnPropertyChanged(nameof(RotationXDeg));
			OnPropertyChanged(nameof(RotationYDeg));
			OnPropertyChanged(nameof(RotationZDeg));
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>A lane for a non-camera track (Overlay, LookAt, etc.) showing keyframe markers.</summary>
	public class TrackLaneVM : INotifyPropertyChanged
	{
		public CinematicTrack Track { get; }
		internal readonly CinematicTimelineVM _parent;
		public ObservableCollection<KeyframeMarkerVM> Markers { get; } = new();

		public string DisplayName => string.IsNullOrWhiteSpace(Track?.Name)
			? Track?.GetType().Name.Replace("Track", "") ?? "Track"
			: Track.Name;

		/// <summary>Left-column header: agent lanes read "Agent" (the target button below carries the details).</summary>
		public string HeaderText => HasAgentTarget ? "Agent" : DisplayName;

		/// <summary>Agent lanes show their target as a wide clickable button on top of the lane.</summary>
		public bool HasAgentTarget => Track is AgentActionTrack;
		public string AgentTargetLabel => (Track as AgentActionTrack)?.Target?.EditorLabel ?? "set agent target";
		public ICommand EditAgentTargetCommand { get; }

		/// <summary>Fill color for this lane's markers (per track type).</summary>
		public SolidColorBrush NormalFill => CinematicTimelineVM.BrushForTrack(Track);

		public ICommand DeleteCommand { get; }
		public ICommand AddKeyCommand { get; }

		public TrackLaneVM(CinematicTrack track, CinematicTimelineVM parent)
		{
			Track = track;
			_parent = parent;
			DeleteCommand = new RelayCommand(_ => _parent.DeleteTrack(this));
			AddKeyCommand = new RelayCommand(_ => AddKey());
			EditAgentTargetCommand = new RelayCommand(_ => EditAgentTarget());
			RebuildMarkers();
		}

		/// <summary>Opens the object editor on the lane's CinematicAgent: mode toggle, the true-agent
		/// variable list, or the staged-extras formation definition (auto-rendered from ConfigProperty).</summary>
		private void EditAgentTarget()
		{
			if (Track is not AgentActionTrack agentTrack) return;
			EditorToolsManager.OpenEditor(agentTrack.Target,
				_ =>
				{
					OnPropertyChanged(nameof(AgentTargetLabel));
					// The selected keyframe's layout follows the target mode (actions vs clips, facial...).
					_parent?.SelectedGenericKeyframe?.RefreshTargetMode();
				},
				_parent?.Scenario);
		}

		public void AddKey()
		{
			float t = _parent.CurrentTime;
			if (Markers.Any(m => Math.Abs(m.Time - t) < 0.05f))
				t = Markers.Max(m => m.Time) + 1f;
			CinematicKeyframe kf = Track switch
			{
				OverlayTrack _ => new OverlayKeyframe(t) { Letterbox = 0.1f },
				AudioTrack _ => new AudioKeyframe(t) { Volume = 1f },
				SubtitleTrack _ => new SubtitleKeyframe(t) { Duration = 3f },
				EntityTrack _ => new EntityActionKeyframe(t),
				AgentActionTrack _ => new AgentActionKeyframe(t),
				EventTrack _ => new EventKeyframe(t),
				_ => null
			};
			if (kf == null) return;
			Track.GetKeyframes().Add(kf);
			RebuildMarkers();
			_parent.RecomputeKeyframePositions();
			Markers.FirstOrDefault(m => ReferenceEquals(m.Keyframe, kf))?.SelectCommand?.Execute(null);
		}

		public void RebuildMarkers()
		{
			Markers.Clear();
			if (Track == null) return;
			var kfs = Track.GetKeyframes();
			if (kfs == null) return;
			foreach (CinematicKeyframe kf in kfs)
				Markers.Add(new KeyframeMarkerVM(kf, this));
		}

		public void RecomputePositions(float dur, float usable, float pad)
		{
			foreach (var m in Markers) m.RecomputeX(dur, usable, pad);
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	/// <summary>A lightweight keyframe marker (time + X position) for non-camera tracks.</summary>
	public class KeyframeMarkerVM : INotifyPropertyChanged
	{
		private float _x;
		private bool _isSelected;
		private readonly TrackLaneVM _lane;
		public CinematicKeyframe Keyframe { get; }
		public float Time
		{
			get => Keyframe.Time;
			set { Keyframe.Time = Math.Max(0f, value); _lane?._parent?.RecomputeKeyframePositions(); OnPropertyChanged(); }
		}
		public float X { get => _x; set { _x = value; OnPropertyChanged(); } }
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				_isSelected = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(MarkerFill));
				OnPropertyChanged(nameof(MarkerStroke));
			}
		}
		public SolidColorBrush MarkerFill => _isSelected ? CinematicTimelineVM.SelectedFill : (_lane?.NormalFill ?? CinematicTimelineVM.DefaultFill);
		public SolidColorBrush MarkerStroke => _isSelected ? (_lane?.NormalFill ?? CinematicTimelineVM.DefaultFill) : CinematicTimelineVM.NormalStroke;
		public ICommand SelectCommand { get; }

		public KeyframeMarkerVM(CinematicKeyframe keyframe, TrackLaneVM lane)
		{
			Keyframe = keyframe;
			_lane = lane;
			SelectCommand = new RelayCommand(_ =>
			{
				if (_lane != null)
					foreach (var m in _lane.Markers)
						m.IsSelected = false;
				IsSelected = true;
				if (_lane?._parent != null)
					_lane._parent.SelectedGenericKeyframe = GenericKeyframeVM.Create(keyframe, _lane._parent);
			});
		}

		public void RecomputeX(float dur, float usable, float pad) => X = pad + (Time / dur) * usable;

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	// -- Generic keyframe wrapper VMs ---------------------------------------------------
	// The model keyframe classes store their data as fields (for the serializer + object editor).
	// WPF can only bind to properties, so these thin wrappers expose each field as an INPC property
	// for the inspector DataTemplates.

	/// <summary>Base wrapper for any non-camera keyframe. Exposes <see cref="Time"/> as a property and
	/// provides a <see cref="Model"/> reference for deletion.</summary>
	public class GenericKeyframeVM : INotifyPropertyChanged
	{
		public CinematicKeyframe Model { get; }
		public string KeyTypeName => Model?.GetType().Name.Replace("Keyframe", " Key");

		/// <summary>Hook for subclasses whose layout depends on context that can change while the
		/// keyframe stays selected (e.g. the agent track's target mode).</summary>
		public virtual void RefreshTargetMode() { }

		/// <summary>The model field's ConfigProperty tooltip, so hand-written timeline labels can show
		/// the same (i) info icon as the auto-generated object editor.</summary>
		protected string TooltipOf(string fieldName)
		{
			FieldInfo field = Model?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
			return field?.GetCustomAttribute<ConfigPropertyAttribute>()?.Tooltip;
		}

		public float Time
		{
			get => Model.Time;
			set { Model.Time = Math.Max(0f, value); OnTimeChanged?.Invoke(); OnPropertyChanged(); }
		}

		public Interpolation Interpolation { get => Model.Interpolation; set { Model.Interpolation = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCatmullRom)); } }
		public float Tension { get => Model.Tension; set { Model.Tension = value; OnPropertyChanged(); } }
		public bool IsCatmullRom => Model.Interpolation == Interpolation.CatmullRom;
		public Array InterpolationOptions => Enum.GetValues(typeof(Interpolation));

		public Action OnTimeChanged;

		protected GenericKeyframeVM(CinematicKeyframe model) { Model = model; }

		public static GenericKeyframeVM Create(CinematicKeyframe kf, CinematicTimelineVM timeline = null)
		{
			switch (kf)
			{
				case OverlayKeyframe ov: return new OverlayKeyframeVM(ov);
				case AudioKeyframe au: return new AudioKeyframeVM(au);
				case EntityActionKeyframe ea: return new EntityActionKeyframeVM(ea, timeline);
				case AgentActionKeyframe ak: return new AgentActionKeyframeVM(ak, timeline);
				case SubtitleKeyframe st: return new SubtitleKeyframeVM(st, timeline);
				case LookAtKeyframe la: return new LookAtKeyframeVM(la, timeline);
				case EventKeyframe evk: return new EventKeyframeVM(evk, timeline);
				default: return new GenericKeyframeVM(kf);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}

	public class OverlayKeyframeVM : GenericKeyframeVM
	{
		private OverlayKeyframe Kf => (OverlayKeyframe)Model;
		public float Letterbox { get => Kf.Letterbox; set { Kf.Letterbox = value; OnPropertyChanged(); } }
		public float FadeAlpha { get => Kf.FadeAlpha; set { Kf.FadeAlpha = value; OnPropertyChanged(); } }
		public OverlayKeyframeVM(OverlayKeyframe kf) : base(kf) { }
	}

	public class AudioKeyframeVM : GenericKeyframeVM
	{
		private AudioKeyframe Kf => (AudioKeyframe)Model;
		public string SoundEvent { get => Kf.SoundEvent; set { Kf.SoundEvent = value; OnPropertyChanged(); } }
		public float Volume { get => Kf.Volume; set { Kf.Volume = value; OnPropertyChanged(); } }
		public bool Loop { get => Kf.Loop; set { Kf.Loop = value; OnPropertyChanged(); } }
		public AudioKeyframeVM(AudioKeyframe kf) : base(kf) { }
	}

	public class EntityActionKeyframeVM : GenericKeyframeVM
	{
		private const float Rad2Deg = 180f / (float)Math.PI;
		private const float Deg2Rad = (float)Math.PI / 180f;

		private EntityActionKeyframe Kf => (EntityActionKeyframe)Model;
		private readonly CinematicTimelineVM _parent;

		public EntityActionKind Kind
		{
			get => Kf.Kind;
			set { Kf.Kind = value; OnPropertyChanged(); RefreshVisibilities(); }
		}
		public Array KindOptions => Enum.GetValues(typeof(EntityActionKind));
		public bool IsSetVisible => Kf.Kind == EntityActionKind.SetVisible;
		public bool IsDestination => Kf.Kind == EntityActionKind.Teleport || Kf.Kind == EntityActionKind.MoveTo;
		public bool IsMoveTo => Kf.Kind == EntityActionKind.MoveTo;
		public bool IsFx => Kf.Kind == EntityActionKind.Fx;

		public string EntityDisplay => CinematicTargetEditing.DescribeEntity(Kf.Entity);
		public ICommand EditEntityCommand { get; }
		public ICommand PickDestinationCommand { get; }

		/// <summary>Destination kind: a position with facing, or the frame of a marker/variable entity
		/// (use a marker's rotation to author gate openings).</summary>
		public CinematicTargetType DestinationType
		{
			get => Kf.Destination.Type;
			set { Kf.Destination.Type = value; OnPropertyChanged(); RefreshDestinationVisibilities(); }
		}
		public CinematicTargetType[] DestinationTypeOptions { get; } =
			{ CinematicTargetType.Position, CinematicTargetType.SpecificEntity };
		public bool IsDestinationPosition => Kf.Destination.Type == CinematicTargetType.Position;
		public bool IsDestinationEntity => Kf.Destination.Type == CinematicTargetType.SpecificEntity;
		public string DestinationEntityDisplay => CinematicTargetEditing.DescribeEntity(Kf.Destination.Entity);
		public ICommand EditDestinationEntityCommand { get; }

		public bool Visible { get => Kf.Visible; set { Kf.Visible = value; OnPropertyChanged(); } }
		public float DestX { get => Kf.Destination.Position.Px; set { Kf.Destination.Position.Px = value; OnPropertyChanged(); } }
		public float DestY { get => Kf.Destination.Position.Py; set { Kf.Destination.Position.Py = value; OnPropertyChanged(); } }
		public float DestZ { get => Kf.Destination.Position.Pz; set { Kf.Destination.Position.Pz = value; OnPropertyChanged(); } }
		public float DestYawDeg { get => Kf.Destination.Position.Rz * Rad2Deg; set { Kf.Destination.Position.Rz = value * Deg2Rad; OnPropertyChanged(); } }
		public float MoveDuration { get => Kf.MoveDuration; set { Kf.MoveDuration = Math.Max(0.1f, value); OnPropertyChanged(); } }
		public EntityMoveEasing Easing { get => Kf.Easing; set { Kf.Easing = value; OnPropertyChanged(); } }
		public Array EasingOptions => Enum.GetValues(typeof(EntityMoveEasing));
		public EntityFxMode Fx { get => Kf.Fx; set { Kf.Fx = value; OnPropertyChanged(); } }
		public Array FxOptions => Enum.GetValues(typeof(EntityFxMode));
		public bool FxChildren { get => Kf.FxChildren; set { Kf.FxChildren = value; OnPropertyChanged(); } }

		public EntityActionKeyframeVM(EntityActionKeyframe kf, CinematicTimelineVM parent = null) : base(kf)
		{
			_parent = parent;
			EditEntityCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditEntity(_parent, Kf.Entity,
					value => { Kf.Entity = value; OnPropertyChanged(nameof(EntityDisplay)); }));
			EditDestinationEntityCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditEntity(_parent, Kf.Destination.Entity,
					value =>
					{
						Kf.Destination.Entity = value;
						OnPropertyChanged(nameof(DestinationEntityDisplay));
					}));
			PickDestinationCommand = new RelayCommand(_ => EditFrameView.BeginPlace(
				owner: Model,
				onPlaced: frameValue =>
				{
					Kf.Destination.Position.CopyFrom(frameValue);
					OnPropertyChanged(nameof(DestX));
					OnPropertyChanged(nameof(DestY));
					OnPropertyChanged(nameof(DestZ));
					OnPropertyChanged(nameof(DestYawDeg));
				},
				baseFrame: Kf.Destination.Position.ToFrame()));
		}

		private void RefreshVisibilities()
		{
			OnPropertyChanged(nameof(IsSetVisible));
			OnPropertyChanged(nameof(IsDestination));
			OnPropertyChanged(nameof(IsMoveTo));
			OnPropertyChanged(nameof(IsFx));
			RefreshDestinationVisibilities();
		}

		private void RefreshDestinationVisibilities()
		{
			OnPropertyChanged(nameof(IsDestinationPosition));
			OnPropertyChanged(nameof(IsDestinationEntity));
			OnPropertyChanged(nameof(DestinationEntityDisplay));
		}
	}

	public class AgentActionKeyframeVM : GenericKeyframeVM
	{
		private const float Rad2Deg = 180f / (float)Math.PI;
		private const float Deg2Rad = (float)Math.PI / 180f;

		private AgentActionKeyframe Kf => (AgentActionKeyframe)Model;
		private readonly CinematicTimelineVM _parent;

		/// <summary>Target mode of the owning lane, driving which fields apply: true agents
		/// (AgentsFromScene) play actions and support facial animations - including the kit
		/// stand-in, rendered as a full AgentVisuals; fake agents (NewFakeAgents) play raw
		/// animation clips only. Unknown lane defaults to the true-agent layout.
		/// Re-evaluated through <see cref="RefreshTargetMode"/> when the mode is edited while
		/// the keyframe stays selected.</summary>
		private bool _stagedTarget;
		public bool StagedTarget => _stagedTarget;
		public bool TrueTarget => !_stagedTarget;

		/// <summary>The lane's agent target is shown/edited on the lane itself (in place of the name).</summary>
		public AgentActionKind Kind
		{
			get => Kf.Kind;
			set { Kf.Kind = value; OnPropertyChanged(); RefreshVisibilities(); }
		}
		public Array KindOptions => Enum.GetValues(typeof(AgentActionKind));
		public bool IsTeleport => Kf.Kind == AgentActionKind.Teleport;
		public bool IsMoveTo => Kf.Kind == AgentActionKind.MoveTo;
		public bool IsDestination => Kf.Kind == AgentActionKind.Teleport || Kf.Kind == AgentActionKind.MoveTo;
		public bool IsPlayAnimation => Kf.Kind == AgentActionKind.PlayAnimation;
		public bool IsPlayFacial => Kf.Kind == AgentActionKind.PlayFacial;
		public bool IsSetVisible => Kf.Kind == AgentActionKind.SetVisible;

		/// <summary>Destination kind: a position, the frame of a marker/variable entity, or an agent's
		/// position (same picker pattern as the camera frame target). Shared by Teleport and MoveTo.</summary>
		public CinematicTargetType DestinationType
		{
			get => Kf.Destination.Type;
			set { Kf.Destination.Type = value; OnPropertyChanged(); RefreshDestinationVisibilities(); }
		}
		public CinematicTargetType[] DestinationTypeOptions { get; } =
			{ CinematicTargetType.Position, CinematicTargetType.SpecificEntity, CinematicTargetType.SpecificAgent };
		public bool IsDestinationPosition => Kf.Destination.Type == CinematicTargetType.Position;
		public bool IsDestinationEntity => Kf.Destination.Type == CinematicTargetType.SpecificEntity;
		public string DestinationEntityDisplay => CinematicTargetEditing.DescribeEntity(Kf.Destination.Entity);
		public ICommand EditDestinationEntityCommand { get; }
		public ICommand PickDestinationCommand { get; }

		// Destination position (when DestinationType = Position) - shared by Teleport and MoveTo.
		public float DstX { get => Kf.Destination.Position.Px; set { Kf.Destination.Position.Px = value; OnPropertyChanged(); } }
		public float DstY { get => Kf.Destination.Position.Py; set { Kf.Destination.Position.Py = value; OnPropertyChanged(); } }
		public float DstZ { get => Kf.Destination.Position.Pz; set { Kf.Destination.Position.Pz = value; OnPropertyChanged(); } }
		public float DstYawDeg { get => Kf.Destination.Position.Rz * Rad2Deg; set { Kf.Destination.Position.Rz = value * Deg2Rad; OnPropertyChanged(); } }
		public bool KeepFormationOffset { get => Kf.KeepFormationOffset; set { Kf.KeepFormationOffset = value; OnPropertyChanged(); } }
		public AgentMoveSpeed Speed { get => Kf.Speed; set { Kf.Speed = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCustomSpeed)); } }
		public Array SpeedOptions => Enum.GetValues(typeof(AgentMoveSpeed));
		public bool IsCustomSpeed => Kf.Speed == AgentMoveSpeed.Custom;
		public float CustomSpeed { get => Kf.CustomSpeed; set { Kf.CustomSpeed = Math.Max(0.1f, Math.Min(15f, value)); OnPropertyChanged(); } }
		public float ArrivalFacingDeg { get => Kf.ArrivalFacingDeg; set { Kf.ArrivalFacingDeg = value; OnPropertyChanged(); } }
		public string MoveAnimation { get => Kf.MoveAnimation; set { Kf.MoveAnimation = value; OnPropertyChanged(); } }
		public string MountMoveAnimation { get => Kf.MountMoveAnimation; set { Kf.MountMoveAnimation = value; OnPropertyChanged(); } }
		public string[] MoveClipOptions => AllianceData.AvailableClips();
		public string[] MountMoveClipOptions => AllianceData.AvailableClips();

		public string ActionName { get => Kf.ActionName; set { Kf.ActionName = value; OnPropertyChanged(); } }
		public string ClipName { get => Kf.ClipName; set { Kf.ClipName = value; OnPropertyChanged(); } }
		public string MountActionName { get => Kf.MountActionName; set { Kf.MountActionName = value; OnPropertyChanged(); } }
		public string MountClipName { get => Kf.MountClipName; set { Kf.MountClipName = value; OnPropertyChanged(); } }
		public string[] ActionOptions => AllianceData.AvailableAnimations();
		public string[] ClipOptions => AllianceData.AvailableClips();
		public int Channel { get => Kf.Channel; set { Kf.Channel = Math.Max(0, Math.Min(3, value)); OnPropertyChanged(); } }
		public bool Loop { get => Kf.Loop; set { Kf.Loop = value; OnPropertyChanged(); } }
		public float ActionSpeed { get => Kf.ActionSpeed; set { Kf.ActionSpeed = Math.Max(0.1f, value); OnPropertyChanged(); } }

		public string FacialAnimName { get => Kf.FacialAnimName; set { Kf.FacialAnimName = value; OnPropertyChanged(); } }
		public bool FacialLoop { get => Kf.FacialLoop; set { Kf.FacialLoop = value; OnPropertyChanged(); } }
		public string[] FacialAnimOptions => AllianceData.AvailableFacialAnimations();

		public bool Visible { get => Kf.Visible; set { Kf.Visible = value; OnPropertyChanged(); } }
		public bool IncludeMount { get => Kf.IncludeMount; set { Kf.IncludeMount = value; OnPropertyChanged(); } }

		public AgentActionKeyframeVM(AgentActionKeyframe kf, CinematicTimelineVM parent = null) : base(kf)
		{
			_parent = parent;
			RefreshTargetMode();
			EditDestinationEntityCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditEntity(parent, Kf.Destination.Entity,
					value =>
					{
						Kf.Destination.Entity = value;
						OnPropertyChanged(nameof(DestinationEntityDisplay));
					}));
			PickDestinationCommand = new RelayCommand(_ => EditFrameView.BeginPlace(
				owner: Model,
				onPlaced: frameValue =>
				{
					Kf.Destination.Position.CopyFrom(frameValue);
					OnPropertyChanged(nameof(DstX));
					OnPropertyChanged(nameof(DstY));
					OnPropertyChanged(nameof(DstZ));
					OnPropertyChanged(nameof(DstYawDeg));
				},
				baseFrame: Kf.Destination.Position.ToFrame()));
		}

		/// <summary>The AgentActionTrack whose keyframes own this keyframe (null when detached).</summary>
		private AgentActionTrack FindOwningTrack()
		{
			return _parent?.OtherTracks?
				.Select(l => l.Track).OfType<AgentActionTrack>()
				.FirstOrDefault(t => t.Keyframes?.Contains(Kf) == true);
		}

		/// <summary>Re-evaluates the owning lane's target mode (called at selection and when the
		/// mode is edited while the keyframe stays selected).</summary>
		public override void RefreshTargetMode()
		{
			_stagedTarget = FindOwningTrack()?.Target?.IsStagedMode == true;
			OnPropertyChanged(nameof(StagedTarget));
			OnPropertyChanged(nameof(TrueTarget));
		}

		private void RefreshVisibilities()
		{
			OnPropertyChanged(nameof(IsTeleport));
			OnPropertyChanged(nameof(IsMoveTo));
			OnPropertyChanged(nameof(IsDestination));
			OnPropertyChanged(nameof(IsPlayAnimation));
			OnPropertyChanged(nameof(IsPlayFacial));
			OnPropertyChanged(nameof(IsSetVisible));
			RefreshDestinationVisibilities();
		}

		private void RefreshDestinationVisibilities()
		{
			OnPropertyChanged(nameof(IsDestinationPosition));
			OnPropertyChanged(nameof(IsDestinationEntity));
			OnPropertyChanged(nameof(DestinationEntityDisplay));
		}
	}

	public class SubtitleKeyframeVM : GenericKeyframeVM
	{
		private SubtitleKeyframe Kf => (SubtitleKeyframe)Model;
		private readonly CinematicTimelineVM _parent;
		public string TextTooltip => TooltipOf(nameof(SubtitleKeyframe.Text));
		public string Text { get => Kf.Text?.GetText() ?? ""; set { if (Kf.Text != null) { Kf.Text.SetText("English", value); OnPropertyChanged(); } } }
		public int ArgCount => Kf.TextArgs?.Count ?? 0;
		public ICommand EditTextArgsCommand { get; }
		public bool Scroll { get => Kf.Scroll; set { Kf.Scroll = value; OnPropertyChanged(); } }
		public float Duration { get => Kf.Duration; set { Kf.Duration = value; OnPropertyChanged(); } }
		public float FadeSec { get => Kf.FadeSec; set { Kf.FadeSec = value; OnPropertyChanged(); } }
		public int FontSize { get => Kf.FontSize; set { Kf.FontSize = value; OnPropertyChanged(); } }
		public float GlowRadius { get => Kf.GlowRadius; set { Kf.GlowRadius = Clamp01(value); OnPropertyChanged(); } }
		public float Blur { get => Kf.Blur; set { Kf.Blur = Clamp01(value); OnPropertyChanged(); } }
		public float ShadowOffset { get => Kf.ShadowOffset; set { Kf.ShadowOffset = Clamp01(value); OnPropertyChanged(); } }
		public float OutlineAmount { get => Kf.OutlineAmount; set { Kf.OutlineAmount = Clamp01(value); OnPropertyChanged(); } }
		public string FontColor { get => Kf.FontColor; set { Kf.FontColor = HexColor.Normalize(value); OnPropertyChanged(); } }
		public string Font { get => Kf.Font; set { Kf.Font = value; OnPropertyChanged(); } }
		public SubtitleHPosition HPosition { get => Kf.HPosition; set { Kf.HPosition = value; OnPropertyChanged(); } }
		public SubtitleVPosition VPosition { get => Kf.VPosition; set { Kf.VPosition = value; OnPropertyChanged(); } }
		public Array HPositionOptions => Enum.GetValues(typeof(SubtitleHPosition));
		public Array VPositionOptions => Enum.GetValues(typeof(SubtitleVPosition));
		public string[] FontOptions => AllianceData.AvailableFonts();

		public SubtitleKeyframeVM(SubtitleKeyframe kf, CinematicTimelineVM parent = null) : base(kf)
		{
			_parent = parent;
			EditTextArgsCommand = new RelayCommand(_ => EditorToolsManager.OpenEditor(
				new SubtitleTextArgsEditor { TextArgs = kf.TextArgs },
				_ => OnPropertyChanged(nameof(ArgCount)), _parent?.Scenario));
		}

		/// <summary>Editor-only wrapper so the text-arguments popup shows just that list, not the whole
		/// keyframe. Holds the keyframe's list instance - edits apply directly.</summary>
		private class SubtitleTextArgsEditor
		{
			[ConfigProperty(label: "Text arguments", tooltip: "Values filling the {0}, {1}... placeholders of the text, in order.")]
			public List<ValueSource<string>> TextArgs;
		}

		// Text effect values are normalized 0..1 (Gauntlet brush style range).
		private static float Clamp01(float v) => Math.Min(1f, Math.Max(0f, v));
	}

	public class LookAtKeyframeVM : GenericKeyframeVM
	{
		private readonly CinematicTimelineVM _parent;
		private LookAtKeyframe Kf => (LookAtKeyframe)Model;
		public CinematicTargetType TargetType
		{
			get => Kf.Target.Type;
			set { Kf.Target.Type = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsPosition)); OnPropertyChanged(nameof(IsAgent)); OnPropertyChanged(nameof(IsEntity)); }
		}
		public bool IsPosition => Kf.Target.Type == CinematicTargetType.Position;
		public bool IsAgent => Kf.Target.Type == CinematicTargetType.SpecificAgent;
		public bool IsEntity => Kf.Target.Type == CinematicTargetType.SpecificEntity;
		public string AgentDisplay => CinematicTargetEditing.DescribeAgent(Kf.Target.AgentVariable);
		public string EntityDisplay => CinematicTargetEditing.DescribeEntity(Kf.Target.Entity);
		public ICommand EditAgentCommand { get; }
		public ICommand EditEntityCommand { get; }
		public float PositionX { get => Kf.Target.Position.Px; set { Kf.Target.Position.Px = value; OnPropertyChanged(); } }
		public float PositionY { get => Kf.Target.Position.Py; set { Kf.Target.Position.Py = value; OnPropertyChanged(); } }
		public float PositionZ { get => Kf.Target.Position.Pz; set { Kf.Target.Position.Pz = value; OnPropertyChanged(); } }
		public Array TargetTypeOptions => Enum.GetValues(typeof(CinematicTargetType));

		public LookAtKeyframeVM(LookAtKeyframe kf, CinematicTimelineVM parent = null) : base(kf)
		{
			_parent = parent;
			EditAgentCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditAgent(_parent, Kf.Target.AgentVariable,
					value => { Kf.Target.AgentVariable = value; OnPropertyChanged(nameof(AgentDisplay)); }));
			EditEntityCommand = new RelayCommand(_ =>
				CinematicTargetEditing.EditEntity(_parent, Kf.Target.Entity,
					value => { Kf.Target.Entity = value; OnPropertyChanged(nameof(EntityDisplay)); }));
		}
	}

	public class EventKeyframeVM : GenericKeyframeVM
	{
		private EventKeyframe Kf => (EventKeyframe)Model;
		private readonly CinematicTimelineVM _parent;
		public int ActionCount => Kf.Actions?.Count ?? 0;
		public ICommand EditActionsCommand { get; }
		public EventKeyframeVM(EventKeyframe kf, CinematicTimelineVM parent = null) : base(kf)
		{
			_parent = parent;
			EditActionsCommand = new RelayCommand(_ => EditorToolsManager.OpenEditor(Kf, _ => OnPropertyChanged(nameof(ActionCount)), _parent?.Scenario));
		}
	}

	internal static class CinematicTargetEditing
	{
		/// <summary>Short label describing an agent target slot for the inspector hyperlinks.</summary>
		public static string DescribeAgent(ValueSource<Agent> slot)
		{
			return slot switch
			{
				null => "set target agent",
				VariableValue<Agent> v when !string.IsNullOrEmpty(v.VariableName) => "Variable: " + v.VariableName,
				VariableValue<Agent> => "set target agent",
				_ => slot.GetType().Name,
			};
		}

		/// <summary>Short label describing an entity target slot for the inspector hyperlinks.</summary>
		public static string DescribeEntity(ValueSource<WeakGameEntity> slot)
		{
			return slot switch
			{
				null => "set target entity",
				SceneEntityLiteralValue lit when !string.IsNullOrEmpty(lit.Ref?.RefId)
					=> string.IsNullOrEmpty(lit.Ref.DisplayName) ? lit.Ref.RefId : lit.Ref.DisplayName,
				SceneEntityLiteralValue => "set target entity",
				VariableValue<WeakGameEntity> v when !string.IsNullOrEmpty(v.VariableName) => "Variable: " + v.VariableName,
				_ => slot.GetType().Name,
			};
		}

		/// <summary>Opens the value source editor popup on an agent target slot. The wrapper gives the
		/// popup a single-field object to edit; its value is written back through <paramref name="apply"/>.</summary>
		public static void EditAgent(CinematicTimelineVM timeline, ValueSource<Agent> current, Action<ValueSource<Agent>> apply)
		{
			var wrapper = new TargetAgentEditor { Agent = current ?? new VariableValue<Agent>() };
			TimelineValueSourceEditor.Open(timeline, wrapper, nameof(TargetAgentEditor.Agent),
				value => apply((ValueSource<Agent>)value));
		}

		/// <summary>Entity-flavoured counterpart of <see cref="EditAgent"/>.</summary>
		public static void EditEntity(CinematicTimelineVM timeline, ValueSource<WeakGameEntity> current, Action<ValueSource<WeakGameEntity>> apply)
		{
			var wrapper = new TargetEntityEditor { Entity = current ?? new SceneEntityLiteralValue() };
			TimelineValueSourceEditor.Open(timeline, wrapper, nameof(TargetEntityEditor.Entity),
				value => apply((ValueSource<WeakGameEntity>)value));
		}
	}

	/// <summary>Shows the value source editor popup for one field of a small wrapper object, with the
	/// timeline's scenario as variable context (the wrapper has no scenario-editor parent chain).</summary>
	internal static class TimelineValueSourceEditor
	{
		public static void Open(CinematicTimelineVM timeline, object wrapper, string fieldName, Action<object> onClosed)
		{
			var editorVm = new ObjectEditorViewModel(wrapper, null, null, fieldName, WeakGameEntity.Invalid, timeline?.Scenario);
			FieldViewModel field = editorVm.Fields.FirstOrDefault(f => f.FieldInfo?.Name == fieldName)
				?? editorVm.FieldCategories?.SelectMany(c => c.Fields).FirstOrDefault(f => f.FieldInfo?.Name == fieldName);
			if (field == null) return;

			var popup = new Alliance.Editor.GameModes.Story.Views.ValueSourceEditorPopup(field)
			{
				Owner = System.Windows.Application.Current?.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.IsActive)
			};
			popup.Closed += (_, _) => onClosed(field.FieldInfo?.GetValue(wrapper));
			popup.Show();
		}
	}

	/// <summary>Single-field editor objects for the target slot popups (see CinematicTargetEditing).</summary>
	internal class TargetAgentEditor
	{
		[ConfigProperty(label: "Agent target", tooltip: "Value source resolving to the Agent the camera frame is relative to.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();
	}

	internal class TargetEntityEditor
	{
		[ConfigProperty(label: "Entity target", tooltip: "Value source resolving to the Entity the camera frame is relative to: a scene entity (literal) or a variable holding one.")]
		public ValueSource<WeakGameEntity> Entity = new SceneEntityLiteralValue();
	}
}
