using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Story.Utilities;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// View model for a formation in the Player Spawn menu.
	/// </summary>
	public class PlayerFormationVM : ViewModel
	{
		private bool _editMode;
		private PlayerTeam _team;
		private PlayerFormation _formation;
		private readonly Action<PlayerFormationVM> _onFormationSelected;
		private readonly Action<PlayerFormationVM> _onFormationEdited;
		private readonly Action<PlayerFormationVM> _onFormationDeleted;
		private bool _isSelected;
		private string _mainLanguages;

		public PlayerTeam Team
		{
			get => _team;
			set
			{
				if (value != _team)
				{
					_team = value;
				}
			}
		}

		public PlayerFormation Formation
		{
			get => _formation;
			set
			{
				if (value != _formation)
				{
					_formation = value;
					RefreshValues();
				}
			}
		}

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

		public PlayerFormationVM(PlayerTeam team, PlayerFormation playerFormation, Action<PlayerFormationVM> onFormationSelected, Action<PlayerFormationVM> onFormationEdited = null, Action<PlayerFormationVM> onFormationDeleted = null, bool editMode = false)
		{
			Team = team;
			Formation = playerFormation;
			_onFormationSelected = onFormationSelected;
			_onFormationEdited = onFormationEdited;
			_onFormationDeleted = onFormationDeleted;
			_editMode = editMode;
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(MainCultureId));
			OnPropertyChanged(nameof(Occupation));
			OnPropertyChanged(nameof(IsFull));
			RefreshMainLanguages();
		}

		private void RefreshMainLanguages()
		{
			Dictionary<string, int> languagesRepartition = new Dictionary<string, int>();
			foreach (NetworkCommunicator networkCommunicator in Formation.Members)
			{
			}
			int nbLg = LocalizationHelper.GetAvailableLanguages().Count;
			Random rnd = new Random();
			MainLanguages = LocalizationHelper.GetAvailableLanguages()[Formation.Index];
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