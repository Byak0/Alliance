using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Check if the game has loaded characters, provides dummy characters if not.
	/// </summary>
	public class Characters
	{
		public List<BasicCharacterObject> CharacterObjects;
		public List<BasicCharacterStub> CharacterStubs;
		public Dictionary<string, BasicCharacterObject> CharacterDictionary;
		public Dictionary<string, BasicCharacterStub> CharacterStubDictionary;

		private bool _trueCharactersLoaded = false;
		public bool TrueCharactersLoaded { get { return _trueCharactersLoaded; } }

		private static readonly Characters instance = new Characters();
		public static Characters Instance { get { return instance; } }

		static Characters()
		{
			Instance.RefreshAvailableCharacters();
		}

		public BasicCharacterObject GetCharacterObject(string stringId)
		{
			if (stringId == null || !CharacterDictionary.TryGetValue(stringId, out BasicCharacterObject characterObject))
			{
				return null;
			}
			return characterObject;
		}

		public BasicCharacterStub GetCharacterStub(string stringId)
		{
			if (stringId == null || !CharacterStubDictionary.TryGetValue(stringId, out BasicCharacterStub characterStub))
			{
				return null;
			}
			return characterStub;
		}

		public bool TryRefreshCharacters()
		{
			if (MBObjectManager.Instance != null && CharacterObjects?.Count > 0)
				return true;

			RefreshAvailableCharacters();

			return CharacterObjects?.Count > 0;
		}

		public void RefreshAvailableCharacters()
		{
			// Clear existing lists and dictionaries
			CharacterObjects = new List<BasicCharacterObject>();
			CharacterStubs = new List<BasicCharacterStub>();
			CharacterDictionary = new Dictionary<string, BasicCharacterObject>();
			CharacterStubDictionary = new Dictionary<string, BasicCharacterStub>();

			if (MBObjectManager.Instance != null && MBObjectManager.Instance.HasType(typeof(BasicCharacterObject)))
			{
				// Retrieve the list of available characters from MBObjectManager
				CharacterObjects = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>().ToList();
				CharacterDictionary = CharacterObjects.ToDictionary(x => x.StringId);
				_trueCharactersLoaded = true;

				// Ensure MPClassDivisions are loaded
				if (!MBObjectManager.Instance.HasType(typeof(MultiplayerClassDivisions.MPHeroClass)))
				{
					MBObjectManager.Instance.RegisterType<MultiplayerClassDivisions.MPHeroClass>("MPClassDivision", "MPClassDivisions", 45U, true, false);
					MBObjectManager.Instance.LoadXML("MPClassDivisions", true, "", false);
				}
			}
			else
			{
				_trueCharactersLoaded = false;
			}

			// Refresh cultures manually. This also initializes MBObjectManager if it wasn't already.
			Factions.Instance.RefreshAvailablecultures();

			// Manually load MPCharacters into CharacterStubs
			XmlDocument mergedXmlForManaged = MBObjectManager.GetMergedXmlForManaged("MPCharacters", false);
			XmlNodeList characterNodes = mergedXmlForManaged.SelectNodes("//NPCCharacter");
			foreach (XmlNode node in characterNodes)
			{
				string stringId = node.Attributes["id"]?.Value;
				string name = node.Attributes["name"]?.Value;
				string cultureId = node.Attributes["culture"]?.Value;
				if (!string.IsNullOrEmpty(stringId) && !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(cultureId))
				{
					BasicCultureObject culture = MBObjectManager.Instance.ReadObjectReferenceFromXml<BasicCultureObject>("culture", node);
					CharacterStubDictionary[stringId] = new BasicCharacterStub(stringId, new TextObject(name), culture);
				}
			}
			CharacterStubs = CharacterStubDictionary.Values.ToList();
		}

		public class BasicCharacterStub
		{
			public string StringId { get; set; }
			public TextObject Name { get; set; }
			public BasicCultureObject Culture { get; set; }

			public BasicCharacterStub(string stringId, TextObject name, BasicCultureObject culture)
			{
				StringId = stringId;
				Name = name;
				Culture = culture;
			}
		}
	}
}
