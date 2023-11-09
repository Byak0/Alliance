using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using HAlign = TaleWorlds.GauntletUI.HorizontalAlignment;
using SP = TaleWorlds.GauntletUI.SizePolicy;
using VAlign = TaleWorlds.GauntletUI.VerticalAlignment;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    /*
     * Custom button widget
     * Several of them can be put in a container to automatically unselect the others when one is selected
     * It also has an editable IntTextWidget IntegerInputTextWidget
     * Editing the IntTextWidget will trigger the button
     * The IntTextWidget can also be edited with scroll up/down
     */
    public class ScrollableButtonWidget : ButtonWidget
    {
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

        public PvCIntegerInputWidget IntTextWidget
        {
            get
            {
                return _intTextWidget;
            }
            set
            {
                if (_intTextWidget != value)
                {
                    _intTextWidget = value;
                    OnPropertyChanged(value, "IntTextWidget");
                }
            }
        }

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

        public int ButtonId = 0;
        public bool EditableText;

        private bool _allowSwitchOff = false;
        private bool _notifyParentForSelection = true;
        private int _soldierIcons = 0;

        private PvCIntegerInputWidget _intTextWidget;
        private Widget _soldierIconsContainer;

        public ScrollableButtonWidget(UIContext context, bool editableText = false) : base(context)
        {
            IntTextWidget = new PvCIntegerInputWidget(context);

            EditableText = editableText;

            if (editableText)
            {
                DoNotPassEventsToChildren = false;
                AddSortIcons();
                IntTextWidget.EventFire += OnMouseScroll;
                IntTextWidget.EventFire += OnMouseClick;
                //IntTextWidget.EventFire += OnTextValidate;
                IntTextWidget.EventFire += OnHover;
            }
            else
            {
                DoNotPassEventsToChildren = true;
            }

            UpdateChildrenStates = true;

            _soldierIconsContainer = new Widget(context);
            _soldierIconsContainer.Init(SP.StretchToParent, SP.StretchToParent,
                hAlign: HAlign.Center, vAlign: VAlign.Center, marginLeft: 28);
            _soldierIconsContainer.UpdateChildrenStates = true;
            AddChild(_soldierIconsContainer);

            IntTextWidget.Init(width: SP.StretchToParent, height: SP.StretchToParent,
                hAlign: HAlign.Right, vAlign: VAlign.Center, marginLeft: 20, brush: Context.GetBrush("Popup.Button.Text"));
            IntTextWidget.DoNotPassEventsToChildren = true;
            AddChild(IntTextWidget);
        }

        public void AddSortIcons()
        {
            SortButtonWidget sortUp = new SortButtonWidget(Context);
            sortUp.Init(SP.CoverChildren, SP.CoverChildren, hAlign: HAlign.Right, vAlign: VAlign.Center, marginLeft: 74);
            sortUp.SortState = 1;
            sortUp.DoNotPassEventsToChildren = true;
            sortUp.UpdateChildrenStates = true;

            BrushWidget sortVisualUp = new BrushWidget(Context);
            sortVisualUp.Init(SP.Fixed, SP.Fixed, 10, 11, HAlign.Right, VAlign.Center, brush: Context.GetBrush("MPLobby.Clan.Sort.ArrowBrush"));
            sortVisualUp.PositionXOffset = -18;
            sortVisualUp.PositionYOffset = -7;
            sortUp.SortVisualWidget = sortVisualUp;
            sortUp.AddChild(sortVisualUp);

            SortButtonWidget sortDown = new SortButtonWidget(Context);
            sortDown.Init(SP.CoverChildren, SP.CoverChildren, hAlign: HAlign.Right, vAlign: VAlign.Center, marginLeft: 74);
            sortDown.SortState = 2;
            sortDown.DoNotPassEventsToChildren = true;
            sortDown.UpdateChildrenStates = true;

            BrushWidget sortVisualDown = new BrushWidget(Context);
            sortVisualDown.Init(SP.Fixed, SP.Fixed, 10, 11, HAlign.Right, VAlign.Center, brush: Context.GetBrush("MPLobby.Clan.Sort.ArrowBrush"));
            sortVisualDown.PositionXOffset = -18;
            sortVisualDown.PositionYOffset = 7;
            sortDown.SortVisualWidget = sortVisualDown;
            sortDown.AddChild(sortVisualDown);

            AddChild(sortUp);
            AddChild(sortDown);
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

        private BrushWidget BuildSoldierIcon(float width, float height, float xOffset, float yOffset, float marginBot = 0, string previousState = "")
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

        private void OnMouseScroll(Widget widget, string eventName, object[] args)
        {
            // Edit mode - Using both "MouseMove" event && Input scroll checks to get a "working" scroll event
            if (eventName == "MouseMove" && (Input.IsKeyDown(InputKey.MouseScrollUp) || Input.IsKeyDown(InputKey.MouseScrollDown)))
            {
                int newInt = IntTextWidget.IntText + (int)Math.Round(EventManager.DeltaMouseScroll * 0.02f);
                IntTextWidget.IntText = MBMath.ClampInt(newInt, IntTextWidget.MinInt, IntTextWidget.MaxInt);
                // On scroll, also fire a click to simulate button selection
                if (!IsSelected) HandleClick();
                else EventFired("OnTroopCountSelected", Array.Empty<object>());
            }
        }

        private void OnMouseClick(Widget widget, string eventName, object[] args)
        {
            // Edit mode - Simulate click on button when clicking on text or pressing Enter
            if (eventName == "TextEntered" || eventName == "MouseMove" && Input.IsKeyReleased(InputKey.LeftMouseButton))
            {
                if (!IsSelected) HandleClick();
                else EventFired("OnTroopCountSelected", Array.Empty<object>());
            }
        }

        /*private void OnTextValidate(Widget widget, string eventName, object[] args)
        {
            // Add text validation on numpad Enter or focus lost
            if (Input.IsKeyReleased(InputKey.NumpadEnter) || eventName == "FocusLost")
            {
                if (!IsSelected) HandleClick();
                else EventFired("OnTroopCountSelected", Array.Empty<object>());
            }
        }*/

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
            EventFired("OnTroopCountSelected", Array.Empty<object>());
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
        }

        protected override void OnLoseFocus()
        {
            if (!Input.IsKeyReleased(InputKey.Escape))
            {
                EventFired("TextEntered", Array.Empty<object>());
            }

            base.OnLoseFocus();
        }
    }
}
