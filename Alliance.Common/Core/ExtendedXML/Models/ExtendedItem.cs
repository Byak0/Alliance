using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML.Models
{
    /// <summary>
    /// Extends the native Item XML with additional infos.
    /// </summary>
    public class ExtendedItem : MBObjectBase
    {
        public bool Usable { get; private set; }
        public string Prefab { get; private set; }
        public string ParticleName { get; private set; }
        public string SoundOnUse { get; private set; }
        public string AnimationOnUse { get; private set; }
        public int SoundDistance { get; private set; }
        public float Cooldown { get; private set; }
        public int UseLimit { get; private set; }
        public List<ItemEffect> Effects { get; private set; }

        public ExtendedItem()
        {
        }

        public override void Deserialize(MBObjectManager objectManager, XmlNode node)
        {
            base.Deserialize(objectManager, node);
            Usable = XmlHelper.ReadBool(node, "usable");
            Prefab = XmlHelper.ReadString(node, "prefab");
            ParticleName = XmlHelper.ReadString(node, "particle_name");
            SoundOnUse = XmlHelper.ReadString(node, "sound_on_use");
            AnimationOnUse = XmlHelper.ReadString(node, "animation_on_use");
            Cooldown = XmlHelper.ReadFloat(node, "cooldown");
            SoundDistance = XmlHelper.ReadInt(node, "sound_distance");
            UseLimit = XmlHelper.ReadInt(node, "use_limit");
            Effects = new List<ItemEffect>();
            if (node.FirstChild?.Name == "Effects")
            {
                foreach (XmlNode child in node.FirstChild.ChildNodes)
                {
                    Effects.Add(new ItemEffect(
                        XmlHelper.ReadString(child, "type"),
                        XmlHelper.ReadString(child, "value"),
                        XmlHelper.ReadString(child, "position")
                    ));
                }
            }

        }
    }

    public class ItemEffect
    {
        public string Type { get; private set; }
        public string Value { get; private set; }
        public string Position { get; private set; }

        public ItemEffect(string type, string value, string position)
        {
            Type = type;
            Value = value;
            Position = position;
        }
    }
}
