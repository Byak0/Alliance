using Alliance.Common.Core.ExtendedXML.Models;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.ExtendedXML
{
	/// <summary>
	/// Initializer for our custom XML files.
	/// </summary>
	public class ExtendedXMLLoader
	{
		/// <summary>
		/// Load our custom XML.
		/// </summary>
		public static void Init()
		{
			CopyXSDs();

			MBObjectManager.Instance.RegisterType<ExtendedCharacter>("CharacterExtended", "CharactersExtended", 2001, true, false);
			MBObjectManager.Instance.LoadXML("CharactersExtended", false);

			MBObjectManager.Instance.RegisterType<ExtendedItem>("ItemExtended", "ItemsExtended", 2002, true, false);
			MBObjectManager.Instance.LoadXML("ItemsExtended", false);
		}

		// Game requires the XSD to be in Mount & Blade II Bannerlord\XmlSchemas to load custom schemas
		public static void CopyXSDs()
		{
			string moduleFullPath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
			string[] files = Directory.GetFiles(moduleFullPath + "/XmlSchemas");
			foreach (string file in files)
			{
				File.Copy(file, Path.Combine(moduleFullPath, "..\\..\\XmlSchemas/" + Path.GetFileName(file)), true);
			}
		}

		// Test to auto generate XML
		private static void InitializeXML()
		{
			string moduleFullPath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
			XmlDocument xmlDoc = new();
			XmlElement mpCharacters = xmlDoc.CreateElement("MPCharacters");
			List<BasicCharacterObject> gameCharacters = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();

			foreach (BasicCharacterObject character in gameCharacters)
			{
				XmlElement extendedCharacterNode = xmlDoc.CreateElement("CharacterExtended");

				XmlAttribute characterAttribute = xmlDoc.CreateAttribute("id");
				characterAttribute.Value = "NPCCharacter." + character.StringId;
				extendedCharacterNode.Attributes.Append(characterAttribute);

				XmlAttribute troopLimitAttribute = xmlDoc.CreateAttribute("troop_limit");
				troopLimitAttribute.Value = "1000";
				extendedCharacterNode.Attributes.Append(troopLimitAttribute);

				mpCharacters.AppendChild(extendedCharacterNode);
			}

			XmlElement CharactersExtended = xmlDoc.CreateElement("CharactersExtended");
			CharactersExtended.InnerXml = mpCharacters.InnerXml;
			xmlDoc.AppendChild(CharactersExtended);

			xmlDoc.Save(moduleFullPath + "/ModuleData/CharactersExtended.xml");
		}
	}
}
