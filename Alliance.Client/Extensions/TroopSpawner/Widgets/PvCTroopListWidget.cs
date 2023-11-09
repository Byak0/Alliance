using Alliance.Client.Extensions.TroopSpawner.Models;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedCharacter.Extension;
using Alliance.Common.Core.ExtendedCharacter.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    /*
     * This widget is called in the prefab SpawnMenu.xml and handles the list of troops
     */
    public class PvCTroopListWidget : Widget
    {
        public ScrollablePanel ScrollablePanel;
        public ScrollbarWidget ScrollbarWidget;
        public Widget ScrollbarContainer;
        public Widget ClipRect;
        public ListPanel InnerPanel;
        public ButtonWidget SelectedTroopButton;

        public string InfantryType = GameTexts.FindText("str_troop_type_name", "Infantry").ToString();
        public string RangedType = GameTexts.FindText("str_troop_type_name", "Ranged").ToString();
        public string CavalryType = GameTexts.FindText("str_troop_type_name", "Cavalry").ToString();
        public string HorseArcherType = GameTexts.FindText("str_troop_type_name", "HorseArcher").ToString();
        public int SelectedCultureIndex = 0;
        public Dictionary<string, List<BasicCharacterObject>> TroopsPerGroup;
        public List<BasicCharacterObject> AvailableTroops;
        public Dictionary<BasicCharacterObject, string> TroopsType;
        public Dictionary<BasicCharacterObject, TextWidget> TroopsLeft;
        public Dictionary<BasicCharacterObject, TextWidget> TroopsCost;

        public PvCTroopListWidget(UIContext context) : base(context)
        {
            TroopsLeft = new Dictionary<BasicCharacterObject, TextWidget>();
            TroopsCost = new Dictionary<BasicCharacterObject, TextWidget>();
            TroopsPerGroup = new Dictionary<string, List<BasicCharacterObject>>
            {
                { InfantryType, new List<BasicCharacterObject>()},
                { RangedType, new List<BasicCharacterObject>()},
                { CavalryType, new List<BasicCharacterObject>()},
                { HorseArcherType, new List<BasicCharacterObject>()}
            };

            WidthSizePolicy = SizePolicy.StretchToParent;
            HeightSizePolicy = SizePolicy.StretchToParent;

            ScrollablePanel = new ScrollablePanel(context);
            ScrollablePanel.HeightSizePolicy = SizePolicy.StretchToParent;
            ScrollablePanel.WidthSizePolicy = SizePolicy.StretchToParent;
            ScrollablePanel.AutoHideScrollBars = true;
            AddChild(ScrollablePanel);

            // Instantiating the scrollable content and giving it to the panel
            AddClipRect();
            ScrollablePanel.AddChild(ClipRect);
            ScrollablePanel.ClipRect = ClipRect;

            // Instantiating the scrollbar and giving it to the panel
            AddScrollbar();
            ScrollablePanel.VerticalScrollbar = ScrollbarWidget;

            AddInnerPanel();
            ScrollablePanel.InnerPanel = InnerPanel;
            ClipRect.AddChild(InnerPanel);

            OnSelectedCultureChange(SpawnTroopsModel.Instance.SelectedFaction);
            SpawnTroopsModel.Instance.OnFactionSelected += OnFactionSelected;
            SpawnTroopsModel.Instance.OnTroopSpawned += OnTroopSpawned;
            SpawnTroopsModel.Instance.OnDifficultyUpdated += OnDifficultyUpdated;
        }

        private void AddScrollbar()
        {
            ScrollbarWidget = new ScrollbarWidget(Context);
            ScrollbarWidget.WidthSizePolicy = SizePolicy.Fixed;
            ScrollbarWidget.SuggestedWidth = 8;
            ScrollbarWidget.HeightSizePolicy = SizePolicy.StretchToParent;
            ScrollbarWidget.HorizontalAlignment = HorizontalAlignment.Left;
            ScrollbarWidget.UpdateChildrenStates = true;
            ScrollbarWidget.MarginTop = 6;
            ScrollbarWidget.MarginBottom = 15;
            ScrollbarWidget.MarginLeft = 15;
            ScrollbarWidget.AlignmentAxis = AlignmentAxis.Vertical;
            ScrollbarWidget.MaxValue = 100;
            ScrollbarWidget.MinValue = 0;

            // Adding Scrollbar's children: the background color and its handle
            Widget backgroundColorSprite = new Widget(Context);
            backgroundColorSprite.WidthSizePolicy = SizePolicy.Fixed;
            backgroundColorSprite.HeightSizePolicy = SizePolicy.StretchToParent;
            backgroundColorSprite.SuggestedWidth = 4;
            backgroundColorSprite.HorizontalAlignment = HorizontalAlignment.Center;
            backgroundColorSprite.Sprite = Context.SpriteData.GetSprite("lobby_slider_bed_9");
            backgroundColorSprite.AlphaFactor = 0.2f;
            ScrollbarWidget.AddChild(backgroundColorSprite);

            ImageWidget handle = new ImageWidget(Context);
            handle.WidthSizePolicy = SizePolicy.Fixed;
            handle.MinHeight = 50;
            handle.SuggestedWidth = 8;
            handle.HorizontalAlignment = HorizontalAlignment.Center;
            handle.Brush = Context.GetBrush("MPLobby.CustomServer.ScrollHandle");
            ScrollbarWidget.AddChild(handle);

            ScrollbarWidget.Handle = handle;

            //ScrollbarContainer.AddChild(ScrollbarWidget);
            //AddChild(ScrollbarContainer);
            AddChild(ScrollbarWidget);
        }

        // ClipRect's only purpose in life is to only show a part of its children
        private void AddClipRect()
        {
            ClipRect = new Widget(Context);
            ClipRect.WidthSizePolicy = SizePolicy.StretchToParent;
            ClipRect.HeightSizePolicy = SizePolicy.StretchToParent;
            // That's where it got its name from!
            ClipRect.ClipContents = true;
        }

        private void AddInnerPanel()
        {
            InnerPanel = new ListPanel(Context);
            InnerPanel.WidthSizePolicy = SizePolicy.StretchToParent;
            InnerPanel.HeightSizePolicy = SizePolicy.CoverChildren;
            InnerPanel.StackLayout.LayoutMethod = LayoutMethod.VerticalBottomToTop;
        }

        private void RegenerateInnerPanelContent()
        {
            TroopsLeft.Clear();
            TroopsCost.Clear();
            InnerPanel.RemoveAllChildren();
            // Not entirely satisfied of that whole yOffset reassignment thing...
            int yOffset = 16;
            if (TroopsPerGroup[InfantryType].Count > 0)
            {
                InnerPanel.AddChild(CreateTroopGroupWidget(InfantryType, yOffset));
                yOffset += 30;
                foreach (BasicCharacterObject troop in TroopsPerGroup[InfantryType])
                {
                    InnerPanel.AddChild(CreateTroopButtonWidget(troop, yOffset));
                    yOffset += 44;
                }
            }
            if (TroopsPerGroup[RangedType].Count > 0)
            {
                InnerPanel.AddChild(CreateTroopGroupWidget(RangedType, yOffset));
                yOffset += 30;
                foreach (BasicCharacterObject troop in TroopsPerGroup[RangedType])
                {
                    InnerPanel.AddChild(CreateTroopButtonWidget(troop, yOffset));
                    yOffset += 44;
                }
            }
            if (TroopsPerGroup[CavalryType].Count > 0)
            {
                InnerPanel.AddChild(CreateTroopGroupWidget(CavalryType, yOffset));
                yOffset += 30;
                foreach (BasicCharacterObject troop in TroopsPerGroup[CavalryType])
                {
                    InnerPanel.AddChild(CreateTroopButtonWidget(troop, yOffset));
                    yOffset += 44;
                }
            }
            if (TroopsPerGroup[HorseArcherType].Count > 0)
            {
                InnerPanel.AddChild(CreateTroopGroupWidget(HorseArcherType, yOffset));
                yOffset += 30;
                foreach (BasicCharacterObject troop in TroopsPerGroup[HorseArcherType])
                {
                    InnerPanel.AddChild(CreateTroopButtonWidget(troop, yOffset));
                    yOffset += 44;
                }
            }
        }

        private Widget CreateTroopGroupWidget(string name, int yOffset)
        {
            TextWidget troopGroup = new TextWidget(Context);
            troopGroup.Text = name;
            troopGroup.WidthSizePolicy = SizePolicy.StretchToParent;
            troopGroup.HeightSizePolicy = SizePolicy.Fixed;
            troopGroup.SuggestedHeight = 30;
            troopGroup.Brush = Context.GetBrush("MPLobby.ClassFilter.ClassTuple.Text");
            //troopGroup.PositionYOffset = yOffset;
            troopGroup.MarginLeft = 24;
            troopGroup.DoNotAcceptEvents = true;
            return troopGroup;
        }

        private BcoButtonWidget CreateTroopButtonWidget(BasicCharacterObject troop, int yOffset)
        {
            BcoButtonWidget troopButton = new BcoButtonWidget(Context);
            troopButton.WidthSizePolicy = SizePolicy.Fixed;
            troopButton.HeightSizePolicy = SizePolicy.Fixed;
            troopButton.SuggestedWidth = 394;
            troopButton.SuggestedHeight = 44;
            //troopButton.MarginTop = yOffset;
            troopButton.Brush = Context.GetBrush("MPLobby.ClassFilter.ClassTuple.Background");
            troopButton.BasicCharacterObject = troop;
            troopButton.MarginLeft = 24;
            troopButton.DoNotPassEventsToChildren = true;
            troopButton.UpdateChildrenStates = true;
            if (troop == SpawnTroopsModel.Instance.SelectedTroop)
            {
                troopButton.IsSelected = true;
                SelectedTroopButton = troopButton;
            }
            else
            {
                troopButton.IsSelected = false;
            }

            Widget backgroundColorWidget = new Widget(Context);
            backgroundColorWidget.WidthSizePolicy = SizePolicy.Fixed;
            backgroundColorWidget.HeightSizePolicy = SizePolicy.Fixed;
            backgroundColorWidget.SuggestedWidth = 15;
            backgroundColorWidget.SuggestedHeight = 40;
            backgroundColorWidget.VerticalAlignment = VerticalAlignment.Center;
            backgroundColorWidget.MarginLeft = 5;
            backgroundColorWidget.PositionYOffset = -1;
            backgroundColorWidget.Sprite = Context.SpriteData.GetSprite("MPLobby\\Store\\class_tuple_faction_color");
            backgroundColorWidget.AlphaFactor = 0.7f;
            troopButton.AddChild(backgroundColorWidget);

            Widget icon = new Widget(Context);
            icon.WidthSizePolicy = SizePolicy.Fixed;
            icon.HeightSizePolicy = SizePolicy.Fixed;
            icon.SuggestedHeight = 34;
            icon.SuggestedWidth = 34;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.MarginLeft = 16;
            icon.PositionYOffset = -1;
            icon.Sprite = Context.SpriteData.GetSprite("General\\compass\\" + TroopsType[troop]);
            troopButton.AddChild(icon);

            ScrollingTextWidget troopName = new ScrollingTextWidget(Context);
            troopName.Text = troop.Name.ToString();
            troopName.IsAutoScrolling = false;
            troopName.InbetweenScrollDuration = 0.5f;
            troopName.ScrollOnHoverWidget = troopButton;
            troopName.WidthSizePolicy = SizePolicy.Fixed;
            troopName.SuggestedWidth = 150 + (!Config.Instance.UseTroopLimit ? 50 : 0) + (!Config.Instance.UseTroopCost ? 60 : 0);
            troopName.HeightSizePolicy = SizePolicy.StretchToParent;
            troopName.MarginLeft = 60;
            troopName.PositionYOffset = -1;
            troopName.Brush = Context.GetBrush("MPLobby.ClassFilter.ClassTuple.Text");
            troopButton.AddChild(troopName);

            if (Config.Instance.UseTroopLimit)
            {
                TextWidget troopLimit = new TextWidget(Context);
                ExtendedCharacterObject pvcCharacter = troop.GetExtendedCharacterObject();
                troopLimit.Text = pvcCharacter.TroopLeft + "/" + pvcCharacter.TroopLimit;
                troopLimit.WidthSizePolicy = SizePolicy.CoverChildren;
                troopLimit.HeightSizePolicy = SizePolicy.StretchToParent;
                troopLimit.HorizontalAlignment = HorizontalAlignment.Right;
                troopLimit.MarginRight = Config.Instance.UseTroopCost ? 80 : 20;
                troopLimit.Brush = Context.GetBrush("MPLobby.ClassFilter.ClassTuple.Text");
                troopButton.AddChild(troopLimit);
                TroopsLeft.Add(troop, troopLimit);
            }

            if (Config.Instance.UseTroopCost)
            {
                TextWidget troopCost = new TextWidget(Context);
                troopCost.IntText = SpawnHelper.GetTroopCost(troop, SpawnTroopsModel.Instance.Difficulty);
                troopCost.WidthSizePolicy = SizePolicy.CoverChildren;
                troopCost.HeightSizePolicy = SizePolicy.StretchToParent;
                troopCost.HorizontalAlignment = HorizontalAlignment.Right;
                troopCost.MarginRight = 40;
                troopCost.Brush = Context.GetBrush("MPLobby.ClassFilter.ClassTuple.Text");
                troopButton.AddChild(troopCost);
                TroopsCost.Add(troop, troopCost);

                Widget goldIcon = new Widget(Context);
                goldIcon.WidthSizePolicy = SizePolicy.Fixed;
                goldIcon.HeightSizePolicy = SizePolicy.Fixed;
                goldIcon.SuggestedWidth = 20;
                goldIcon.SuggestedHeight = 18;
                goldIcon.HorizontalAlignment = HorizontalAlignment.Right;
                goldIcon.VerticalAlignment = VerticalAlignment.Center;
                goldIcon.MarginRight = 12;
                goldIcon.Sprite = Context.SpriteData.GetSprite("General\\Mission\\PersonalKillfeed\\bracelet_icon_shadow");
                troopButton.AddChild(goldIcon);
            }

            troopButton.EventFire += OnTroopButtonEvent;

            return troopButton;
        }

        private void OnTroopButtonEvent(Widget widget, string eventName, object[] args)
        {
            if (IsVisible)
            {
                if (eventName == "Click")
                {
                    BcoButtonWidget bcoButtonWidget = (BcoButtonWidget)widget;
                    if (SelectedTroopButton != null)
                    {
                        SelectedTroopButton.IsSelected = false;
                    }
                    bcoButtonWidget.IsSelected = true;
                    SelectedTroopButton = bcoButtonWidget;

                    SpawnTroopsModel.Instance.SelectedTroop = bcoButtonWidget.BasicCharacterObject;
                }
            }
        }

        private void OnSelectedCultureChange(BasicCultureObject culture)
        {
            AvailableTroops = new List<BasicCharacterObject>();
            TroopsType = new Dictionary<BasicCharacterObject, string>();
            foreach (MultiplayerClassDivisions.MPHeroClass heroClass in MultiplayerClassDivisions.GetMPHeroClasses(culture).ToList())
            {
                AvailableTroops.Add(heroClass.TroopCharacter);
                TroopsType.Add(heroClass.TroopCharacter, heroClass.IconType.ToString());
            }
            TroopsPerGroup[InfantryType] = new List<BasicCharacterObject>();
            TroopsPerGroup[RangedType] = new List<BasicCharacterObject>();
            TroopsPerGroup[CavalryType] = new List<BasicCharacterObject>();
            TroopsPerGroup[HorseArcherType] = new List<BasicCharacterObject>();
            foreach (BasicCharacterObject troop in AvailableTroops)
            {
                if (troop.IsMounted)
                {
                    if (troop.IsRanged)
                    {
                        TroopsPerGroup[HorseArcherType].Add(troop);
                        continue;
                    }
                    TroopsPerGroup[CavalryType].Add(troop);
                    continue;
                }
                if (troop.IsRanged)
                {
                    TroopsPerGroup[RangedType].Add(troop);
                    continue;
                }
                TroopsPerGroup[InfantryType].Add(troop);
            }
            RegenerateInnerPanelContent();
        }

        private void OnFactionSelected(object sender, EventArgs e)
        {
            OnSelectedCultureChange(SpawnTroopsModel.Instance.SelectedFaction);
        }

        private void OnTroopSpawned(object sender, TroopSpawnedEventArgs e)
        {
            if (!TroopsLeft.ContainsKey(e.Troop)) return;
            TroopsLeft[e.Troop].Text = e.Troop.GetExtendedCharacterObject().TroopLeft + "/" + e.Troop.GetExtendedCharacterObject().TroopLimit;
        }

        private void OnDifficultyUpdated(object sender, EventArgs e)
        {
            foreach (KeyValuePair<BasicCharacterObject, TextWidget> costWidget in TroopsCost)
            {
                costWidget.Value.IntText = SpawnHelper.GetTroopCost(costWidget.Key, SpawnTroopsModel.Instance.Difficulty);
            }
        }
    }
}