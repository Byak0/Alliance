using Alliance.Client.Extensions.TroopSpawner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.ObjectSystem;
using Color = TaleWorlds.Library.Color;

namespace Alliance.Client.Extensions.TroopSpawner.Widgets
{
    /*
     * > Widget DecorationBar
     *     > Widget NameContainer
     *         > BrushWidget BannerBackground
     *         > Widget CultureIcon
     *         > Text Widget Name
     *     > ButtonWidget LeftArrow
     *     > ButtonWidget RightArrow
     */
    public class CultureFilterWidget : Widget
    {
        public Widget DecorationBar;
        public Widget NameContainer;
        public BrushWidget BannerBackground;
        public Widget CultureIcon;
        public TextWidget Name;
        public ButtonWidget LeftArrow;
        public ButtonWidget RightArrow;

        private int _selectedCulture = 0;
        private BasicCultureObject[] _availableCultures = (from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().ToArray()
                                                           where x.IsMainCulture
                                                           select x).ToArray();

        public CultureFilterWidget(UIContext context) : base(context)
        {
            WidthSizePolicy = SizePolicy.Fixed;
            HeightSizePolicy = SizePolicy.Fixed;
            VerticalAlignment = VerticalAlignment.Top;
            SuggestedWidth = 500;
            SuggestedHeight = 120;

            // Initialize faction selected from model
            for (int i = 0; i < _availableCultures.Length; i++)
            {
                if (_availableCultures[i] == SpawnTroopsModel.Instance.SelectedFaction) _selectedCulture = i;
            }

            AddDecorationBar();
            SetCulture(SpawnTroopsModel.Instance.SelectedFaction);
            SpawnTroopsModel.Instance.OnFactionSelected += OnFactionSelected;
        }

        private void OnFactionSelected(object sender, EventArgs e)
        {
            SetCulture(SpawnTroopsModel.Instance.SelectedFaction);
        }

        private void AddDecorationBar()
        {
            DecorationBar = new Widget(Context);
            DecorationBar.WidthSizePolicy = SizePolicy.StretchToParent;
            DecorationBar.HeightSizePolicy = SizePolicy.StretchToParent;
            DecorationBar.VerticalAlignment = VerticalAlignment.Top;
            DecorationBar.MarginLeft = -30;
            DecorationBar.Sprite = Context.SpriteData.GetSprite("StdAssets\\tabbar_long");

            AddChild(DecorationBar);
            AddNameContainer();
            DecorationBar.AddChild(NameContainer);
            AddLeftArrow();
            DecorationBar.AddChild(LeftArrow);
            AddRightArrow();
            DecorationBar.AddChild(RightArrow);
        }

        private void AddLeftArrow()
        {
            LeftArrow = new ButtonWidget(Context);
            LeftArrow.DoNotPassEventsToChildren = true;
            LeftArrow.UpdateChildrenStates = true;
            LeftArrow.WidthSizePolicy = SizePolicy.Fixed;
            LeftArrow.HeightSizePolicy = SizePolicy.Fixed;
            LeftArrow.SuggestedWidth = 32;
            LeftArrow.SuggestedHeight = 32;
            LeftArrow.VerticalAlignment = VerticalAlignment.Top;
            LeftArrow.HorizontalAlignment = HorizontalAlignment.Left;
            LeftArrow.MarginTop = 10;
            LeftArrow.MarginLeft = 100;
            // This brush is interverted natively
            LeftArrow.Brush = Context.GetBrush("ButtonRightArrowBrush1");
            LeftArrow.EventFire += OnLeftArrowClick;
        }

        private void AddRightArrow()
        {
            RightArrow = new ButtonWidget(Context);
            RightArrow.DoNotPassEventsToChildren = true;
            RightArrow.UpdateChildrenStates = true;
            RightArrow.WidthSizePolicy = SizePolicy.Fixed;
            RightArrow.HeightSizePolicy = SizePolicy.Fixed;
            RightArrow.SuggestedWidth = 32;
            RightArrow.SuggestedHeight = 32;
            RightArrow.VerticalAlignment = VerticalAlignment.Top;
            RightArrow.HorizontalAlignment = HorizontalAlignment.Right;
            RightArrow.MarginTop = 10;
            RightArrow.MarginRight = 100;
            // This brush is interverted natively
            RightArrow.Brush = Context.GetBrush("ButtonLeftArrowBrush1");
            RightArrow.EventFire += OnRightArrowClick;
        }
        private void AddNameContainer()
        {
            NameContainer = new Widget(Context);
            NameContainer.WidthSizePolicy = SizePolicy.Fixed;
            NameContainer.HeightSizePolicy = SizePolicy.Fixed;
            NameContainer.SuggestedWidth = 240;
            NameContainer.SuggestedHeight = 47;
            NameContainer.ClipContents = true;
            NameContainer.VerticalAlignment = VerticalAlignment.Top;
            NameContainer.HorizontalAlignment = HorizontalAlignment.Center;
            NameContainer.MarginTop = 1;
            NameContainer.Sprite = Context.SpriteData.GetSprite("StdAssets\\tabbar_long_namebox");

            AddBannerBackground();
            AddCultureIcon();
            NameContainer.AddChild(BannerBackground);
            NameContainer.AddChild(CultureIcon);
            AddName();
            NameContainer.AddChild(Name);
        }

