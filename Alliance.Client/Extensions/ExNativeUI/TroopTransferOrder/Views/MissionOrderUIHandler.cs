using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Order;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.Views
{
    [OverrideView(typeof(MultiplayerMissionOrderUIHandler))]
    public class MissionOrderUIHandler : MissionView
    {
        private float _minHoldTimeForActivation
        {
            get
            {
                return 0f;
            }
        }

        public MissionOrderUIHandler()
        {
            ViewOrderPriority = 19;
        }

        public override void AfterStart()
        {
            base.AfterStart();
            int num;
            MultiplayerOptions.Instance.GetOptionFromOptionType(MultiplayerOptions.OptionType.NumberOfBotsPerFormation, MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions).GetValue(out num);
            _shouldTick = num > 0;
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            _latestDt = dt;
            if (_shouldTick && (!MissionScreen.IsRadialMenuActive || _dataSource.IsToggleOrderShown))
            {
                if (!_isInitialized)
                {
                    Team team = GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null;
                    if (team != null && (team == Mission.AttackerTeam || team == Mission.DefenderTeam))
                    {
                        InitializeInADisgustingManner();
                    }
                }
                if (!_isValid)
                {
                    Team team2 = GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null;
                    if (team2 != null && (team2 == Mission.AttackerTeam || team2 == Mission.DefenderTeam))
                    {
                        ValidateInADisgustingManner();
                    }
                    return;
                }
                if (_shouldInitializeFormationInfo)
                {
                    Team team3 = GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null;
                    if (_dataSource != null && team3 != null)
                    {
                        _dataSource.AfterInitialize();
                        _shouldInitializeFormationInfo = false;
                    }
                }
                TickInput(dt);
                _dataSource.Update();
                if (_dataSource.IsToggleOrderShown)
                {
                    _orderTroopPlacer.IsDrawingForced = _dataSource.IsMovementSubOrdersShown;
                    _orderTroopPlacer.IsDrawingFacing = _dataSource.IsFacingSubOrdersShown;
                    _orderTroopPlacer.IsDrawingForming = false;
                    if (cursorState == ViewModels.MissionOrderVM.CursorState.Face)
                    {
                        Vec2 orderLookAtDirection = OrderController.GetOrderLookAtDirection(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position.AsVec2);
                        MissionScreen.OrderFlag.SetArrowVisibility(true, orderLookAtDirection);
                    }
                    else
                    {
                        MissionScreen.OrderFlag.SetArrowVisibility(false, Vec2.Invalid);
                    }
                    if (cursorState == ViewModels.MissionOrderVM.CursorState.Form)
                    {
                        float orderFormCustomWidth = OrderController.GetOrderFormCustomWidth(Mission.MainAgent.Team.PlayerOrderController.SelectedFormations, MissionScreen.OrderFlag.Position);
                        MissionScreen.OrderFlag.SetWidthVisibility(true, orderFormCustomWidth);
                    }
                    else
                    {
                        MissionScreen.OrderFlag.SetWidthVisibility(false, -1f);
                    }
                    if (TaleWorlds.InputSystem.Input.IsGamepadActive)
                    {
                        ViewModels.OrderItemVM lastSelectedOrderItem = _dataSource.LastSelectedOrderItem;
                        if (lastSelectedOrderItem == null || lastSelectedOrderItem.IsTitle)
                        {
                            MissionScreen.SetRadialMenuActiveState(false);
                            if (_orderTroopPlacer.SuspendTroopPlacer && _dataSource.ActiveTargetState == 0)
                            {
                                _orderTroopPlacer.SuspendTroopPlacer = false;
                            }
                        }
                        else
                        {
                            MissionScreen.SetRadialMenuActiveState(true);
                            if (!_orderTroopPlacer.SuspendTroopPlacer)
                            {
                                _orderTroopPlacer.SuspendTroopPlacer = true;
                            }
                        }
                    }
                }
                else if (_dataSource.TroopController.IsTransferActive)
                {
                    _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                }
                else
                {
                    if (!_orderTroopPlacer.SuspendTroopPlacer)
                    {
                        _orderTroopPlacer.SuspendTroopPlacer = true;
                    }
                    _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                }
                MissionScreen.OrderFlag.IsTroop = _dataSource.ActiveTargetState == 0;
                TickOrderFlag(dt, false);
            }
        }

        public override bool OnEscape()
        {
            if (_dataSource != null)
            {
                _dataSource.OnEscape();
                return _dataSource.IsToggleOrderShown;
            }
            return false;
        }

        public override void OnMissionScreenActivate()
        {
            base.OnMissionScreenActivate();
            if (_dataSource != null)
            {
                _dataSource.AfterInitialize();
                _isInitialized = true;
            }
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            if (!_isValid)
            {
                return;
            }
            if (agent.IsHuman)
            {
                ViewModels.MissionOrderVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.TroopController.AddTroops(agent);
            }
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
            if (affectedAgent.IsHuman)
            {
                ViewModels.MissionOrderVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.TroopController.RemoveTroops(affectedAgent);
            }
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("MissionOrderHotkeyCategory"));
            _siegeDeploymentHandler = null;
            IsDeployment = false;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
            MissionMultiplayerGameModeBaseClient missionBehavior = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            _roundComponent = missionBehavior != null ? missionBehavior.RoundComponent : null;
            if (_roundComponent != null)
            {
                _roundComponent.OnRoundStarted += OnRoundStarted;
                _roundComponent.OnPreparationEnded += OnPreparationEnded;
            }
        }

        private void OnRoundStarted()
        {
            ViewModels.MissionOrderVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.AfterInitialize();
        }

        private void OnPreparationEnded()
        {
            _shouldInitializeFormationInfo = true;
        }

        private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
        {
            if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.OrderType)
            {
                if (_gauntletLayer != null && _viewMovie != null)
                {
                    _gauntletLayer.ReleaseMovie(_viewMovie);
                    string text = BannerlordConfig.OrderType == 0 ? "OrderBar" : "OrderRadial";
                    _viewMovie = _gauntletLayer.LoadMovie(text, _dataSource);
                    return;
                }
            }
            else if (changedManagedOptionsType == ManagedOptions.ManagedOptionsType.OrderLayoutType)
            {
                ViewModels.MissionOrderVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.OnOrderLayoutTypeChanged();
            }
        }

        public override void OnMissionScreenFinalize()
        {
            Clear();
            _orderTroopPlacer = null;
            MissionPeer.OnTeamChanged -= TeamChange;
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
            if (_roundComponent != null)
            {
                _roundComponent.OnRoundStarted -= OnRoundStarted;
                _roundComponent.OnPreparationEnded -= OnPreparationEnded;
            }
            base.OnMissionScreenFinalize();
        }

        public void OnActivateToggleOrder()
        {
            SetLayerEnabled(true);
        }

        public void OnDeactivateToggleOrder()
        {
            if (!_dataSource.TroopController.IsTransferActive)
            {
                SetLayerEnabled(false);
            }
        }

        private void OnTransferFinished()
        {
        }

        private void SetLayerEnabled(bool isEnabled)
        {
            if (isEnabled)
            {
                if (_dataSource == null || _dataSource.ActiveTargetState == 0)
                {
                    _orderTroopPlacer.SuspendTroopPlacer = false;
                }
                MissionScreen.SetOrderFlagVisibility(true);
                if (_gauntletLayer != null)
                {
                    ScreenManager.SetSuspendLayer(_gauntletLayer, false);
                }
                Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(true));
                return;
            }
            _orderTroopPlacer.SuspendTroopPlacer = true;
            MissionScreen.SetOrderFlagVisibility(false);
            if (_gauntletLayer != null)
            {
                ScreenManager.SetSuspendLayer(_gauntletLayer, true);
            }
            MissionScreen.SetRadialMenuActiveState(false);
            Game.Current.EventManager.TriggerEvent(new MissionPlayerToggledOrderViewEvent(false));
        }

        public void InitializeInADisgustingManner()
        {
            base.AfterStart();
            MissionScreen.OrderFlag = new OrderFlag(Mission, MissionScreen);
            _orderTroopPlacer = Mission.GetMissionBehavior<OrderTroopPlacer>();
            MissionScreen.SetOrderFlagVisibility(false);
            MissionPeer.OnTeamChanged += TeamChange;
            _isInitialized = true;
        }

        public void ValidateInADisgustingManner()
        {
            _dataSource = new ViewModels.MissionOrderVM(MissionScreen.CombatCamera, IsDeployment ? _siegeDeploymentHandler.PlayerDeploymentPoints.ToList() : new List<DeploymentPoint>(), new Action<bool>(ToggleScreenRotation), IsDeployment, new GetOrderFlagPositionDelegate(MissionScreen.GetOrderFlagPosition), new OnRefreshVisualsDelegate(RefreshVisuals), new ToggleOrderPositionVisibilityDelegate(SetSuspendTroopPlacer), new OnToggleActivateOrderStateDelegate(OnActivateToggleOrder), new OnToggleActivateOrderStateDelegate(OnDeactivateToggleOrder), new OnToggleActivateOrderStateDelegate(OnTransferFinished), new OnBeforeOrderDelegate(OnBeforeOrder), true);
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            _spriteCategory = spriteData.SpriteCategories["ui_order"];
            _spriteCategory.Load(resourceContext, uiresourceDepot);
            string text = BannerlordConfig.OrderType == 0 ? "OrderBar" : "OrderRadial";
            _viewMovie = _gauntletLayer.LoadMovie(text, _dataSource);
            _dataSource.InputRestrictions = _gauntletLayer.InputRestrictions;
            MissionScreen.AddLayer(_gauntletLayer);
            _dataSource.AfterInitialize();
            _isValid = true;
        }

        private void OnBeforeOrder()
        {
            TickOrderFlag(_latestDt, true);
        }

        private void TickOrderFlag(float dt, bool forceUpdate)
        {
            if ((MissionScreen.OrderFlag.IsVisible || forceUpdate) && Utilities.EngineFrameNo != MissionScreen.OrderFlag.LatestUpdateFrameNo)
            {
                MissionScreen.OrderFlag.Tick(_latestDt);
            }
        }

        private void RefreshVisuals()
        {
        }

        private IOrderable GetFocusedOrderableObject()
        {
            return MissionScreen.OrderFlag.FocusedOrderableObject;
        }

        private void SetSuspendTroopPlacer(bool value)
        {
            _orderTroopPlacer.SuspendTroopPlacer = value;
            MissionScreen.SetOrderFlagVisibility(!value);
        }

        private void ToggleScreenRotation(bool isLocked)
        {
            MissionScreen.SetFixedMissionCameraActive(isLocked);
        }

        private void Clear()
        {
            if (_gauntletLayer != null)
            {
                MissionScreen.RemoveLayer(_gauntletLayer);
            }
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
            }
            _gauntletLayer = null;
            _dataSource = null;
            _viewMovie = null;
            if (_isValid)
            {
                _spriteCategory.Unload();
            }
        }

        private void TeamChange(NetworkCommunicator peer, Team previousTeam, Team newTeam)
        {
            if (peer.IsMine)
            {
                Clear();
                _isValid = false;
            }
        }

        [Conditional("DEBUG")]
        private void TickInputDebug()
        {
            if (_dataSource.IsToggleOrderShown && DebugInput.IsKeyPressed(InputKey.F11) && !DebugInput.IsKeyPressed(InputKey.LeftControl) && !DebugInput.IsKeyPressed(InputKey.RightControl))
            {
                OrderType orderType = OrderController.GetActiveArrangementOrderOf(_dataSource.OrderController.SelectedFormations[0]);
                OrderType orderType2 = OrderType.ArrangementLine;
                OrderType orderType3 = OrderType.ArrangementScatter;
                orderType++;
                if (orderType > orderType3)
                {
                    orderType = orderType2;
                }
                _dataSource.OrderController.SetOrder(orderType);
                MBTextManager.SetTextVariable("ARRANGEMENT_ORDER", orderType.ToString(), false);
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=RGzyUTzm}Formation arrangement switching to {ARRANGEMENT_ORDER}", null).ToString()));
            }
        }

        public ViewModels.MissionOrderVM.CursorState cursorState
        {
            get
            {
                if (_dataSource.IsFacingSubOrdersShown)
                {
                    return ViewModels.MissionOrderVM.CursorState.Face;
                }
                return ViewModels.MissionOrderVM.CursorState.Move;
            }
        }

        private void TickInput(float dt)
        {
            if (Input.IsGameKeyDown(86) && !_dataSource.IsToggleOrderShown)
            {
                _holdTime += dt;
                if (_holdTime >= _minHoldTimeForActivation)
                {
                    _dataSource.OpenToggleOrder(true, !_holdExecuted);
                    _holdExecuted = true;
                }
            }
            else if (!Input.IsGameKeyDown(86))
            {
                if (_holdExecuted && _dataSource.IsToggleOrderShown)
                {
                    _dataSource.TryCloseToggleOrder(false);
                }
                _holdExecuted = false;
                _holdTime = 0f;
            }
            if (_dataSource.IsToggleOrderShown)
            {
                if (_dataSource.TroopController.IsTransferActive && _gauntletLayer.Input.IsHotKeyPressed("Exit"))
                {
                    _dataSource.TroopController.IsTransferActive = false;
                }
                if (_dataSource.TroopController.IsTransferActive != _isTransferEnabled)
                {
                    _isTransferEnabled = _dataSource.TroopController.IsTransferActive;
                    if (!_isTransferEnabled)
                    {
                        _gauntletLayer.IsFocusLayer = false;
                        ScreenManager.TryLoseFocus(_gauntletLayer);
                    }
                    else
                    {
                        _gauntletLayer.IsFocusLayer = true;
                        ScreenManager.TrySetFocus(_gauntletLayer);
                    }
                }
                if (_dataSource.ActiveTargetState == 0 && (Input.IsKeyReleased(InputKey.LeftMouseButton) || Input.IsKeyReleased(InputKey.ControllerRTrigger)))
                {
                    ViewModels.OrderItemVM lastSelectedOrderItem = _dataSource.LastSelectedOrderItem;
                    if (lastSelectedOrderItem != null && !lastSelectedOrderItem.IsTitle && TaleWorlds.InputSystem.Input.IsGamepadActive)
                    {
                        _dataSource.ApplySelectedOrder();
                    }
                    else
                    {
                        switch (cursorState)
                        {
                            case ViewModels.MissionOrderVM.CursorState.Move:
                                {
                                    IOrderable focusedOrderableObject = GetFocusedOrderableObject();
                                    if (focusedOrderableObject != null)
                                    {
                                        _dataSource.OrderController.SetOrderWithOrderableObject(focusedOrderableObject);
                                    }
                                    break;
                                }
                            case ViewModels.MissionOrderVM.CursorState.Face:
                                _dataSource.OrderController.SetOrderWithPosition(OrderType.LookAtDirection, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                            case ViewModels.MissionOrderVM.CursorState.Form:
                                _dataSource.OrderController.SetOrderWithPosition(OrderType.FormCustom, new WorldPosition(Mission.Current.Scene, UIntPtr.Zero, MissionScreen.GetOrderFlagPosition(), false));
                                break;
                            default:
                                TaleWorlds.Library.Debug.FailedAssert("false", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\Mission\\Multiplayer\\MissionGauntletMultiplayerOrderUIHandler.cs", "TickInput", 578);
                                break;
                        }
                    }
                }
                if (Input.IsKeyReleased(InputKey.RightMouseButton))
                {
                    _dataSource.OnEscape();
                }
            }
            int num = -1;
            if ((!TaleWorlds.InputSystem.Input.IsGamepadActive || _dataSource.IsToggleOrderShown) && !DebugInput.IsControlDown())
            {
                if (Input.IsGameKeyPressed(68))
                {
                    num = 0;
                }
                else if (Input.IsGameKeyPressed(69))
                {
                    num = 1;
                }
                else if (Input.IsGameKeyPressed(70))
                {
                    num = 2;
                }
                else if (Input.IsGameKeyPressed(71))
                {
                    num = 3;
                }
                else if (Input.IsGameKeyPressed(72))
                {
                    num = 4;
                }
                else if (Input.IsGameKeyPressed(73))
                {
                    num = 5;
                }
                else if (Input.IsGameKeyPressed(74))
                {
                    num = 6;
                }
                else if (Input.IsGameKeyPressed(75))
                {
                    num = 7;
                }
                else if (Input.IsGameKeyPressed(76))
                {
                    num = 8;
                }
            }
            if (num > -1)
            {
                _dataSource.OnGiveOrder(num);
            }
            int num2 = -1;
            if (Input.IsGameKeyPressed(77))
            {
                num2 = 100;
            }
            else if (Input.IsGameKeyPressed(78))
            {
                num2 = 0;
            }
            else if (Input.IsGameKeyPressed(79))
            {
                num2 = 1;
            }
            else if (Input.IsGameKeyPressed(80))
            {
                num2 = 2;
            }
            else if (Input.IsGameKeyPressed(81))
            {
                num2 = 3;
            }
            else if (Input.IsGameKeyPressed(82))
            {
                num2 = 4;
            }
            else if (Input.IsGameKeyPressed(83))
            {
                num2 = 5;
            }
            else if (Input.IsGameKeyPressed(84))
            {
                num2 = 6;
            }
            else if (Input.IsGameKeyPressed(85))
            {
                num2 = 7;
            }
            if (Input.IsGameKeyPressed(87))
            {
                _dataSource.SelectNextTroop(1);
            }
            else if (Input.IsGameKeyPressed(88))
            {
                _dataSource.SelectNextTroop(-1);
            }
            else if (Input.IsGameKeyPressed(89))
            {
                _dataSource.ToggleSelectionForCurrentTroop();
            }
            if (num2 != -1)
            {
                _dataSource.OnSelect(num2);
            }
            if (Input.IsGameKeyPressed(67))
            {
                _dataSource.ViewOrders();
            }
        }

        private const string _radialOrderMovieName = "OrderRadial";

        private const string _barOrderMovieName = "OrderBar";

        private float _holdTime;

        private bool _holdExecuted;

        private OrderTroopPlacer _orderTroopPlacer;

        private GauntletLayer _gauntletLayer;

        private ViewModels.MissionOrderVM _dataSource;

        private IGauntletMovie _viewMovie;

        private IRoundComponent _roundComponent;

        private SpriteCategory _spriteCategory;

        private SiegeDeploymentHandler _siegeDeploymentHandler;

        private bool _isValid;

        private bool _isInitialized;

        private bool IsDeployment;

        private bool _shouldTick;

        private bool _shouldInitializeFormationInfo;

        private float _latestDt;

        private bool _isTransferEnabled;
    }
}
