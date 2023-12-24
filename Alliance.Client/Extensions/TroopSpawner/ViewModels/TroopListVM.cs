using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Utilities;
using System;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
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
        private string _cultureSprite;
        private string _cultureName;
        private MBBindingList<TroopGroupVM> _troopGroups;

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
        }

        public void SelectPreviousCulture()
        {
            BasicCultureObject previousCulture = Factions.Instance.GetPreviousCulture(SpawnTroopsModel.Instance.SelectedFaction);
            SetCulture(previousCulture);
        }

        public void SelectNextCulture()
        {
            BasicCultureObject nextCulture = Factions.Instance.GetNextCulture(SpawnTroopsModel.Instance.SelectedFaction);
            SetCulture(nextCulture);
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
        }

        private MBBindingList<TroopVM> GetTroopsFromClass(BasicCultureObject culture, MultiplayerClassDivisions.MPHeroClassGroup mpheroClassGroup)
        {
            MBBindingList<TroopVM> troopVMs = new MBBindingList<TroopVM>();
            foreach (MultiplayerClassDivisions.MPHeroClass heroClass in from h in MultiplayerClassDivisions.GetMPHeroClasses(culture)
                                                                        where h.ClassGroup.Equals(mpheroClassGroup)
                                                                        select h)
            {
                troopVMs.Add(new TroopVM(heroClass, ClassType.Troop, _selectTroop, _selectPerk));
                // Todo : add a way to filter hero, troop, banner bearers, etc.
                //troopVMs.Add(new TroopVM(heroClass, ClassType.Hero, _selectTroop, _selectPerk));
                if (heroClass.BannerBearerCharacter != null)
                {
                    troopVMs.Add(new TroopVM(heroClass, ClassType.BannerBearer, _selectTroop, _selectPerk));
                }
            }

            return troopVMs;
        }
    }
}