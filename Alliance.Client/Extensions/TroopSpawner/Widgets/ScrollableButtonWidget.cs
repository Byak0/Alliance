using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using static Alliance.Common.Utilities.Logger;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    /// <summary>
    /// Custom button widget.
    /// Several of them can be put in a container to automatically unselect the others when one is selected.
    /// It also has an editable IntTextWidget IntegerInputTextWidget.
    /// Editing the IntTextWidget will trigger the button.
    /// The IntTextWidget can also be edited with scroll up/down.
    /// </summary>
    public class ScrollableButtonWidget : ButtonWidget
    {
        private bool _allowSwitchOff;
        private bool _notifyParentForSelection = true;
        private int _soldierIcons = 0;
        private int _intText = 0;
        private bool _editableText;

        private Widget _soldierIconsContainer;
        private PvCIntegerInputWidget _intTextWidget;
        private BrushWidget _sortUp;
        private BrushWidget _sortDown;

        [Editor(false)]
        public bool AllowSwitchOff
        {
            get
            {
                return _allowSwitchOff;
            }
            set
            {
                if (_allowSwitchOff != value)
                {
                    _allowSwitchOff = value;
                    OnPropertyChanged(value, "AllowSwitchOff");
                }
            }
        }

        [Editor(false)]
        public bool NotifyParentForSelection
        {
            get
            {
                return _notifyParentForSelection;
            }
            set
            {
                if (_notifyParentForSelection != value)
                {
                    _notifyParentForSelection = value;
                    OnPropertyChanged(value, "NotifyParentForSelection");
                }
            }
        }

        [Editor(false)]
        public int SoldierIcons
        {
            get
            {
                return _soldierIcons;
            }
            set
            {
                if (_soldierIcons != value)
                {
                    _soldierIcons = value;
                    RefreshSoldierIcons();
                }
            }
        }

        [Editor(false)]
        public int IntText
        {
            get
            {
                return _intText;
            }
            set
            {
                if (_intText != value)
                {
                    _intText = value;
                    _intTextWidget.IntText = MBMath.ClampInt(value, _intTextWidget.MinInt, _intTextWidget.MaxInt);
                    SoldierIcons = TroopCountToSoldierIcons(value);
                    OnPropertyChanged(value, "IntText");
                    Log("IntText = " + value);
                }
            }
        }

        [Editor(false)]
        public bool EditableText
        {
            get
            {
                return _editableText;
            }
            set
            {
                if (_editableText != value)
                {
                    _editableText = value;
                    InitEditState();
                }
            }
        }

        public ScrollableButtonWidget(UIContext context) : base(context)
        {
            _intTextWidget = new PvCIntegerInputWidget(context);
            _intTextWidget.IntText = _intText;

            UpdateChildrenStates = true;

            _soldierIconsContainer = new Widget(context);
            _soldierIconsContainer.Init(SP.StretchToParent, SP.StretchToParent,
                hAlign: HAlign.Center, vAlign: VAlign.Center, marginLeft: 28);
            _soldierIconsContainer.UpdateChildrenStates = true;
            AddChild(_soldierIconsContainer);
            RefreshSoldierIcons();

            _intTextWidget.Init(width: SP.StretchToParent, height: SP.StretchToParent,
                hAlign: HAlign.Right, vAlign: VAlign.Center, marginLeft: 20, brush: Context.GetBrush("Popup.Button.Text"));
            _intTextWidget.DoNotPassEventsToChildren = true;
            AddChild(_intTextWidget);
            InitEditState();
        }

        private void InitEditState()
        {
            if (EditableText)
            {
                DoNotPassEventsToChildren = false;
                RemoveSortIcons();
                AddSortIcons();
                _intTextWidget.EventFire -= OnMouseScroll;
                _intTextWidget.EventFire += OnMouseScroll;
                _intTextWidget.EventFire -= OnMouseClick;
                _intTextWidget.EventFire += OnMouseClick;
                _intTextWidget.EventFire -= OnTextValidate;
                _intTextWidget.EventFire += OnTextValidate;
                _intTextWidget.EventFire -= OnHover;
                _intTextWidget.EventFire += OnHover;
            }
            else
            {
                DoNotPassEventsToChildren = true;
            }
        }

        private void RemoveSortIcons()
        {
            if (_sortDown != null) RemoveChild(_sortDown);
            if (_sortUp != null) RemoveChild(_sortUp);
        }

        private void AddSortIcons()
        {
            _sortUp = new BrushWidget(Context);
            _sortUp.Init(SP.Fixed, SP.Fixed, 10, 11, HAlign.Right, VAlign.Center, marginLeft: 74, brush: Context.GetBrush("Alliance.SortArrowUp"));
            _sortUp.PositionXOffset = -18;
            _sortUp.PositionYOffset = -7;

            _sortDown = new BrushWidget(Context);
            _sortDown.Init(SP.Fixed, SP.Fixed, 10, 11, HAlign.Right, VAlign.Center, marginLeft: 74, brush: Context.GetBrush("Alliance.SortArrowDown"));
            _sortDown.PositionXOffset = -18;
            _sortDown.PositionYOffset = 7;

            AddChild(_sortUp);
            AddChild(_sortDown);
        }

        public void RefreshSoldierIcons()
        {
            _soldierIconsContainer.RemoveAllChildren();

            switch (SoldierIcons)
            {
                case 0:
                    break;
                case 1:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(25, 30, -10, 2, 5));
                    break;
                case 2:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(25, 30, -10, 2, 2));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(25, 30, -20, -2, 2));
                    break;
                case 3:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(25, 30, -5, -2, 2));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(25, 30, -15, 2, 2));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(25, 30, -25, -2, 2));
                    break;
                case 4:
                    break;
                case 5:
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(15, 20, 0, -6));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(15, 20, -5, -3));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(15, 20, -10, 3));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(15, 20, -15, -3));
                    _soldierIconsContainer.AddChild(BuildSoldierIcon(15, 20, -20, -6));
                    break;
            }
        }

        private BrushWidget BuildSoldierIcon(float width, float height, float xOffset, float yOffset, float marginBot = 0)
        {
            BrushWidget soldierIcon = new BrushWidget(Context);
            soldierIcon.SetState(_soldierIconsContainer.CurrentState);
            soldierIcon.Init(width: SP.Fixed, height: SP.Fixed, suggestedWidth: width, suggestedHeight: height,
                hAlign: HAlign.Left, vAlign: VAlign.Center, marginLeft: 8, marginBottom: marginBot,
                brush: Context.GetBrush("Alliance.Soldiers"));
            soldierIcon.PositionXOffset = xOffset;
            soldierIcon.PositionYOffset = yOffset;
            return soldierIcon;
        }

        // Returns the number of icons wanted for the specified troop count on the TroopCountButton
        private int TroopCountToSoldierIcons(int troopCount)
        {
            int soldierIcons;
            if (troopCount >= 100) soldierIcons = 5;
            else if (troopCount >= 50) soldierIcons = 3;
            else if (troopCount >= 10) soldierIcons = 2;
            else if (troopCount >= 1) soldierIcons = 1;
            else soldierIcons = 0;
            return soldierIcons;
        }

        private void OnMouseScroll(Widget widget, string eventName, object[] args)
        {
            // Edit mode - Using both "MouseMove" event && Input scroll checks to get a "working" scroll event
            if (eventName == "MouseMove" && (Input.IsKeyDown(InputKey.MouseScrollUp) || Input.IsKeyDown(InputKey.MouseScrollDown)))
            {
                int newInt = _intTextWidget.IntText + (int)Math.Round(EventManager.DeltaMouseScroll * 0.02f);
                _intTextWidget.IntText = MBMath.ClampInt(newInt, _intTextWidget.MinInt, _intTextWidget.MaxInt);
                IntText = _intTextWidget.IntText;
                // On scroll, also fire a click to simulate button selection
                HandleClick();
            }
        }

        private void OnMouseClick(Widget widget, string eventName, object[] args)
        {
            // Edit mode - Simulate click on button when clicking on text or pressing Enter
            if (eventName == "TextEntered" || eventName == "MouseMove" && Input.IsKeyReleased(InputKey.LeftMouseButton))
            {
                HandleClick();
            }
        }

        private void OnTextValidate(Widget widget, string eventName, object[] args)
        {
            // Add text validation on numpad Enter or focus lost
            if (Input.IsKeyReleased(InputKey.NumpadEnter) || eventName == "FocusLost")
            {
                if (!IsSelected) HandleClick();
                IntText = _intTextWidget.IntText;
            }
        }

        private void OnHover(Widget widget, string eventName, object[] args)
        {
            // Edit mode - Hover on button when hovering text
            if (eventName == "HoverBegin")
            {
                OnHoverBegin();
            }
            else if (eventName == "HoverEnd")
            {
                OnHoverEnd();
            }
        }

        protected override void HandleClick()
        {
            foreach (Action<Widget> action in ClickEventHandlers)
            {
                action(this);
            }

            bool wasSelected = IsSelected;
            if (!IsSelected)
            {
                IsSelected = true;
            }
            else if (AllowSwitchOff)
            {
                IsSelected = false;
            }

            if (IsSelected && !wasSelected && NotifyParentForSelection && ParentWidget is Container)
            {
                (ParentWidget as Container).OnChildSelected(this);
            }
            if (AllowSwitchOff && !IsSelected && NotifyParentForSelection && ParentWidget is Container)
            {
                (ParentWidget as Container).OnChildSelected(null);
            }
            OnClick();
            EventFired("Click", Array.Empty<object>());
            if (Context.EventManager.Time - _lastClickTime < 0.5f)
            {
                EventFired("DoubleClick", Array.Empty<object>());
                return;
            }
            _lastClickTime = Context.EventManager.Time;
        }
    }

    public class PvCIntegerInputWidget : IntegerInputTextWidget
    {
        public PvCIntegerInputWidget(UIContext context) : base(context)
        {
            MinInt = 0;
            MaxInt = SpawnHelper.MaxBotsPerSpawn;
        }

        protected override void OnLoseFocus()
        {
            if (!Input.IsKeyReleased(InputKey.Escape))
            {
                EventFired("TextEntered", Array.Empty<object>());
                EventFired("Click", Array.Empty<object>());
            }

            base.OnLoseFocus();
        }
    }
}
