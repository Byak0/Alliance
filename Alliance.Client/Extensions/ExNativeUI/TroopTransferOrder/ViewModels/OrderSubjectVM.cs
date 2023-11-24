using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace Alliance.Client.Extensions.ExNativeUI.TroopTransferOrder.ViewModels
{
    public class OrderSubjectVM : ViewModel
    {
        internal List<OrderItemVM> ActiveOrders;

        private int _behaviorType;

        private int _underAttackOfType;

        private bool _isSelectable;

        private bool _isSelected;

        private bool _isSelectionActive;

        private string _shortcutText;

        private InputKeyItemVM _selectionKey;

        private bool _isGamepadActive
        {
            get
            {
                if (TaleWorlds.InputSystem.Input.IsControllerConnected)
                {
                    return !TaleWorlds.InputSystem.Input.IsMouseActive;
                }

                return false;
            }
        }

        [DataSourceProperty]
        public bool IsSelectable
        {
            get
            {
                return _isSelectable;
            }
            set
            {
                if (value != _isSelectable)
                {
                    _isSelectable = value;
                    OnPropertyChangedWithValue(value, "IsSelectable");
                }
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
                if ((!value || IsSelectable) && value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelectionActive
        {
            get
            {
                return _isSelectionActive;
            }
            set
            {
                if (value != _isSelectionActive)
                {
                    _isSelectionActive = value;
                    OnPropertyChangedWithValue(value, "IsSelectionActive");
                }
            }
        }

        [DataSourceProperty]
        public int BehaviorType
        {
            get
            {
                return _behaviorType;
            }
            set
            {
                if (value != _behaviorType)
                {
                    _behaviorType = value;
                    OnPropertyChangedWithValue(value, "BehaviorType");
                }
            }
        }

        [DataSourceProperty]
        public int UnderAttackOfType
        {
            get
            {
                return _underAttackOfType;
            }
            set
            {
                if (value != _underAttackOfType)
                {
                    _underAttackOfType = value;
                    OnPropertyChangedWithValue(value, "UnderAttackOfType");
                }
            }
        }

        [DataSourceProperty]
        public string ShortcutText
        {
            get
            {
                return _shortcutText;
            }
            set
            {
                if (value != _shortcutText)
                {
                    _shortcutText = value;
                    OnPropertyChangedWithValue(value, "ShortcutText");
                }
            }
        }

        [DataSourceProperty]
        public InputKeyItemVM SelectionKey
        {
            get
            {
                return _selectionKey;
            }
            set
            {
                if (value != _selectionKey)
                {
                    _selectionKey = value;
                    OnPropertyChangedWithValue(value, "SelectionKey");
                }
            }
        }

        public OrderSubjectVM()
        {
            ActiveOrders = new List<OrderItemVM>();
        }
    }
}
