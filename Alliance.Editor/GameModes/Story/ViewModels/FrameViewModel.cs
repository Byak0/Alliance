using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Editor.GameModes.Story.Views;
using System;
using System.ComponentModel;
using System.Windows.Input;
using TaleWorlds.Engine;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM wrapping a <see cref="FrameValue"/> for in-editor editing, exposing the nine position/rotation/
	/// scale components as inline floats (three rows of three, like the Zone position editor). The ghost
	/// preview is owned by the enclosing slot and stays visible while the action is displayed; this VM only
	/// provides the numeric editors and the "Place on map" command (which reuses that ghost).
	/// </summary>
	public class FrameViewModel : INotifyPropertyChanged
	{
		private readonly FrameValue _frame;
		private readonly FieldViewModel _fieldViewModel;

		public ICommand PlaceCommand { get; }
		public ICommand CopyFromOriginCommand { get; }

		/// <summary>True when this frame belongs to a MoveEntityAction whose Entity resolves to a live
		/// scene entity, so its current frame can be copied into the destination.</summary>
		public bool CanCopyFromOrigin { get; private set; }

		private const float Rad2Deg = 180f / (float)Math.PI;
		private const float Deg2Rad = (float)Math.PI / 180f;

		public FrameViewModel(FrameValue frame, FieldViewModel fieldViewModel)
		{
			_fieldViewModel = fieldViewModel;
			_frame = frame;
			if (_frame == null)
			{
				_frame = new FrameValue();
				_fieldViewModel?.FieldInfo?.SetValue(_fieldViewModel.parentViewModel?.Object, _frame);
			}

			PlaceCommand = new RelayCommand(_ => Place());
			CopyFromOriginCommand = new RelayCommand(_ => CopyFromOrigin(), _ => CanCopyFromOrigin);
			EvaluateCanCopyFromOrigin();
		}

		public float PositionX { get => _frame.Px; set => Set(ref _frame.Px, value, nameof(PositionX)); }
		public float PositionY { get => _frame.Py; set => Set(ref _frame.Py, value, nameof(PositionY)); }
		public float PositionZ { get => _frame.Pz; set => Set(ref _frame.Pz, value, nameof(PositionZ)); }

		// Rotation is stored in radians (native ApplyEulerAngles convention) but edited in degrees.
		public float RotationX { get => _frame.Rx * Rad2Deg; set => Set(ref _frame.Rx, value * Deg2Rad, nameof(RotationX)); }
		public float RotationY { get => _frame.Ry * Rad2Deg; set => Set(ref _frame.Ry, value * Deg2Rad, nameof(RotationY)); }
		public float RotationZ { get => _frame.Rz * Rad2Deg; set => Set(ref _frame.Rz, value * Deg2Rad, nameof(RotationZ)); }

		public float ScaleX { get => _frame.Sx; set => Set(ref _frame.Sx, value, nameof(ScaleX)); }
		public float ScaleY { get => _frame.Sy; set => Set(ref _frame.Sy, value, nameof(ScaleY)); }
		public float ScaleZ { get => _frame.Sz; set => Set(ref _frame.Sz, value, nameof(ScaleZ)); }

		public void Place()
		{
			// baseFrame carries the current pitch/roll/scale so placement only overrides position + yaw.
			EditFrameView.BeginPlace(OwnerAction, captured =>
			{
				_frame.CopyFrom(captured);
				RefreshAll();
			}, _frame.ToFrame());
		}

		/// <summary>Copies the MoveEntityAction's resolved entity frame into this destination, so the
		/// ghost starts on the entity and the user nudges from there.</summary>
		public void CopyFromOrigin()
		{
			if (OwnerAction is MoveEntityAction mea)
			{
				WeakGameEntity entity = mea.Entity?.Resolve(null) ?? WeakGameEntity.Invalid;
				if (entity.IsValid)
				{
					_frame.CopyFrom(FrameValue.FromFrame(entity.GetGlobalFrame()));
					RefreshAll();
				}
			}
		}

		private void EvaluateCanCopyFromOrigin()
		{
			if (OwnerAction is MoveEntityAction mea)
			{
				WeakGameEntity entity = mea.Entity?.Resolve(null) ?? WeakGameEntity.Invalid;
				CanCopyFromOrigin = entity.IsValid;
				return;
			}
			CanCopyFromOrigin = false;
		}

		private object OwnerAction => _fieldViewModel?.parentViewModel?.ParentEditor?.Object;

		private void RefreshAll()
		{
			OnPropertyChanged(nameof(PositionX));
			OnPropertyChanged(nameof(PositionY));
			OnPropertyChanged(nameof(PositionZ));
			OnPropertyChanged(nameof(RotationX));
			OnPropertyChanged(nameof(RotationY));
			OnPropertyChanged(nameof(RotationZ));
			OnPropertyChanged(nameof(ScaleX));
			OnPropertyChanged(nameof(ScaleY));
			OnPropertyChanged(nameof(ScaleZ));
		}

		private void Set(ref float field, float value, string propertyName)
		{
			if (field != value)
			{
				field = value;
				OnPropertyChanged(propertyName);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		internal void Close()
		{
		}
	}
}
