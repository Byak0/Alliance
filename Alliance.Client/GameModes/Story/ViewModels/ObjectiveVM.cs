using Alliance.Common.GameModes.Story.Models.Objectives;
using TaleWorlds.Library;

namespace Alliance.Client.GameModes.Story.ViewModels
{
    public class ObjectiveVM : ViewModel
    {
        private ObjectiveBase _objective;

        private string _name;
        private string _description;
        private string _progress;

        public ObjectiveVM(ObjectiveBase objective)
        {
            _objective = objective;
            Name = _objective.Name;
            Description = _objective.Description;
            RefreshProgress();
        }

        public void RefreshProgress()
        {
            Progress = _objective.GetProgressAsString();
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
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (value != _description)
                {
                    _description = value;
                    OnPropertyChangedWithValue(value, "Description");
                }
            }
        }

        [DataSourceProperty]
        public string Progress
        {
            get
            {
                return _progress;
            }
            set
            {
                if (value != _progress)
                {
                    _progress = value;
                    OnPropertyChangedWithValue(value, "Progress");
                }
            }
        }
    }
}