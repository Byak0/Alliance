using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.Order;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order;
using ListChangedEventArgs = TaleWorlds.Library.ListChangedEventArgs;
using ListChangedType = TaleWorlds.Library.ListChangedType;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    public class FormationCardWidget : OrderTroopItemBrushWidget
    {
        public FormationCardWidget(UIContext context)
            : base(context)
        {
        }

        public virtual void CreateWidgets()
        {
            _rootWidget = this;
            _baseGridWidget = new GridWidget(Context);
            _rootWidget.AddChild(_baseGridWidget);
            _moraleFillBar = new FillBar(Context);
            _rootWidget.AddChild(_moraleFillBar);
            _listPanel = new ListPanel(Context);
            _rootWidget.AddChild(_listPanel);
            _moraleIcon = new Widget(Context);
            _listPanel.AddChild(_moraleIcon);
            _troopCount = new TextWidget(Context);
            _listPanel.AddChild(_troopCount);
            _toprightContainer = new Widget(Context);
            _rootWidget.AddChild(_toprightContainer);
            _shortcutContainer = new Widget(Context);
            _toprightContainer.AddChild(_shortcutContainer);
            _shortcutInput = new InputKeyVisualWidget(Context);
            _shortcutContainer.AddChild(_shortcutInput);
            _ammoPercentage = new SliderWidget(Context);
            _rootWidget.AddChild(_ammoPercentage);
            _filler = new Widget(Context);
            _ammoPercentage.AddChild(_filler);
            _sliderHandle = new Widget(Context);
            _ammoPercentage.AddChild(_sliderHandle);
            _selectionFrame = new Widget(Context);
            _rootWidget.AddChild(_selectionFrame);
            _commanderImage = new ImageIdentifierWidget(Context);
            _rootWidget.AddChild(_commanderImage);
            _troopTypeList = new GridWidget(Context);
            _rootWidget.AddChild(_troopTypeList);
        }

        public virtual void SetIds()
        {
            _baseGridWidget.Id = "PrimaryColorGrid";
            _filler.Id = "Filler";
            _sliderHandle.Id = "SliderHandle";
            _selectionFrame.Id = "SelectionFrame";
        }

        public virtual void SetAttributes()
        {
            WidthSizePolicy = SizePolicy.Fixed;
            HeightSizePolicy = SizePolicy.Fixed;
            SuggestedWidth = 116.8f;
            SuggestedHeight = 198.4f;
            RangedCardBrush = Context.GetBrush("Order.Card.Background.Ranged");
            MeleeCardBrush = Context.GetBrush("Order.Card.Background.Melee");
            Brush = Context.GetBrush("Order.Card.Background");
            MarginTop = 25f;
            SelectionFrameWidget = _selectionFrame;
            _baseGridWidget.WidthSizePolicy = SizePolicy.CoverChildren;
            _baseGridWidget.HeightSizePolicy = SizePolicy.CoverChildren;
            _baseGridWidget.HorizontalAlignment = HorizontalAlignment.Center;
            _baseGridWidget.VerticalAlignment = VerticalAlignment.Top;
            _baseGridWidget.MarginTop = 50f;
            _baseGridWidget.ColumnCount = 2;
            _baseGridWidget.DefaultCellHeight = 46f;
            _baseGridWidget.DefaultCellWidth = 46f;
            _moraleFillBar.WidthSizePolicy = SizePolicy.StretchToParent;
            _moraleFillBar.HeightSizePolicy = SizePolicy.Fixed;
            _moraleFillBar.SuggestedHeight = 13f;
            _moraleFillBar.VerticalAlignment = VerticalAlignment.Bottom;
            _moraleFillBar.MarginLeft = 16f;
            _moraleFillBar.MarginRight = 16f;
            _moraleFillBar.MarginBottom = 18f;
            _moraleFillBar.MaxAmount = 100;
            _moraleFillBar.Brush = Context.GetBrush("Order.Troop.MoraleFillBar");
            _moraleFillBar.IsVisible = false;
            _listPanel.WidthSizePolicy = SizePolicy.CoverChildren;
            _listPanel.HeightSizePolicy = SizePolicy.CoverChildren;
            _listPanel.MarginLeft = 15f;
            _listPanel.MarginTop = 10f;
            _moraleIcon.WidthSizePolicy = SizePolicy.Fixed;
            _moraleIcon.HeightSizePolicy = SizePolicy.Fixed;
            _moraleIcon.SuggestedWidth = 30f;
            _moraleIcon.SuggestedHeight = 30f;
            _moraleIcon.VerticalAlignment = VerticalAlignment.Center;
            _moraleIcon.Sprite = Context.SpriteData.GetSprite("General\\Icons\\Morale@2x");
            _troopCount.WidthSizePolicy = SizePolicy.CoverChildren;
            _troopCount.HeightSizePolicy = SizePolicy.CoverChildren;
            _troopCount.MarginLeft = 1f;
            _troopCount.MarginTop = 8f;
            _troopCount.VerticalAlignment = VerticalAlignment.Center;
            _troopCount.Brush = Context.GetBrush("Order.Troop.CountText");
            _troopCount.Brush.FontSize = 20;
            _toprightContainer.WidthSizePolicy = SizePolicy.CoverChildren;
            _toprightContainer.HeightSizePolicy = SizePolicy.CoverChildren;
            _toprightContainer.HorizontalAlignment = HorizontalAlignment.Right;
            _toprightContainer.MarginTop = 5f;
            _toprightContainer.PositionXOffset = 15f;
            _shortcutContainer.WidthSizePolicy = SizePolicy.CoverChildren;
            _shortcutContainer.HeightSizePolicy = SizePolicy.CoverChildren;
            _shortcutInput.WidthSizePolicy = SizePolicy.Fixed;
            _shortcutInput.HeightSizePolicy = SizePolicy.Fixed;
            _shortcutInput.SuggestedWidth = 60f;
            _shortcutInput.SuggestedHeight = 60f;
            _ammoPercentage.WidthSizePolicy = SizePolicy.Fixed;
            _ammoPercentage.HeightSizePolicy = SizePolicy.Fixed;
            _ammoPercentage.SuggestedHeight = 3f;
            _ammoPercentage.SuggestedWidth = 65f;
            _ammoPercentage.HorizontalAlignment = HorizontalAlignment.Center;
            _ammoPercentage.VerticalAlignment = VerticalAlignment.Bottom;
            _ammoPercentage.MarginBottom = 13f;
            _ammoPercentage.MarginLeft = 3f;
            _ammoPercentage.DoNotUpdateHandleSize = true;
            _ammoPercentage.Filler = _filler;
            _ammoPercentage.Handle = _sliderHandle;
            _ammoPercentage.MaxValueFloat = 1f;
            _ammoPercentage.MinValueFloat = 0f;
            _ammoPercentage.AlignmentAxis = AlignmentAxis.Horizontal;
            _filler.DoNotAcceptEvents = true;
            _filler.WidthSizePolicy = SizePolicy.Fixed;
            _filler.HeightSizePolicy = SizePolicy.Fixed;
            _filler.SuggestedHeight = 3f;
            _filler.SuggestedWidth = 100f;
            _filler.VerticalAlignment = VerticalAlignment.Bottom;
            _filler.Sprite = Context.SpriteData.GetSprite("BlankWhiteSquare_9");
            _sliderHandle.WidthSizePolicy = SizePolicy.Fixed;
            _sliderHandle.HeightSizePolicy = SizePolicy.Fixed;
            _sliderHandle.SuggestedWidth = 2f;
            _sliderHandle.SuggestedHeight = 2f;
            _sliderHandle.HorizontalAlignment = HorizontalAlignment.Left;
            _sliderHandle.VerticalAlignment = VerticalAlignment.Center;
            _sliderHandle.IsVisible = false;
            _selectionFrame.WidthSizePolicy = SizePolicy.StretchToParent;
            _selectionFrame.HeightSizePolicy = SizePolicy.StretchToParent;
            _selectionFrame.Sprite = Context.SpriteData.GetSprite("leader_frame_9");
            _selectionFrame.Color = new Color(1f, 0f, 1f, 1f);
            _selectionFrame.IsVisible = false;
            _commanderImage.DoNotAcceptEvents = true;
            _commanderImage.WidthSizePolicy = SizePolicy.Fixed;
            _commanderImage.HeightSizePolicy = SizePolicy.Fixed;
            _commanderImage.SuggestedWidth = 31f;
            _commanderImage.SuggestedHeight = 22f;
            _commanderImage.HorizontalAlignment = HorizontalAlignment.Center;
            _commanderImage.VerticalAlignment = VerticalAlignment.Top;
            _commanderImage.PositionYOffset = -20f;
            _troopTypeList.WidthSizePolicy = SizePolicy.CoverChildren;
            _troopTypeList.HeightSizePolicy = SizePolicy.CoverChildren;
            _troopTypeList.HorizontalAlignment = HorizontalAlignment.Left;
            _troopTypeList.VerticalAlignment = VerticalAlignment.Bottom;
            _troopTypeList.MarginBottom = 37f;
            _troopTypeList.MarginLeft = 17f;
            _troopTypeList.ColumnCount = 2;
            _troopTypeList.DefaultCellHeight = 20f;
            _troopTypeList.DefaultCellWidth = 20f;
        }

        public virtual void DestroyDataSource()
        {
            if (_datasource != null)
            {
                _datasource.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _rootWidget.PropertyChanged -= PropertyChangedListenerOf_widget;
                _moraleFillBar.PropertyChanged -= PropertyChangedListenerOf_widget_1;
                _troopCount.PropertyChanged -= PropertyChangedListenerOf_widget_2_1;
                _toprightContainer.PropertyChanged -= PropertyChangedListenerOf_widget_3;
                _shortcutContainer.PropertyChanged -= PropertyChangedListenerOf_widget_3_0;
                _ammoPercentage.PropertyChanged -= PropertyChangedListenerOf_widget_4;
                if (_datasourceActiveFormationClasses != null)
                {
                    _datasourceActiveFormationClasses.ListChanged -= OnList_datasource_Root_ActiveFormationClassesChanged;
                    for (int i = _baseGridWidget.ChildCount - 1; i >= 0; i--)
                    {
                        Widget child = _baseGridWidget.GetChild(i);
                        ((CustomOrderFormationClassVisualBrushWidget)child).OnBeforeRemovedChild(child);
                        ((CustomOrderFormationClassVisualBrushWidget)_baseGridWidget.GetChild(i)).DestroyDataSource();
                    }
                    _datasourceActiveFormationClasses = null;
                }
                if (_datasourceSelectionKey != null)
                {
                    _datasourceSelectionKey.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root_SelectionKey;
                    _datasourceSelectionKey.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root_SelectionKey;
                    _shortcutInput.PropertyChanged -= PropertyChangedListenerOf_widget_3_0_0;
                    _datasourceSelectionKey = null;
                }
                if (_datasourceCommanderImageIdentifier != null)
                {
                    _datasourceCommanderImageIdentifier.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root_CommanderImageIdentifier;
                    _datasourceCommanderImageIdentifier.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root_CommanderImageIdentifier;
                    _commanderImage.PropertyChanged -= PropertyChangedListenerOf_widget_6;
                    _datasourceCommanderImageIdentifier = null;
                }
                if (_datasourceActiveFilters != null)
                {
                    _datasourceActiveFilters.ListChanged -= OnListActiveFiltersChanged;
                    for (int j = _troopTypeList.ChildCount - 1; j >= 0; j--)
                    {
                        Widget child2 = _troopTypeList.GetChild(j);
                        ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child2).OnBeforeRemovedChild(child2);
                        ((CustomOrderOfBattleFormationFilterVisualBrushWidget)_troopTypeList.GetChild(j)).DestroyDataSource();
                    }
                    _datasourceActiveFilters = null;
                }
                _datasource = null;
            }
        }

        public virtual void SetDataSource(OrderTroopItemVM dataSource)
        {
            RefreshDataSource(dataSource);
        }

        private void PropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "HasAmmo")
            {
                _datasource.IsAmmoAvailable = _rootWidget.HasAmmo;
                return;
            }
            if (propertyName == "CurrentMemberCount")
            {
                _datasource.CurrentMemberCount = _rootWidget.CurrentMemberCount;
                return;
            }
            if (propertyName == "IsSelectable")
            {
                _datasource.IsSelectable = _rootWidget.IsSelectable;
                return;
            }
            if (propertyName == "IsSelected")
            {
                _datasource.IsSelected = _rootWidget.IsSelected;
                return;
            }
            if (propertyName == "FormationClass")
            {
                return;
            }
            if (propertyName == "IsSelectionActive")
            {
                _datasource.IsSelectionActive = _rootWidget.IsSelectionActive;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "CurrentAmount")
            {
                _datasource.Morale = _moraleFillBar.CurrentAmount;
                return;
            }
            if (propertyName == "InitialAmount")
            {
                _datasource.Morale = _moraleFillBar.InitialAmount;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_2_1(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "IntText")
            {
                _datasource.Morale = _troopCount.IntText;
                return;
            }
            if (propertyName == "IsVisible")
            {
                _datasource.HaveTroops = _troopCount.IsVisible;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_3(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "IsVisible")
            {
                _datasource.HaveTroops = _toprightContainer.IsVisible;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_3_0(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "IsVisible")
            {
                _datasource.HaveTroops = _shortcutContainer.IsVisible;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_4(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "ValueFloat")
            {
                _datasource.AmmoPercentage = _ammoPercentage.ValueFloat;
                return;
            }
            if (propertyName == "IsVisible")
            {
                _datasource.IsAmmoAvailable = _ammoPercentage.IsVisible;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_3_0_0(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "KeyID")
            {
                _datasourceSelectionKey.KeyID = _shortcutInput.KeyID;
                return;
            }
            if (propertyName == "IsVisible")
            {
                _datasourceSelectionKey.IsVisible = _shortcutInput.IsVisible;
                return;
            }
        }

        private void PropertyChangedListenerOf_widget_6(PropertyOwnerObject propertyOwnerObject, string propertyName, object value)
        {
            if (propertyName == "AdditionalArgs")
            {
                return;
            }
            if (propertyName == "ImageId")
            {
                return;
            }
            //propertyName == "ImageTypeCode";
        }

        private void ViewModelPropertyChangedListenerOf_datasource_Root(object sender, PropertyChangedEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithValueListenerOf_datasource_Root(object sender, PropertyChangedWithValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void HandleViewModelPropertyChangeOf_datasource_Root(string propertyName)
        {
            if (propertyName == "ActiveFormationClasses")
            {
                RefreshActiveFormationClasses(_datasource.ActiveFormationClasses);
                return;
            }
            if (propertyName == "SelectionKey")
            {
                RefreshSelectionKey(_datasource.SelectionKey);
                return;
            }
            if (propertyName == "CommanderImageIdentifier")
            {
                RefreshCommanderImageIdentifier(_datasource.CommanderImageIdentifier);
                return;
            }
            if (propertyName == "ActiveFilters")
            {
                RefreshActiveFilters(_datasource.ActiveFilters);
                return;
            }
            if (propertyName == "IsAmmoAvailable")
            {
                _rootWidget.HasAmmo = _datasource.IsAmmoAvailable;
                _ammoPercentage.IsVisible = _datasource.IsAmmoAvailable;
                return;
            }
            if (propertyName == "CurrentMemberCount")
            {
                _rootWidget.CurrentMemberCount = _datasource.CurrentMemberCount;
                return;
            }
            if (propertyName == "IsSelectable")
            {
                _rootWidget.IsSelectable = _datasource.IsSelectable;
                return;
            }
            if (propertyName == "IsSelected")
            {
                _rootWidget.IsSelected = _datasource.IsSelected;
                return;
            }
            if (propertyName == "OrderOfBattleFormationClass")
            {
                return;
            }
            if (propertyName == "IsSelectionActive")
            {
                _rootWidget.IsSelectionActive = _datasource.IsSelectionActive;
                return;
            }
            if (propertyName == "Morale")
            {
                _moraleFillBar.CurrentAmount = _datasource.Morale;
                _moraleFillBar.InitialAmount = _datasource.Morale;
                _troopCount.IntText = _datasource.Morale;
                return;
            }
            if (propertyName == "HaveTroops")
            {
                _troopCount.IsVisible = _datasource.HaveTroops;
                _shortcutContainer.IsVisible = _datasource.HaveTroops;
                return;
            }
            if (propertyName == "CanUseShortcuts")
            {
                return;
            }
            if (propertyName == "AmmoPercentage")
            {
                _ammoPercentage.ValueFloat = _datasource.AmmoPercentage;
                return;
            }
        }

        private void ViewModelPropertyChangedListenerOf_datasource_Root_SelectionKey(object sender, PropertyChangedEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root_SelectionKey(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithValueListenerOf_datasource_Root_SelectionKey(object sender, PropertyChangedWithValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root_SelectionKey(e.PropertyName);
        }

        private void HandleViewModelPropertyChangeOf_datasource_Root_SelectionKey(string propertyName)
        {
            if (propertyName == "KeyID")
            {
                _shortcutInput.KeyID = _datasourceSelectionKey.KeyID;
                return;
            }
            if (propertyName == "IsVisible")
            {
                _shortcutInput.IsVisible = _datasourceSelectionKey.IsVisible;
                return;
            }
        }

        private void ViewModelPropertyChangedListenerOf_datasource_Root_CommanderImageIdentifier(object sender, PropertyChangedEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root_CommanderImageIdentifier(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithValueListenerOf_datasource_Root_CommanderImageIdentifier(object sender, PropertyChangedWithValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root_CommanderImageIdentifier(e.PropertyName);
        }

        private void HandleViewModelPropertyChangeOf_datasource_Root_CommanderImageIdentifier(string propertyName)
        {
            if (propertyName == "AdditionalArgs")
            {
                _commanderImage.AdditionalArgs = _datasourceCommanderImageIdentifier.AdditionalArgs;
                return;
            }
            if (propertyName == "Id")
            {
                _commanderImage.ImageId = _datasourceCommanderImageIdentifier.Id;
                return;
            }
            if (propertyName == "ImageTypeCode")
            {
                _commanderImage.ImageTypeCode = _datasourceCommanderImageIdentifier.ImageTypeCode;
                return;
            }
        }

        public void OnList_datasource_Root_ActiveFormationClassesChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    {
                        for (int i = _baseGridWidget.ChildCount - 1; i >= 0; i--)
                        {
                            Widget child = _baseGridWidget.GetChild(i);
                            ((CustomOrderFormationClassVisualBrushWidget)child).OnBeforeRemovedChild(child);
                            Widget child2 = _baseGridWidget.GetChild(i);
                            ((CustomOrderFormationClassVisualBrushWidget)child2).SetDataSource(null);
                            _baseGridWidget.RemoveChild(child2);
                        }
                        return;
                    }
                case ListChangedType.Sorted:
                    {
                        for (int j = 0; j < _datasourceActiveFormationClasses.Count; j++)
                        {
                            OrderTroopItemFormationClassVM bindingObject = _datasourceActiveFormationClasses[j];
                            _baseGridWidget.FindChild((widget) => widget.GetComponent<GeneratedWidgetData>().Data == bindingObject).SetSiblingIndex(j, false);
                        }
                        return;
                    }
                case ListChangedType.ItemAdded:
                    {
                        CustomOrderFormationClassVisualBrushWidget customOrderFormationClassVisualBrushWidget = new CustomOrderFormationClassVisualBrushWidget(Context);
                        GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(customOrderFormationClassVisualBrushWidget);
                        OrderTroopItemFormationClassVM orderTroopItemFormationClassVM = _datasourceActiveFormationClasses[e.NewIndex];
                        generatedWidgetData.Data = orderTroopItemFormationClassVM;
                        customOrderFormationClassVisualBrushWidget.AddComponent(generatedWidgetData);
                        _baseGridWidget.AddChildAtIndex(customOrderFormationClassVisualBrushWidget, e.NewIndex);
                        customOrderFormationClassVisualBrushWidget.CreateWidgets();
                        customOrderFormationClassVisualBrushWidget.SetIds();
                        customOrderFormationClassVisualBrushWidget.SetAttributes();
                        customOrderFormationClassVisualBrushWidget.SetDataSource(orderTroopItemFormationClassVM);
                        return;
                    }
                case ListChangedType.ItemBeforeDeleted:
                    {
                        Widget child3 = _baseGridWidget.GetChild(e.NewIndex);
                        ((CustomOrderFormationClassVisualBrushWidget)child3).OnBeforeRemovedChild(child3);
                        return;
                    }
                case ListChangedType.ItemDeleted:
                    {
                        Widget child4 = _baseGridWidget.GetChild(e.NewIndex);
                        ((CustomOrderFormationClassVisualBrushWidget)child4).SetDataSource(null);
                        _baseGridWidget.RemoveChild(child4);
                        break;
                    }
                case ListChangedType.ItemChanged:
                    break;
                default:
                    return;
            }
        }

        public void OnListActiveFiltersChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    {
                        for (int i = _troopTypeList.ChildCount - 1; i >= 0; i--)
                        {
                            Widget child = _troopTypeList.GetChild(i);
                            ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child).OnBeforeRemovedChild(child);
                            Widget child2 = _troopTypeList.GetChild(i);
                            ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child2).SetDataSource(null);
                            _troopTypeList.RemoveChild(child2);
                        }
                        return;
                    }
                case ListChangedType.Sorted:
                    {
                        for (int j = 0; j < _datasourceActiveFilters.Count; j++)
                        {
                            OrderTroopItemFilterVM bindingObject = _datasourceActiveFilters[j];
                            _troopTypeList.FindChild((widget) => widget.GetComponent<GeneratedWidgetData>().Data == bindingObject).SetSiblingIndex(j, false);
                        }
                        return;
                    }
                case ListChangedType.ItemAdded:
                    {
                        CustomOrderOfBattleFormationFilterVisualBrushWidget customOrderOfBattleFormationFilterVisualBrushWidget = new CustomOrderOfBattleFormationFilterVisualBrushWidget(Context);
                        GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(customOrderOfBattleFormationFilterVisualBrushWidget);
                        OrderTroopItemFilterVM orderTroopItemFilterVM = _datasourceActiveFilters[e.NewIndex];
                        generatedWidgetData.Data = orderTroopItemFilterVM;
                        customOrderOfBattleFormationFilterVisualBrushWidget.AddComponent(generatedWidgetData);
                        _troopTypeList.AddChildAtIndex(customOrderOfBattleFormationFilterVisualBrushWidget, e.NewIndex);
                        customOrderOfBattleFormationFilterVisualBrushWidget.CreateWidgets();
                        customOrderOfBattleFormationFilterVisualBrushWidget.SetIds();
                        customOrderOfBattleFormationFilterVisualBrushWidget.SetAttributes();
                        customOrderOfBattleFormationFilterVisualBrushWidget.SetDataSource(orderTroopItemFilterVM);
                        return;
                    }
                case ListChangedType.ItemBeforeDeleted:
                    {
                        Widget child3 = _troopTypeList.GetChild(e.NewIndex);
                        ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child3).OnBeforeRemovedChild(child3);
                        return;
                    }
                case ListChangedType.ItemDeleted:
                    {
                        Widget child4 = _troopTypeList.GetChild(e.NewIndex);
                        ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child4).SetDataSource(null);
                        _troopTypeList.RemoveChild(child4);
                        break;
                    }
                case ListChangedType.ItemChanged:
                    break;
                default:
                    return;
            }
        }

        private void RefreshDataSource(OrderTroopItemVM newDataSource)
        {
            if (_datasource != null)
            {
                _datasource.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _rootWidget.PropertyChanged -= PropertyChangedListenerOf_widget;
                _moraleFillBar.PropertyChanged -= PropertyChangedListenerOf_widget_1;
                _troopCount.PropertyChanged -= PropertyChangedListenerOf_widget_2_1;
                _toprightContainer.PropertyChanged -= PropertyChangedListenerOf_widget_3;
                _shortcutContainer.PropertyChanged -= PropertyChangedListenerOf_widget_3_0;
                _ammoPercentage.PropertyChanged -= PropertyChangedListenerOf_widget_4;
                if (_datasourceActiveFormationClasses != null)
                {
                    _datasourceActiveFormationClasses.ListChanged -= OnList_datasource_Root_ActiveFormationClassesChanged;
                    for (int i = _baseGridWidget.ChildCount - 1; i >= 0; i--)
                    {
                        Widget child = _baseGridWidget.GetChild(i);
                        ((CustomOrderFormationClassVisualBrushWidget)child).OnBeforeRemovedChild(child);
                        Widget child2 = _baseGridWidget.GetChild(i);
                        ((CustomOrderFormationClassVisualBrushWidget)child2).SetDataSource(null);
                        _baseGridWidget.RemoveChild(child2);
                    }
                    _datasourceActiveFormationClasses = null;
                }
                if (_datasourceSelectionKey != null)
                {
                    _datasourceSelectionKey.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root_SelectionKey;
                    _datasourceSelectionKey.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root_SelectionKey;
                    _shortcutInput.PropertyChanged -= PropertyChangedListenerOf_widget_3_0_0;
                    _datasourceSelectionKey = null;
                }
                if (_datasourceCommanderImageIdentifier != null)
                {
                    _datasourceCommanderImageIdentifier.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root_CommanderImageIdentifier;
                    _datasourceCommanderImageIdentifier.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root_CommanderImageIdentifier;
                    _commanderImage.PropertyChanged -= PropertyChangedListenerOf_widget_6;
                    _datasourceCommanderImageIdentifier = null;
                }
                if (_datasourceActiveFilters != null)
                {
                    _datasourceActiveFilters.ListChanged -= OnListActiveFiltersChanged;
                    for (int j = _troopTypeList.ChildCount - 1; j >= 0; j--)
                    {
                        Widget child3 = _troopTypeList.GetChild(j);
                        ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child3).OnBeforeRemovedChild(child3);
                        Widget child4 = _troopTypeList.GetChild(j);
                        ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child4).SetDataSource(null);
                        _troopTypeList.RemoveChild(child4);
                    }
                    _datasourceActiveFilters = null;
                }
                _datasource = null;
            }
            _datasource = newDataSource;
            if (_datasource != null)
            {
                _datasource.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _rootWidget.HasAmmo = _datasource.IsAmmoAvailable;
                _rootWidget.CurrentMemberCount = _datasource.CurrentMemberCount;
                _rootWidget.IsSelectable = _datasource.IsSelectable;
                _rootWidget.IsSelected = _datasource.IsSelected;
                _rootWidget.IsSelectionActive = _datasource.IsSelectionActive;
                _rootWidget.PropertyChanged += PropertyChangedListenerOf_widget;
                _moraleFillBar.CurrentAmount = _datasource.Morale;
                _moraleFillBar.InitialAmount = _datasource.Morale;
                _moraleFillBar.PropertyChanged += PropertyChangedListenerOf_widget_1;
                _troopCount.IntText = _datasource.Morale;
                _troopCount.IsVisible = _datasource.HaveTroops;
                _troopCount.PropertyChanged += PropertyChangedListenerOf_widget_2_1;
                _toprightContainer.PropertyChanged += PropertyChangedListenerOf_widget_3;
                _shortcutContainer.IsVisible = _datasource.HaveTroops;
                _shortcutContainer.PropertyChanged += PropertyChangedListenerOf_widget_3_0;
                _ammoPercentage.ValueFloat = _datasource.AmmoPercentage;
                _ammoPercentage.IsVisible = _datasource.IsAmmoAvailable;
                _ammoPercentage.PropertyChanged += PropertyChangedListenerOf_widget_4;
                _datasourceActiveFormationClasses = _datasource.ActiveFormationClasses;
                if (_datasourceActiveFormationClasses != null)
                {
                    _datasourceActiveFormationClasses.ListChanged += OnList_datasource_Root_ActiveFormationClassesChanged;
                    for (int k = 0; k < _datasourceActiveFormationClasses.Count; k++)
                    {
                        CustomOrderFormationClassVisualBrushWidget customOrderFormationClassVisualBrushWidget = new CustomOrderFormationClassVisualBrushWidget(Context);
                        GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(customOrderFormationClassVisualBrushWidget);
                        OrderTroopItemFormationClassVM orderTroopItemFormationClassVM = _datasourceActiveFormationClasses[k];
                        generatedWidgetData.Data = orderTroopItemFormationClassVM;
                        customOrderFormationClassVisualBrushWidget.AddComponent(generatedWidgetData);
                        _baseGridWidget.AddChildAtIndex(customOrderFormationClassVisualBrushWidget, k);
                        customOrderFormationClassVisualBrushWidget.CreateWidgets();
                        customOrderFormationClassVisualBrushWidget.SetIds();
                        customOrderFormationClassVisualBrushWidget.SetAttributes();
                        customOrderFormationClassVisualBrushWidget.SetDataSource(orderTroopItemFormationClassVM);
                    }
                }
                _datasourceSelectionKey = _datasource.SelectionKey;
                if (_datasourceSelectionKey != null)
                {
                    _datasourceSelectionKey.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root_SelectionKey;
                    _datasourceSelectionKey.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root_SelectionKey;
                    _shortcutInput.KeyID = _datasourceSelectionKey.KeyID;
                    _shortcutInput.IsVisible = _datasourceSelectionKey.IsVisible;
                    _shortcutInput.PropertyChanged += PropertyChangedListenerOf_widget_3_0_0;
                }
                _datasourceCommanderImageIdentifier = _datasource.CommanderImageIdentifier;
                if (_datasourceCommanderImageIdentifier != null)
                {
                    _datasourceCommanderImageIdentifier.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root_CommanderImageIdentifier;
                    _datasourceCommanderImageIdentifier.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root_CommanderImageIdentifier;
                    _commanderImage.AdditionalArgs = _datasourceCommanderImageIdentifier.AdditionalArgs;
                    _commanderImage.ImageId = _datasourceCommanderImageIdentifier.Id;
                    _commanderImage.ImageTypeCode = _datasourceCommanderImageIdentifier.ImageTypeCode;
                    _commanderImage.PropertyChanged += PropertyChangedListenerOf_widget_6;
                }
                _datasourceActiveFilters = _datasource.ActiveFilters;
                if (_datasourceActiveFilters != null)
                {
                    _datasourceActiveFilters.ListChanged += OnListActiveFiltersChanged;
                    for (int l = 0; l < _datasourceActiveFilters.Count; l++)
                    {
                        CustomOrderOfBattleFormationFilterVisualBrushWidget customOrderOfBattleFormationFilterVisualBrushWidget = new CustomOrderOfBattleFormationFilterVisualBrushWidget(Context);
                        GeneratedWidgetData generatedWidgetData2 = new GeneratedWidgetData(customOrderOfBattleFormationFilterVisualBrushWidget);
                        OrderTroopItemFilterVM orderTroopItemFilterVM = _datasourceActiveFilters[l];
                        generatedWidgetData2.Data = orderTroopItemFilterVM;
                        customOrderOfBattleFormationFilterVisualBrushWidget.AddComponent(generatedWidgetData2);
                        _troopTypeList.AddChildAtIndex(customOrderOfBattleFormationFilterVisualBrushWidget, l);
                        customOrderOfBattleFormationFilterVisualBrushWidget.CreateWidgets();
                        customOrderOfBattleFormationFilterVisualBrushWidget.SetIds();
                        customOrderOfBattleFormationFilterVisualBrushWidget.SetAttributes();
                        customOrderOfBattleFormationFilterVisualBrushWidget.SetDataSource(orderTroopItemFilterVM);
                    }
                }
            }
        }

        private void RefreshActiveFormationClasses(MBBindingList<OrderTroopItemFormationClassVM> newDataSource)
        {
            if (_datasourceActiveFormationClasses != null)
            {
                _datasourceActiveFormationClasses.ListChanged -= OnList_datasource_Root_ActiveFormationClassesChanged;
                for (int i = _baseGridWidget.ChildCount - 1; i >= 0; i--)
                {
                    Widget child = _baseGridWidget.GetChild(i);
                    ((CustomOrderFormationClassVisualBrushWidget)child).OnBeforeRemovedChild(child);
                    Widget child2 = _baseGridWidget.GetChild(i);
                    ((CustomOrderFormationClassVisualBrushWidget)child2).SetDataSource(null);
                    _baseGridWidget.RemoveChild(child2);
                }
                _datasourceActiveFormationClasses = null;
            }
            _datasourceActiveFormationClasses = newDataSource;
            _datasourceActiveFormationClasses = _datasource.ActiveFormationClasses;
            if (_datasourceActiveFormationClasses != null)
            {
                _datasourceActiveFormationClasses.ListChanged += OnList_datasource_Root_ActiveFormationClassesChanged;
                for (int j = 0; j < _datasourceActiveFormationClasses.Count; j++)
                {
                    CustomOrderFormationClassVisualBrushWidget customOrderFormationClassVisualBrushWidget = new(Context);
                    GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(customOrderFormationClassVisualBrushWidget);
                    OrderTroopItemFormationClassVM orderTroopItemFormationClassVM = _datasourceActiveFormationClasses[j];
                    generatedWidgetData.Data = orderTroopItemFormationClassVM;
                    customOrderFormationClassVisualBrushWidget.AddComponent(generatedWidgetData);
                    _baseGridWidget.AddChildAtIndex(customOrderFormationClassVisualBrushWidget, j);
                    customOrderFormationClassVisualBrushWidget.CreateWidgets();
                    customOrderFormationClassVisualBrushWidget.SetIds();
                    customOrderFormationClassVisualBrushWidget.SetAttributes();
                    customOrderFormationClassVisualBrushWidget.SetDataSource(orderTroopItemFormationClassVM);
                }
            }
        }

        private void RefreshSelectionKey(InputKeyItemVM newDataSource)
        {
            if (_datasourceSelectionKey != null)
            {
                _datasourceSelectionKey.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root_SelectionKey;
                _datasourceSelectionKey.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root_SelectionKey;
                _shortcutInput.PropertyChanged -= PropertyChangedListenerOf_widget_3_0_0;
                _datasourceSelectionKey = null;
            }
            _datasourceSelectionKey = newDataSource;
            _datasourceSelectionKey = _datasource.SelectionKey;
            if (_datasourceSelectionKey != null)
            {
                _datasourceSelectionKey.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root_SelectionKey;
                _datasourceSelectionKey.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root_SelectionKey;
                _shortcutInput.KeyID = _datasourceSelectionKey.KeyID;
                _shortcutInput.IsVisible = _datasourceSelectionKey.IsVisible;
                _shortcutInput.PropertyChanged += PropertyChangedListenerOf_widget_3_0_0;
            }
        }

        private void RefreshCommanderImageIdentifier(ImageIdentifierVM newDataSource)
        {
            if (_datasourceCommanderImageIdentifier != null)
            {
                _datasourceCommanderImageIdentifier.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root_CommanderImageIdentifier;
                _datasourceCommanderImageIdentifier.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root_CommanderImageIdentifier;
                _commanderImage.PropertyChanged -= PropertyChangedListenerOf_widget_6;
                _datasourceCommanderImageIdentifier = null;
            }
            _datasourceCommanderImageIdentifier = newDataSource;
            _datasourceCommanderImageIdentifier = _datasource.CommanderImageIdentifier;
            if (_datasourceCommanderImageIdentifier != null)
            {
                _datasourceCommanderImageIdentifier.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root_CommanderImageIdentifier;
                _datasourceCommanderImageIdentifier.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root_CommanderImageIdentifier;
                _commanderImage.AdditionalArgs = _datasourceCommanderImageIdentifier.AdditionalArgs;
                _commanderImage.ImageId = _datasourceCommanderImageIdentifier.Id;
                _commanderImage.ImageTypeCode = _datasourceCommanderImageIdentifier.ImageTypeCode;
                _commanderImage.PropertyChanged += PropertyChangedListenerOf_widget_6;
            }
        }

        private void RefreshActiveFilters(MBBindingList<OrderTroopItemFilterVM> newDataSource)
        {
            if (_datasourceActiveFilters != null)
            {
                _datasourceActiveFilters.ListChanged -= OnListActiveFiltersChanged;
                for (int i = _troopTypeList.ChildCount - 1; i >= 0; i--)
                {
                    Widget child = _troopTypeList.GetChild(i);
                    ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child).OnBeforeRemovedChild(child);
                    Widget child2 = _troopTypeList.GetChild(i);
                    ((CustomOrderOfBattleFormationFilterVisualBrushWidget)child2).SetDataSource(null);
                    _troopTypeList.RemoveChild(child2);
                }
                _datasourceActiveFilters = null;
            }
            _datasourceActiveFilters = newDataSource;
            _datasourceActiveFilters = _datasource.ActiveFilters;
            if (_datasourceActiveFilters != null)
            {
                _datasourceActiveFilters.ListChanged += OnListActiveFiltersChanged;
                for (int j = 0; j < _datasourceActiveFilters.Count; j++)
                {
                    CustomOrderOfBattleFormationFilterVisualBrushWidget customOrderOfBattleFormationFilterVisualBrushWidget = new CustomOrderOfBattleFormationFilterVisualBrushWidget(Context);
                    GeneratedWidgetData generatedWidgetData = new GeneratedWidgetData(customOrderOfBattleFormationFilterVisualBrushWidget);
                    OrderTroopItemFilterVM orderTroopItemFilterVM = _datasourceActiveFilters[j];
                    generatedWidgetData.Data = orderTroopItemFilterVM;
                    customOrderOfBattleFormationFilterVisualBrushWidget.AddComponent(generatedWidgetData);
                    _troopTypeList.AddChildAtIndex(customOrderOfBattleFormationFilterVisualBrushWidget, j);
                    customOrderOfBattleFormationFilterVisualBrushWidget.CreateWidgets();
                    customOrderOfBattleFormationFilterVisualBrushWidget.SetIds();
                    customOrderOfBattleFormationFilterVisualBrushWidget.SetAttributes();
                    customOrderOfBattleFormationFilterVisualBrushWidget.SetDataSource(orderTroopItemFilterVM);
                }
            }
        }

        private OrderTroopItemBrushWidget _rootWidget;

        private GridWidget _baseGridWidget;

        private FillBar _moraleFillBar;

        private ListPanel _listPanel;

        private Widget _moraleIcon;

        private TextWidget _troopCount;

        private Widget _toprightContainer;

        private Widget _shortcutContainer;

        private InputKeyVisualWidget _shortcutInput;

        private SliderWidget _ammoPercentage;

        private Widget _filler;

        private Widget _sliderHandle;

        private Widget _selectionFrame;

        private ImageIdentifierWidget _commanderImage;

        private GridWidget _troopTypeList;

        private OrderTroopItemVM _datasource;

        private MBBindingList<OrderTroopItemFormationClassVM> _datasourceActiveFormationClasses;

        private InputKeyItemVM _datasourceSelectionKey;

        private ImageIdentifierVM _datasourceCommanderImageIdentifier;

        private MBBindingList<OrderTroopItemFilterVM> _datasourceActiveFilters;
    }



    /// <summary>
    /// Custom widget with integrated VM - OrderFormationClassVisualBrushWidget
    /// </summary>
    public class CustomOrderFormationClassVisualBrushWidget : ListPanel
    {
        private ListPanel _widget;

        private OrderFormationClassVisualBrushWidget _widget_0;

        private TextWidget _widget_1;

        private OrderTroopItemFormationClassVM _datasource_Root;

        public CustomOrderFormationClassVisualBrushWidget(UIContext context)
            : base(context)
        {
        }

        public void CreateWidgets()
        {
            _widget = this;
            _widget_0 = new OrderFormationClassVisualBrushWidget(Context);
            _widget.AddChild(_widget_0);
            _widget_1 = new TextWidget(Context);
            _widget.AddChild(_widget_1);
        }

        public void SetIds()
        {
        }

        public void SetAttributes()
        {
            WidthSizePolicy = SizePolicy.CoverChildren;
            HeightSizePolicy = SizePolicy.CoverChildren;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            StackLayout.LayoutMethod = LayoutMethod.VerticalBottomToTop;
            _widget_0.WidthSizePolicy = SizePolicy.Fixed;
            _widget_0.HeightSizePolicy = SizePolicy.Fixed;
            _widget_0.SuggestedWidth = 41f;
            _widget_0.SuggestedHeight = 39.5f;
            _widget_0.Brush = Context.GetBrush("Order.Troop.Icon");
            _widget_1.WidthSizePolicy = SizePolicy.CoverChildren;
            _widget_1.HeightSizePolicy = SizePolicy.CoverChildren;
            _widget_1.HorizontalAlignment = HorizontalAlignment.Center;
            _widget_1.VerticalAlignment = VerticalAlignment.Center;
            _widget_1.PositionYOffset = -5f;
            _widget_1.Brush = Context.GetBrush("Order.Troop.CountText");
        }

        public void DestroyDataSource()
        {
            if (_datasource_Root != null)
            {
                _datasource_Root.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithBoolValue -= ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithIntValue -= ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithFloatValue -= ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithUIntValue -= ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithColorValue -= ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithDoubleValue -= ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithVec2Value -= ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root;
                _widget_0.PropertyChanged -= PropertyChangedListenerOf_widget_0;
                _widget_0.boolPropertyChanged -= boolPropertyChangedListenerOf_widget_0;
                _widget_0.floatPropertyChanged -= floatPropertyChangedListenerOf_widget_0;
                _widget_0.Vec2PropertyChanged -= Vec2PropertyChangedListenerOf_widget_0;
                //_widget_0.Vector2PropertyChanged -= Vector2PropertyChangedListenerOf_widget_0;
                _widget_0.doublePropertyChanged -= doublePropertyChangedListenerOf_widget_0;
                _widget_0.intPropertyChanged -= intPropertyChangedListenerOf_widget_0;
                _widget_0.uintPropertyChanged -= uintPropertyChangedListenerOf_widget_0;
                _widget_0.ColorPropertyChanged -= ColorPropertyChangedListenerOf_widget_0;
                _widget_1.PropertyChanged -= PropertyChangedListenerOf_widget_1;
                _widget_1.boolPropertyChanged -= boolPropertyChangedListenerOf_widget_1;
                _widget_1.floatPropertyChanged -= floatPropertyChangedListenerOf_widget_1;
                _widget_1.Vec2PropertyChanged -= Vec2PropertyChangedListenerOf_widget_1;
                //_widget_1.Vector2PropertyChanged -= Vector2PropertyChangedListenerOf_widget_1;
                _widget_1.doublePropertyChanged -= doublePropertyChangedListenerOf_widget_1;
                _widget_1.intPropertyChanged -= intPropertyChangedListenerOf_widget_1;
                _widget_1.uintPropertyChanged -= uintPropertyChangedListenerOf_widget_1;
                _widget_1.ColorPropertyChanged -= ColorPropertyChangedListenerOf_widget_1;
                _datasource_Root = null;
            }
        }

        public void SetDataSource(OrderTroopItemFormationClassVM dataSource)
        {
            RefreshDataSource_datasource_Root(dataSource);
        }

        private void PropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, object e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void boolPropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, bool e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void floatPropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, float e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void Vec2PropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, Vec2 e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        //private void Vector2PropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, Vector2 e)
        //{
        //    HandleWidgetPropertyChangeOf_widget_0(propertyName);
        //}

        private void doublePropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, double e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void intPropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, int e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void uintPropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, uint e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void ColorPropertyChangedListenerOf_widget_0(PropertyOwnerObject propertyOwnerObject, string propertyName, Color e)
        {
            HandleWidgetPropertyChangeOf_widget_0(propertyName);
        }

        private void HandleWidgetPropertyChangeOf_widget_0(string propertyName)
        {
            if (propertyName == "FormationClassValue")
            {
                _datasource_Root.FormationClassValue = _widget_0.FormationClassValue;
            }
        }

        private void PropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, object e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void boolPropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, bool e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void floatPropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, float e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void Vec2PropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, Vec2 e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        //private void Vector2PropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, Vector2 e)
        //{
        //    HandleWidgetPropertyChangeOf_widget_1(propertyName);
        //}

        private void doublePropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, double e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void intPropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, int e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void uintPropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, uint e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void ColorPropertyChangedListenerOf_widget_1(PropertyOwnerObject propertyOwnerObject, string propertyName, Color e)
        {
            HandleWidgetPropertyChangeOf_widget_1(propertyName);
        }

        private void HandleWidgetPropertyChangeOf_widget_1(string propertyName)
        {
            if (propertyName == "IntText")
            {
                _datasource_Root.TroopCount = _widget_1.IntText;
            }
        }

        private void ViewModelPropertyChangedListenerOf_datasource_Root(object sender, PropertyChangedEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithValueListenerOf_datasource_Root(object sender, PropertyChangedWithValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root(object sender, PropertyChangedWithIntValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root(object sender, PropertyChangedWithFloatValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root(object sender, PropertyChangedWithUIntValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root(object sender, PropertyChangedWithColorValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root(object sender, PropertyChangedWithDoubleValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root(object sender, PropertyChangedWithVec2ValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void HandleViewModelPropertyChangeOf_datasource_Root(string propertyName)
        {
            if (propertyName == "FormationClassValue")
            {
                _widget_0.FormationClassValue = _datasource_Root.FormationClassValue;
            }
            else if (propertyName == "TroopCount")
            {
                _widget_1.IntText = _datasource_Root.TroopCount;
            }
        }

        private void RefreshDataSource_datasource_Root(OrderTroopItemFormationClassVM newDataSource)
        {
            if (_datasource_Root != null)
            {
                _datasource_Root.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithBoolValue -= ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithIntValue -= ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithFloatValue -= ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithUIntValue -= ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithColorValue -= ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithDoubleValue -= ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithVec2Value -= ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root;
                _widget_0.PropertyChanged -= PropertyChangedListenerOf_widget_0;
                _widget_0.boolPropertyChanged -= boolPropertyChangedListenerOf_widget_0;
                _widget_0.floatPropertyChanged -= floatPropertyChangedListenerOf_widget_0;
                _widget_0.Vec2PropertyChanged -= Vec2PropertyChangedListenerOf_widget_0;
                //_widget_0.Vector2PropertyChanged -= Vector2PropertyChangedListenerOf_widget_0;
                _widget_0.doublePropertyChanged -= doublePropertyChangedListenerOf_widget_0;
                _widget_0.intPropertyChanged -= intPropertyChangedListenerOf_widget_0;
                _widget_0.uintPropertyChanged -= uintPropertyChangedListenerOf_widget_0;
                _widget_0.ColorPropertyChanged -= ColorPropertyChangedListenerOf_widget_0;
                _widget_1.PropertyChanged -= PropertyChangedListenerOf_widget_1;
                _widget_1.boolPropertyChanged -= boolPropertyChangedListenerOf_widget_1;
                _widget_1.floatPropertyChanged -= floatPropertyChangedListenerOf_widget_1;
                _widget_1.Vec2PropertyChanged -= Vec2PropertyChangedListenerOf_widget_1;
                //_widget_1.Vector2PropertyChanged -= Vector2PropertyChangedListenerOf_widget_1;
                _widget_1.doublePropertyChanged -= doublePropertyChangedListenerOf_widget_1;
                _widget_1.intPropertyChanged -= intPropertyChangedListenerOf_widget_1;
                _widget_1.uintPropertyChanged -= uintPropertyChangedListenerOf_widget_1;
                _widget_1.ColorPropertyChanged -= ColorPropertyChangedListenerOf_widget_1;
                _datasource_Root = null;
            }
            _datasource_Root = newDataSource;
            if (_datasource_Root != null)
            {
                _datasource_Root.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithBoolValue += ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithIntValue += ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithFloatValue += ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithUIntValue += ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithColorValue += ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithDoubleValue += ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithVec2Value += ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root;
                _widget_0.FormationClassValue = _datasource_Root.FormationClassValue;
                _widget_0.PropertyChanged += PropertyChangedListenerOf_widget_0;
                _widget_0.boolPropertyChanged += boolPropertyChangedListenerOf_widget_0;
                _widget_0.floatPropertyChanged += floatPropertyChangedListenerOf_widget_0;
                _widget_0.Vec2PropertyChanged += Vec2PropertyChangedListenerOf_widget_0;
                //_widget_0.Vector2PropertyChanged += Vector2PropertyChangedListenerOf_widget_0;
                _widget_0.doublePropertyChanged += doublePropertyChangedListenerOf_widget_0;
                _widget_0.intPropertyChanged += intPropertyChangedListenerOf_widget_0;
                _widget_0.uintPropertyChanged += uintPropertyChangedListenerOf_widget_0;
                _widget_0.ColorPropertyChanged += ColorPropertyChangedListenerOf_widget_0;
                _widget_1.IntText = _datasource_Root.TroopCount;
                _widget_1.PropertyChanged += PropertyChangedListenerOf_widget_1;
                _widget_1.boolPropertyChanged += boolPropertyChangedListenerOf_widget_1;
                _widget_1.floatPropertyChanged += floatPropertyChangedListenerOf_widget_1;
                _widget_1.Vec2PropertyChanged += Vec2PropertyChangedListenerOf_widget_1;
                //_widget_1.Vector2PropertyChanged += Vector2PropertyChangedListenerOf_widget_1;
                _widget_1.doublePropertyChanged += doublePropertyChangedListenerOf_widget_1;
                _widget_1.intPropertyChanged += intPropertyChangedListenerOf_widget_1;
                _widget_1.uintPropertyChanged += uintPropertyChangedListenerOf_widget_1;
                _widget_1.ColorPropertyChanged += ColorPropertyChangedListenerOf_widget_1;
            }
        }
    }




    /// <summary>
    /// Custom widget with integrated VM - OrderOfBattleFormationFilterVisualBrushWidget
    /// </summary>
    public class CustomOrderOfBattleFormationFilterVisualBrushWidget : OrderOfBattleFormationFilterVisualBrushWidget
    {
        private OrderOfBattleFormationFilterVisualBrushWidget _widget;

        private OrderTroopItemFilterVM _datasource_Root;

        public CustomOrderOfBattleFormationFilterVisualBrushWidget(UIContext context)
            : base(context)
        {
        }

        public void CreateWidgets()
        {
            _widget = this;
        }

        public void SetIds()
        {
        }

        public void SetAttributes()
        {
            WidthSizePolicy = SizePolicy.Fixed;
            HeightSizePolicy = SizePolicy.Fixed;
            SuggestedWidth = 20f;
            SuggestedHeight = 20f;
            Brush = Context.GetBrush("OrderOfBattle.Formation.Class.Type");
            UnsetBrush = Context.GetBrush("OrderOfBattle.Filter.Unset");
            SpearBrush = Context.GetBrush("OrderOfBattle.Filter.Spear");
            ShieldBrush = Context.GetBrush("OrderOfBattle.Filter.Shield");
            ThrownBrush = Context.GetBrush("OrderOfBattle.Filter.Thrown");
            HeavyBrush = Context.GetBrush("OrderOfBattle.Filter.Heavy");
            HighTierBrush = Context.GetBrush("OrderOfBattle.Filter.HighTier");
            LowTierBrush = Context.GetBrush("OrderOfBattle.Filter.LowTier");
        }

        public void DestroyDataSource()
        {
            if (_datasource_Root != null)
            {
                _datasource_Root.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithBoolValue -= ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithIntValue -= ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithFloatValue -= ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithUIntValue -= ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithColorValue -= ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithDoubleValue -= ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithVec2Value -= ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root;
                _widget.PropertyChanged -= PropertyChangedListenerOf_widget;
                _widget.boolPropertyChanged -= boolPropertyChangedListenerOf_widget;
                _widget.floatPropertyChanged -= floatPropertyChangedListenerOf_widget;
                _widget.Vec2PropertyChanged -= Vec2PropertyChangedListenerOf_widget;
                //_widget.Vector2PropertyChanged -= Vector2PropertyChangedListenerOf_widget;
                _widget.doublePropertyChanged -= doublePropertyChangedListenerOf_widget;
                _widget.intPropertyChanged -= intPropertyChangedListenerOf_widget;
                _widget.uintPropertyChanged -= uintPropertyChangedListenerOf_widget;
                _widget.ColorPropertyChanged -= ColorPropertyChangedListenerOf_widget;
                _datasource_Root = null;
            }
        }

        public void SetDataSource(OrderTroopItemFilterVM dataSource)
        {
            RefreshDataSource_datasource_Root(dataSource);
        }

        private void PropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, object e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void boolPropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, bool e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void floatPropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, float e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void Vec2PropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, Vec2 e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        //private void Vector2PropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, Vector2 e)
        //{
        //    HandleWidgetPropertyChangeOf_widget(propertyName);
        //}

        private void doublePropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, double e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void intPropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, int e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void uintPropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, uint e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void ColorPropertyChangedListenerOf_widget(PropertyOwnerObject propertyOwnerObject, string propertyName, Color e)
        {
            HandleWidgetPropertyChangeOf_widget(propertyName);
        }

        private void HandleWidgetPropertyChangeOf_widget(string propertyName)
        {
            if (propertyName == "FormationFilter")
            {
                _datasource_Root.FilterTypeValue = _widget.FormationFilter;
            }
        }

        private void ViewModelPropertyChangedListenerOf_datasource_Root(object sender, PropertyChangedEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithValueListenerOf_datasource_Root(object sender, PropertyChangedWithValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root(object sender, PropertyChangedWithBoolValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root(object sender, PropertyChangedWithIntValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root(object sender, PropertyChangedWithFloatValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root(object sender, PropertyChangedWithUIntValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root(object sender, PropertyChangedWithColorValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root(object sender, PropertyChangedWithDoubleValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root(object sender, PropertyChangedWithVec2ValueEventArgs e)
        {
            HandleViewModelPropertyChangeOf_datasource_Root(e.PropertyName);
        }

        private void HandleViewModelPropertyChangeOf_datasource_Root(string propertyName)
        {
            if (propertyName == "FilterTypeValue")
            {
                _widget.FormationFilter = _datasource_Root.FilterTypeValue;
            }
        }

        private void RefreshDataSource_datasource_Root(OrderTroopItemFilterVM newDataSource)
        {
            if (_datasource_Root != null)
            {
                _datasource_Root.PropertyChanged -= ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithValue -= ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithBoolValue -= ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithIntValue -= ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithFloatValue -= ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithUIntValue -= ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithColorValue -= ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithDoubleValue -= ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithVec2Value -= ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root;
                _widget.PropertyChanged -= PropertyChangedListenerOf_widget;
                _widget.boolPropertyChanged -= boolPropertyChangedListenerOf_widget;
                _widget.floatPropertyChanged -= floatPropertyChangedListenerOf_widget;
                _widget.Vec2PropertyChanged -= Vec2PropertyChangedListenerOf_widget;
                //_widget.Vector2PropertyChanged -= Vector2PropertyChangedListenerOf_widget;
                _widget.doublePropertyChanged -= doublePropertyChangedListenerOf_widget;
                _widget.intPropertyChanged -= intPropertyChangedListenerOf_widget;
                _widget.uintPropertyChanged -= uintPropertyChangedListenerOf_widget;
                _widget.ColorPropertyChanged -= ColorPropertyChangedListenerOf_widget;
                _datasource_Root = null;
            }
            _datasource_Root = newDataSource;
            if (_datasource_Root != null)
            {
                _datasource_Root.PropertyChanged += ViewModelPropertyChangedListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithValue += ViewModelPropertyChangedWithValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithBoolValue += ViewModelPropertyChangedWithBoolValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithIntValue += ViewModelPropertyChangedWithIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithFloatValue += ViewModelPropertyChangedWithFloatValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithUIntValue += ViewModelPropertyChangedWithUIntValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithColorValue += ViewModelPropertyChangedWithColorValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithDoubleValue += ViewModelPropertyChangedWithDoubleValueListenerOf_datasource_Root;
                _datasource_Root.PropertyChangedWithVec2Value += ViewModelPropertyChangedWithVec2ValueListenerOf_datasource_Root;
                _widget.FormationFilter = _datasource_Root.FilterTypeValue;
                _widget.PropertyChanged += PropertyChangedListenerOf_widget;
                _widget.boolPropertyChanged += boolPropertyChangedListenerOf_widget;
                _widget.floatPropertyChanged += floatPropertyChangedListenerOf_widget;
                _widget.Vec2PropertyChanged += Vec2PropertyChangedListenerOf_widget;
                //_widget.Vector2PropertyChanged += Vector2PropertyChangedListenerOf_widget;
                _widget.doublePropertyChanged += doublePropertyChangedListenerOf_widget;
                _widget.intPropertyChanged += intPropertyChangedListenerOf_widget;
                _widget.uintPropertyChanged += uintPropertyChangedListenerOf_widget;
                _widget.ColorPropertyChanged += ColorPropertyChangedListenerOf_widget;
            }
        }
    }
}
