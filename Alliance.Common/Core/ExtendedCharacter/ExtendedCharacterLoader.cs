using Alliance.Common.Core.ExtendedCharacter.Models;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedCharacter
{
    /// <summary>
    /// Initializer for ExtendedCharacter.
    /// </summary>
    public class ExtendedCharacterLoader
    {
        /// <summary>
        /// Load the ExtendedCharacters file into usable game objects : ExtendedCharacterObject.
        /// </summary>
        public static void Init()
        {
            if (!File.Exists(ModuleHelper.GetModuleFullPath("Alliance") + "/ModuleData/ExtendedCharacters.xml"))
            {
                InitializeXML();
            }
            CopyXSDs();
            MBObjectManager.Instance.RegisterType<ExtendedCharacterObject>("ExtendedCharacter", "ExtendedCharacters", 2001, true, false);
            MBObjectManager.Instance.LoadXML("ExtendedCharacters", false);
        }

        // Game requires the XSD to be in Mount & Blade II Bannerlord\XmlSchemas to load custom schemas
        public static void CopyXSDs()
        {
            string moduleFullPath = ModuleHelper.GetModuleFullPath("Alliance");
            string[] files = Directory.GetFiles(moduleFullPath + "/XmlSchemas");
            foreach (string file in files)
            {
                File.Copy(file, Path.Combine(moduleFullPath, "..\\..\\XmlSchemas/" + Path.GetFileName(file)), true);
            }
        }

        // Test to auto generate XML
        private static void InitializeXML()
        {
            string moduleFullPath = ModuleHelper.GetModuleFullPath("Alliance");
            XmlDocument xmlDoc = new();
            XmlElement mpCharacters = xmlDoc.CreateElement("MPCharacters");
            List<BasicCharacterObject> gameCharacters = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();

            foreach (BasicCharacterObject character in gameCharacters)
            {
                XmlElement extendedCharacterNode = xmlDoc.CreateElement("ExtendedCharacter");

                XmlAttribute characterAttribute = xmlDoc.CreateAttribute("id");
                characterAttribute.Value = "NPCCharacter." + character.StringId;
                extendedCharacterNode.Attributes.Append(characterAttribute);

                XmlAttribute troopLimitAttribute = xmlDoc.CreateAttribute("troop_limit");
                troopLimitAttribute.Value = "1000";
                extendedCharacterNode.Attributes.Append(troopLimitAttribute);

                mpCharacters.AppendChild(extendedCharacterNode);
            }

            XmlElement extendedCharacters = xmlDoc.CreateElement("ExtendedCharacters");
            extendedCharacters.InnerXml = mpCharacters.InnerXml;
            xmlDoc.AppendChild(extendedCharacters);

            xmlDoc.Save(moduleFullPath + "/ModuleData/ExtendedCharacters.xml");
        }
    }
}
