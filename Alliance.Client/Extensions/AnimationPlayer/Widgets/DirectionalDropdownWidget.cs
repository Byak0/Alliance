using System;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;

namespace Alliance.Client.Extensions.AnimationPlayer.Widgets
{
    public class DirectionalListPanelDropdownWidget : DirectionalDropdownWidget
    {
        private Widget _listPanelContainer;

        [Editor(false)]
        public Widget ListPanelContainer
        {
            get
            {
                return _listPanelContainer;
            }
            set
            {
                if (_listPanelContainer != value)
                {
                    _listPanelContainer = value;
                    OnPropertyChanged(value, "ListPanelContainer");
                }
            }
        }

        public DirectionalListPanelDropdownWidget(UIContext context)
            : base(context)
        {
        }

        protected override void OpenPanel()
        {
            base.OpenPanel();
            if (ListPanelContainer != null)
            {
                ListPanelContainer.IsVisible = true;
            }

            Button.IsSelected = true;
        }

        protected override void ClosePanel()
        {
            if (ListPanelContainer != null)
            {
                ListPanelContainer.IsVisible = false;
            }

            Button.IsSelected = false;
            base.ClosePanel();
        }
    }

    public class DirectionalDropdownWidget : Widget
    {
        private Action<Widget> _clickHandler;

        private Action<Widget> _listSelectionHandler;

        private Action<Widget, Widget> _listItemRemovedHandler;

        private Action<Widget, Widget> _listItemAddedHandler;

        private Vector2 _listPanelOpenPosition;

        private int _openFrameCounter;

        private bool _changedByControllerNavigation;

        private GamepadNavigationScope _navigationScope;

        private GamepadNavigationForcedScopeCollection _scopeCollection;

        private ButtonWidget _button;

        private ListPanel _listPanel;

        private int _currentSelectedIndex;

        private bool _closeNextFrame;

        private bool _isOpen;

        private bool _buttonClicked;

        private bool _updateSelectedItem = true;

        private Vector2 ListPanelPositionInsideUsableArea => ListPanel.GlobalPosition - new Vector2(EventManager.LeftUsableAreaStart, EventManager.TopUsableAreaStart);

        [Editor(false)]
        public RichTextWidget RichTextWidget { get; set; }

        [Editor(false)]
        public bool DoNotHandleDropdownListPanel { get; set; }

        [Editor(false)]
        public ButtonWidget Button
        {
            get
            {
                return _button;
            }
            set
            {
                if (_button != null)
                {
                    _button.ClickEventHandlers.Remove(_clickHandler);
                }

                _button = value;
                if (_button != null)
                {
                    _button.ClickEventHandlers.Add(_clickHandler);
                }

                RefreshSelectedItem();
            }
        }

        [Editor(false)]
        public ListPanel ListPanel
        {
            get
            {
                return _listPanel;
            }
            set
            {
                if (_listPanel != null)
                {
                    _listPanel.SelectEventHandlers.Remove(_listSelectionHandler);
                    _listPanel.ItemAddEventHandlers.Remove(_listItemAddedHandler);
                    _listPanel.ItemRemoveEventHandlers.Remove(_listItemRemovedHandler);
                }

                _listPanel = value;
                if (_listPanel != null)
                {
                    if (!DoNotHandleDropdownListPanel)
                    {
                        _listPanel.ParentWidget = EventManager.Root;
                        //_listPanel.HorizontalAlignment = HorizontalAlignment.Left;
                        //_listPanel.VerticalAlignment = VerticalAlignment.Top;
                    }

                    _listPanel.SelectEventHandlers.Add(_listSelectionHandler);
                    _listPanel.ItemAddEventHandlers.Add(_listItemAddedHandler);
                    _listPanel.ItemRemoveEventHandlers.Add(_listItemRemovedHandler);
                }

                RefreshSelectedItem();
            }
        }

        [Editor(false)]
        public int ListPanelValue
        {
            get
            {
                if (ListPanel != null)
                {
                    return ListPanel.IntValue;
                }

                return -1;
            }
            set
            {
                if (ListPanel != null && ListPanel.IntValue != value)
                {
                    ListPanel.IntValue = value;
                }
            }
        }

        [Editor(false)]
        public int CurrentSelectedIndex
        {
            get
            {
                return _currentSelectedIndex;
            }
            set
            {
                if (_currentSelectedIndex != value)
                {
                    _currentSelectedIndex = value;
                }
            }
        }

        [Editor(false)]
        public bool UpdateSelectedItem
        {
            get
            {
                return _updateSelectedItem;
            }
            set
            {
                if (_updateSelectedItem != value)
                {
                    _updateSelectedItem = value;
                }
            }
        }

