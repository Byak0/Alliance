using Alliance.Common.Core.Security.Extension;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Utility class to iterate over the available cultures.
	/// </summary>
	public class Factions
	{
		public List<string> NativeCultures = new List<string>() { "vlandia", "battania", "empire", "sturgia", "aserai", "khuzait" };
		public Dictionary<string, BasicCultureObject> AvailableCultures;
		public List<string> OrderedCultureKeys;

		private static readonly Factions instance = new Factions();
		public static Factions Instance { get { return instance; } }

		static Factions()
		{
			instance.RefreshAvailablecultures();
		}

		public void RefreshAvailablecultures()
		{
			// Manually load cultures if MBObjectManager is not initialized
			if (MBObjectManager.Instance == null)
			{
				MBObjectManager.Init();
			}
			if (!MBObjectManager.Instance.HasType(typeof(BasicCultureObject)))
			{
				MBObjectManager.Instance.RegisterType<BasicCultureObject>("Culture", "SPCultures", 17U, true, false);
				MBObjectManager.Instance.LoadXML("SPCultures", true, "", false);
			}

			// Refresh the list of available cultures
			AvailableCultures = (from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().ToArray()
								 where x.IsMainCulture
								 select x).ToDictionary(x => x.StringId);
			// Remove monsters from available cultures for everyone except devs
			if (!GameNetwork.IsServer && (GameNetwork.MyPeer == null || !GameNetwork.MyPeer.IsDev()))
			{
				AvailableCultures.Remove("monsters");
			}
			OrderedCultureKeys = AvailableCultures.Keys.ToList();
		}

		public BasicCultureObject GetNextCulture(BasicCultureObject currentCulture)
		{
			int currentIndex = OrderedCultureKeys.IndexOf(currentCulture.StringId);
			int nextIndex = (currentIndex + 1) % OrderedCultureKeys.Count;
			return AvailableCultures[OrderedCultureKeys[nextIndex]];
		}

		public BasicCultureObject GetPreviousCulture(BasicCultureObject currentCulture)
		{
			int currentIndex = OrderedCultureKeys.IndexOf(currentCulture.StringId);
			int previousIndex = (currentIndex - 1 + OrderedCultureKeys.Count) % OrderedCultureKeys.Count;
			return AvailableCultures[OrderedCultureKeys[previousIndex]];
		}
	}
}
