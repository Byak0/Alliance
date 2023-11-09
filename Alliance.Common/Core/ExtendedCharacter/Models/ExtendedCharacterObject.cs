using System;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedCharacter.Models
{
    /// <summary>
    /// WIP
    /// This class mostly serves as a proof of concept for storing our own XML files and loading them in-game like native ModuleData files.
    /// Currently only used to store a TroopLimit count for each BasicCharacterObject.
    /// It could be further developped to store additional infos.
    /// </summary>
    public class ExtendedCharacterObject : MBObjectBase
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

        public ExtendedCharacterObject()
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
