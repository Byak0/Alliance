using Alliance.Common.Core.ExtendedXML.Models;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

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
			// Manually add our custom XSD to the list so the game can look them up when loading XML
			RegisterNewXMLTypeWithXSD<ExtendedCharacter>("CharacterExtended", "CharactersExtended", 2001, "CharactersExtended", SubModule.CurrentModuleName);
			RegisterNewXMLTypeWithXSD<ExtendedItem>("ItemExtended", "ItemsExtended", 2002, "ItemsExtended", SubModule.CurrentModuleName);

			LoadXML("CharactersExtended");
			LoadXML("ItemsExtended");
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

		public static void RegisterNewXMLTypeWithXSD<T>(string classPrefix, string classListPrefix, uint typeId, string xsdFileName, string moduleName) where T : MBObjectBase
		{
			string xsdFilePath = ModuleHelper.GetXsdPathForModules(moduleName, xsdFileName);
			if (!File.Exists(xsdFilePath))
			{
				Log("XSD file not found: " + xsdFilePath, LogLevel.Error);
				return;
			}
			// Read the XSD file and store extracted information
			XmlResource.ReadXsdFileAndExtractInformation(xsdFilePath);
			// Duplicate extracted information under default path (used when loading from other modules)
			XmlResource.XsdElementDictionary[ModuleHelper.GetXsdPath(xsdFileName)] = XmlResource.XsdElementDictionary[xsdFilePath];
			// Register the new type with MBObjectManager
			MBObjectManager.Instance.RegisterType<T>(classPrefix, classListPrefix, typeId, true, false);
		}

		public static void LoadXML(string xmlType)
		{
			if (XmlResource.XmlInformationList.Exists(xmlInfo => xmlInfo.Id == xmlType))
			{
				XmlDocument xmlDocument = MBObjectManager.GetMergedXmlForManaged(xmlType, true);
				MBObjectManager.Instance.LoadXml(xmlDocument);
			}
		}
	}
}
