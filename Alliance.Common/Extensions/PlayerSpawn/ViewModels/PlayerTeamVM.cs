using Alliance.Common.Extensions.PlayerSpawn.Models;
using JetBrains.Annotations;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// View model for a team in the Player Spawn menu.
	/// </summary>
	public class PlayerTeamVM : ViewModel
	{
		private bool _editMode;
		private PlayerTeam _team;
		private readonly Action<PlayerTeamVM> _onTeamSelected;
		private readonly Action<PlayerTeamVM> _onTeamEdited;
		private readonly Action<PlayerTeamVM> _onTeamDeleted;
		private bool _isSelected;

		public PlayerTeam Team
		{
			get => _team;
			set
			{
				if (value != _team)
				{
					_team = value;
					Name = value.Name;
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
		public string Name
		{
			get => Team.Name;
			set
			{
				if (value != Team.Name)
				{
					Team.Name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		public PlayerTeamVM(PlayerTeam playerTeam, Action<PlayerTeamVM> onTeamSelected, Action<PlayerTeamVM> onTeamEdited = null, Action<PlayerTeamVM> onTeamDeleted = null, bool editMode = false)
		{
			Team = playerTeam;
			_onTeamSelected = onTeamSelected;
			_onTeamEdited = onTeamEdited;
			_onTeamDeleted = onTeamDeleted;
			_editMode = editMode;
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
			OnPropertyChanged(nameof(Name));
		}

		[UsedImplicitly]
		public void SelectTeam()
		{
			_onTeamSelected?.Invoke(this);
		}

		[UsedImplicitly]
		public void EditTeam()
		{
			_onTeamEdited?.Invoke(this);
		}

		[UsedImplicitly]
		public void DeleteTeam()
		{
			_onTeamDeleted?.Invoke(this);
		}
	}
}