using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.ViewModels
{
    public class OrderSetVM : ViewModel
    {
        private Action<OrderItemVM, OrderSetType, bool> OnSetExecution;

        internal OrderSetType OrderSetType = OrderSetType.None;

        internal OrderSubType OrderSubType = OrderSubType.None;

        private OrderSubType _selectedOrder = OrderSubType.None;

        private bool _isMultiplayer;

        private int _index = -1;

        private bool _isToggleActivationOrder;

        private bool _showOrders;

        private bool _canUseShortcuts;

        private OrderItemVM _titleOrder;

        private MBBindingList<OrderItemVM> _orders;

        private string _titleText;

        private InputKeyItemVM _titleOrderKey;

        private string _selectedOrderText;

        internal IEnumerable<OrderSubType> SubOrdersSP
        {
            get
            {
                switch (OrderSetType)
                {
                    case OrderSetType.Movement:
                        yield return OrderSubType.MoveToPosition;
                        yield return OrderSubType.FollowMe;
                        yield return OrderSubType.Charge;
                        yield return OrderSubType.Advance;
                        yield return OrderSubType.Fallback;
                        yield return OrderSubType.Stop;
                        yield return OrderSubType.Retreat;
                        yield return OrderSubType.Return;
                        break;
                    case OrderSetType.Form:
                        yield return OrderSubType.FormLine;
                        yield return OrderSubType.FormClose;
                        yield return OrderSubType.FormLoose;
                        yield return OrderSubType.FormCircular;
                        yield return OrderSubType.FormSchiltron;
                        yield return OrderSubType.FormV;
                        yield return OrderSubType.FormColumn;
                        yield return OrderSubType.FormScatter;
                        yield return OrderSubType.Return;
                        break;
                    case OrderSetType.Toggle:
                        yield return OrderSubType.ToggleFacing;
                        yield return OrderSubType.ToggleFire;
                        yield return OrderSubType.ToggleMount;
                        yield return OrderSubType.ToggleAI;
                        yield return OrderSubType.ToggleTransfer;
                        yield return OrderSubType.Return;
                        break;
                    case OrderSetType.Facing:
                        yield return OrderSubType.ActivationFaceDirection;
                        yield return OrderSubType.FaceEnemy;
                        break;
                    default:
                        yield return OrderSubType.None;
                        break;
                }
            }
        }

        internal IEnumerable<OrderSubType> SubOrdersMP
        {
            get
            {
                switch (OrderSetType)
                {
                    case OrderSetType.Movement:
                        yield return OrderSubType.MoveToPosition;
                        yield return OrderSubType.FollowMe;
                        yield return OrderSubType.Charge;
                        yield return OrderSubType.Advance;
                        yield return OrderSubType.Fallback;
                        yield return OrderSubType.Stop;
                        yield return OrderSubType.Retreat;
                        yield return OrderSubType.Return;
                        break;
                    case OrderSetType.Form:
                        yield return OrderSubType.FormLine;
                        yield return OrderSubType.FormClose;
                        yield return OrderSubType.FormLoose;
                        yield return OrderSubType.FormCircular;
                        yield return OrderSubType.FormSchiltron;
                        yield return OrderSubType.FormV;
                        yield return OrderSubType.FormColumn;
                        yield return OrderSubType.FormScatter;
                        yield return OrderSubType.Return;
                        break;
                    case OrderSetType.Toggle:
                        yield return OrderSubType.ToggleFacing;
                        yield return OrderSubType.ToggleFire;
                        yield return OrderSubType.ToggleMount;
                        yield return OrderSubType.ToggleAI;
                        yield return OrderSubType.ToggleTransfer;
                        yield return OrderSubType.Return;
                        break;
                    case OrderSetType.Facing:
                        yield return OrderSubType.ActivationFaceDirection;
                        yield return OrderSubType.FaceEnemy;
                        break;
                    default:
                        yield return OrderSubType.None;
                        break;
                }
            }
        }

        public bool ContainsOrders { get; private set; }

        [DataSourceProperty]
        public bool CanUseShortcuts
        {
            get
            {
                return _canUseShortcuts;
            }
            set
            {
                if (value != _canUseShortcuts)
                {
                    _canUseShortcuts = value;
                    OnPropertyChangedWithValue(value, "CanUseShortcuts");
                    if (TitleOrder != null)
                    {
                        TitleOrder.CanUseShortcuts = value;
                    }

                    for (int i = 0; i < Orders.Count; i++)
                    {
                        Orders[i].CanUseShortcuts = value;
                    }
                }
            }
        }

        [DataSourceProperty]
        public string SelectedOrderText
        {
            get
            {
                return _selectedOrderText;
            }
            set
            {
                if (value != _selectedOrderText)
                {
                    _selectedOrderText = value;
                    OnPropertyChangedWithValue(value, "SelectedOrderText");
                }
            }
        }

        [DataSourceProperty]
        public string TitleText
        {
            get
            {
                return _titleText;
            }
            set
            {
                if (value != _titleText)
                {
                    _titleText = value;
                    OnPropertyChangedWithValue(value, "TitleText");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<OrderItemVM> Orders
        {
            get
            {
                return _orders;
            }
            set
            {
                if (value != _orders)
                {
                    _orders = value;
                    OnPropertyChangedWithValue(value, "Orders");
                }
            }
        }

        [DataSourceProperty]
        public OrderItemVM TitleOrder
        {
            get
            {
                return _titleOrder;
            }
            set
            {
                if (value != _titleOrder)
                {
                    _titleOrder = value;
                    OnPropertyChangedWithValue(value, "TitleOrder");
                }
            }
        }

        [DataSourceProperty]
        public InputKeyItemVM TitleOrderKey
        {
            get
            {
                return _titleOrderKey;
            }
            set
            {
                if (value != _titleOrderKey)
                {
                    _titleOrderKey = value;
                    OnPropertyChanged("TitleOrderKey");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowOrders
        {
            get
            {
                if (_showOrders)
                {
                    return ContainsOrders;
                }

                return false;
            }
            set
            {
                _showOrders = value;
                OnPropertyChangedWithValue(value, "ShowOrders");
            }
        }

        internal OrderSetVM(OrderSetType orderSetType, Action<OrderItemVM, OrderSetType, bool> onExecution, bool isMultiplayer)
        {
            ContainsOrders = true;
            OrderSetType = orderSetType;
            OnSetExecution = onExecution;
            _isMultiplayer = isMultiplayer;
            Orders = new MBBindingList<OrderItemVM>();
            TitleOrderKey = InputKeyItemVM.CreateFromGameKey(GetOrderGameKey((int)orderSetType), isConsoleOnly: false);
            RefreshValues();
            TitleOrder.IsActive = true;
        }

        internal OrderSetVM(OrderSubType orderSubType, int index, Action<OrderItemVM, OrderSetType, bool> onExecution, bool isMultiplayer)
        {
            ContainsOrders = false;
            OrderSubType = orderSubType;
            OnSetExecution = onExecution;
            _isMultiplayer = isMultiplayer;
            _index = index;
            Orders = new MBBindingList<OrderItemVM>();
            TitleOrderKey = InputKeyItemVM.CreateFromGameKey(GetOrderGameKey(index), isConsoleOnly: false);
            RefreshValues();
            TitleOrder.IsActive = true;
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            if (TitleOrder != null)
            {
                TitleOrder.OnFinalize();
            }

            if (ContainsOrders)
            {
                TitleOrder = new OrderItemVM(OrderSetType, GameTexts.FindText("str_order_set_name", OrderSetType.ToString()), OnExecuteOrderSet);
                TitleOrder.ShortcutKey = InputKeyItemVM.CreateFromGameKey(GetOrderGameKey(GetOrderIndexFromOrderSetType(OrderSetType)), isConsoleOnly: false);
                TitleOrder.IsTitle = true;
                TitleText = GameTexts.FindText("str_order_set_name", OrderSetType.ToString()).ToString().Trim(' ');
            }
            else
            {
                _isToggleActivationOrder = OrderSubType > OrderSubType.ToggleStart && OrderSubType < OrderSubType.ToggleEnd;
                TextObject textObject = null;
                textObject = !_isToggleActivationOrder ? GameTexts.FindText("str_order_name", OrderSubType.ToString()) : GameTexts.FindText("str_order_name_off", OrderSubType.ToString());
                TitleText = textObject.ToString();
                TitleOrder = new OrderItemVM(OrderSubType, OrderSetType.None, textObject, OnExecuteOrderSet);
                TitleOrder.IsTitle = true;
                TitleOrder.ShortcutKey = InputKeyItemVM.CreateFromGameKey(GetOrderGameKey(_index), isConsoleOnly: false);
            }

            MBTextManager.SetTextVariable("SHORTCUT", "");
            if (!ContainsOrders)
            {
                return;
            }

            OrderSubType[] array = _isMultiplayer ? SubOrdersMP.ToArray() : SubOrdersSP.ToArray();
            foreach (OrderItemVM order in Orders)
            {
                order.ShortcutKey.OnFinalize();
            }

            Orders.Clear();
            int num = 0;
            OrderSubType[] array2 = array;
            for (int i = 0; i < array2.Length; i++)
            {
                OrderSubType orderSubType = array2[i];
                OrderItemVM orderItemVM = new OrderItemVM(tooltipText: OrderSetType != OrderSetType.Toggle ? GameTexts.FindText("str_order_name", orderSubType.ToString()) : GameTexts.FindText("str_order_name_off", orderSubType.ToString()), orderSubType: orderSubType, orderSetType: OrderSetType, onExecuteAction: OnExecuteSubOrder);
                Orders.Add(orderItemVM);
                if (orderSubType == OrderSubType.Return)
                {
                    orderItemVM.ShortcutKey = InputKeyItemVM.CreateFromGameKey(HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(76), isConsoleOnly: false);
                }
                else
                {
                    orderItemVM.ShortcutKey = InputKeyItemVM.CreateFromGameKey(GetOrderGameKey(num), isConsoleOnly: false);
                }

                num++;
            }
        }

        private int GetOrderIndexFromOrderSetType(OrderSetType orderSetType)
        {
            if (BannerlordConfig.OrderLayoutType == 0)
            {
                return (int)orderSetType;
            }

            return orderSetType switch
            {
                OrderSetType.Movement => 0,
                OrderSetType.Facing => 1,
                OrderSetType.Form => 2,
                _ => -1,
            };
        }

        private static GameKey GetOrderGameKey(int index)
        {
            switch (index)
            {
                case 0:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(68);
                case 1:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(69);
                case 2:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(70);
                case 3:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(71);
                case 4:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(72);
                case 5:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(73);
                case 6:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(74);
                case 7:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(75);
                case 8:
                    return HotKeyManager.GetCategory("MissionOrderHotkeyCategory").GetGameKey(76);
                default:
                    Debug.FailedAssert("Invalid order game key index", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\OrderSetVM.cs", "GetOrderGameKey", 345);
                    return null;
            }
        }

        private void OnExecuteSubOrder(OrderItemVM orderItem, bool fromSelection)
        {
            OnSetExecution(orderItem, OrderSetType, fromSelection);
            if (fromSelection)
            {
                SelectedOrderText = orderItem.TooltipText;
            }
        }

        private void OnExecuteOrderSet(OrderItemVM orderItem, bool fromSelection)
        {
            OnSetExecution(orderItem, OrderSetType, fromSelection);
            if (fromSelection)
            {
                SelectedOrderText = orderItem.TooltipText;
            }
        }

        public void ResetActiveStatus(bool disable = false)
        {
            TitleOrder.SelectionState = !disable ? 1 : 0;
            if (ContainsOrders)
            {
                foreach (OrderItemVM order in Orders)
                {
                    order.SelectionState = !disable ? 1 : 0;
                }

                if (OrderSetType == OrderSetType.Toggle)
                {
                    Orders.ApplyActionOnAllItems(delegate (OrderItemVM o)
                    {
                        o.SetActiveState(isActive: false);
                    });
                }
            }
            else
            {
                TitleOrder.SetActiveState(isActive: false);
            }
        }

        public void FinalizeActiveStatus(bool forceDisable = false)
        {
            TitleOrder.FinalizeActiveStatus();
            if (forceDisable)
            {
                return;
            }

            foreach (OrderItemVM order in Orders)
            {
                order.FinalizeActiveStatus();
            }
        }

        internal OrderItemVM GetOrder(OrderSubType type)
        {
            if (ContainsOrders)
            {
                return Orders.FirstOrDefault((order) => order.OrderSubType == type);
            }

            if (type == TitleOrder.OrderSubType)
            {
                return TitleOrder;
            }

            Debug.FailedAssert("Couldn't find order item " + type, "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.ViewModelCollection\\Order\\OrderSetVM.cs", "GetOrder", 441);
            return null;
        }

        public void SetActiveOrder(OrderItemVM order)
        {
            if (OrderSetType != OrderSetType.Toggle)
            {
                _selectedOrder = order.OrderSubType;
                TitleOrder.OrderIconID = order.OrderSubType == OrderSubType.None ? "MultipleSelection" : order.OrderIconID;
                TitleOrder.TooltipText = order.OrderSubType == OrderSubType.None ? GameTexts.FindText("str_order_set_name", OrderSetType.ToString()).ToString() : order.TooltipText;
                SelectedOrderText = order.TooltipText;
            }
            else
            {
                order.SetActiveState(isActive: true);
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            if (ContainsOrders)
            {
                foreach (OrderItemVM order in Orders)
                {
                    order.ShortcutKey.OnFinalize();
                }
            }

            TitleOrder.ShortcutKey.OnFinalize();
            TitleOrderKey.OnFinalize();
        }
    }
}
