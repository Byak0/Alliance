using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Utils;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
using static Alliance.Common.Core.Utils.AgentExtensions;
using Color = TaleWorlds.Library.Color;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
	/// <summary>
	/// View model for the culture selection and troop list.
	/// </summary>
	public class TroopListVM : ViewModel
	{
		private Action<TroopVM> _selectTroop;
		private Action<HeroPerkVM, MPPerkVM> _selectPerk;
		private Color _cultureBackgroundColor;
		private Color _cultureForegroundColor;
		private bool _showTroops = true;
		private bool _showHeroes;
		private bool _showBannerBearers;
		private string _cultureSprite;
		private string _cultureName;
		private MBBindingList<TroopGroupVM> _troopGroups;

		[DataSourceProperty]
		public bool ShowTroops
		{
			get
			{
				return _showTroops;
			}
			set
			{
				if (_showTroops != value)
				{
					_showTroops = value;
					OnPropertyChangedWithValue(value, "ShowTroops");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowHeroes
		{
			get
			{
				return _showHeroes;
			}
			set
			{
				if (_showHeroes != value)
				{
					_showHeroes = value;
					OnPropertyChangedWithValue(value, "ShowHeroes");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowBannerBearers
		{
			get
			{
				return _showBannerBearers;
			}
			set
			{
				if (_showBannerBearers != value)
				{
					_showBannerBearers = value;
					OnPropertyChangedWithValue(value, "ShowBannerBearers");
				}
			}
		}

		[DataSourceProperty]
		public Color CultureBackgroundColor
		{
			get
			{
				return _cultureBackgroundColor;
			}
			set
			{
				if (_cultureBackgroundColor != value)
				{
					_cultureBackgroundColor = value;
					OnPropertyChangedWithValue(value, "CultureBackgroundColor");
				}
			}
		}

		[DataSourceProperty]
		public Color CultureForegroundColor
		{
			get
			{
				return _cultureForegroundColor;
			}
			set
			{
				if (_cultureForegroundColor != value)
				{
					_cultureForegroundColor = value;
					OnPropertyChangedWithValue(value, "CultureForegroundColor");
				}
			}
		}

		[DataSourceProperty]
		public string CultureSprite
		{
			get
			{
				return _cultureSprite;
			}
			set
			{
				if (_cultureSprite != value)
				{
					_cultureSprite = value;
					OnPropertyChangedWithValue(value, "CultureSprite");
				}
			}
		}

		[DataSourceProperty]
		public string CultureName
		{
			get
			{
				return _cultureName;
			}
			set
			{
				if (_cultureName != value)
				{
					_cultureName = value;
					OnPropertyChangedWithValue(value, "CultureName");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<TroopGroupVM> TroopGroups
		{
			get
			{
				return _troopGroups;
			}
			set
			{
				if (_troopGroups != value)
				{
					_troopGroups = value;
					OnPropertyChangedWithValue(value, "TroopGroups");
				}
			}
		}

		public TroopListVM(Action<TroopVM> selectTroop, Action<HeroPerkVM, MPPerkVM> selectPerk)
		{
			_selectTroop = selectTroop;
			_selectPerk = selectPerk;
			TroopGroups = new MBBindingList<TroopGroupVM>();
			SetCulture(SpawnTroopsModel.Instance.SelectedFaction);
			SpawnTroopsModel.Instance.OnFactionSelected += RefreshCulture;
		}

		public override void OnFinalize()
		{
			SpawnTroopsModel.Instance.OnFactionSelected -= RefreshCulture;
		}

		public void SelectPreviousCulture()
		{
			if (MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC" && !GameNetwork.MyPeer.IsAdmin()) return;
			BasicCultureObject previousCulture = Factions.Instance.GetPreviousCulture(SpawnTroopsModel.Instance.SelectedFaction);
			SetCulture(previousCulture);
		}

		public void SelectNextCulture()
		{
			if (MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC" && !GameNetwork.MyPeer.IsAdmin()) return;
			BasicCultureObject nextCulture = Factions.Instance.GetNextCulture(SpawnTroopsModel.Instance.SelectedFaction);
			SetCulture(nextCulture);
		}

		public void ToggleShowTroops()
		{
			ShowTroops = !ShowTroops;
			RefreshTroopGroups(SpawnTroopsModel.Instance.SelectedFaction);
		}

		public void ToggleShowHeroes()
		{
			ShowHeroes = !ShowHeroes;
			RefreshTroopGroups(SpawnTroopsModel.Instance.SelectedFaction);
		}

		public void ToggleShowBannerBearers()
		{
			ShowBannerBearers = !ShowBannerBearers;
			RefreshTroopGroups(SpawnTroopsModel.Instance.SelectedFaction);
		}

		private void RefreshCulture()
		{
			SetCulture(SpawnTroopsModel.Instance.SelectedFaction);
		}

		private void SetCulture(BasicCultureObject culture)
		{
			SpawnTroopsModel.Instance.SelectedFaction = culture;
			CultureName = culture.Name.ToString();
			CultureBackgroundColor = Color.FromUint(culture.BackgroundColor1);
			CultureForegroundColor = Color.FromUint(culture.ForegroundColor1);
			CultureSprite = "StdAssets\\FactionIcons\\LargeIcons\\" + culture.StringId;

			RefreshTroopGroups(culture);
		}

		private void RefreshTroopGroups(BasicCultureObject culture)
		{
			TroopGroups.Clear();
			foreach (MultiplayerClassDivisions.MPHeroClassGroup mpheroClassGroup in MultiplayerClassDivisions.MultiplayerHeroClassGroups)
			{
				MBBindingList<TroopVM> troopVMs = GetTroopsFromClass(culture, mpheroClassGroup);

				if (troopVMs.Count > 0) TroopGroups.Add(new TroopGroupVM(mpheroClassGroup, troopVMs));
			}
			TroopVM troop = TroopGroups.FirstOrDefault()?.Troops.FirstOrDefault();
			_selectTroop(troop);
		}

		private MBBindingList<TroopVM> GetTroopsFromClass(BasicCultureObject culture, MultiplayerClassDivisions.MPHeroClassGroup mpheroClassGroup)
		{
			MBBindingList<TroopVM> troopVMs = new MBBindingList<TroopVM>();
			foreach (MultiplayerClassDivisions.MPHeroClass heroClass in from h in MultiplayerClassDivisions.GetMPHeroClasses(culture)
																		where h.ClassGroup.Equals(mpheroClassGroup)
																		select h)
			{
				if (ShowTroops) troopVMs.Add(new TroopVM(heroClass, ClassType.Troop, _selectTroop, _selectPerk));
				if (ShowHeroes && heroClass.HeroCharacter != null) troopVMs.Add(new TroopVM(heroClass, ClassType.Hero, _selectTroop, _selectPerk));
				if (ShowBannerBearers && heroClass.BannerBearerCharacter != null) troopVMs.Add(new TroopVM(heroClass, ClassType.BannerBearer, _selectTroop, _selectPerk));
			}

			return troopVMs;
		}
	}
}