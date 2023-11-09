using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AgentsCount.ViewModels
{
    public class AgentsCountVM : ViewModel
    {
        public AgentsCountVM()
        {
        }

        [DataSourceProperty]
        public int AgentsAlive
        {
            get
            {
                return _agentsAlive;
            }
            set
            {
                if (value != _agentsAlive)
                {
                    _agentsAlive = value;
                    OnPropertyChangedWithValue(value, "AgentsAlive");
                }
            }
        }

        [DataSourceProperty]
        public int AgentsDead
        {
            get
            {
                return _agentsDead;
            }
            set
            {
                if (value != _agentsDead)
                {
                    _agentsDead = value;
                    OnPropertyChangedWithValue(value, "AgentsDead");
                }
            }
        }

        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }

        private bool _isVisible;
        private int _agentsAlive = 0;
        private int _agentsDead = 0;
    }
}