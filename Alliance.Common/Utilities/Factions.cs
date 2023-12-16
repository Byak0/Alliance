﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Utilities
{
    /// <summary>
    /// Utility class to iterate over the available cultures.
    /// </summary>
    public class Factions
    {
        public Dictionary<string, BasicCultureObject> AvailableCultures;
        public List<string> OrderedCultureKeys;

        private static readonly Factions instance = new Factions();
        public static Factions Instance { get { return instance; } }

        static Factions()
        {
            instance.AvailableCultures = (from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().ToArray()
                                          where x.IsMainCulture
                                          select x).ToDictionary(x => x.StringId);
            instance.OrderedCultureKeys = instance.AvailableCultures.Keys.ToList();
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