        public DirectionalDropdownWidget(UIContext context)
            : base(context)
        {
            _clickHandler = OnButtonClick;
            _listSelectionHandler = OnSelectionChanged;
            _listItemRemovedHandler = OnListItemRemoved;
            _listItemAddedHandler = OnListItemAdded;
            UsedNavigationMovements = GamepadNavigationTypes.Horizontal;
        }

        protected override void OnUpdate(float dt)
        {
            base.OnUpdate(dt);
            if (!DoNotHandleDropdownListPanel)
            {
                UpdateListPanelPosition();
            }

            if (_buttonClicked)
            {
                if (ListPanel != null && !_changedByControllerNavigation)
                {
                    _isOpen = !_isOpen;
                    if (_isOpen)
                    {
                        OpenPanel();
                    }
                    else
                    {
                        ClosePanel();
                    }
                }

                _buttonClicked = false;
            }
            else if (_closeNextFrame && _isOpen)
            {
                ClosePanel();
                _isOpen = false;
                _closeNextFrame = false;
            }
            else if (EventManager.LatestMouseUpWidget != _button && _isOpen)
            {
                if (ListPanel.IsVisible)
                {
                    _closeNextFrame = true;
                }
            }
            else if (_isOpen)
            {
                _openFrameCounter++;
                if (_openFrameCounter > 5)
                {
                    if (Vector2.Distance(ListPanelPositionInsideUsableArea, _listPanelOpenPosition) > 20f && !DoNotHandleDropdownListPanel)
                    {
                        _closeNextFrame = true;
                    }
                }
                else
                {
                    _listPanelOpenPosition = ListPanelPositionInsideUsableArea;
                }
            }

            RefreshSelectedItem();
        }

        protected override void OnLateUpdate(float dt)
        {
            base.OnLateUpdate(dt);
            if (!DoNotHandleDropdownListPanel)
            {
                UpdateListPanelPosition();
            }

            UpdateGamepadNavigationControls();
        }

        private void UpdateGamepadNavigationControls()
        {
            if (_isOpen && EventManager.IsControllerActive && (Input.IsKeyPressed(InputKey.ControllerLBumper) || Input.IsKeyPressed(InputKey.ControllerLTrigger) || Input.IsKeyPressed(InputKey.ControllerRBumper) || Input.IsKeyPressed(InputKey.ControllerRTrigger)))
            {
                ClosePanel();
            }

            if (!_isOpen && (IsPressed || _button.IsPressed) && IsRecursivelyVisible() && EventManager.GetIsHitThisFrame())
            {
                if (Input.IsKeyReleased(InputKey.ControllerLLeft))
                {
                    if (CurrentSelectedIndex > 0)
                    {
                        CurrentSelectedIndex--;
                    }
                    else
                    {
                        CurrentSelectedIndex = ListPanel.ChildCount - 1;
                    }

                    RefreshSelectedItem();
                    _changedByControllerNavigation = true;
                }
                else if (Input.IsKeyReleased(InputKey.ControllerLRight))
                {
                    if (CurrentSelectedIndex < ListPanel.ChildCount - 1)
                    {
                        CurrentSelectedIndex++;
                    }
                    else
                    {
                        CurrentSelectedIndex = 0;
                    }

                    RefreshSelectedItem();
                    _changedByControllerNavigation = true;
                }

                IsUsingNavigation = true;
            }
            else
            {
                _changedByControllerNavigation = false;
                IsUsingNavigation = false;
            }
        }

        private void UpdateListPanelPosition()
        {
            //ListPanel.HorizontalAlignment = HorizontalAlignment.Left;
            //ListPanel.VerticalAlignment = VerticalAlignment.Top;
            float num = (Size.X - _listPanel.Size.X) * 0.5f;

            // Update margin depending on alignment (top for a dropdown, bottom for a dropup)
            if (ListPanel.VerticalAlignment == VerticalAlignment.Top)
            {
                ListPanel.MarginTop = (GlobalPosition.Y + Button.Size.Y - EventManager.TopUsableAreaStart) * _inverseScaleToUse;
            }
            else if (ListPanel.VerticalAlignment == VerticalAlignment.Bottom)
            {
                ListPanel.MarginBottom = (GlobalPosition.Y + Button.Size.Y - EventManager.TopUsableAreaStart) * _inverseScaleToUse;
            }
            ListPanel.MarginLeft = (GlobalPosition.X + num - EventManager.LeftUsableAreaStart) * _inverseScaleToUse;
        }

        protected virtual void OpenPanel()
        {
            ListPanel.IsVisible = true;
            _listPanelOpenPosition = ListPanelPositionInsideUsableArea;
            _openFrameCounter = 0;
            CreateGamepadNavigationScopeData();
        }

        protected virtual void ClosePanel()
        {
            ListPanel.IsVisible = false;
            _buttonClicked = false;
            _isOpen = false;
            ClearGamepadScopeData();
        }

