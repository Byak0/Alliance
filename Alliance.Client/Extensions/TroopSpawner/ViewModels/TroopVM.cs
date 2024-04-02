using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// View model for a troop.
    /// </summary>
    public class TroopVM : ViewModel
    {
        private readonly MissionMultiplayerGameModeBaseClient _gameMode;
        public readonly MultiplayerClassDivisions.MPHeroClass HeroClass;
        public readonly ClassType TroopType;
        public readonly BasicCharacterObject Troop;
        public readonly ExtendedCharacter ExtendedTroop;

        private Action<TroopVM> _onTroopSelected;
        private Action<HeroPerkVM, MPPerkVM> _onPerkSelect;
        private bool _isSelected;
        private bool _useSecondary;
        private bool _useTroopLimit;
        private bool _useTroopCost;
        private int _troopCost;
        private int _troopLimitMarginR;
        private int _troopNameWidth;
        private string _name;
        private string _iconType;
        private string _troopTypeId;
        private string _troopSprite;
        private string _troopLimit;
        private MBBindingList<HeroPerkVM> _perks;

        public List<IReadOnlyPerkObject> SelectedPerks { get; private set; }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool UseSecondary
        {
            get
            {
                return _useSecondary;
            }
            set
            {
                if (value != _useSecondary)
                {
                    _useSecondary = value;
                    OnPropertyChangedWithValue(value, "UseSecondary");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<HeroPerkVM> Perks
        {
            get
            {
                return _perks;
            }
            set
            {
                if (value != _perks)
                {
                    _perks = value;
                    OnPropertyChangedWithValue(value, "Perks");
                }
            }
        }

        [DataSourceProperty]
        public string TroopTypeId
        {
            get
            {
                return _troopTypeId;
            }
            set
            {
                if (value != _troopTypeId)
                {
                    _troopTypeId = value;
                    OnPropertyChangedWithValue(value, "TroopTypeId");
                }
            }
        }

        [DataSourceProperty]
        public bool UseTroopLimit
        {
            get
            {
                return _useTroopLimit;
            }
            set
            {
                if (value != _useTroopLimit)
                {
                    _useTroopLimit = value;
                    OnPropertyChangedWithValue(value, "UseTroopLimit");
                }
            }
        }

        [DataSourceProperty]
        public bool UseTroopCost
        {
            get
            {
                return _useTroopCost;
            }
            set
            {
                if (value != _useTroopCost)
                {
                    _useTroopCost = value;
                    OnPropertyChangedWithValue(value, "UseTroopCost");
                }
            }
        }

        [DataSourceProperty]
        public int TroopLimitMarginR
        {
            get
            {
                return _troopLimitMarginR;
            }
            set
            {
                if (value != _troopLimitMarginR)
                {
                    _troopLimitMarginR = value;
                    OnPropertyChangedWithValue(value, "TroopLimitMarginR");
                }
            }
        }

        [DataSourceProperty]
        public int TroopNameWidth
        {
            get
            {
                return _troopNameWidth;
            }
            set
            {
                if (value != _troopNameWidth)
                {
                    _troopNameWidth = value;
                    OnPropertyChangedWithValue(value, "TroopNameWidth");
                }
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
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
                    OnPropertyChangedWithValue(value, "IconType");
                }
            }
        }

        [DataSourceProperty]
        public string TroopSprite
        {
            get
            {
                return _troopSprite;
            }
            set
            {
                if (_troopSprite != value)
                {
                    _troopSprite = value;
                    OnPropertyChangedWithValue(value, "TroopSprite");
                }
            }
        }

        [DataSourceProperty]
        public string TroopLimit
        {
            get
            {
                return _troopLimit;
            }
            set
            {
                if (_troopLimit != value)
                {
                    _troopLimit = value;
                    OnPropertyChangedWithValue(value, "TroopLimit");
                }
            }
        }

        [DataSourceProperty]
        public int TroopCost
        {
            get
            {
                return _troopCost;
            }
            set
            {
                if (_troopCost != value)
                {
                    _troopCost = value;
                    OnPropertyChangedWithValue(value, "TroopCost");
                }
            }
        }

        [DataSourceProperty]
        public HeroPerkVM FirstPerk => Perks.ElementAtOrDefault(0);

        [DataSourceProperty]
        public HeroPerkVM SecondPerk => Perks.ElementAtOrDefault(1);

        [DataSourceProperty]
        public HeroPerkVM ThirdPerk => Perks.ElementAtOrDefault(2);

        public TroopVM(MultiplayerClassDivisions.MPHeroClass heroClass, ClassType troopType, Action<TroopVM> onSelect, Action<HeroPerkVM, MPPerkVM> onPerkSelect)
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
            ExtendedTroop = Troop.GetExtendedCharacterObject();
            TroopLimit = ExtendedTroop?.TroopLeft + "/" + ExtendedTroop?.TroopLimit;
            TroopCost = SpawnHelper.GetTroopCost(Troop, SpawnHelper.DifficultyMultiplierFromLevel(SpawnTroopsModel.Instance.DifficultyLevel));
            UseTroopLimit = Config.Instance.UseTroopLimit;
            UseTroopCost = Config.Instance.UseTroopCost;
            TroopLimitMarginR = Config.Instance.UseTroopCost ? 80 : 20;
            TroopNameWidth = 150 + (!Config.Instance.UseTroopLimit ? 50 : 0) + (!Config.Instance.UseTroopCost ? 60 : 0);
            _onTroopSelected = onSelect;
            _onPerkSelect = onPerkSelect;
            IconType = heroClass.IconType.ToString();
            TroopTypeId = heroClass.ClassGroup.StringId;
            _gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();

            InitPerksList();
            RefreshValues();

            SpawnTroopsModel.Instance.OnDifficultyUpdated += RefreshCost;
        }

        public override void OnFinalize()
        {
            SpawnTroopsModel.Instance.OnDifficultyUpdated -= RefreshCost;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            //Name = HeroClass.HeroName.ToString();
            Perks.ApplyActionOnAllItems(delegate (HeroPerkVM x)
            {
                x.RefreshValues();
            });
        }

        [UsedImplicitly]
        public void SelectTroop()
        {
            _onTroopSelected?.Invoke(this);
        }

        private void RefreshCost()
        {
            TroopCost = SpawnHelper.GetTroopCost(Troop, SpawnHelper.DifficultyMultiplierFromLevel(SpawnTroopsModel.Instance.DifficultyLevel));
        }

        private void InitPerksList()
        {
            List<List<IReadOnlyPerkObject>> allPerksForHeroClass = MultiplayerClassDivisions.GetAllPerksForHeroClass(HeroClass);
            if (SelectedPerks == null)
            {
                SelectedPerks = new List<IReadOnlyPerkObject>();
            }
            else
            {
                SelectedPerks.Clear();
            }

            List<List<IReadOnlyPerkObject>> perksToShow = new List<List<IReadOnlyPerkObject>>();

            // Filter out BannerBearer perks if necessary
            bool isTroopTypeBannerBearer = TroopType == ClassType.BannerBearer;
            for (int i = 0; i < allPerksForHeroClass.Count; i++)
            {
                bool hasBannerBearerPerk = allPerksForHeroClass[i].Exists(perk => ((MPPerkObject)perk).HasBannerBearer);

                // Show the perk if it's not a BannerBearer perk, or if it's a BannerBearer perk and the troop is a BannerBearer.
                bool showPerk = !hasBannerBearerPerk || (isTroopTypeBannerBearer && hasBannerBearerPerk);
                if (allPerksForHeroClass[i].Count > 0 && showPerk)
                {
                    perksToShow.Add(allPerksForHeroClass[i]);
                    SelectedPerks.Add(allPerksForHeroClass[i][0]);
                }
            }

            if (GameNetwork.IsMyPeerReady)
            {
                MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                int troopIndex = (component.NextSelectedTroopIndex = MultiplayerClassDivisions.GetMPHeroClasses(HeroClass.Culture).ToList().IndexOf(HeroClass));
                for (int j = 0; j < perksToShow.Count; j++)
                {
                    int num2 = component.GetSelectedPerkIndexWithPerkListIndex(troopIndex, j);
                    if (num2 >= perksToShow[j].Count)
                    {
                        num2 = 0;
                    }

                    IReadOnlyPerkObject value = perksToShow[j][num2];
                    SelectedPerks[j] = value;
                }
            }

            MBBindingList<HeroPerkVM> mBBindingList = new MBBindingList<HeroPerkVM>();
            for (int k = 0; k < perksToShow.Count; k++)
            {
                mBBindingList.Add(new HeroPerkVM(_onPerkSelect, SelectedPerks[k], perksToShow[k], k));
            }

            Perks = mBBindingList;
        }
    }

    public enum ClassType
    {
        Troop,
        Hero,
        BannerBearer
    }
}