using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Client.Extensions.TroopSpawner.Utilities;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedCharacter.Extension;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    /// <summary>
    /// Main view model for the Recruitment menu.
    /// </summary>
    public class SpawnTroopsVM : ViewModel
    {
        private bool _isVisible;
        private bool _useTroopLimit;
        private bool _useTroopCost;
        private bool _canRecruit;
        private Color _totalCostColor;
        private int _totalCost;
        private int _totalGold;
        private int _troopCount;
        private int _customTroopCount;
        private int _difficulty;
        private TroopVM _selectedTroopVM;
        private TroopListVM _troopList;
        private CharacterViewModel _troopPreview;
        private FormationVM _selectedFormation;
        private MBBindingList<FormationVM> _formations;
        private MissionRepresentativeBase _myRepresentative;

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
        public bool CanRecruit
        {
            get
            {
                return _canRecruit;
            }
            set
            {
                if (value != _canRecruit)
                {
                    _canRecruit = value;
                    OnPropertyChangedWithValue(value, "CanRecruit");
                }
            }
        }

        [DataSourceProperty]
        public Color TotalCostColor
        {
            get
            {
                return _totalCostColor;
            }
            set
            {
                if (value != _totalCostColor)
                {
                    _totalCostColor = value;
                    OnPropertyChangedWithValue(value, "TotalCostColor");
                }
            }
        }

        [DataSourceProperty]
        public int TotalCost
        {
            get
            {
                return _totalCost;
            }
            set
            {
                if (value != _totalCost)
                {
                    _totalCost = value;
                    OnPropertyChangedWithValue(value, "TotalCost");
                }
            }
        }

        [DataSourceProperty]
        public int TotalGold
        {
            get
            {
                return _totalGold;
            }
            set
            {
                if (value != _totalGold)
                {
                    _totalGold = value;
                    OnPropertyChangedWithValue(value, "TotalGold");
                }
            }
        }

        [DataSourceProperty]
        public int TroopCount
        {
            get
            {
                return _troopCount;
            }
            set
            {
                if (value != _troopCount)
                {
                    _troopCount = value;
                    SpawnTroopsModel.Instance.TroopCount = value;
                }
            }
        }

        /// <summary>
        /// This property is updated automatically by the last ScrollableButtonWidget in RecruitmentOptions.
        /// </summary>
        [DataSourceProperty]
        public int CustomTroopCount
        {
            get
            {
                return _customTroopCount;
            }
            set
            {
                if (value != _customTroopCount)
                {
                    _customTroopCount = value;
                    SpawnTroopsModel.Instance.CustomTroopCount = value;
                }
            }
        }

        /// <summary>
        /// This property is updated automatically by the DifficultySliderWidget.
        /// </summary>
        [DataSourceProperty]
        public int Difficulty
        {
            get
            {
                return _difficulty;
            }
            set
            {
                if (value != _difficulty)
                {
                    _difficulty = value;
                    SpawnTroopsModel.Instance.DifficultyLevel = value;
                }
            }
        }

        [DataSourceProperty]
        public TroopListVM TroopList
        {
            get
            {
                return _troopList;
            }
            set
            {
                if (value != _troopList)
                {
                    _troopList = value;
                    OnPropertyChangedWithValue(value, "TroopList");
                }
            }
        }

        [DataSourceProperty]
        public CharacterViewModel TroopPreview
        {
            get
            {
                return _troopPreview;
            }
            set
            {
                if (value != _troopPreview)
                {
                    _troopPreview = value;
                    OnPropertyChangedWithValue(value, "TroopPreview");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<FormationVM> Formations
        {
            get
            {
                return _formations;
            }
            set
            {
                if (_formations != value)
                {
                    _formations = value;
                    OnPropertyChangedWithValue(value, "Formations");
                }
            }
        }

        public event EventHandler OnCloseMenu;

        public SpawnTroopsVM()
        {
            _myRepresentative = GameNetwork.MyPeer?.VirtualPlayer.GetComponent<MissionRepresentativeBase>();
            TroopPreview = new CharacterViewModel();
            TroopPreview.FillFrom(SpawnTroopsModel.Instance.SelectedTroop);
            TroopList = new TroopListVM(SelectTroop);
            TroopCount = SpawnTroopsModel.Instance.TroopCount;
            CustomTroopCount = SpawnTroopsModel.Instance.CustomTroopCount;
            Difficulty = SpawnTroopsModel.Instance.DifficultyLevel;

            Formations = new MBBindingList<FormationVM>();
            for (int i = 0; i < 8; i++)
            {
                Formation formation = Mission.Current.PlayerTeam.GetFormation((FormationClass)i);
                FormationVM formationVM = new FormationVM(formation, SelectFormation);
                if (i == SpawnTroopsModel.Instance.FormationSelected)
                {
                    _selectedFormation = formationVM;
                    _selectedFormation.Selected = true;
                }
                Formations.Add(formationVM);
                formationVM.Formation.OnUnitAdded += RefreshCommanderVisual;
            }
            SpawnTroopsModel.Instance.OnDifficultyUpdated += RefreshGold;
            SpawnTroopsModel.Instance.OnTroopSelected += RefreshGold;
            SpawnTroopsModel.Instance.OnTroopCountUpdated += RefreshGold;
            _myRepresentative.OnGoldUpdated += RefreshGold;
            RefreshGold();
        }

        public override void OnFinalize()
        {
            foreach (FormationVM formationVM in Formations)
            {
                formationVM.Formation.OnUnitAdded -= RefreshCommanderVisual;
            }
            SpawnTroopsModel.Instance.OnDifficultyUpdated -= RefreshGold;
            SpawnTroopsModel.Instance.OnTroopSelected -= RefreshGold;
            SpawnTroopsModel.Instance.OnTroopCountUpdated -= RefreshGold;
        }

        private void RefreshGold()
        {
            // Check if we exceeded troop limit
            UseTroopLimit = Config.Instance.UseTroopLimit;
            bool troopOverLimit = UseTroopLimit && SpawnTroopsModel.Instance.SelectedTroop.GetExtendedCharacterObject().TroopLeft <= 0;

            // Check if we can afford the troops 
            UseTroopCost = Config.Instance.UseTroopCost;
            bool troopTooCostly = false;
            if (UseTroopCost)
            {
                int totalGold = _myRepresentative?.Gold ?? 0;
                int troopCost = SpawnTroopsModel.Instance.TroopCount * SpawnHelper.GetTroopCost(SpawnTroopsModel.Instance.SelectedTroop, SpawnTroopsModel.Instance.Difficulty);
                TotalCost = -troopCost;
                if (troopCost > totalGold)
                {
                    // Can't afford the troop
                    TotalCostColor = Colors.Red;
                    troopTooCostly = true;
                }
                else
                {
                    TotalCostColor = Colors.White;
                }
                TotalGold = totalGold;
            }
            CanRecruit = GameNetwork.MyPeer.IsCommander() && !(troopOverLimit || troopTooCostly);
        }

        private void RefreshCommanderVisual(Formation formation, Agent agent)
        {
            if (formation.Captain == null && agent.MissionPeer != null)
            {
                // If new agent is the commander
                if (FormationControlModel.Instance.GetControllerOfFormation(formation) == agent.MissionPeer)
                {
                    // Update the commander visual in all of his formations
                    List<FormationClass> formationToRefresh = FormationControlModel.Instance.GetControlledFormations(agent.MissionPeer);
                    foreach (FormationClass formationClass in formationToRefresh)
                    {
                        Formations[(int)formationClass].RefreshCommanderVisual(agent);
                        Log("Updating commander visual for formation " + (int)formationClass, LogLevel.Debug);
                    }
                }
            }
        }

        public void SelectTroop(TroopVM troopVM)
        {
            // Unselect previous troop
            if (_selectedTroopVM != null) _selectedTroopVM.IsSelected = false;
            // Select new troop
            _selectedTroopVM = troopVM;
            _selectedTroopVM.IsSelected = true;
            // Update model
            SpawnTroopsModel.Instance.SelectedTroop = troopVM.Troop;
            // Update preview
            TroopPreview?.FillFrom(troopVM.Troop);
        }

        // Called by ScrollableButtonWidget
        public void Select1()
        {
            TroopCount = 1;
        }

        // Called by ScrollableButtonWidget
        public void Select10()
        {
            TroopCount = 10;
        }

        // Called by ScrollableButtonWidget
        public void Select50()
        {
            TroopCount = 50;
        }

        // Called by ScrollableButtonWidget
        public void SelectCustom()
        {
            TroopCount = CustomTroopCount;
        }

        public void SelectFormation(FormationVM formationVM)
        {
            // Unselect previous formation
            if (_selectedFormation != null) _selectedFormation.Selected = false;
            // Select new formation
            _selectedFormation = formationVM;
            _selectedFormation.Selected = true;
            // Update model
            SpawnTroopsModel.Instance.FormationSelected = formationVM.Formation.Index;
        }

        public void RecruitTroop()
        {
            SpawnRequestHelper.RequestSpawnTroop();
        }

        public void CloseMenu()
        {
            OnCloseMenu(this, EventArgs.Empty);
        }
    }
}