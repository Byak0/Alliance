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
				.Where(c => c.ClassTypes.Contains(classType))
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

			// Manually load character information from MPClassDivisions (should work in every context).
			// A single CharacterObject can appear in multiple MPClassDivisions with different roles
			Dictionary<string, HashSet<ClassType>> characterClassTypes = LoadCharacterClassTypesFromMPClassDivisions();

			// Retrieve all loaded BasicCharacterObjects (only works in game context, will be empty in modding kit context)
			MBReadOnlyList<BasicCharacterObject> _characterObjects = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>();

			// Case 1: We have loaded BasicCharacterObjects (game context) - create stubs from these objects and supplement with MPClassDivisions info
			if (_characterObjects?.Count > 0)
			{
				foreach (BasicCharacterObject character in _characterObjects)
				{
					if (character?.Culture == null) continue;
					HashSet<ClassType> classTypes = characterClassTypes.TryGetValue(character.StringId, out HashSet<ClassType> foundClassTypes)
						? foundClassTypes
						: new HashSet<ClassType> { ClassType.None };
					BasicCharacterStub stub = new BasicCharacterStub(character.StringId, character.Name, character.Culture, classTypes, character);
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
		/// Reads MPClassDivisions XML to build a mapping of character ID to all their ClassTypes.
		/// </summary>
		/// <returns>Dictionary mapping character StringId to the set of all registered ClassTypes</returns>
		private Dictionary<string, HashSet<ClassType>> LoadCharacterClassTypesFromMPClassDivisions()
		{
			Dictionary<string, HashSet<ClassType>> characterClassTypes = new Dictionary<string, HashSet<ClassType>>();

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
						RegisterRole(characterClassTypes, heroCharacter, ClassType.Hero);
					if (!string.IsNullOrEmpty(troopCharacter))
						RegisterRole(characterClassTypes, troopCharacter, ClassType.Troop);
					if (!string.IsNullOrEmpty(bannerBearerCharacter))
						RegisterRole(characterClassTypes, bannerBearerCharacter, ClassType.BannerBearer);
				}
			}
			catch (Exception ex)
			{
				Log($"Error loading MPClassDivisions XML: {ex.Message}", LogLevel.Error);
				return new Dictionary<string, HashSet<ClassType>>();
			}

			return characterClassTypes;
		}

		private static void RegisterRole(Dictionary<string, HashSet<ClassType>> dict, string characterId, ClassType role)
		{
			if (!dict.TryGetValue(characterId, out HashSet<ClassType> roles))
			{
				roles = new HashSet<ClassType>();
				dict[characterId] = roles;
			}
			roles.Add(role);
		}

		/// <summary>
		/// Loads character stubs from MPCharacters XML.
		/// This is used when running in modding kit context where full character objects are not loaded.
		/// </summary>
		private void LoadCharacterStubsFromXml(Dictionary<string, HashSet<ClassType>> characterClassTypes)
		{
			try
			{
				if (!MBObjectManager.Instance.HasType(typeof(BasicCharacterObject)))
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

					HashSet<ClassType> classTypes = characterClassTypes.TryGetValue(stringId, out HashSet<ClassType> found)
						? found
						: new HashSet<ClassType> { ClassType.None };

					BasicCharacterStub stub = new BasicCharacterStub(
						stringId,
						new TextObject(name),
						culture,
						classTypes,
						null); // No BasicCharacterObject available in modding kit context

					_characterStubs.Add(stub);
				}
			}
			catch (Exception ex)
			{
				Log($"Error loading MPCharacters XML: {ex.Message}", LogLevel.Error);
			}
		}

		public class BasicCharacterStub
		{
			public string StringId { get; set; }
			public TextObject Name { get; set; }
			public BasicCultureObject Culture { get; set; }
			public BasicCharacterObject CharacterObject { get; set; }
			public HashSet<ClassType> ClassTypes { get; set; }

			public BasicCharacterStub(string stringId, TextObject name, BasicCultureObject culture, HashSet<ClassType> classTypes, BasicCharacterObject characterObject)
			{
				StringId = stringId;
				Name = name;
				Culture = culture;
				ClassTypes = classTypes ?? new HashSet<ClassType> { ClassType.None };
				CharacterObject = characterObject;
			}

			public bool HasClassType(ClassType classType)
			{
				return ClassTypes.Contains(classType);
			}
		}
	}
}
