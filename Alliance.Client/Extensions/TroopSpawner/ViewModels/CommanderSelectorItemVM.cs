using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// Custom selector for choosing commanders.
    /// Unused for now.
    /// </summary>
    public class CommanderSelectorItemVM : SelectorItemVM
    {
        private MissionPeer _commander;
        private int _formation;

        [DataSourceProperty]
        public MissionPeer Commander
        {
            get
            {
                return _commander;
            }
            set
            {
                if (_commander != value)
                {
                    _commander = value;
                    OnPropertyChangedWithValue(value, "Commander");
                }
            }
        }

        [DataSourceProperty]
        public int Formation
        {
            get
            {
                return _formation;
            }
            set
            {
                if (_formation != value)
                {
                    _formation = value;
                    OnPropertyChangedWithValue(value, "Formation");
                }
            }
        }

        public CommanderSelectorItemVM(MissionPeer commander, int formation) : base(commander.Name)
        {
            Commander = commander;
            Formation = formation;
        }
    }
}