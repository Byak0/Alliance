using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML.Models
{
    /// <summary>
    /// Extends the native Character XML with additional infos.
    /// </summary>
    public class ExtendedCharacter : MBObjectBase
    {
        private int _troopLeft = 0;
        private int _troopLimit = 0;
        BasicCharacterObject _basicCharacterObject;

        public int TroopLeft
        {
            get
            {
                return _troopLeft;
            }
            set
            {
                _troopLeft = Math.Max(0, value);
            }
        }

        public int TroopLimit
        {
            get
            {
                return _troopLimit;
            }
            set
            {
                _troopLimit = value;
            }
        }

        public BasicCharacterObject BasicCharacterObject
        {
            get
            {
                return _basicCharacterObject;
            }
            private set
            {
                _basicCharacterObject = value;
            }
        }

        public ExtendedCharacter()
        {
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            XmlHelper.ReadInt(ref _troopLimit, node, "troop_limit");
            _basicCharacterObject = objectManager.ReadObjectReferenceFromXml<BasicCharacterObject>("id", node);
            TroopLeft = TroopLimit;
        }
    }
}
