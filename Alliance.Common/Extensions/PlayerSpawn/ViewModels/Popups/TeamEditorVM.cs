using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using JetBrains.Annotations;
using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels.Popups
{
	/// <summary>
	/// View model for editing a team in the Player Spawn menu.
	/// </summary>
	public class TeamEditorVM : ViewModel
	{
		public event EventHandler OnCloseMenu;

		private PlayerTeam _teamCopy;
		private PlayerTeam _team;
		public PlayerTeam Team => _team;

		private MBBindingList<OptionVM> _options;

		[DataSourceProperty]
		public MBBindingList<OptionVM> Options
		{
			get
			{
				return _options;
			}
			set
			{
				if (value != _options)
				{
					_options = value;
					OnPropertyChangedWithValue(value, nameof(Options));
				}
			}
		}

		[DataSourceProperty]
		public string Name
		{
			get => _team.Name;
			set
			{
				if (_team.Name != value)
				{
					_team.Name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		public TeamEditorVM(PlayerTeam team)
		{
			_team = team;
			_teamCopy = new PlayerTeam
			{
				TeamSide = team.TeamSide,
				Name = team.Name
			};
			InitializeOptions();
		}

		private void InitializeOptions()
		{
			_options = new MBBindingList<OptionVM>();

			_options.Add(new SelectionOptionVM(
								new TextObject(nameof(Team.TeamSide)),
								new TextObject(nameof(Team.TeamSide)),
								new SelectionOptionData(
									() => (int)Team.TeamSide,
									newValue => Team.TeamSide = (TaleWorlds.Core.BattleSideEnum)newValue,
									AllianceData.AvailableSides.Length,
									AllianceData.AvailableSides),
								false));

			// Add more options as needed
		}

		[UsedImplicitly]
		public void Cancel()
		{
			// Reset the team to its original state
			_team.Name = _teamCopy.Name;
			_team.TeamSide = _teamCopy.TeamSide;
			OnCloseMenu(this, EventArgs.Empty);
		}

		[UsedImplicitly]
		public void Save()
		{
			OnCloseMenu(this, EventArgs.Empty);
		}
	}
}