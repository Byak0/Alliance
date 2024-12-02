using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.ViewModels
{
    public class OrderItemVM : ViewModel
    {
        public enum OrderSelectionState
        {
            Disabled,
            Default,
            PartiallyActive,
            Active
        }

        public Action<OrderItemVM, bool> OnExecuteAction;

        public bool IsTitle;

        private bool _isActivationOrder;

        private bool _isToggleActivationOrder;

        private bool _isActive;

        private bool _isSelected;

        private bool _canUseShortcuts;

        private int _selectionState = -1;

        private string _tooltipText;

        private InputKeyItemVM _shortcutKey;

        private string _orderIconID = "";

        internal OrderSubType OrderSubType { get; private set; }

        internal OrderSetType OrderSetType { get; private set; }

        [DataSourceProperty]
        public string OrderIconID
        {
            get
            {
                return _orderIconID;
            }
            set
            {
                if (value != _orderIconID)
                {
                    _orderIconID = value;
                    OnPropertyChangedWithValue(value, "OrderIconID");
                }
            }
        }

        [DataSourceProperty]
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                value = value && _selectionState != 0;
                _isActive = value;
                OnPropertyChangedWithValue(value, "IsActive");
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                    if (value)
                    {
                        OnExecuteAction(this, arg2: true);
                    }
                }
            }
        }

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
                    OnPropertyChanged("CanUseShortcuts");
                }
            }
        }

        [DataSourceProperty]
        public int SelectionState
        {
            get
            {
                return _selectionState;
            }
            set
            {
                if (value != _selectionState)
                {
                    _selectionState = value;
                    OnPropertyChangedWithValue(value, "SelectionState");
                }
            }
        }

        [DataSourceProperty]
        public InputKeyItemVM ShortcutKey
        {
            get
            {
                return _shortcutKey;
            }
            set
            {
                if (value != _shortcutKey)
                {
                    _shortcutKey = value;
                    OnPropertyChangedWithValue(value, "ShortcutKey");
                }
            }
        }

        [DataSourceProperty]
        public string TooltipText
        {
            get
            {
                return _tooltipText;
            }
            set
            {
                if (value != _tooltipText)
                {
                    _tooltipText = value;
                    OnPropertyChangedWithValue(value, "TooltipText");
                }
            }
        }

        public OrderItemVM(OrderSubType orderSubType, OrderSetType orderSetType, TextObject tooltipText, Action<OrderItemVM, bool> onExecuteAction)
        {
            OrderSetType = orderSetType;
            OrderSubType = orderSubType;
            OrderIconID = GetIconIDFromSubType(OrderSubType);
            if (orderSetType == OrderSetType.Toggle)
            {
                OrderIconID += "Active";
            }

            OnExecuteAction = onExecuteAction;
            TooltipText = tooltipText.ToString();
            IsActive = true;
            _isActivationOrder = IsTitle && OrderSetType == OrderSetType.None;
            _isToggleActivationOrder = OrderSubType > OrderSubType.ToggleStart && OrderSubType < OrderSubType.ToggleEnd;
        }

        public OrderItemVM(OrderSetType orderSetType, TextObject tooltipText, Action<OrderItemVM, bool> onExecuteAction)
        {
            OrderSubType = OrderSubType.None;
            IsTitle = true;
            OrderSetType = orderSetType;
            OrderIconID = orderSetType.ToString();
            OnExecuteAction = onExecuteAction;
            TooltipText = tooltipText.ToString();
            IsActive = true;
        }

        public void SetActiveState(bool isActive)
        {
            if (OrderSetType == OrderSetType.Toggle && !IsTitle || _isToggleActivationOrder)
            {
                TooltipText = GameTexts.FindText(isActive ? "str_order_name_on" : "str_order_name_off", OrderSubType.ToString()).ToString();
            }

            if (_isToggleActivationOrder)
            {
                OrderIconID = isActive ? OrderSubType.ToString() : OrderSubType.ToString() + "Active";
            }

            IsActive = true;
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            ShortcutKey.OnFinalize();
        }

        public void ExecuteAction()
        {
            OnExecuteAction(this, arg2: false);
        }

        public void FinalizeActiveStatus()
        {
            OnPropertyChanged("SelectionState");
        }

        private string GetIconIDFromSubType(OrderSubType orderSubType)
        {
            string empty = string.Empty;
            return orderSubType switch
            {
                OrderSubType.ActivationFaceDirection => OrderSubType.ToggleFacing.ToString(),
                OrderSubType.FaceEnemy => OrderSubType.ToggleFacing.ToString() + "Active",
                _ => orderSubType.ToString(),
            };
        }
    }
}
