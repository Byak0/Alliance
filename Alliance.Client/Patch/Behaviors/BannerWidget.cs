using Alliance.Common.Patch.Utilities;
using TaleWorlds.Core;
using TaleWorlds.GauntletUI;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.ObjectSystem;

namespace Alliance.Client.Patch.Behaviors
{
	/// <summary>
	/// Simple widget that displays a banner based on the culture ID.
	/// </summary>
	public class BannerWidget : ImageIdentifierWidget
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
					OnPropertyChanged(value, "CultureID");
				}
			}
		}

		public BannerWidget(UIContext context)
			: base(context)
		{
			CultureID = "sturgia";
			RefreshBanner();
		}

		public void RefreshBanner()
		{
			BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(_cultureID);
			if (basicCultureObject == null)
			{
				return;
			}

			uint color = basicCultureObject.BackgroundColor1;
			uint color2 = basicCultureObject.ForegroundColor1;
			BannerCode bannerCode = BannerCode.CreateFrom(BannerToCultureHelper.GetBannerCodeFromCulture(basicCultureObject.StringId, color, color2));
			ImageTypeCode = (int)ImageIdentifierType.BannerCode;
			ImageId = bannerCode != null ? bannerCode.Code : "";
			AdditionalArgs = "";
		}
	}
}


