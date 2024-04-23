using Alliance.Common.Core.ExtendedXML.Models;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML.Extension
{
    public static class ExtendedXMLExtension
    {
        private static readonly Dictionary<string, ExtendedCharacter> ExtendedCharacterCache = new Dictionary<string, ExtendedCharacter>();
        private static readonly Dictionary<string, ExtendedItem> ExtendedItemCache = new Dictionary<string, ExtendedItem>();

        public static ExtendedCharacter GetExtendedCharacterObject(this BasicCharacterObject basicCharacterObject)
        {
            string key = "NPCCharacter." + basicCharacterObject.StringId;
            if (!ExtendedCharacterCache.TryGetValue(key, out ExtendedCharacter cachedCharacter))
            {
                cachedCharacter = MBObjectManager.Instance.GetObject<ExtendedCharacter>(key);
                ExtendedCharacterCache[key] = cachedCharacter;
            }
            return cachedCharacter;
        }

        public static ExtendedItem GetExtendedItem(this ItemObject itemObject)
        {
            string key = "item." + itemObject.StringId;
            if (!ExtendedItemCache.TryGetValue(key, out ExtendedItem cachedItem))
            {
                cachedItem = MBObjectManager.Instance.GetObject<ExtendedItem>(key);
                ExtendedItemCache[key] = cachedItem;
            }
            return cachedItem;
        }
    }
}
