using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using Color = TaleWorlds.Library.Color;

namespace PvC.Client.Widgets
{
    /*
     *  Banner Widget used by HUDExtension.xml to show banners of factions.
     */
    public class BannerWidget : Widget
    {
        private Widget BannerBackground;
        private Widget CultureIcon;
        private BasicCultureObject _faction;

        [Editor(false)]
        public BasicCultureObject Faction
        {
            get
            {
                return _faction;
            }
            set
            {
                if (_faction != value)
                {
                    _faction = value;
                    SetBanner(_faction);
                }
            }
        }

        public BannerWidget(UIContext context) : base(context)
        {
            WidthSizePolicy = SizePolicy.Fixed;
            HeightSizePolicy = SizePolicy.Fixed;
            SuggestedWidth = 60;
            SuggestedHeight = 60;
            AddBannerBackground();
            AddCultureIcon();
            AddChild(BannerBackground);
            AddChild(CultureIcon);
        }

        private void AddBannerBackground()
        {
            BannerBackground = new Widget(Context);
            BannerBackground.WidthSizePolicy = SizePolicy.Fixed;
            BannerBackground.HeightSizePolicy = SizePolicy.Fixed;
            BannerBackground.SuggestedWidth = 60;
            BannerBackground.SuggestedHeight = 60;
            BannerBackground.HorizontalAlignment = HorizontalAlignment.Center;
            BannerBackground.VerticalAlignment = VerticalAlignment.Center;
        }

        private void AddCultureIcon()
        {
            CultureIcon = new Widget(Context);
            CultureIcon.WidthSizePolicy = SizePolicy.Fixed;
            CultureIcon.HeightSizePolicy = SizePolicy.Fixed;
            CultureIcon.SuggestedWidth = 48;
            CultureIcon.SuggestedHeight = 48;
            CultureIcon.HorizontalAlignment = HorizontalAlignment.Center;
            CultureIcon.VerticalAlignment = VerticalAlignment.Center;
        }

        private void SetBanner(BasicCultureObject culture)
        {
            BannerBackground.Sprite = Context.SpriteData.GetSprite("MPHud\\banner_right");
            BannerBackground.Color = Color.FromUint(culture.BackgroundColor1);

            CultureIcon.Sprite = Context.SpriteData.GetSprite("StdAssets\\FactionIcons\\LargeIcons\\" + culture.StringId);
            CultureIcon.Color = Color.FromUint(culture.ForegroundColor1);
        }
    }
}
