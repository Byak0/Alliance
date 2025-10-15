using Alliance.Common.Extensions.PlayerSpawn.Models;
using JetBrains.Annotations;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// View model for a formation in the Player Spawn menu.
	/// </summary>
	public class PlayerFormationVM : ViewModel
	{
		private bool _editMode;
		private readonly PlayerTeamVM _teamVM;
		private readonly PlayerFormation _formation;
		private readonly Action<PlayerFormationVM> _onFormationSelected;
		private readonly Action<PlayerFormationVM> _onFormationEdited;
		private readonly Action<PlayerFormationVM> _onFormationDeleted;
		private bool _isSelected;
		private string _mainLanguages;

		public PlayerTeamVM TeamVM => _teamVM;
		public PlayerFormation Formation => _formation;

		[DataSourceProperty]
		public bool EditMode
		{
			get => _editMode;
			set
			{
				if (value != _editMode)
				{
					_editMode = value;
					OnPropertyChangedWithValue(value, nameof(EditMode));
				}
			}
		}

		[DataSourceProperty]
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					OnPropertyChangedWithValue(value, nameof(IsSelected));
				}
			}
		}

		[DataSourceProperty]
		public string MainLanguages
		{
			get => _mainLanguages;
			set
			{
				if (value != _mainLanguages)
				{
					_mainLanguages = value;
					OnPropertyChangedWithValue(value, nameof(MainLanguages));
				}
			}
		}

		[DataSourceProperty]
		public string MainCultureId
		{
			get => Formation.MainCultureId;
			set
			{
				if (value != Formation.MainCultureId)
				{
					Formation.MainCultureId = value;
					OnPropertyChangedWithValue(value, nameof(MainCultureId));
				}
			}
		}

		[DataSourceProperty]
		public string Name
		{
			get => Formation.Name;
			set
			{
				if (value != Formation.Name)
				{
					Formation.Name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		[DataSourceProperty]
		public string Occupation
		{
			get => Formation.GetOccupiedSlots() + "/" + Formation.GetTotalSlots();
		}

		[DataSourceProperty]
		public bool IsFull
		{
			get => Formation.GetAvailableSlots() <= 0;
		}

		public PlayerFormationVM(PlayerTeamVM teamVM, PlayerFormation playerFormation, Action<PlayerFormationVM> onFormationSelected, Action<PlayerFormationVM> onFormationEdited = null, Action<PlayerFormationVM> onFormationDeleted = null, bool editMode = false)
		{
			_teamVM = teamVM;
			_formation = playerFormation;
			_onFormationSelected = onFormationSelected;
			_onFormationEdited = onFormationEdited;
			_onFormationDeleted = onFormationDeleted;
			_editMode = editMode;
			MainLanguages = playerFormation.MainLanguage;
			RefreshValues();
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(MainCultureId));
			OnPropertyChanged(nameof(Occupation));
			OnPropertyChanged(nameof(IsFull));
		}

		[UsedImplicitly]
		public void SelectFormation()
		{
			_onFormationSelected?.Invoke(this);
		}

		[UsedImplicitly]
		public void EditFormation()
		{
			_onFormationEdited?.Invoke(this);
		}

		[UsedImplicitly]
		public void DeleteFormation()
		{
			_onFormationDeleted?.Invoke(this);
		}
	}
}