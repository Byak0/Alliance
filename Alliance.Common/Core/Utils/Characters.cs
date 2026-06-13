using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Core.Utils.AgentExtensions;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Check if the game has loaded characters, provides dummy characters if not.
	/// </summary>
	public class Characters
	{
		public List<BasicCharacterStub> CharacterStubs => _characterStubs;
		public Dictionary<string, BasicCharacterStub> CharacterStubDictionary => _characterStubDictionary;
		public Dictionary<BasicCultureObject, List<BasicCharacterStub>> MPCharactersByCulture => _charactersByCulture;

		private List<BasicCharacterStub> _characterStubs;
		private Dictionary<string, BasicCharacterStub> _characterStubDictionary;
		private Dictionary<BasicCultureObject, List<BasicCharacterStub>> _charactersByCulture;

		private static readonly Characters instance = new Characters();
		public static Characters Instance { get { return instance; } }

		static Characters()
		{
			Instance.RefreshAvailableCharacters();
		}

		public BasicCharacterObject GetCharacterObject(string stringId)
		{
			return GetCharacterStub(stringId)?.CharacterObject;
		}

		public BasicCharacterStub GetCharacterStub(string stringId)
		{
			if (stringId == null || !CharacterStubDictionary.TryGetValue(stringId, out BasicCharacterStub characterStub))
			{
				return null;
			}
			return characterStub;
		}

		public List<BasicCharacterStub> GetCharactersByCulture(BasicCultureObject culture, ClassType classType)
		{
			List<BasicCharacterStub> characters = new List<BasicCharacterStub>();

			if (culture == null || !MPCharactersByCulture.ContainsKey(culture))
			{
				return characters;
			}

			characters = MPCharactersByCulture[culture]
				.Where(c => c.ClassType == classType)
				.ToList();

			return characters;
		}

		public void RefreshAvailableCharacters()
		{
			_characterStubs = new List<BasicCharacterStub>();
			_characterStubDictionary = new Dictionary<string, BasicCharacterStub>();
			_charactersByCulture = new Dictionary<BasicCultureObject, List<BasicCharacterStub>>();

			// Refresh cultures manually. This also initializes MBObjectManager if it wasn't already.
			Factions.Instance.RefreshAvailablecultures();

			// If MPClassDivisions have not been initialized, try to initialize them
			if (MultiplayerClassDivisions.MultiplayerHeroClassGroups == null)
			{
				GameTextManager textManager = new GameTextManager();
				textManager.LoadGameTexts();
				GameTexts.Initialize(textManager);
				MultiplayerClassDivisions.Initialize();
			}

			// Manually load character information from MPClassDivisions (should work in every context)
			Dictionary<string, ClassType> characterClassTypes = LoadCharacterClassTypesFromMPClassDivisions();

			// Retrieve all loaded BasicCharacterObjects (only works in game context, will be empty in modding kit context)
			MBReadOnlyList<BasicCharacterObject> _characterObjects = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();

			// Case 1: We have loaded BasicCharacterObjects (game context) - create stubs from these objects and supplement with MPClassDivisions info
			if (_characterObjects?.Count > 0)
			{
				foreach (BasicCharacterObject character in _characterObjects)
				{
					if (character?.Culture == null) continue;
					ClassType classType = ClassType.None;
					if (characterClassTypes.TryGetValue(character.StringId, out ClassType foundClassType))
					{
						classType = foundClassType;
					}
					BasicCharacterStub stub = new BasicCharacterStub(character.StringId, character.Name, character.Culture, classType, character);
					_characterStubs.Add(stub);
				}
			}
			// Case 2: No BasicCharacterObjects loaded (modding kit context) - create stubs from MPCharacters XML
			else
			{
				LoadCharacterStubsFromXml(characterClassTypes);
			}

			foreach (BasicCharacterStub stub in _characterStubs)
			{
				_characterStubDictionary[stub.StringId] = stub;
				// Organize by culture
				if (!MPCharactersByCulture.ContainsKey(stub.Culture))
				{
					MPCharactersByCulture[stub.Culture] = new List<BasicCharacterStub>();
				}
				MPCharactersByCulture[stub.Culture].Add(stub);
			}
		}

		/// <summary>
		/// Reads MPClassDivisions XML to build a mapping of character ID to ClassType.
		/// </summary>
		/// <returns>Dictionary mapping character StringId to ClassType</returns>
		private Dictionary<string, ClassType> LoadCharacterClassTypesFromMPClassDivisions()
		{
			Dictionary<string, ClassType> characterClassTypes = new Dictionary<string, ClassType>();

			try
			{
				if (!MBObjectManager.Instance.HasType(typeof(MultiplayerClassDivisions.MPHeroClass)))
				{
					MBObjectManager.Instance.RegisterType<MultiplayerClassDivisions.MPHeroClass>("MPClassDivision", "MPClassDivisions", 45U, true, false);
				}
				XmlDocument mpClassDivisionsXml = MBObjectManager.GetMergedXmlForManaged("MPClassDivisions", false);
				if (mpClassDivisionsXml == null)
				{
					Log("MPClassDivisions XML not found. No character stubs will be loaded.", LogLevel.Warning);
					return characterClassTypes;
				}

				XmlNodeList classNodes = mpClassDivisionsXml.SelectNodes("//MPClassDivision");
				foreach (XmlNode node in classNodes)
				{
					string heroCharacter = node.Attributes["hero"]?.Value;
					string troopCharacter = node.Attributes["troop"]?.Value;
					string bannerBearerCharacter = node.Attributes["banner_bearer"]?.Value;

					if (!string.IsNullOrEmpty(heroCharacter))
						characterClassTypes[heroCharacter] = ClassType.Hero;
					if (!string.IsNullOrEmpty(troopCharacter))
						characterClassTypes[troopCharacter] = ClassType.Troop;
					if (!string.IsNullOrEmpty(bannerBearerCharacter))
						characterClassTypes[bannerBearerCharacter] = ClassType.BannerBearer;
				}
			}
			catch(Exception ex)
			{
				Log($"Error loading MPClassDivisions XML: {ex.Message}", LogLevel.Error);
				return new Dictionary<string, ClassType>();
			}

			return characterClassTypes;
		}

		/// <summary>
		/// Loads character stubs from MPCharacters XML.
		/// This is used when running in modding kit context where full character objects are not loaded.
		/// </summary>
		private void LoadCharacterStubsFromXml(Dictionary<string, ClassType> characterClassTypes)
		{
			try
			{
				if(!MBObjectManager.Instance.HasType(typeof(BasicCharacterObject)))
				{
					MBObjectManager.Instance.RegisterType<BasicCharacterObject>("NPCCharacter", "MPCharacters", 43U, true, false);
				}
				XmlDocument mergedMPCharactersXML = MBObjectManager.GetMergedXmlForManaged("MPCharacters", false);
				if (mergedMPCharactersXML == null)
				{
					Log("MPCharacters XML not found. No character stubs will be loaded.", LogLevel.Warning);
					return;
				}

				XmlNodeList characterNodes = mergedMPCharactersXML.SelectNodes("//NPCCharacter");
				foreach (XmlNode node in characterNodes)
				{
					string stringId = node.Attributes["id"]?.Value;
					string name = node.Attributes["name"]?.Value;
					string cultureId = node.Attributes["culture"]?.Value;

					if (string.IsNullOrEmpty(stringId) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(cultureId))
						continue;

					BasicCultureObject culture = MBObjectManager.Instance.ReadObjectReferenceFromXml<BasicCultureObject>("culture", node);
					if (culture == null)
						continue;

					ClassType classType = characterClassTypes.TryGetValue(stringId, out ClassType value) ? value : ClassType.None;

					BasicCharacterStub stub = new BasicCharacterStub(
						stringId,
						new TextObject(name),
						culture,
						classType,
						null); // No BasicCharacterObject available in modding kit context

					_characterStubs.Add(stub);
				}
			}
			catch(Exception ex)
			{
				Log($"Error loading MPCharacters XML: {ex.Message}", LogLevel.Error);
			}
		}

		public class BasicCharacterStub
		{
			public string StringId { get; set; }
			public TextObject Name { get; set; }
			public BasicCultureObject Culture { get; set; }
			public ClassType ClassType { get; set; }
			public BasicCharacterObject CharacterObject { get; set; }

			public BasicCharacterStub(string stringId, TextObject name, BasicCultureObject culture, ClassType classType, BasicCharacterObject characterObject)
			{
				StringId = stringId;
				Name = name;
				Culture = culture;
				ClassType = classType;
				CharacterObject = characterObject;
			}
		}
	}
}