        private void CreateGamepadNavigationScopeData()
        {
            if (_navigationScope != null)
            {
                GamepadNavigationContext.RemoveNavigationScope(_navigationScope);
            }

            _scopeCollection = new GamepadNavigationForcedScopeCollection();
            _scopeCollection.ParentWidget = ParentWidget ?? this;
            _scopeCollection.CollectionOrder = 999;
            _navigationScope = BuildGamepadNavigationScopeData();
            GamepadNavigationContext.AddNavigationScope(_navigationScope, initialize: true);
            _button.GamepadNavigationIndex = 0;
            _navigationScope.AddWidgetAtIndex(_button, 0);
            ButtonWidget button = _button;
            button.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(button.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
            for (int i = 0; i < ListPanel.Children.Count; i++)
            {
                ListPanel.Children[i].GamepadNavigationIndex = i + 1;
                _navigationScope.AddWidgetAtIndex(ListPanel.Children[i], i + 1);
                Widget widget = ListPanel.Children[i];
                widget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(widget.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
            }

            GamepadNavigationContext.AddForcedScopeCollection(_scopeCollection);
        }

        private void OnWidgetGainedNavigationFocus(Widget widget)
        {
            GetParentScrollablePanelOfWidget(widget)?.ScrollToChild(widget);
        }

        private ScrollablePanel GetParentScrollablePanelOfWidget(Widget widget)
        {
            for (Widget widget2 = widget; widget2 != null; widget2 = widget2.ParentWidget)
            {
                ScrollablePanel result;
                if ((result = widget2 as ScrollablePanel) != null)
                {
                    return result;
                }
            }

            return null;
        }

        private GamepadNavigationScope BuildGamepadNavigationScopeData()
        {
            return new GamepadNavigationScope
            {
                ScopeMovements = GamepadNavigationTypes.Vertical,
                DoNotAutomaticallyFindChildren = true,
                DoNotAutoNavigateAfterSort = true,
                HasCircularMovement = true,
                ParentWidget = ParentWidget ?? this,
                ScopeID = "DropdownScope"
            };
        }

        private void ClearGamepadScopeData()
        {
            if (_navigationScope != null)
            {
                GamepadNavigationContext.RemoveNavigationScope(_navigationScope);
                for (int i = 0; i < ListPanel.Children.Count; i++)
                {
                    ListPanel.Children[i].GamepadNavigationIndex = -1;
                    Widget widget = ListPanel.Children[i];
                    widget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(widget.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
                }

                _button.GamepadNavigationIndex = -1;
                ButtonWidget button = _button;
                button.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(button.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
                _navigationScope = null;
            }

            if (_scopeCollection != null)
            {
                GamepadNavigationContext.RemoveForcedScopeCollection(_scopeCollection);
            }
        }

        public void OnButtonClick(Widget widget)
        {
            _buttonClicked = true;
            _closeNextFrame = false;
        }

        public void UpdateButtonText(string text)
        {
            if (RichTextWidget != null)
            {
                if (text != null)
                {
                    RichTextWidget.Text = text;
                }
                else
                {
                    RichTextWidget.Text = " ";
                }
            }
        }

        public void OnListItemAdded(Widget parentWidget, Widget newChild)
        {
            RefreshSelectedItem();
        }

        public void OnListItemRemoved(Widget removedItem, Widget removedChild)
        {
            RefreshSelectedItem();
        }

        public void OnSelectionChanged(Widget widget)
        {
            if (UpdateSelectedItem)
            {
                CurrentSelectedIndex = ListPanelValue;
                RefreshSelectedItem();
                OnPropertyChanged(CurrentSelectedIndex, "CurrentSelectedIndex");
            }
        }

        private void RefreshSelectedItem()
        {
            if (!UpdateSelectedItem)
            {
                return;
            }

            string text = "";

            ListPanelValue = CurrentSelectedIndex;
            if (ListPanelValue >= 0)
            {
                if (ListPanel != null)
                {
                    Widget child = ListPanel.GetChild(ListPanelValue);
                    if (child != null)
                    {
                        foreach (Widget allChild in child.AllChildren)
                        {
                            RichTextWidget richTextWidget;
                            if ((richTextWidget = allChild as RichTextWidget) != null)
                            {
                                text = richTextWidget.Text;
                            }
                        }
                    }
                }
            }

            UpdateButtonText(text);

            if (ListPanel == null)
            {
                return;
            }

            for (int i = 0; i < ListPanel.ChildCount; i++)
            {
                Widget child2 = ListPanel.GetChild(i);
                if (child2 is ButtonWidget)
                {
                    (child2 as ButtonWidget).IsSelected = CurrentSelectedIndex == i;
                }
            }
        }
    }
}


