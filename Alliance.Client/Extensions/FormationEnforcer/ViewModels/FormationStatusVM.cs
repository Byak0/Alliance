using Alliance.Common.Extensions.FormationEnforcer.Component;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.FormationEnforcer.ViewModels
{
    public class FormationStatusVM : ViewModel
    {
        public FormationStatusVM()
        {
        }

        public FormationState FormationStatusState
        {
            get
            {
                return _formationStatusState;
            }
            set
            {
                if (value != _formationStatusState)
                {
                    _formationStatusState = value;
                    switch (value)
                    {
                        case FormationState.None:
                        case FormationState.Formation:
                            NbSoldiers = 3;
                            break;
                        case FormationState.Skirmish:
                            NbSoldiers = 2;
                            break;
                        case FormationState.Rambo:
                            NbSoldiers = 1;
                            break;
                    }
                }
            }
        }

        [DataSourceProperty]
        public int NbSoldiers
        {
            get
            {
                return _nbSoldiers;
            }
            set
            {
                if (value != _nbSoldiers)
                {
                    _nbSoldiers = value;
                    OnPropertyChangedWithValue(value, "NbSoldiers");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowFormationStatus
        {
            get
            {
                return _showFormationStatus;
            }
            set
            {
                if (value != _showFormationStatus)
                {
                    _showFormationStatus = value;
                    OnPropertyChangedWithValue(value, "ShowFormationStatus");
                }
            }
        }

        private FormationState _formationStatusState;

        private bool _showFormationStatus;
        private int _nbSoldiers;
    }
}