using Alliance.Common.Core.ExtendedXML.Models;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML.Extension
{
    public static class ExtendedXMLExtension
    {
        public static ExtendedCharacter GetExtendedCharacterObject(this BasicCharacterObject basicCharacterObject)
        {
            return MBObjectManager.Instance.GetObject<ExtendedCharacter>("NPCCharacter." + basicCharacterObject.StringId);
        }

        public static ExtendedItem GetExtendedItem(this ItemObject itemObject)
        {
            return MBObjectManager.Instance.GetObject<ExtendedItem>("item." + itemObject.StringId);
        }
    }
}