        private void AddBannerBackground()
        {
            BannerBackground = new BrushWidget(Context);
            BannerBackground.WidthSizePolicy = SizePolicy.Fixed;
            BannerBackground.HeightSizePolicy = SizePolicy.Fixed;
            BannerBackground.SuggestedWidth = 40;
            BannerBackground.SuggestedHeight = 40;
            BannerBackground.VerticalAlignment = VerticalAlignment.Center;
            BannerBackground.Brush = Context.GetBrush("MPTeamSelection.Banner.Right");
            BannerBackground.MarginLeft = 46;
            BannerBackground.MarginTop = 4;
        }
        private void AddCultureIcon()
        {
            CultureIcon = new Widget(Context);
            CultureIcon.WidthSizePolicy = SizePolicy.Fixed;
            CultureIcon.HeightSizePolicy = SizePolicy.Fixed;
            CultureIcon.SuggestedWidth = 30;
            CultureIcon.SuggestedHeight = 30;
            CultureIcon.MarginLeft = 50;
            CultureIcon.VerticalAlignment = VerticalAlignment.Center;
        }
        private void AddName()
        {
            Name = new TextWidget(Context);
            Name.HeightSizePolicy = SizePolicy.Fixed;
            Name.WidthSizePolicy = SizePolicy.StretchToParent;
            Name.SuggestedHeight = 32;
            Name.Brush = Context.GetBrush("Clan.NameTitle.Text");
            Name.Text = AvailableCultures[SelectedCulture].Name.ToString();
            Name.VerticalAlignment = VerticalAlignment.Center;
            Name.MarginLeft = 30;
        }

        private void SetCulture(BasicCultureObject culture)
        {
            Name.Text = culture.Name.ToString();

            Color color = Color.FromUint(culture.BackgroundColor1);
            using (Dictionary<string, Style>.ValueCollection.Enumerator enumerator = Context.GetBrush("MPTeamSelection.Banner.Right").Styles.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Style style = enumerator.Current;
                    foreach (StyleLayer styleLayer in style.Layers)
                    {
                        styleLayer.Color = color;
                    }
                }
            }
            BannerBackground.Color = color;

            CultureIcon.Sprite = Context.SpriteData.GetSprite("StdAssets\\FactionIcons\\LargeIcons\\" + culture.StringId);
            CultureIcon.Color = Color.FromUint(culture.ForegroundColor1);
        }

        private void RefreshSelectedCulture()
        {
            SpawnTroopsModel.Instance.SelectedFaction = AvailableCultures[SelectedCulture];
        }

        private void OnLeftArrowClick(Widget widget, string eventName, object[] args)
        {
            if (IsVisible)
            {
                if (eventName == "Click")
                {
                    _selectedCulture--;
                    if (_selectedCulture < 0)
                    {
                        _selectedCulture = AvailableCultures.Length - 1;
                    }
                    RefreshSelectedCulture();
                }
            }
        }

        private void OnRightArrowClick(Widget widget, string eventName, object[] args)
        {
            if (IsVisible)
            {
                if (eventName == "Click")
                {
                    _selectedCulture++;
                    if (_selectedCulture >= AvailableCultures.Length)
                    {
                        _selectedCulture = 0;
                    }
                    RefreshSelectedCulture();
                }
            }
        }

        public int SelectedCulture
        {
            get
            {
                return _selectedCulture;
            }
            set
            {
                _selectedCulture = value;
                RefreshSelectedCulture();
            }
        }

        public BasicCultureObject[] AvailableCultures
        {
            get
            {
                return _availableCultures;
            }
            set
            {
                _availableCultures = value;
                if (_selectedCulture >= _availableCultures.Length)
                {
                    _selectedCulture = 0;
                }
                RefreshSelectedCulture();
            }
        }
    }
}
