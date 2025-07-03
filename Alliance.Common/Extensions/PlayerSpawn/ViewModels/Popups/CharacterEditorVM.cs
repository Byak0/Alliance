using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.UI.VM.Options;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static Alliance.Common.Core.Utils.Characters;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Common.Extensions.PlayerSpawn.ViewModels.Popups
{
	/// <summary>
	/// View model for editing a character in the Player Spawn menu.
	/// </summary>
	public class CharacterEditorVM : ViewModel
	{
		public event EventHandler OnCloseMenu;

		private readonly AvailableCharacter _availableCharacterCopy;
		private AvailableCharacter _availableCharacter;
		private MBBindingList<TroopGroupVM> _troopGroups;
		private TroopVM _selectedTroop;
		private bool _showTroops = true;
		private bool _showHeroes;
		private bool _showBannerBearers;
		private string _cultureName;
		private string _cultureId;
		private BasicCultureObject _selectedCulture;
		private MBBindingList<OptionVM> _options;

		public AvailableCharacter AvailableCharacter => _availableCharacter;

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
		public bool ShowTroops
		{
			get => _showTroops;
			set
			{
				if (_showTroops != value)
				{
					_showTroops = value;
					OnPropertyChangedWithValue(value, nameof(ShowTroops));
				}
			}
		}

		[DataSourceProperty]
		public bool ShowHeroes
		{
			get => _showHeroes;
			set
			{
				if (_showHeroes != value)
				{
					_showHeroes = value;
					OnPropertyChangedWithValue(value, nameof(ShowHeroes));
				}
			}
		}

		[DataSourceProperty]
		public bool ShowBannerBearers
		{
			get => _showBannerBearers;
			set
			{
				if (_showBannerBearers != value)
				{
					_showBannerBearers = value;
					OnPropertyChangedWithValue(value, nameof(ShowBannerBearers));
				}
			}
		}

		[DataSourceProperty]
		public string CultureName
		{
			get => _cultureName;
			set
			{
				if (_cultureName != value)
				{
					_cultureName = value;
					OnPropertyChangedWithValue(value, nameof(CultureName));
				}
			}
		}

		[DataSourceProperty]
		public string CultureId
		{
			get => _cultureId;
			set
			{
				if (_cultureId != value)
				{
					_cultureId = value;
					OnPropertyChangedWithValue(value, nameof(CultureId));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<TroopGroupVM> TroopGroups
		{
			get => _troopGroups;
			set
			{
				if (_troopGroups != value)
				{
					_troopGroups = value;
					OnPropertyChangedWithValue(value, nameof(TroopGroups));
				}
			}
		}

		[DataSourceProperty]
		public string CharacterId
		{
			get => _availableCharacter.CharacterId;
			set
			{
				if (value != _availableCharacter.CharacterId)
				{
					_availableCharacter.CharacterId = value;
					OnPropertyChangedWithValue(value, nameof(CharacterId));
				}
			}
		}

		[DataSourceProperty]
		public bool Officer
		{
			get => _availableCharacter.Officer;
			set
			{
				if (value != _availableCharacter.Officer)
				{
					_availableCharacter.Officer = value;
					OnPropertyChangedWithValue(value, nameof(Officer));
				}
			}
		}

		[DataSourceProperty]
		public int SpawnCount
		{
			get => _availableCharacter.SpawnCount;
			set
			{
				if (value != _availableCharacter.SpawnCount)
				{
					_availableCharacter.SpawnCount = value;
					OnPropertyChangedWithValue(value, nameof(SpawnCount));
					OnPropertyChangedWithValue(value, nameof(MaxSlots));
					OnPropertyChangedWithValue(value, nameof(AvailableSlots));
				}
			}
		}

		[DataSourceProperty]
		public bool IsPercentage
		{
			get => _availableCharacter.IsPercentage;
			set
			{
				if (value != _availableCharacter.IsPercentage)
				{
					_availableCharacter.IsPercentage = value;
					OnPropertyChangedWithValue(value, nameof(IsPercentage));
					OnPropertyChangedWithValue(value, nameof(MaxSlots));
					OnPropertyChangedWithValue(value, nameof(AvailableSlots));
				}
			}
		}

		[DataSourceProperty]
		public float HealthMultiplier
		{
			get => _availableCharacter.HealthMultiplier;
			set
			{
				if (value != _availableCharacter.HealthMultiplier)
				{
					_availableCharacter.HealthMultiplier = value;
					OnPropertyChangedWithValue(value, nameof(HealthMultiplier));
				}
			}
		}

		[DataSourceProperty]
		public int MaxSlots => _availableCharacter.MaxSlots;

		[DataSourceProperty]
		public int UsedSlots => _availableCharacter.UsedSlots;

		[DataSourceProperty]
		public int AvailableSlots => _availableCharacter.AvailableSlots;

		public CharacterEditorVM(AvailableCharacter availableCharacter, BasicCultureObject defaultCulture = null)
		{
			_availableCharacter = availableCharacter;
			_availableCharacterCopy = new AvailableCharacter
			{
				CharacterId = availableCharacter.CharacterId,
				Officer = availableCharacter.Officer,
				SpawnCount = availableCharacter.SpawnCount,
				IsPercentage = availableCharacter.IsPercentage,
				Difficulty = availableCharacter.Difficulty,
				HealthMultiplier = availableCharacter.HealthMultiplier
			};
			TroopGroups = new MBBindingList<TroopGroupVM>();
			if (_availableCharacter.Culture != null)
			{
				SetCulture(_availableCharacter.Culture);
			}
			else if (defaultCulture != null)
			{
				SetCulture(defaultCulture);
			}
			else
			{
				SetCulture(Factions.Instance.AvailableCultures.Values?.First());
			}
			InitializeOptions();
		}

		private void InitializeOptions()
		{
			Options = new MBBindingList<OptionVM>();

			_options.Add(new BoolOptionVM(
							new TextObject(nameof(AvailableCharacter.Officer)),
							new TextObject(nameof(AvailableCharacter.Officer)),
							() => AvailableCharacter.Officer,
							newValue => AvailableCharacter.Officer = newValue));

			_options.Add(new NumericOptionVM(
							new TextObject(nameof(AvailableCharacter.SpawnCount)),
							new TextObject(nameof(AvailableCharacter.SpawnCount)),
							() => AvailableCharacter.SpawnCount,
							newValue => AvailableCharacter.SpawnCount = (int)newValue,
							0, 500, true, true));

			_options.Add(new BoolOptionVM(
							new TextObject(nameof(AvailableCharacter.IsPercentage)),
							new TextObject(nameof(AvailableCharacter.IsPercentage)),
							() => AvailableCharacter.IsPercentage,
							newValue => AvailableCharacter.IsPercentage = newValue));

			_options.Add(new SelectionOptionVM(
							new TextObject(nameof(AvailableCharacter.Difficulty)),
							new TextObject(nameof(AvailableCharacter.Difficulty)),
							new SelectionOptionData(
								() => (int)AvailableCharacter.Difficulty,
								newValue => AvailableCharacter.Difficulty = (AllianceData.Difficulty)newValue,
								AllianceData.AvailableDifficulties.Length,
								AllianceData.AvailableDifficulties),
							false));

			_options.Add(new NumericOptionVM(
							new TextObject(nameof(AvailableCharacter.HealthMultiplier)),
							new TextObject(nameof(AvailableCharacter.HealthMultiplier)),
							() => AvailableCharacter.HealthMultiplier,
							newValue => AvailableCharacter.HealthMultiplier = (float)Math.Round(newValue, 1, MidpointRounding.AwayFromZero),
							0.1f, 10f, false, true));
		}

		public void SelectTroop(TroopVM troop)
		{
			if (_selectedTroop != null) _selectedTroop.IsSelected = false;
			_selectedTroop = troop;
			if (_selectedTroop != null)
			{
				_selectedTroop.IsSelected = true;
				_availableCharacter.CharacterId = _selectedTroop.StringId;
			}
		}

		[UsedImplicitly]
		public void Cancel()
		{
			// Reset the available character to its original state
			_availableCharacter.CharacterId = _availableCharacterCopy.CharacterId;
			_availableCharacter.Officer = _availableCharacterCopy.Officer;
			_availableCharacter.SpawnCount = _availableCharacterCopy.SpawnCount;
			_availableCharacter.IsPercentage = _availableCharacterCopy.IsPercentage;
			_availableCharacter.Difficulty = _availableCharacterCopy.Difficulty;
			_availableCharacter.HealthMultiplier = _availableCharacterCopy.HealthMultiplier;
			OnCloseMenu(this, EventArgs.Empty);
		}

		[UsedImplicitly]
		public void Save()
		{
			OnCloseMenu(this, EventArgs.Empty);
		}

		[UsedImplicitly]
		public void SelectPreviousCulture()
		{
			BasicCultureObject previousCulture = Factions.Instance.GetPreviousCulture(_selectedCulture);
			SetCulture(previousCulture);
		}

		[UsedImplicitly]
		public void SelectNextCulture()
		{
			BasicCultureObject nextCulture = Factions.Instance.GetNextCulture(_selectedCulture);
			SetCulture(nextCulture);
		}

		[UsedImplicitly]
		public void ToggleShowTroops()
		{
			ShowTroops = !ShowTroops;
			RefreshTroopGroups(_selectedCulture);
		}

		[UsedImplicitly]
		public void ToggleShowHeroes()
		{
			ShowHeroes = !ShowHeroes;
			RefreshTroopGroups(_selectedCulture);
		}

		[UsedImplicitly]
		public void ToggleShowBannerBearers()
		{
			ShowBannerBearers = !ShowBannerBearers;
			RefreshTroopGroups(_selectedCulture);
		}

		private void SetCulture(BasicCultureObject culture)
		{
			if (culture == null) return;

			_selectedCulture = culture;
			CultureName = culture.Name.ToString();
			CultureId = culture.StringId;

			RefreshTroopGroups(culture);
		}

		private void RefreshTroopGroups(BasicCultureObject culture)
		{
			TroopGroups.Clear();

			// Add troops from multiplayer class divisions
			if (MultiplayerHeroClassGroups != null)
			{
				foreach (MPHeroClassGroup mpheroClassGroup in MultiplayerHeroClassGroups)
				{
					MBBindingList<TroopVM> troopVMs = GetTroopsFromClass(culture, mpheroClassGroup);

					if (troopVMs.Count > 0) TroopGroups.Add(new TroopGroupVM(mpheroClassGroup, troopVMs));
				}
			}

			// Add troops from the culture's default classes
			List<BasicCharacterStub> characterStubs = Instance.CharacterStubs
				.Where(c => c.Culture?.Id == culture.Id)
				.ToList();
			if (characterStubs.Count > 0)
			{
				MBBindingList<TroopVM> troopVMs = new MBBindingList<TroopVM>();
				foreach (BasicCharacterStub characterStub in characterStubs)
				{
					TroopVM troopVM;
					BasicCharacterObject trueCharacter = Instance.GetCharacterObject(characterStub.StringId);
					if (trueCharacter != null)
					{
						troopVM = new TroopVM(trueCharacter, SelectTroop);
					}
					else
					{
						troopVM = new TroopVM(characterStub, SelectTroop);
					}

					if (_availableCharacter.CharacterId == characterStub.StringId)
					{
						SelectTroop(troopVM);
					}
					troopVMs.Add(troopVM);
				}
				TroopGroups.Add(new TroopGroupVM(culture.Name.ToString(), culture.StringId, troopVMs));
			}

			TroopVM troop = TroopGroups.FirstOrDefault()?.Troops.FirstOrDefault();
		}

		private MBBindingList<TroopVM> GetTroopsFromClass(BasicCultureObject culture, MPHeroClassGroup mpheroClassGroup)
		{
			MBBindingList<TroopVM> troopVMs = new MBBindingList<TroopVM>();
			foreach (MPHeroClass heroClass in from h in GetMPHeroClasses(culture)
											  where h.ClassGroup.Equals(mpheroClassGroup)
											  select h)
			{
				if (ShowTroops) troopVMs.Add(new TroopVM(heroClass, ClassType.Troop, SelectTroop));
				if (ShowHeroes && heroClass.HeroCharacter != null) troopVMs.Add(new TroopVM(heroClass, ClassType.Hero, SelectTroop));
				if (ShowBannerBearers && heroClass.BannerBearerCharacter != null) troopVMs.Add(new TroopVM(heroClass, ClassType.BannerBearer, SelectTroop));
			}

			return troopVMs;
		}
	}

	/// <summary>
	/// View model for a group of troops.
	/// </summary>
	public class TroopGroupVM : ViewModel
	{
		private string _name;
		private string _iconType;
		private string _iconPath;
		private MBBindingList<TroopVM> _troops;

		public bool IsValid => _troops.Count > 0;

		[DataSourceProperty]
		public string Name
		{
			get
			{
				return _name;
			}
			set
			{
				if (value != _name)
				{
					_name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		[DataSourceProperty]
		public string IconType
		{
			get
			{
				return _iconType;
			}
			set
			{
				if (value != _iconType)
				{
					_iconType = value;
					OnPropertyChangedWithValue(value, nameof(IconType));
					IconPath = "TroopBanners\\ClassType_" + value;
				}
			}
		}

		[DataSourceProperty]
		public string IconPath
		{
			get
			{
				return _iconPath;
			}
			set
			{
				if (value != _iconPath)
				{
					_iconPath = value;
					OnPropertyChangedWithValue(value, nameof(IconPath));
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<TroopVM> Troops
		{
			get
			{
				return _troops;
			}
			set
			{
				if (_troops != value)
				{
					_troops = value;
					OnPropertyChangedWithValue(value, nameof(Troops));
				}
			}
		}


		public TroopGroupVM(MPHeroClassGroup heroClassGroup, MBBindingList<TroopVM> troops)
		{
			Name = heroClassGroup.Name.ToString();
			IconType = heroClassGroup.StringId;
			Troops = troops;
		}

		public TroopGroupVM(string name, string iconType, MBBindingList<TroopVM> troops)
		{
			Name = name;
			IconType = iconType;
			Troops = troops;
		}
	}

	/// <summary>
	/// View model for a troop.
	/// </summary>
	public class TroopVM : ViewModel
	{
		public readonly MPHeroClass HeroClass;
		public readonly ClassType TroopType;
		public readonly BasicCharacterObject Troop;
		public readonly string StringId;

		private Action<TroopVM> _onTroopSelected;
		private bool _isSelected;
		private string _name;
		private string _iconType;
		private string _troopTypeId;

		[DataSourceProperty]
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					OnPropertyChangedWithValue(value, nameof(IsSelected));
				}
			}
		}

		[DataSourceProperty]
		public string TroopTypeId
		{
			get => _troopTypeId;
			set
			{
				if (value != _troopTypeId)
				{
					_troopTypeId = value;
					OnPropertyChangedWithValue(value, nameof(TroopTypeId));
				}
			}
		}

		[DataSourceProperty]
		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					OnPropertyChangedWithValue(value, nameof(Name));
				}
			}
		}

		[DataSourceProperty]
		public string IconType
		{
			get => _iconType;
			set
			{
				if (value != _iconType)
				{
					_iconType = value;
					OnPropertyChangedWithValue(value, nameof(IconType));
				}
			}
		}

		public TroopVM(MPHeroClass heroClass, ClassType troopType, Action<TroopVM> onSelect)
		{
			IsSelected = false;
			HeroClass = heroClass;
			TroopType = troopType;
			switch (TroopType)
			{
				case ClassType.Troop:
					Troop = heroClass.TroopCharacter;
					Name = Troop.Name.ToString();
					break;
				case ClassType.Hero:
					Troop = heroClass.HeroCharacter;
					Name = Troop.Name.ToString() + " (Hero)";
					break;
				case ClassType.BannerBearer:
					Troop = heroClass.BannerBearerCharacter;
					Name = Troop.Name.ToString() + " (Banner Bearer)";
					break;
			}
			StringId = Troop.StringId;
			_onTroopSelected = onSelect;
			IconType = heroClass.IconType.ToString();
			TroopTypeId = heroClass.ClassGroup.StringId;
		}

		public TroopVM(BasicCharacterObject character, Action<TroopVM> onSelect)
		{
			IsSelected = false;
			Troop = character;
			StringId = Troop.StringId;
			Name = Troop.Name.ToString() + " (" + StringId + ")";
			_onTroopSelected = onSelect;
		}

		public TroopVM(BasicCharacterStub characterStub, Action<TroopVM> onSelect)
		{
			IsSelected = false;
			StringId = characterStub.StringId;
			Name = characterStub.Name.ToString() + " (" + StringId + ")";
			_onTroopSelected = onSelect;
		}

		[UsedImplicitly]
		public void SelectTroop()
		{
			_onTroopSelected?.Invoke(this);
		}
	}

	public enum ClassType
	{
		Troop,
		Hero,
		BannerBearer
	}
}