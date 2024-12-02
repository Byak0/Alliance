using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.ViewModels
{
    public class MissionOrderDeploymentControllerVM : ViewModel
    {
        public const uint ENTITYHIGHLIGHTCOLOR = 4289622555u;

        public const uint ENTITYSELECTEDCOLOR = 4293481743u;

        private GameEntity _currentSelectedEntity;

        private GameEntity _currentHoveredEntity;

        private InquiryData _siegeDeployQueryData;

        private SiegeDeploymentHandler _siegeDeploymentHandler;

        private BattleDeploymentHandler _battleDeploymentHandler;

        private readonly List<DeploymentPoint> _deploymentPoints;

        internal DeploymentSiegeMachineVM _selectedDeploymentPointVM;

        private readonly MissionOrderVM _missionOrder;

        private readonly Camera _deploymentCamera;

        private readonly Action<bool> _toggleMissionInputs;

        private readonly OnRefreshVisualsDelegate _onRefreshVisuals;

        private bool _isOrderPreconfigured;

        private MBBindingList<OrderSiegeMachineVM> _siegeMachineList;

        private MBBindingList<DeploymentSiegeMachineVM> _siegeDeploymentList;

        private MBBindingList<DeploymentSiegeMachineVM> _deploymentTargets;

        private bool _isSiegeDeploymentListActive;

        private Mission Mission => Mission.Current;

        private Team Team => Mission.Current.PlayerTeam;

        public OrderController OrderController => Team.PlayerOrderController;

        [DataSourceProperty]
        public MBBindingList<OrderSiegeMachineVM> SiegeMachineList
        {
            get
            {
                return _siegeMachineList;
            }
            set
            {
                if (value != _siegeMachineList)
                {
                    _siegeMachineList = value;
                    OnPropertyChanged("SiegeMachineList");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<DeploymentSiegeMachineVM> DeploymentTargets
        {
            get
            {
                return _deploymentTargets;
            }
            set
            {
                if (value != _deploymentTargets)
                {
                    _deploymentTargets = value;
                    OnPropertyChanged("DeploymentTargets");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSiegeDeploymentListActive
        {
            get
            {
                return _isSiegeDeploymentListActive;
            }
            set
            {
                if (value != _isSiegeDeploymentListActive)
                {
                    _isSiegeDeploymentListActive = value;
                    OnPropertyChanged("IsSiegeDeploymentListActive");
                    _toggleMissionInputs(value);
                    _onRefreshVisuals();
                    if (_selectedDeploymentPointVM != null)
                    {
                        _selectedDeploymentPointVM.IsSelected = value;
                    }
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<DeploymentSiegeMachineVM> SiegeDeploymentList
        {
            get
            {
                return _siegeDeploymentList;
            }
            set
            {
                if (value != _siegeDeploymentList)
                {
                    _siegeDeploymentList = value;
                    OnPropertyChanged("SiegeDeploymentList");
                }
            }
        }

        public MissionOrderDeploymentControllerVM(List<DeploymentPoint> deploymentPoints, MissionOrderVM missionOrder, Camera deploymentCamera, Action<bool> toggleMissionInputs, OnRefreshVisualsDelegate onRefreshVisuals)
        {
            _deploymentPoints = deploymentPoints;
            _missionOrder = missionOrder;
            _deploymentCamera = deploymentCamera;
            _toggleMissionInputs = toggleMissionInputs;
            _onRefreshVisuals = onRefreshVisuals;
            SiegeMachineList = new MBBindingList<OrderSiegeMachineVM>();
            SiegeDeploymentList = new MBBindingList<DeploymentSiegeMachineVM>();
            DeploymentTargets = new MBBindingList<DeploymentSiegeMachineVM>();
            MBTextManager.SetTextVariable("UNDEPLOYED_SIEGE_MACHINE_COUNT", SiegeMachineList.Count((s) => !s.SiegeWeapon.IsUsed).ToString());
            _siegeDeployQueryData = new InquiryData(new TextObject("{=TxphX8Uk}Deployment").ToString(), new TextObject("{=LlrlE199}You can still deploy siege engines.\nBegin anyway?").ToString(), isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_ok").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate
            {
                _siegeDeploymentHandler.FinishDeployment();
                _missionOrder.TryCloseToggleOrder();
            }, null);
            SiegeMachineList.Clear();
            OrderController.SiegeWeaponController.OnSelectedSiegeWeaponsChanged += OnSelectedSiegeWeaponsChanged;
            _siegeDeploymentHandler = Mission.GetMissionBehavior<SiegeDeploymentHandler>();
            if (_siegeDeploymentHandler != null)
            {
                _siegeDeploymentHandler.OnDeploymentReady += ExecuteDeployAll;
            }
            else
            {
                _battleDeploymentHandler = Mission.GetMissionBehavior<BattleDeploymentHandler>();
                if (_battleDeploymentHandler != null)
                {
                    _battleDeploymentHandler.OnDeploymentReady += ExecuteDeployAll;
                }
            }

            SiegeDeploymentList.Clear();
            if (_siegeDeploymentHandler != null)
            {
                int num = 1;
                foreach (DeploymentPoint deploymentPoint in _deploymentPoints)
                {
                    OrderSiegeMachineVM item = new OrderSiegeMachineVM(deploymentPoint, OnSelectOrderSiegeMachine, num++);
                    SiegeMachineList.Add(item);
                    if (deploymentPoint.DeployableWeapons.Any((x) => _siegeDeploymentHandler.GetMaxDeployableWeaponCountOfPlayer(x.GetType()) > 0))
                    {
                        DeploymentSiegeMachineVM item2 = new DeploymentSiegeMachineVM(deploymentPoint, null, _deploymentCamera, OnRefreshSelectedDeploymentPoint, OnEntityHover, deploymentPoint.IsDeployed);
                        DeploymentTargets.Add(item2);
                    }
                }
            }

            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            _siegeMachineList.ApplyActionOnAllItems(delegate (OrderSiegeMachineVM x)
            {
                x.RefreshValues();
            });
            _siegeDeploymentList.ApplyActionOnAllItems(delegate (DeploymentSiegeMachineVM x)
            {
                x.RefreshValues();
            });
            _deploymentTargets.ApplyActionOnAllItems(delegate (DeploymentSiegeMachineVM x)
            {
                x.RefreshValues();
            });
        }

        public void SetIsOrderPreconfigured(bool isPreconfigured)
        {
            _isOrderPreconfigured = isPreconfigured;
        }

        internal void Update()
        {
            for (int i = 0; i < DeploymentTargets.Count; i++)
            {
                DeploymentTargets[i].Update();
            }
        }

        internal void DeployFormationsOfPlayer()
        {
            if (!Mission.PlayerTeam.IsPlayerGeneral)
            {
                Mission.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam);
            }
            else if (Mission.IsSiegeBattle)
            {
                Mission.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam);
            }

            Mission.Current.GetMissionBehavior<AssignPlayerRoleInTeamMissionController>()?.OnPlayerTeamDeployed();
        }

        internal void SetSiegeMachineActiveOrders(OrderSiegeMachineVM siegeItemVM)
        {
            siegeItemVM.ActiveOrders.Clear();
        }

        internal void ProcessSiegeMachines()
        {
            foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
            {
                siegeMachine.RefreshSiegeWeapon();
                if (!siegeMachine.IsSelectable && siegeMachine.IsSelected)
                {
                    OnDeselectSiegeMachine(siegeMachine);
                }
            }
        }

        internal void SelectAllSiegeMachines()
        {
            OrderController.SiegeWeaponController.SelectAll();
            foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
            {
                siegeMachine.IsSelected = siegeMachine.IsSelectable;
                if (siegeMachine.IsSelectable)
                {
                    SetSiegeMachineActiveOrders(siegeMachine);
                }
            }

            _missionOrder.SetActiveOrders();
            _onRefreshVisuals();
        }

        internal void AddSelectedSiegeMachine(OrderSiegeMachineVM item)
        {
            if (item.IsSelectable)
            {
                OrderController.SiegeWeaponController.Select(item.SiegeWeapon);
                item.IsSelected = true;
                _missionOrder.SetActiveOrders();
                _onRefreshVisuals();
            }
        }

        internal void SetSelectedSiegeMachine(OrderSiegeMachineVM item)
        {
            ProcessSiegeMachines();
            if (!item.IsSelectable)
            {
                return;
            }

            SetSiegeMachineActiveOrders(item);
            OrderController.SiegeWeaponController.ClearSelectedWeapons();
            foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
            {
                siegeMachine.IsSelected = false;
            }

            AddSelectedSiegeMachine(item);
            _onRefreshVisuals();
        }

        internal void OnDeselectSiegeMachine(OrderSiegeMachineVM item)
        {
            if (OrderController.SiegeWeaponController.SelectedWeapons.Contains(item.SiegeWeapon))
            {
                OrderController.SiegeWeaponController.Deselect(item.SiegeWeapon);
            }

            item.IsSelected = false;
            _missionOrder.SetActiveOrders();
            _onRefreshVisuals();
        }

        internal void OnSelectOrderSiegeMachine(OrderSiegeMachineVM item)
        {
            ProcessSiegeMachines();
            _missionOrder.IsTroopPlacingActive = false;
            if (!item.IsSelectable)
            {
                return;
            }

            if (TaleWorlds.InputSystem.Input.DebugInput.IsControlDown())
            {
                if (item.IsSelected)
                {
                    OnDeselectSiegeMachine(item);
                }
                else
                {
                    AddSelectedSiegeMachine(item);
                }
            }
            else
            {
                SetSelectedSiegeMachine(item);
            }

            _onRefreshVisuals();
        }

        internal void OnSelectDeploymentSiegeMachine(DeploymentSiegeMachineVM item)
        {
            IsSiegeDeploymentListActive = false;
            _currentSelectedEntity?.SetContourColor(null);
            _currentSelectedEntity = null;
            _selectedDeploymentPointVM = null;
            SiegeDeploymentList.Clear();
            bool flag = false;
            if (item != null && (!(item.MachineType != null) || _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(item.MachineType) != 0) && (item.DeploymentPoint.DeployedWeapon == null || !(item.DeploymentPoint.DeployedWeapon.GetType() == item.MachineType)))
            {
                bool num = !item.DeploymentPoint.IsDeployed || item.DeploymentPoint.DeployedWeapon != item.SiegeWeapon;
                if (item.DeploymentPoint.IsDeployed)
                {
                    if (item.SiegeWeapon == null)
                    {
                        SoundEvent.PlaySound2D("event:/ui/dropdown");
                    }

                    item.DeploymentPoint.Disband();
                }

                flag = !SiegeMachineList.Any((s) => s.DeploymentPoint.IsDeployed);
                if (num && item.SiegeWeapon != null)
                {
                    SiegeEngineType machine = item.Machine;
                    if (machine == DefaultSiegeEngineTypes.Catapult || machine == DefaultSiegeEngineTypes.FireCatapult || machine == DefaultSiegeEngineTypes.Onager || machine == DefaultSiegeEngineTypes.FireOnager)
                    {
                        SoundEvent.PlaySound2D("event:/ui/mission/catapult");
                    }
                    else if (machine == DefaultSiegeEngineTypes.Ram)
                    {
                        SoundEvent.PlaySound2D("event:/ui/mission/batteringram");
                    }
                    else if (machine == DefaultSiegeEngineTypes.SiegeTower)
                    {
                        SoundEvent.PlaySound2D("event:/ui/mission/siegetower");
                    }
                    else if (machine == DefaultSiegeEngineTypes.Trebuchet || machine == DefaultSiegeEngineTypes.Bricole)
                    {
                        SoundEvent.PlaySound2D("event:/ui/mission/catapult");
                    }
                    else if (machine == DefaultSiegeEngineTypes.Ballista || machine == DefaultSiegeEngineTypes.FireBallista)
                    {
                        SoundEvent.PlaySound2D("event:/ui/mission/ballista");
                    }

                    item.DeploymentPoint.Deploy(item.SiegeWeapon);
                }
            }

            ProcessSiegeMachines();
            if (flag && _missionOrder.IsToggleOrderShown)
            {
                _missionOrder.SetActiveOrders();
            }

            _onRefreshVisuals();
            foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
            {
                deploymentTarget.RefreshWithDeployedWeapon();
            }
        }

        internal void OnSelectedSiegeWeaponsChanged()
        {
        }

        public void OnRefreshSelectedDeploymentPoint(DeploymentSiegeMachineVM item)
        {
            RefreshSelectedDeploymentPoint(item.DeploymentPoint);
        }

        public void OnEntityHover(GameEntity hoveredEntity)
        {
            if (_currentHoveredEntity == hoveredEntity)
            {
                return;
            }

            DeploymentPoint deploymentPoint = null;
            if (hoveredEntity != null)
            {
                if (hoveredEntity.HasScriptOfType<DeploymentPoint>())
                {
                    deploymentPoint = hoveredEntity.GetFirstScriptOfType<DeploymentPoint>();
                }
                else if (_siegeDeploymentHandler != null)
                {
                    deploymentPoint = _siegeDeploymentHandler.PlayerDeploymentPoints.SingleOrDefault((dp) => dp.IsDeployed && hoveredEntity.GetScriptComponents().Any((sc) => dp.DeployedWeapon == sc));
                }
            }

            OnEntityHover(deploymentPoint);
        }

        public void OnEntityHover(DeploymentPoint deploymentPoint)
        {
            if (_currentSelectedEntity != _currentHoveredEntity)
            {
                _currentHoveredEntity?.SetContourColor(null);
            }

            if (deploymentPoint != null)
            {
                _currentHoveredEntity = deploymentPoint.IsDeployed ? deploymentPoint.DeployedWeapon.GameEntity : deploymentPoint.GameEntity;
            }
            else
            {
                _currentHoveredEntity = null;
            }

            if (_currentSelectedEntity != _currentHoveredEntity)
            {
                _currentHoveredEntity?.SetContourColor(4289622555u);
            }
        }

        public void OnEntitySelect(GameEntity selectedEntity)
        {
            if (_currentSelectedEntity == selectedEntity)
            {
                return;
            }

            DeploymentPoint deploymentPoint = null;
            if (selectedEntity != null && _siegeDeploymentHandler != null)
            {
                if (selectedEntity.HasScriptOfType<DeploymentPoint>())
                {
                    deploymentPoint = selectedEntity.GetFirstScriptOfType<DeploymentPoint>();
                }
                else if (_siegeDeploymentHandler != null)
                {
                    deploymentPoint = _siegeDeploymentHandler.PlayerDeploymentPoints.SingleOrDefault((dp) => dp.IsDeployed && selectedEntity.GetScriptComponents().Any((sc) => dp.DeployedWeapon == sc));
                }
            }

            if (deploymentPoint != null)
            {
                _missionOrder.IsTroopPlacingActive = false;
                RefreshSelectedDeploymentPoint(deploymentPoint);
            }
            else
            {
                ExecuteCancelSelectedDeploymentPoint();
            }
        }

        public void RefreshSelectedDeploymentPoint(DeploymentPoint selectedDeploymentPoint)
        {
            IsSiegeDeploymentListActive = false;
            foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
            {
                if (deploymentTarget.DeploymentPoint == selectedDeploymentPoint)
                {
                    _selectedDeploymentPointVM = deploymentTarget;
                }
            }

            if (!_selectedDeploymentPointVM.IsSelected)
            {
                _selectedDeploymentPointVM.IsSelected = true;
            }

            SiegeDeploymentList.Clear();
            DeploymentSiegeMachineVM deploymentSiegeMachineVM;
            foreach (SynchedMissionObject deployableWeapon in selectedDeploymentPoint.DeployableWeapons)
            {
                Type type = deployableWeapon.GetType();
                if (_siegeDeploymentHandler.GetMaxDeployableWeaponCountOfPlayer(type) > 0)
                {
                    deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(selectedDeploymentPoint, deployableWeapon as SiegeWeapon, _deploymentCamera, OnSelectDeploymentSiegeMachine, null, selectedDeploymentPoint.IsDeployed && selectedDeploymentPoint.DeployedWeapon == deployableWeapon);
                    SiegeDeploymentList.Add(deploymentSiegeMachineVM);
                    deploymentSiegeMachineVM.RemainingCount = _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(type);
                }
            }

            deploymentSiegeMachineVM = new DeploymentSiegeMachineVM(selectedDeploymentPoint, null, _deploymentCamera, OnSelectDeploymentSiegeMachine, null, !selectedDeploymentPoint.IsDeployed);
            SiegeDeploymentList.Add(deploymentSiegeMachineVM);
            selectedDeploymentPoint.GameEntity.SetContourColor(4293481743u);
            IsSiegeDeploymentListActive = true;
            _currentSelectedEntity?.SetContourColor(null);
            _currentSelectedEntity = selectedDeploymentPoint.GameEntity;
            _currentSelectedEntity?.SetContourColor(4293481743u);
        }

        public void ExecuteCancelSelectedDeploymentPoint()
        {
            OnSelectDeploymentSiegeMachine(null);
        }

        public void ExecuteBeginSiege()
        {
            IsSiegeDeploymentListActive = false;
            if (_siegeDeploymentHandler != null && _siegeDeploymentHandler.PlayerDeploymentPoints.Any((d) => !d.IsDeployed && d.DeployableWeaponTypes.Any((type) => _siegeDeploymentHandler.GetDeployableWeaponCountOfPlayer(type) > 0)))
            {
                InformationManager.ShowInquiry(_siegeDeployQueryData);
                return;
            }

            _missionOrder.TryCloseToggleOrder();
            if (_siegeDeploymentHandler != null)
            {
                _siegeDeploymentHandler.FinishDeployment();
            }
            else
            {
                _battleDeploymentHandler.FinishDeployment();
            }
        }

        public void ExecuteAutoDeploy()
        {
            Mission.TryRemakeInitialDeploymentPlanForBattleSide(Mission.PlayerTeam.Side);
            if (Mission.IsSiegeBattle)
            {
                Mission.AutoDeployTeamUsingTeamAI(Mission.PlayerTeam);
            }
            else
            {
                Mission.AutoDeployTeamUsingDeploymentPlan(Mission.PlayerTeam);
            }
        }

        public void ExecuteDeployAll()
        {
            if (_siegeDeploymentHandler != null)
            {
                _siegeDeploymentHandler.DeployAllSiegeWeaponsOfPlayer();
                Mission.ForceTickOccasionally = true;
                bool isTeleportingAgents = Mission.Current.IsTeleportingAgents;
                Mission.IsTeleportingAgents = true;
                DeployFormationsOfPlayer();
                _siegeDeploymentHandler.ForceUpdateAllUnits();
                _missionOrder.OnDeployAll();
                foreach (OrderSiegeMachineVM siegeMachine in SiegeMachineList)
                {
                    siegeMachine.RefreshSiegeWeapon();
                }

                foreach (DeploymentSiegeMachineVM deploymentTarget in DeploymentTargets)
                {
                    deploymentTarget.RefreshWithDeployedWeapon();
                }

                Mission.IsTeleportingAgents = isTeleportingAgents;
                Mission.ForceTickOccasionally = false;
                SelectAllSiegeMachines();
            }
            else if (_battleDeploymentHandler != null)
            {
                DeployFormationsOfPlayer();
                _battleDeploymentHandler.ForceUpdateAllUnits();
                _missionOrder.OnDeployAll();
            }
        }

        public void FinalizeDeployment()
        {
            _missionOrder.IsDeployment = false;
            foreach (OrderSiegeMachineVM item in SiegeMachineList.ToList())
            {
                if (item.DeploymentPoint.IsDeployed)
                {
                    SetSiegeMachineActiveOrders(item);
                }
                else
                {
                    SiegeMachineList.Remove(item);
                }
            }

            DeploymentTargets.Clear();
            SiegeDeploymentList.Clear();
        }

        internal void OnSelectFormationWithIndex(int formationTroopIndex)
        {
            OrderSiegeMachineVM PvCOrderSiegeMachineVM = SiegeMachineList.ElementAtOrDefault(formationTroopIndex);
            if (PvCOrderSiegeMachineVM != null)
            {
                OnSelectOrderSiegeMachine(PvCOrderSiegeMachineVM);
            }
            else
            {
                SelectAllSiegeMachines();
            }
        }

        internal void SetCurrentActiveOrders()
        {
            List<OrderSubjectVM> list = (from item in SiegeMachineList.Cast<OrderSubjectVM>().ToList()
                                         where item.IsSelected && item.IsSelectable
                                         select item).ToList();
            if (!list.IsEmpty())
            {
                return;
            }

            OrderController.SiegeWeaponController.SelectAll();
            foreach (OrderSiegeMachineVM item in SiegeMachineList.Where((s) => s.IsSelectable && s.DeploymentPoint.IsDeployed))
            {
                item.IsSelected = true;
                SetSiegeMachineActiveOrders(item);
                list.Add(item);
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            OrderController.SiegeWeaponController.OnSelectedSiegeWeaponsChanged -= OnSelectedSiegeWeaponsChanged;
            SiegeDeploymentList.Clear();
            foreach (OrderSiegeMachineVM item in SiegeMachineList.ToList())
            {
                if (!item.DeploymentPoint.IsDeployed)
                {
                    SiegeMachineList.Remove(item);
                }
            }

            _siegeDeploymentHandler = null;
            _siegeDeployQueryData = null;
        }
    }
}
