using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedCharacter.Extension;
using Alliance.Common.Core.ExtendedCharacter.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// View model for a troop.
    /// </summary>
    public class TroopVM : ViewModel
    {
        private Action<TroopVM> _onTroopSelected;
        public BasicCharacterObject Troop { get; }
        private bool _isSelected;
        private bool _useTroopLimit;
        private bool _useTroopCost;
        private int _troopCost;
        private int _troopLimitMarginR;
        private int _troopNameWidth;
        private string _troopName;
        private string _troopSprite;
        private string _troopLimit;

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
        public string TroopName
        {
            get
            {
                return _troopName;
            }
            set
            {
                if (_troopName != value)
                {
                    _troopName = value;
                    OnPropertyChangedWithValue(value, "TroopName");
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

        public TroopVM(BasicCharacterObject troop, Action<TroopVM> selectTroop)
        {
            Troop = troop;
            IsSelected = false;
            UseTroopLimit = Config.Instance.UseTroopLimit;
            UseTroopCost = Config.Instance.UseTroopCost;
            TroopLimitMarginR = Config.Instance.UseTroopCost ? 80 : 20;
            TroopNameWidth = 150 + (!Config.Instance.UseTroopLimit ? 50 : 0) + (!Config.Instance.UseTroopCost ? 60 : 0);
            TroopName = troop.Name.ToString();
            TroopSprite = "General\\compass\\" + TroopTypeModel.Instance.TroopsType[troop];
            ExtendedCharacterObject extendedChar = troop.GetExtendedCharacterObject();
            TroopLimit = extendedChar.TroopLeft + "/" + extendedChar.TroopLimit;
            TroopCost = SpawnHelper.GetTroopCost(troop, SpawnTroopsModel.Instance.Difficulty);
            SpawnTroopsModel.Instance.OnDifficultyUpdated += RefreshCost;
            _onTroopSelected = selectTroop;
        }

        public override void OnFinalize()
        {
            SpawnTroopsModel.Instance.OnDifficultyUpdated -= RefreshCost;
        }

        public void SelectTroop()
        {
            _onTroopSelected?.Invoke(this);
        }

        private void RefreshCost()
        {
            TroopCost = SpawnHelper.GetTroopCost(Troop, SpawnTroopsModel.Instance.Difficulty);
        }
    }
}