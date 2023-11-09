using Alliance.Common.Core.ExtendedCharacter.Models;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedCharacter.Extension
{
    public static class ExtendedCharacterExtension
    {
        public static ExtendedCharacterObject GetExtendedCharacterObject(this BasicCharacterObject basicCharacterObject)
        {
            return MBObjectManager.Instance.GetObject<ExtendedCharacterObject>("NPCCharacter." + basicCharacterObject.StringId);
        }
    }
}
