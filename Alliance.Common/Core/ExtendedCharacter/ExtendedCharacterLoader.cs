using Alliance.Common.Core.ExtendedCharacter.Models;
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
        /// Load the AllianceCharacters file into usable game objects : ExtendedCharacterObject.
        /// </summary>
        public static void Init()
        {
            //if (!File.Exists(ModuleHelper.GetModuleFullPath("Alliance") + "/ModuleData/AllianceCharacters.xml")) 
            InitializeXML();
            CopyXSDs();
            MBObjectManager.Instance.RegisterType<ExtendedCharacterObject>("AllianceCharacter", "AllianceCharacters", 2001, true, false);
            MBObjectManager.Instance.LoadXML("AllianceCharacters", false);
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

            foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
            {
                XmlElement allianceCharacterNode = xmlDoc.CreateElement("AllianceCharacter");

                XmlAttribute characterAttribute = xmlDoc.CreateAttribute("id");
                characterAttribute.Value = "NPCCharacter." + character.StringId;
                allianceCharacterNode.Attributes.Append(characterAttribute);

                XmlAttribute troopLimitAttribute = xmlDoc.CreateAttribute("troop_limit");
                troopLimitAttribute.Value = "1000";
                allianceCharacterNode.Attributes.Append(troopLimitAttribute);

                mpCharacters.AppendChild(allianceCharacterNode);
            }

            XmlElement allianceCharacters = xmlDoc.CreateElement("AllianceCharacters");
            allianceCharacters.InnerXml = mpCharacters.InnerXml;
            xmlDoc.AppendChild(allianceCharacters);

            xmlDoc.Save(moduleFullPath + "/ModuleData/AllianceCharacters.xml");
        }
    }
}
