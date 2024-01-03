using TaleWorlds.Library;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
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
                    OnPropertyChangedWithValue(value, "IconPath");
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
                    OnPropertyChangedWithValue(value, "Troops");
                }
            }
        }


        public TroopGroupVM(MPHeroClassGroup heroClassGroup, MBBindingList<TroopVM> troops)
        {
            Name = heroClassGroup.Name.ToString();
            IconType = heroClassGroup.StringId;
            Troops = troops;
        }
    }
}