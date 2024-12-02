using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.ViewModels
{
    public class OrderTroopItemVM : OrderSubjectVM
    {
        public FormationClass InitialFormationClass;

        public Formation Formation;

        public Type MachineType;

        public Action<OrderTroopItemVM> SetSelected;

        private OrderTroopItemFormationClassVM _cachedInfantryItem;

        private OrderTroopItemFormationClassVM _cachedRangedItem;

        private OrderTroopItemFormationClassVM _cachedCavalryItem;

        private OrderTroopItemFormationClassVM _cachedHorseArcherItem;

        private BasicCharacterObject _cachedCommander;

        private int _currentMemberCount;

        private int _morale;

        private float _ammoPercentage;

        private bool _isAmmoAvailable;

        private bool _haveTroops;

        private ImageIdentifierVM _commanderImageIdentifier;

        private MBBindingList<OrderTroopItemFormationClassVM> _activeFormationClasses;

        private MBBindingList<OrderTroopItemFilterVM> _activeFilters;

        public bool ContainsDeadTroop { get; private set; }

        [DataSourceProperty]
        public int CurrentMemberCount
        {
            get
            {
                return _currentMemberCount;
            }
            set
            {
                if (value != _currentMemberCount)
                {
                    _currentMemberCount = value;
                    OnPropertyChangedWithValue(value, "CurrentMemberCount");
                    HaveTroops = value > 0;
                }
            }
        }

        [DataSourceProperty]
        public int Morale
        {
            get
            {
                return _morale;
            }
            set
            {
                if (value != _morale)
                {
                    _morale = value;
                    OnPropertyChangedWithValue(value, "Morale");
                }
            }
        }

        [DataSourceProperty]
        public float AmmoPercentage
        {
            get
            {
                return _ammoPercentage;
            }
            set
            {
                if (value != _ammoPercentage)
                {
                    _ammoPercentage = value;
                    OnPropertyChangedWithValue(value, "AmmoPercentage");
                }
            }
        }

        [DataSourceProperty]
        public bool IsAmmoAvailable
        {
            get
            {
                return _isAmmoAvailable;
            }
            set
            {
                if (value != _isAmmoAvailable)
                {
                    _isAmmoAvailable = value;
                    OnPropertyChangedWithValue(value, "IsAmmoAvailable");
                }
            }
        }

        [DataSourceProperty]
        public bool HaveTroops
        {
            get
            {
                return _haveTroops;
            }
            set
            {
                if (value != _haveTroops)
                {
                    _haveTroops = value;
                    OnPropertyChangedWithValue(value, "HaveTroops");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM CommanderImageIdentifier
        {
            get
            {
                return _commanderImageIdentifier;
            }
            set
            {
                if (value != _commanderImageIdentifier)
                {
                    _commanderImageIdentifier = value;
                    OnPropertyChangedWithValue(value, "CommanderImageIdentifier");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<OrderTroopItemFormationClassVM> ActiveFormationClasses
        {
            get
            {
                return _activeFormationClasses;
            }
            set
            {
                if (value != _activeFormationClasses)
                {
                    _activeFormationClasses = value;
                    OnPropertyChangedWithValue(value, "ActiveFormationClasses");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<OrderTroopItemFilterVM> ActiveFilters
        {
            get
            {
                return _activeFilters;
            }
            set
            {
                if (value != _activeFilters)
                {
                    _activeFilters = value;
                    OnPropertyChangedWithValue(value, "ActiveFilters");
                }
            }
        }

        public OrderTroopItemVM(Formation formation, Action<OrderTroopItemVM> setSelected, Func<Formation, int> getMorale)
        {
            ActiveFormationClasses = new MBBindingList<OrderTroopItemFormationClassVM>();
            ActiveFilters = new MBBindingList<OrderTroopItemFilterVM>();
            InitialFormationClass = formation.FormationIndex;
            SetFormationClassFromFormation(formation);
            Formation = formation;
            SetSelected = setSelected;
            CurrentMemberCount = formation.IsPlayerTroopInFormation ? formation.CountOfUnits - 1 : formation.CountOfUnits;
            Morale = getMorale(formation);
            UnderAttackOfType = 0;
            BehaviorType = 0;
            if (Input.IsControllerConnected)
            {
                _ = !Input.IsMouseActive;
            }
            else
                _ = 0;
            UpdateSelectionKeyInfo();
            UpdateCommanderInfo();
            Formation.OnUnitCountChanged += FormationOnOnUnitCountChanged;
        }

        public OrderTroopItemVM(OrderTroopItemVM troop, Action<OrderTroopItemVM> setSelected = null)
        {
            ActiveFormationClasses = new MBBindingList<OrderTroopItemFormationClassVM>();
            foreach (OrderTroopItemFormationClassVM activeFormationClass in troop.ActiveFormationClasses)
            {
                ActiveFormationClasses.Add(new OrderTroopItemFormationClassVM(troop.Formation, activeFormationClass.FormationClass));
            }

            ActiveFilters = new MBBindingList<OrderTroopItemFilterVM>();
            foreach (OrderTroopItemFilterVM activeFilter in troop.ActiveFilters)
            {
                ActiveFilters.Add(new OrderTroopItemFilterVM(activeFilter.FilterTypeValue));
            }

            InitialFormationClass = troop.InitialFormationClass;
            Formation = troop.Formation;
            SetSelected = setSelected ?? troop.SetSelected;
            CurrentMemberCount = troop.Formation.IsPlayerTroopInFormation ? troop.CurrentMemberCount - 1 : troop.CurrentMemberCount;
            Morale = troop.Morale;
            UnderAttackOfType = 0;
            BehaviorType = 0;
            UpdateCommanderInfo();
        }

        public override void OnFinalize()
        {
            Formation.OnUnitCountChanged -= FormationOnOnUnitCountChanged;
        }

        private void FormationOnOnUnitCountChanged(Formation formation)
        {
            CurrentMemberCount = formation.IsPlayerTroopInFormation ? formation.CountOfUnits - 1 : formation.CountOfUnits;
            UpdateCommanderInfo();
        }

        public void OnFormationAgentRemoved(Agent agent)
        {
            if (!agent.IsActive())
            {
                ContainsDeadTroop = true;
            }

            UpdateCommanderInfo();
        }

        private void UpdateCommanderInfo()
        {
            if (Formation?.Captain?.Character != null && (Formation.Captain.Character != _cachedCommander || CommanderImageIdentifier == null))
            {
                CommanderImageIdentifier = new ImageIdentifierVM(CharacterCode.CreateFrom(Formation.Captain.Character));
                _cachedCommander = Formation.Captain.Character;
            }
            else
            {
                CommanderImageIdentifier = null;
            }
        }

        private void UpdateSelectionKeyInfo()
        {
            if (Formation != null)
            {
                int num = -1;
                if (Formation.Index == 0)
                {
                    num = 78;
                }
                else if (Formation.Index == 1)
                {
                    num = 79;
                }
                else if (Formation.Index == 2)
                {
                    num = 80;
                }
                else if (Formation.Index == 3)
                {
                    num = 81;
                }
                else if (Formation.Index == 4)
                {
                    num = 82;
                }
                else if (Formation.Index == 5)
                {
                    num = 83;
                }
                else if (Formation.Index == 6)
                {
                    num = 84;
                }
                else if (Formation.Index == 7)
                {
                    num = 85;
                }

                if (num != -1)
                {
                    GameKey gameKey = HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(num);
                    SelectionKey = InputKeyItemVM.CreateFromGameKey(gameKey, isConsoleOnly: false);
                }
            }
        }

        public bool SetFormationClassFromFormation(Formation formation)
        {
            bool flag = formation.QuerySystem.InfantryUnitRatio > 0f;
            bool flag2 = formation.QuerySystem.RangedUnitRatio > 0f;
            bool flag3 = formation.QuerySystem.CavalryUnitRatio > 0f;
            bool flag4 = formation.QuerySystem.RangedCavalryUnitRatio > 0f;
            if (flag && _cachedInfantryItem == null)
            {
                _cachedInfantryItem = new OrderTroopItemFormationClassVM(formation, FormationClass.Infantry);
                ActiveFormationClasses.Add(_cachedInfantryItem);
            }
            else if (!flag)
            {
                ActiveFormationClasses.Remove(_cachedInfantryItem);
                _cachedInfantryItem = null;
            }

            if (flag2 && _cachedRangedItem == null)
            {
                _cachedRangedItem = new OrderTroopItemFormationClassVM(formation, FormationClass.Ranged);
                ActiveFormationClasses.Add(_cachedRangedItem);
            }
            else if (!flag2)
            {
                ActiveFormationClasses.Remove(_cachedRangedItem);
                _cachedRangedItem = null;
            }

            if (flag3 && _cachedCavalryItem == null)
            {
                _cachedCavalryItem = new OrderTroopItemFormationClassVM(formation, FormationClass.Cavalry);
                ActiveFormationClasses.Add(_cachedCavalryItem);
            }
            else if (!flag3)
            {
                ActiveFormationClasses.Remove(_cachedCavalryItem);
                _cachedCavalryItem = null;
            }

            if (flag4 && _cachedHorseArcherItem == null)
            {
                _cachedHorseArcherItem = new OrderTroopItemFormationClassVM(formation, FormationClass.HorseArcher);
                ActiveFormationClasses.Add(_cachedHorseArcherItem);
            }
            else if (!flag4)
            {
                ActiveFormationClasses.Remove(_cachedHorseArcherItem);
                _cachedHorseArcherItem = null;
            }

            foreach (OrderTroopItemFormationClassVM activeFormationClass in ActiveFormationClasses)
            {
                activeFormationClass.UpdateTroopCount();
            }

            UpdateCommanderInfo();
            return false;
        }

        public void UpdateFilterData(List<int> usedFilters)
        {
            ActiveFilters.Clear();
            foreach (int usedFilter in usedFilters)
            {
                ActiveFilters.Add(new OrderTroopItemFilterVM(usedFilter));
            }
        }

        public void ExecuteAction()
        {
            SetSelected(this);
        }
    }
}
