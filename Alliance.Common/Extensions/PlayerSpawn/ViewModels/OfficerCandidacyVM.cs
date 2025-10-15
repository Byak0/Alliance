using Alliance.Common.Extensions.PlayerSpawn.Models;
using JetBrains.Annotations;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// View model for an officer candidacy in the Player Spawn menu.
	/// </summary>
	public class OfficerCandidacyVM : ViewModel
	{
		private PlayerFormation _formation;
		private readonly Action<OfficerCandidacyVM> _onCandidacySelected;
		private bool _isSelected;
		private CandidateInfo _candidate;

		public PlayerFormation Formation => _formation;
		public CandidateInfo Candidate => _candidate;

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
			get => _candidate?.Candidate?.UserName ?? string.Empty;
		}

		[DataSourceProperty]
		public string Pitch
		{
			get => _candidate?.Pitch ?? string.Empty;
		}

		public OfficerCandidacyVM(PlayerFormation formation, Action<OfficerCandidacyVM> onCandidacySelected, CandidateInfo candidate, bool isSelected = false)
		{
			_formation = formation;
			_onCandidacySelected = onCandidacySelected;
			IsSelected = isSelected;
			_candidate = candidate;
		}

		[UsedImplicitly]
		public void ToggleVote()
		{
			_onCandidacySelected?.Invoke(this);
		}
	}
}