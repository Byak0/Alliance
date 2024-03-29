using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Client.Extensions.TroopSpawner.Utilities;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;
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
        private bool _showDifficultySlider;
        private Color _totalCostColor;
        private int _totalCost;
        private int _totalGold;
        private int _troopCount;
        private int _customTroopCount;
        private int _difficulty;
        private TroopVM _selectedTroopVM;
        private TroopInformationVM _troopInformation;
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
        public bool ShowDifficultySlider
        {
            get
            {
                return _showDifficultySlider;
            }
            set
            {
                if (value != _showDifficultySlider)
                {
                    _showDifficultySlider = value;
                    OnPropertyChangedWithValue(value, "ShowDifficultySlider");
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
                    OnPropertyChangedWithValue(value, "Difficulty");
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

        [DataSourceProperty]
        public TroopInformationVM TroopInformation
        {
            get
            {
                return _troopInformation;
            }
            set
            {
                if (value != _troopInformation)
                {
                    _troopInformation = value;
                    OnPropertyChangedWithValue(value, "TroopInformation");
                }
            }
        }

        [DataSourceProperty]
        public TroopVM SelectedTroopVM
        {
            get
            {
                return _selectedTroopVM;
            }
            set
            {
                if (value != _selectedTroopVM)
                {
                    _selectedTroopVM = value;
                    OnPropertyChangedWithValue(value, "SelectedTroopVM");
                }
            }
        }

        public event EventHandler OnCloseMenu;

        public SpawnTroopsVM()
        {
            _myRepresentative = GameNetwork.MyPeer?.VirtualPlayer.GetComponent<MissionRepresentativeBase>();
            TroopPreview = new CharacterViewModel();
            TroopPreview.FillFrom(SpawnTroopsModel.Instance.SelectedTroop);
            TroopList = new TroopListVM(SelectTroop, SelectPerk);
            TroopInformation = new TroopInformationVM();
            TroopCount = 1;
            CustomTroopCount = SpawnTroopsModel.Instance.CustomTroopCount;
            ShowDifficultySlider = Config.Instance.BotDifficulty == nameof(SpawnHelper.Difficulty.PlayerChoice) || GameNetwork.MyPeer.IsAdmin();
            Difficulty = SpawnHelper.DifficultyLevelFromString(Config.Instance.BotDifficulty);
            UseTroopCost = Config.Instance.UseTroopCost;

            Formations = new MBBindingList<FormationVM>();
            for (int i = 0; i < 8; i++)
            {
                Formation formation = SpawnTroopsModel.Instance.SelectedTeam?.GetFormation((FormationClass)i);
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
            SpawnTroopsModel.Instance.OnFactionSelected += RefreshFormations;
            _myRepresentative.OnGoldUpdated += RefreshGold;

            TroopGroupVM troopGroupVM = TroopList.TroopGroups.FirstOrDefault();
            TroopVM defaultTroopVM = (troopGroupVM != null) ? troopGroupVM.Troops.FirstOrDefault() : null;
            SelectTroop(defaultTroopVM);
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
            bool troopOverLimit = Config.Instance.UseTroopLimit && SpawnTroopsModel.Instance.SelectedTroop.GetExtendedCharacterObject().TroopLeft <= 0;

            // Check if we can afford the troops 
            bool troopTooCostly = false;
            if (Config.Instance.UseTroopCost)
            {
                int totalGold = _myRepresentative?.Gold ?? 0;
                int troopCost = SpawnTroopsModel.Instance.TroopCount * SpawnHelper.GetTroopCost(SpawnTroopsModel.Instance.SelectedTroop, SpawnHelper.DifficultyMultiplierFromLevel(SpawnTroopsModel.Instance.DifficultyLevel));
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

            CanRecruit = GameNetwork.MyPeer.IsAdmin() || GameNetwork.MyPeer.IsCommander() && !(troopOverLimit || troopTooCostly);
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

        private void RefreshFormations()
        {
            foreach (FormationVM formationVM in Formations)
            {
                formationVM.Formation.OnUnitAdded -= RefreshCommanderVisual;
            }
            Formations = new MBBindingList<FormationVM>();
            for (int i = 0; i < 8; i++)
            {
                Formation formation = SpawnTroopsModel.Instance.SelectedTeam.GetFormation((FormationClass)i);
                FormationVM formationVM = new FormationVM(formation, SelectFormation);
                if (i == SpawnTroopsModel.Instance.FormationSelected)
                {
                    _selectedFormation = formationVM;
                    _selectedFormation.Selected = true;
                }
                Formations.Add(formationVM);
                formationVM.Formation.OnUnitAdded += RefreshCommanderVisual;
            }
        }

        private void SelectTroop(TroopVM troopVM)
        {
            if (troopVM == null) return;
            // Unselect previous troop
            if (SelectedTroopVM != null) SelectedTroopVM.IsSelected = false;
            // Select new troop
            SelectedTroopVM = troopVM;
            SelectedTroopVM.IsSelected = true;
            // Update model
            SpawnTroopsModel.Instance.SelectedTroop = SelectedTroopVM.Troop;
            SpawnTroopsModel.Instance.SelectedPerks = SelectedTroopVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
            // Update preview
            RefreshTroopPreview();
            RefreshTroopInformations();
        }

        public void RefreshTroopPreview()
        {
            if (TroopPreview == null) return;

            List<IReadOnlyPerkObject> perks = SelectedTroopVM.Perks.Select(p => p.SelectedPerk).ToList();

            Equipment equipment = SelectedTroopVM.Troop.Equipment.Clone();
            MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(perks);
            IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipements = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
            if (alternativeEquipements != null)
            {
                foreach ((EquipmentIndex, EquipmentElement) item in alternativeEquipements)
                {
                    equipment[item.Item1] = item.Item2;
                }
            }

            TroopPreview.FillFrom(SelectedTroopVM.Troop);
            TroopPreview.EquipmentCode = equipment.CalculateEquipmentCode();
            TroopPreview.BannerCodeText = SpawnTroopsModel.Instance.BannerCode?.Code ?? String.Empty;
        }

        private void SelectPerk(HeroPerkVM heroPerk, MPPerkVM candidate)
        {
            if (GameNetwork.IsMyPeerReady && TroopInformation?.HeroClass != null && SelectedTroopVM != null)
            {
                SpawnTroopsModel.Instance.SelectedPerks = SelectedTroopVM.Perks.Select(p => p.CandidatePerks.IndexOf(p.SelectedPerkItem)).ToList();
                RefreshTroopPreview();
                RefreshTroopInformations();
            }
        }

        private void RefreshTroopInformations()
        {
            List<IReadOnlyPerkObject> perks = SelectedTroopVM.Perks.Select(p => p.SelectedPerk).ToList();
            if (perks.Count > 0)
            {
                TroopInformation?.RefreshWith(SelectedTroopVM.HeroClass, SelectedTroopVM.Troop, perks);
            }
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