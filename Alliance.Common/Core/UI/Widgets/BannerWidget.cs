#if !SERVER
using Alliance.Common.Patch.Utilities;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Core.UI.Widgets
{
	/// <summary>
	/// Simple widget that displays a banner based on the culture ID.
	/// </summary>
	public class BannerWidget : MaskedTextureWidget
	{
		private string _cultureID;

		[Editor(false)]
		public string CultureID
		{
			get
			{
				return _cultureID;
			}
			set
			{
				if (_cultureID != value)
				{
					_cultureID = value;
					RefreshBanner();
					OnPropertyChanged(value, nameof(CultureID));
				}
			}
		}

		public BannerWidget(UIContext context)
			: base(context)
		{
		}

		public void RefreshBanner()
		{
			BasicCultureObject basicCultureObject = MBObjectManager.Instance?.GetObject<BasicCultureObject>(_cultureID.ToLower());
			if (basicCultureObject == null)
			{
				return;
			}

			uint color = basicCultureObject.BackgroundColor1;
			uint color2 = basicCultureObject.ForegroundColor1;
			string bannerCode = BannerToCultureHelper.GetBannerCodeFromCulture(basicCultureObject.StringId, color, color2);

			Id = bannerCode ?? "";
			TextureProviderName = "BannerImageTextureProvider";
			AdditionalArgs = "ninegrid";
			ImageId = Id;
		}
	}
}
#endif