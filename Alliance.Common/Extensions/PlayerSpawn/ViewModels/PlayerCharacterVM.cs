#if !SERVER
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview;
using JetBrains.Annotations;
using System;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels
{
	/// <summary>
	/// View model for a character in the Player Spawn menu.
	/// </summary>
	public class PlayerCharacterVM : ViewModel
	{
		private bool _editMode;
		private PlayerTeam _team;
		private PlayerFormation _formation;
		private AvailableCharacter _availableCharacter;
		private AL_CharacterViewModel _characterViewModel;
		private readonly Action<PlayerCharacterVM> _onCharacterSelected;
		private readonly Action<PlayerCharacterVM> _onCharacterEdited;
		private readonly Action<PlayerCharacterVM> _onCharacterDeleted;
		private bool _isSelected;
		private int _siblingOrder = -1;
		private int _width;
		private int _marginLeft;

		public PlayerTeam Team => _team;
		public PlayerFormation Formation => _formation;
		public AvailableCharacter AvailableCharacter => _availableCharacter;

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
		public int SiblingOrder
		{
			get => _siblingOrder;
			set
			{
				_siblingOrder = value;
				OnPropertyChangedWithValue(value, nameof(SiblingOrder));
			}
		}

		[DataSourceProperty]
		public int Width
		{
			get => _width;
			set
			{
				if (value != _width)
				{
					_width = value;
					OnPropertyChangedWithValue(value, nameof(Width));
				}
			}
		}

		[DataSourceProperty]
		public int MarginLeft
		{
			get => _marginLeft;
			set
			{
				if (value != _marginLeft)
				{
					_marginLeft = value;
					OnPropertyChangedWithValue(value, nameof(MarginLeft));
				}
			}
		}

		[DataSourceProperty]
		public AL_CharacterViewModel CharacterViewModel
		{
			get => _characterViewModel;
			set
			{
				if (value != _characterViewModel)
				{
					_characterViewModel = value;
					OnPropertyChangedWithValue(value, nameof(CharacterViewModel));
				}
			}
		}

		[DataSourceProperty]
		public string CharacterId
		{
			get => AvailableCharacter.CharacterId;
		}

		[DataSourceProperty]
		public string Name
		{
			get => AvailableCharacter.Name;
		}

		[DataSourceProperty]
		public bool Officer
		{
			get => AvailableCharacter.Officer;
		}

		[DataSourceProperty]
		public string Occupation
		{
			get => AvailableCharacter.UsedSlots + "/" + AvailableCharacter.MaxSlots;
		}

		[DataSourceProperty]
		public bool IsFull
		{
			get => AvailableCharacter.AvailableSlots <= 0;
		}

		public PlayerCharacterVM(PlayerTeam team, PlayerFormation playerFormation, AvailableCharacter availableCharacter, Action<PlayerCharacterVM> onCharacterSelected, Action<PlayerCharacterVM> onCharacterEdited = null, Action<PlayerCharacterVM> onCharacterDeleted = null, bool editMode = false)
		{
			_team = team;
			_formation = playerFormation;
			_availableCharacter = availableCharacter;
			if (availableCharacter.Character != null)
			{
				CharacterViewModel = new AL_CharacterViewModel();
				CharacterViewModel.FillFrom(availableCharacter);
			}
			_onCharacterSelected = onCharacterSelected;
			_onCharacterEdited = onCharacterEdited;
			_onCharacterDeleted = onCharacterDeleted;
			_editMode = editMode;
		}

		public override void RefreshValues()
		{
			base.RefreshValues();
			OnPropertyChanged(nameof(Name));
			OnPropertyChanged(nameof(Officer));
			OnPropertyChanged(nameof(Occupation));
			OnPropertyChanged(nameof(IsFull));
			OnPropertyChanged(nameof(CharacterViewModel));
			OnPropertyChanged(nameof(IsSelected));
		}

		[UsedImplicitly]
		public void SelectCharacter()
		{
			_onCharacterSelected?.Invoke(this);
		}

		[UsedImplicitly]
		public void EditCharacter()
		{
			_onCharacterEdited?.Invoke(this);
		}

		[UsedImplicitly]
		public void DeleteCharacter()
		{
			_onCharacterDeleted?.Invoke(this);
		}
	}
}
#endif