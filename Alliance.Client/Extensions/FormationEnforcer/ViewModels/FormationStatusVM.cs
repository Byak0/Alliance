using TaleWorlds.Library;
using static Alliance.Common.Extensions.FormationEnforcer.Component.FormationComponent;

namespace Alliance.Client.Extensions.FormationEnforcer.ViewModels
{
    public class FormationStatusVM : ViewModel
    {
        public FormationStatusVM()
        {
        }

        public States FormationStatusState
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
                        case States.None:
                        case States.Formation:
                            NbSoldiers = 3;
                            break;
                        case States.Skirmish:
                            NbSoldiers = 2;
                            break;
                        case States.Rambo:
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

        private States _formationStatusState;

        private bool _showFormationStatus;
        private int _nbSoldiers;
    }
}