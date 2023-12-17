using TaleWorlds.Library;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// View model for a group of troops.
    /// </summary>
    public class TroopGroupVM : ViewModel
    {
        private string _groupName;
        private MBBindingList<TroopVM> _troops;

        [DataSourceProperty]
        public string GroupName
        {
            get
            {
                return _groupName;
            }
            set
            {
                if (_groupName != value)
                {
                    _groupName = value;
                    OnPropertyChangedWithValue(value, "GroupName");
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


        public TroopGroupVM(string groupName, MBBindingList<TroopVM> troops)
        {
            GroupName = groupName;
            Troops = troops;
        }
    }
}