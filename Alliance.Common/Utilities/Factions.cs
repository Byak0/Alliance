using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Utilities
{
    public class Factions
    {
        public Dictionary<string, BasicCultureObject> AvailableCultures;

        private static readonly Factions instance = new Factions();
        public static Factions Instance { get { return instance; } }

        static Factions()
        {
            instance.AvailableCultures = (from x in MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().ToArray()
                                          where x.IsMainCulture
                                          select x).ToDictionary(x => x.StringId);
        }
    }
}
