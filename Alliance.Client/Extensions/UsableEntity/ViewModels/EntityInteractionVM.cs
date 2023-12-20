using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;

namespace Alliance.Client.Extensions.UsableEntity.ViewModels
{
    /// <summary>
    /// View Model storing item infos.
    /// </summary>
    public class EntityInteractionVM : ViewModel
    {
        private AgentInteractionInterfaceVM _agentInteractionVM;
        private bool _isEnabled;

        [DataSourceProperty]
        public AgentInteractionInterfaceVM InteractionInterface
        {
            get
            {
                return _agentInteractionVM;
            }
            set
            {
                bool flag = value != _agentInteractionVM;
                if (flag)
                {
                    _agentInteractionVM = value;
                    OnPropertyChangedWithValue(value, "InteractionInterface");
                }
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    OnPropertyChangedWithValue(value, "IsEnabled");
                }
            }
        }

        public EntityInteractionVM()
        {
            InteractionInterface = new AgentInteractionInterfaceVM(Mission.Current);
        }
    }
}