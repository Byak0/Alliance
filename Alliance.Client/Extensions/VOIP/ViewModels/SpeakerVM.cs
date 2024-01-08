using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.VOIP.ViewModels
{
    public class SpeakerVM : ViewModel
    {
        public int UpdatesSinceSilence;
        public Agent Agent;
        private string _name;

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

        public SpeakerVM(Agent agent)
        {
            UpdatesSinceSilence = 0;
            Agent = agent;
            Name = agent.Index + "|" + agent.Name;
        }
    }
}
