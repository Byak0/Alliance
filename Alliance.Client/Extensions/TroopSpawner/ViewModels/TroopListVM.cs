using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using Color = TaleWorlds.Library.Color;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// View model for the culture selection and troop list.
    /// </summary>
    public class TroopListVM : ViewModel
    {
        private Action<TroopVM> _selectTroop;
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

        public TroopListVM(Action<TroopVM> selectTroop)
        {
            _selectTroop = selectTroop;
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

            RefreshTroops(culture);
        }

        private void RefreshTroops(BasicCultureObject culture)
        {
            TroopGroups.Clear();
            AddTroopOfType(culture, TroopTypeModel.Instance.InfantryType);
            AddTroopOfType(culture, TroopTypeModel.Instance.RangedType);
            AddTroopOfType(culture, TroopTypeModel.Instance.CavalryType);
            AddTroopOfType(culture, TroopTypeModel.Instance.HorseArcherType);
        }

        private void AddTroopOfType(BasicCultureObject culture, string type)
        {
            if (TroopTypeModel.Instance.TroopsPerCultureAndType.TryGetValue(culture, out Dictionary<string, List<BasicCharacterObject>> troopsByType))
            {
                if (troopsByType.TryGetValue(type, out List<BasicCharacterObject> troops))
                {
                    MBBindingList<TroopVM> troopVMs = new MBBindingList<TroopVM>();
                    foreach (BasicCharacterObject troop in troops)
                    {
                        TroopVM troopVM = new TroopVM(troop, _selectTroop);
                        if (troop == SpawnTroopsModel.Instance.SelectedTroop) _selectTroop(troopVM);
                        troopVMs.Add(troopVM);
                    }
                    if (troopVMs.Count > 0) TroopGroups.Add(new TroopGroupVM(type, troopVMs));
                }
            }
        }
    }
}