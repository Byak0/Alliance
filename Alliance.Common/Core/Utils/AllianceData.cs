using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.BuildSystem.Configuration;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Alliance's single source of truth for game data: native catalogs (characters, items, cultures, skins, monsters, action sets, holsters)
	/// AND the option lists exposed to the editor/config dropdowns (maps, cultures, characters, sounds, prefabs, fonts, clips...).
	///
	/// Native catalogs are read with plain XML - deliberately NOT through native object deserialization,
	/// which requires Game.Current lookups that don't exist in the modding-kit editing context.
	/// Managed types (NPCCharacters/MPCharacters, Items, Cultures, MPClassDivisions) are read through MBObjectManager.GetMergedXmlForManaged
	/// Engine-consumed files with no SubModule.xml declaration (skins.xml, action_sets.xml, monsters.xml, item_holsters.xml, voices.xml)
	/// are parsed from each active module's project.mbproj directly.
	///
	/// Live <see cref="BasicCharacterObject"/>s are resolved lazily from the MBObjectManager on demand (<see cref="GetCharacterObject"/>),
	/// so no refresh is required when native objects appear after the catalogs were parsed.
	/// </summary>
	public class AllianceData
	{
		// ---------------------------------------------------------------------------------------
		// Option catalogs (editor & config dropdowns)
		// ---------------------------------------------------------------------------------------

		public enum DataTypes
		{
			None,
			Map,
			Culture,
			BattleSide,
			Character,
			Item,
			GameMode,
			Sounds,
			NativeSounds,
			Difficulty,
			Prefab,
			Font,
			Color,
			Animation,
			FacialAnimation,
			Atmosphere
		}

		public enum Difficulty
		{
			Easy = 0,
			Normal = 1,
			Hard = 2,
			VeryHard = 3,
			Bannerlord = 4
		}

		private string[] _nativeSoundEvents;
		private Dictionary<string, float> _nativeSoundEventLengths;
		private string[] _facialAnimations;
		private string[] _atmospheres;

		public static string[] AvailableMaps() => SceneList.Scenes.ConvertAll(s => s.Name).ToArray();

		public static string[] AvailableCultures() => Factions.Instance.OrderedCultureKeys.ToArray();

		public static string[] AvailableCharacters() => Instance.GetMultiplayerCharacters().ConvertAll(c => c.Id).ToArray();

		public static string[] AvailableItems() => Instance._items.Values.Select(i => i.Id).OrderBy(i => i).ToArray();

		public static string[] AvailableSounds() => AudioPlayer.Instance.GetAvailableSounds();

		public static string[] AvailableNativeSoundEvents() => Instance._nativeSoundEvents;

		public static string[] AvailablePrefabs() => BuildPrefabCatalogManager.AllPrefabNames;

		public static string[] AvailableAnimations() => AnimationSystem.Instance.DefaultAnimations?.Select(a => a.Action.GetName()).ToArray() ?? Array.Empty<string>();

		public static string[] AvailableClips() => Instance._clips.OrderBy(c => c).ToArray();

		public static string[] AvailableFacialAnimations() => Instance._facialAnimations;

		public static string[] AvailableAtmospheres() => Instance._atmospheres;

		public static string[] AvailableFonts()
		{
#if !SERVER
			return TaleWorlds.Engine.GauntletUI.UIResourceManager.FontFactory.GetFonts().Select(TaleWorlds.Engine.GauntletUI.UIResourceManager.FontFactory.GetFontName).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).ToArray();
#else
			// Server don't have access to the GauntletUI assembly, so just return an empty list instead...
			return Array.Empty<string>();
#endif
		}

		public static readonly string[] AvailableSides = new string[] { BattleSideEnum.Defender.ToString(), BattleSideEnum.Attacker.ToString() };

		public static readonly string[] AvailableGameModes = new string[] { "Lobby", "Scenario", "BattleRoyale", "PvC", "CvC", "CaptainX", "BattleX", "SiegeX", "Captain", "Battle", "Siege", "Skirmish" };

		public static readonly string[] AvailableDifficulties = new string[]
		{
			nameof(Difficulty.Easy),
			nameof(Difficulty.Normal),
			nameof(Difficulty.Hard),
			nameof(Difficulty.VeryHard),
			nameof(Difficulty.Bannerlord)
		};

		public static string[] GetAvailableValuesForType(DataTypes dataType)
		{
			return dataType switch
			{
				DataTypes.Map => AvailableMaps(),
				DataTypes.Culture => AvailableCultures(),
				DataTypes.BattleSide => AvailableSides,
				DataTypes.Character => AvailableCharacters(),
				DataTypes.Item => AvailableItems(),
				DataTypes.GameMode => AvailableGameModes,
				DataTypes.Sounds => AvailableSounds(),
				DataTypes.NativeSounds => AvailableNativeSoundEvents(),
				DataTypes.Difficulty => AvailableDifficulties,
				DataTypes.Prefab => AvailablePrefabs(),
				DataTypes.Font => AvailableFonts(),
				DataTypes.Animation => AvailableAnimations(),
				DataTypes.FacialAnimation => AvailableFacialAnimations(),
				DataTypes.Atmosphere => AvailableAtmospheres(),
				_ => Array.Empty<string>(),
			};
		}

		/// <summary>Definition of a character: culture + equipment item ids per slot.</summary>
		public class CharacterDefinition
		{
			public string Id;
			public string Name;
			public string CultureId;
			public string RaceId;
			public bool IsFemale;
			public bool IsMultiplayer;
			public HashSet<AgentExtensions.ClassType> ClassTypes = new HashSet<AgentExtensions.ClassType> { AgentExtensions.ClassType.None };
			public readonly List<Dictionary<int, string>> EquipmentRosters = new List<Dictionary<int, string>>();

			private Dictionary<int, ItemDefinition>[] _resolvedRosters;

			public bool HasClassType(AgentExtensions.ClassType classType) => ClassTypes.Contains(classType);

			// This is null in contexts where native objects aren't loaded, e.g. the modding kit
			public BasicCharacterObject CharacterObject => AllianceData.Instance.GetCharacterObject(Id);

			public BasicCultureObject Culture => CultureId != null && Factions.Instance.AvailableCultures.TryGetValue(CultureId, out BasicCultureObject culture) ? culture : null;

			/// <summary>Display name: localized object name when available, raw XML name otherwise.</summary>
			public string DisplayName => CharacterObject?.Name?.ToString() ?? StripLocalizationPrefix(Name) ?? Id;

			public Dictionary<int, string> SelectRoster(int seed)
			{
				return EquipmentRosters[((seed % EquipmentRosters.Count) + EquipmentRosters.Count) % EquipmentRosters.Count];
			}

			/// <summary>The selected roster resolved to catalog item definitions (unknown ids dropped) -
			/// cached per roster, so a thousand-strong crowd of the same character stops re-resolving
			/// item ids on every spawn.</summary>
			public Dictionary<int, ItemDefinition> SelectResolvedRoster(int seed)
			{
				int index = ((seed % EquipmentRosters.Count) + EquipmentRosters.Count) % EquipmentRosters.Count;
				if (_resolvedRosters == null) _resolvedRosters = new Dictionary<int, ItemDefinition>[EquipmentRosters.Count];
				Dictionary<int, ItemDefinition> resolved = _resolvedRosters[index];
				if (resolved == null)
				{
					resolved = _resolvedRosters[index] = new Dictionary<int, ItemDefinition>();
					foreach (KeyValuePair<int, string> entry in EquipmentRosters[index])
					{
						ItemDefinition item = AllianceData.Instance.GetItem(entry.Value);
						if (item != null) resolved[entry.Key] = item;
					}
				}
				return resolved;
			}

			/// <summary>Shallow copy for display: race and gender overridden when given (null = keep).</summary>
			public CharacterDefinition Clone(string raceId = null, bool? isFemale = null)
			{
				CharacterDefinition clone = new CharacterDefinition
				{
					Id = Id,
					Name = Name,
					CultureId = CultureId,
					RaceId = string.IsNullOrEmpty(raceId) ? RaceId : raceId,
					IsFemale = isFemale ?? IsFemale,
					IsMultiplayer = IsMultiplayer,
					ClassTypes = ClassTypes
				};
				clone.EquipmentRosters.AddRange(EquipmentRosters);
				return clone;
			}
		}

		public enum ItemKind
		{
			/// <summary>Skinned on the body skeleton (clothes, armor, gloves, anything worn).</summary>
			Garment,
			/// <summary>Rigid weapon or throwable, carried on hand/holster bones.</summary>
			Weapon,
			/// <summary>Projectiles - only the quiver mesh is worn.</summary>
			Ammo,
			/// <summary>Hidden while sheathed, carried in the off hand when drawn.</summary>
			Banner,
			Mount,
			MountHarness
		}

		public class ItemDefinition
		{
			public string Id;
			public string MeshName;
			public string ItemType;
			public ItemKind Kind;
			public bool IsOffHand;
			public bool IsCrafted;
			public bool HasArmor;
			public bool UsingTableau;
			public string ManeCoverType;
			public bool HasLowerHolsterPriority;
			public string WeaponClass;
			public bool CoversHead;
			public bool CoversBody;
			public bool CoversHands;
			public bool CoversLegs;
			public string HolsterMesh;
			public string HolsterWithWeaponMesh;
			public Vec3 HolsterPositionShift;
			public Vec3 WeaponPosition;
			public Vec3 WeaponRotation;
			public readonly List<string> Holsters = new List<string>();
			public string MountMonsterId;
			public readonly List<KeyValuePair<string, bool>> AdditionalMeshes = new List<KeyValuePair<string, bool>>();
			public ItemFlags NativeFlags;

			public SkinMask MeshesMask
			{
				get
				{
					SkinMask mask = 0;
					if (!CoversHead) mask |= SkinMask.HeadVisible;
					if (!CoversBody) mask |= SkinMask.BodyVisible;
					if (!CoversHands) mask |= SkinMask.HandsVisible;
					if (!CoversLegs) mask |= SkinMask.LegsVisible;
					return mask;
				}
			}
		}

		public class SkinDefinition
		{
			public string Skeleton;
			public string Head;
			public string Body;
			public string BodyShoulders;
			public string Legs;
			public string Hands;
			public string UnderwearBottom;
			public string UnderwearTop;
			public float MinScale = 1f;
		}

		public class MonsterDefinition
		{
			public string Id;
			public string ActionSetId;
			public string MonsterUsage;
			public float WalkingSpeed;
			public string MainHandItemBone;
			public string OffHandItemBone;
			public string OffHandItemSecondaryBone;
			public string ThoraxBone;
			public string SpineLowerBone;
			public string RiderSitBone;
		}

		public class MonsterUsageDefinition
		{
			public string Id;
			/// <summary>Forward movement action per pace (direction=front, no turn, right foot):
			/// 1 = stand, 2 = walk, 3 = trot, 4 = canter, 5 = gallop.</summary>
			public readonly Dictionary<int, string> MovementActions = new Dictionary<int, string>();
			/// <summary>Upper body action per pace (direction=none, no turn): the natural stand/idle is pace 1.</summary>
			public readonly Dictionary<int, string> UpperBodyActions = new Dictionary<int, string>();

			public string GetMovementAction(int pace) => MovementActions.TryGetValue(pace, out string action) ? action : null;

			public string GetUpperBodyAction(int pace) => UpperBodyActions.TryGetValue(pace, out string action) ? action : null;
		}

		public class HolsterDefinition
		{
			public string Id;
			/// <summary>Bone the holster hangs on (e.g. "biped_thorax"); inherited through base_set chains.</summary>
			public string Bone;
			public string Skeleton;
			public string Group;
			public Vec3 Position;
			public Vec3 RotationYawPitchRoll;
			public bool ShowHolsterWhenDrawn;
		}

		// Managed type ids whose merged XML contains the data we need. Every module can declare
		// these in its SubModule.xml (Xmls/XmlNode) - all active declarations are merged.
		private static readonly string[] CharacterTypeIds = { "NPCCharacters", "MPCharacters" };
		private static readonly string[] ItemTypeIds = { "Items", "SPItems", "MPItems" };

		private static AllianceData _instance;
		public static AllianceData Instance => _instance ??= new AllianceData();
		private static readonly object _loadLock = new object();

		private readonly Dictionary<string, CharacterDefinition> _characters = new Dictionary<string, CharacterDefinition>();
		private readonly Dictionary<string, ItemDefinition> _items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<(string Race, bool Female), SkinDefinition> _skins = new Dictionary<(string, bool), SkinDefinition>();
		private readonly Dictionary<string, string> _skeletonOfActionSet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, Dictionary<string, string>> _actionsByActionSet = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _baseSetOfActionSet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, List<string>> _actionSetIdsBySkeleton = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
		private readonly List<string> _actionSetIdsInOrder = new List<string>();
		private readonly HashSet<string> _clips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, float> _clipDurations = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, MonsterDefinition> _monsters = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, MonsterUsageDefinition> _monsterUsages = new Dictionary<string, MonsterUsageDefinition>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, HolsterDefinition> _holsters = new Dictionary<string, HolsterDefinition>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _holsterBaseIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, List<HolsterDefinition>> _holstersByGroup = new Dictionary<string, List<HolsterDefinition>>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, int> _holsterRigUsage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, string> _monsterBaseIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, HashSet<AgentExtensions.ClassType>> _characterClassTypes = new Dictionary<string, HashSet<AgentExtensions.ClassType>>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, List<CharacterDefinition>> _charactersByCultureId = new Dictionary<string, List<CharacterDefinition>>(StringComparer.OrdinalIgnoreCase);
		private string[] _raceNames;
		private Dictionary<string, int> _raceIds;

		private AllianceData()
		{
			LoadAll();
			InitMPClassDivisions();
		}

		private void LoadAll()
		{
			lock (_loadLock)
			{				
				// Also initializes MBObjectManager if it wasn't already (merged XML access needs it).
				Factions.Instance.RefreshAvailablecultures();
				BuildRaceCache();
				foreach (string typeId in CharacterTypeIds) LoadCharacters(typeId);
				foreach (string typeId in ItemTypeIds) LoadItems(typeId);
				LoadSkins();
				LoadActionSets();
				LoadFacialAnims();
				LoadMonsters();
				LoadMonsterUsages();
				LoadHolsters();
				LoadMPClassDivisions();
				LoadSounds();
				LoadAtmospheres();
				RebuildCultureIndex();
			}

			Log($"[AllianceData] Loaded {_characters.Count} characters, {_items.Count} items, {Factions.Instance.AvailableCultures.Count} cultures, {_skins.Count} skins, {_clips.Count} clips, {_monsters.Count} monsters, {_monsterUsages.Count} monster usages, {_holsters.Count} holsters, {_atmospheres.Length} atmospheres.", LogLevel.Debug);
		}

		private void LoadFacialAnims()
		{
			try
			{
				List<string> ids = new List<string>();
				HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string path in GetNativeXmlPaths("face_animation_record"))
				{
					XmlDocument voices = new XmlDocument();
					voices.Load(path);
					foreach (XmlNode node in voices.SelectNodes("//face_animation_record"))
					{
						string id = node.Attributes?["id"]?.Value;
						if (!string.IsNullOrEmpty(id) && known.Add(id)) ids.Add(id);
					}
				}
				_facialAnimations = ids.ToArray();
			}
			catch (Exception ex)
			{
				Log($"[AllianceData] Failed to read the native facial animation catalog : {ex.Message}", LogLevel.Debug);
				_facialAnimations = Array.Empty<string>();
			}
		}

		/// <summary>Atmospheres are plain XML files under each module's Atmospheres folder, consumed
		/// directly by the engine - there is no native API to enumerate them.</summary>
		private void LoadAtmospheres()
		{
			HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				try
				{
					string atmosphereFolder = Path.Combine(module.FolderPath, "Atmospheres");
					if (!Directory.Exists(atmosphereFolder)) continue;
					foreach (string path in Directory.GetFiles(atmosphereFolder, "*.xml", SearchOption.AllDirectories))
					{
						XmlDocument xml = new XmlDocument();
						xml.Load(path);
						string name = xml.SelectSingleNode("//values/value[@name='name']")?.Attributes?["value"]?.Value;
						if (!string.IsNullOrEmpty(name)) names.Add(name);
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to read atmospheres of module '{module.Id}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			List<string> sorted = names.ToList();
			sorted.Sort(StringComparer.OrdinalIgnoreCase);
			_atmospheres = sorted.ToArray();
		}

		private void LoadSounds()
		{
			HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Dictionary<string, float> lengths = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
			try
			{
				// Native module declare sounds in sound_event_data.xml (and its module_sound.xml is empty)
				foreach (string path in GetNativeXmlPaths("sound_event_data"))
				{
					XmlDocument catalog = new XmlDocument();
					catalog.Load(path);
					foreach (XmlNode node in catalog.SelectNodes("//events/event"))
					{
						string eventPath = node.Attributes?["path"]?.Value;
						if (string.IsNullOrEmpty(eventPath)) continue;
						names.Add(eventPath);
						if (float.TryParse(node.Attributes?["length"]?.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float length) && length > 0f)
							lengths[eventPath] = length;
					}
				}
				// Other modules usually declare them in module_sound.xml
				foreach (string path in GetNativeXmlPaths("module_sound"))
				{
					XmlDocument sounds = new XmlDocument();
					sounds.Load(path);
					ModuleInfo module = null;
					foreach (XmlNode node in sounds.SelectNodes("//module_sound"))
					{
						string name = node.Attributes?["name"]?.Value;
						if (string.IsNullOrEmpty(name)) continue;
						names.Add(name);
						string file = node.Attributes?["path"]?.Value;
						if (string.IsNullOrEmpty(file))
						{
							foreach (XmlNode variation in node.SelectNodes("variation"))
							{
								string variationPath = variation.Attributes?["path"]?.Value;
								if (!string.IsNullOrEmpty(variationPath))
								{
									file = variationPath;
									break;
								}
							}
						}
						if (string.IsNullOrEmpty(file)) continue;
						module = module ?? GetModuleOfPath(path);
						if (module == null) continue;
						string catalogKey = $"{module.Id}/{file.Replace('\\', '/')}".ToLowerInvariant();
						float? duration = AudioPlayer.Instance.GetSoundDuration(catalogKey);
						if (duration.HasValue) lengths[name] = duration.Value;
					}
				}
			}
			catch (Exception ex)
			{
				Log($"Failed to read the native sound event catalogs: {ex.Message}", LogLevel.Debug);
			}
			List<string> sorted = names.ToList();
			sorted.Sort(StringComparer.OrdinalIgnoreCase);
			_nativeSoundEvents = sorted.ToArray();
			_nativeSoundEventLengths = lengths;
		}

		public CharacterDefinition GetCharacter(string characterId)
		{
			if (string.IsNullOrEmpty(characterId)) return null;
			return _characters.TryGetValue(characterId, out CharacterDefinition character) ? character : null;
		}

		/// <summary>Slots of a native equipment code: slot index -> item id, plus the banner item
		/// when the code carries one. Null when the code is empty or fails to parse.</summary>
		public class ParsedEquipmentCode
		{
			public Dictionary<int, string> Slots = new Dictionary<int, string>();
			public ItemObject BannerItem;
		}

		/// <summary>Parses a native equipment code once for all display needs (outfit + banner).</summary>
		public static ParsedEquipmentCode ParseEquipmentCode(string equipmentCode)
		{
			if (string.IsNullOrEmpty(equipmentCode)) return null;
			try
			{
				Equipment equipment = Equipment.CreateFromEquipmentCode(equipmentCode);
				ParsedEquipmentCode parsed = new ParsedEquipmentCode();
				for (EquipmentIndex index = EquipmentIndex.Weapon0; index <= EquipmentIndex.ArmorItemEndSlot; index++)
				{
					ItemObject item = equipment[index].Item;
					if (item != null) parsed.Slots[(int)index] = item.StringId;
				}
				ItemObject banner = equipment[EquipmentIndex.ExtraWeaponSlot].Item;
				if (banner != null && banner.IsBannerItem) parsed.BannerItem = banner;
				return parsed;
			}
			catch (Exception ex)
			{
				Log($"[AllianceData] Failed to parse the equipment code of a display character ({ex.Message}).", LogLevel.Warning);
				return null;
			}
		}

		/// <summary>Character for display: the catalog character as-is when every display input
		/// matches it, otherwise a <see cref="CharacterDefinition.Clone"/> of it. 
		/// The parsed equipment code's outfit, when it has any slot, replaces the character's own rosters entirely.</summary>
		public CharacterDefinition GetDisplayCharacter(string characterId, ParsedEquipmentCode code = null, bool isFemale = false, string raceId = null)
		{
			// Base: the catalog definition, or a bare stub for ids unknown to the catalog.
			CharacterDefinition character = GetCharacter(characterId) ?? new CharacterDefinition { Id = characterId, RaceId = string.IsNullOrEmpty(raceId) ? "human" : raceId };

			bool raceOverride = !string.IsNullOrEmpty(raceId) && raceId != character.RaceId;
			bool female = isFemale || character.IsFemale;
			bool dressFromCode = code != null && code.Slots.Count > 0;
			if (!raceOverride && female == character.IsFemale && !dressFromCode) return character;

			CharacterDefinition display = character.Clone(raceOverride ? raceId : null, female);
			if (dressFromCode)
			{
				// The code's outfit replaces the character's own rosters entirely.
				display.EquipmentRosters.Clear();
				display.EquipmentRosters.Add(code.Slots);
			}
			return display;
		}

		public List<CharacterDefinition> GetAllCharacters()
		{
			return _characters.Values.ToList();
		}

		/// <summary>Every multiplayer character with a resolvable culture.</summary>
		public List<CharacterDefinition> GetMultiplayerCharacters()
		{
			return GetAllCharacters()
				.Where(d => d.IsMultiplayer
					&& !string.IsNullOrEmpty(d.CultureId)
					&& Factions.Instance.AvailableCultures.ContainsKey(d.CultureId))
				.ToList();
		}

		/// <summary>Live character object from the object manager, whatever the load timing.
		/// Null in contexts where native objects aren't loaded (e.g. the modding kit before its game load).</summary>
		public BasicCharacterObject GetCharacterObject(string stringId)
		{
			return string.IsNullOrEmpty(stringId) ? null : MBObjectManager.Instance?.GetObject<BasicCharacterObject>(stringId);
		}

		public List<CharacterDefinition> GetCharactersByCulture(BasicCultureObject culture)
		{
			List<CharacterDefinition> characters = new List<CharacterDefinition>();
			if (culture != null && _charactersByCultureId.TryGetValue(culture.StringId, out List<CharacterDefinition> byCulture))
			{
				characters.AddRange(byCulture);
			}
			return characters;
		}

		public List<CharacterDefinition> GetCharactersByCulture(BasicCultureObject culture, AgentExtensions.ClassType classType)
		{
			return GetCharactersByCulture(culture).Where(c => c.HasClassType(classType)).ToList();
		}

		public ItemDefinition GetItem(string itemId)
		{
			if (string.IsNullOrEmpty(itemId)) return null;
			return _items.TryGetValue(itemId, out ItemDefinition item) ? item : null;
		}

		public SkinDefinition GetSkin(string raceId, bool female)
		{
			if (string.IsNullOrEmpty(raceId)) return null;
			return _skins.TryGetValue((raceId, female), out SkinDefinition skin) ? skin : null;
		}

		private void BuildRaceCache()
		{
			string[] raceNames = TaleWorlds.Core.FaceGen.GetRaceNames();
			if (raceNames == null || raceNames.Length == 0) return;
			_raceIds = new Dictionary<string, int>(raceNames.Length, StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < raceNames.Length; i++) _raceIds[raceNames[i]] = i;
			_raceNames = raceNames;
		}

		public int GetRaceId(string raceId)
		{
			if (_raceIds != null && _raceIds.TryGetValue(raceId, out int race)) return race;
			Log($"[AllianceData] Race '{raceId}' not found", LogLevel.Warning);
			return -1;
		}

		public string GetRaceName(int raceIndex)
		{
			if (_raceNames != null && raceIndex >= 0 && raceIndex < _raceNames.Length) return _raceNames[raceIndex];
			Log($"[AllianceData] Race id '{raceIndex}' not found", LogLevel.Warning);
			return null;
		}

		/// <summary>Skeleton model declared by an action set (e.g. "as_horse" -> "horse_skeleton").
		/// Only used to resolve the mount's skeleton model.</summary>
		public string GetSkeletonForActionSet(string actionSetId)
		{
			return string.IsNullOrEmpty(actionSetId) ? null : _skeletonOfActionSet.TryGetValue(actionSetId, out string skeleton) ? skeleton : null;
		}

		/// <summary>Whether an animation clip exists in any action set.</summary>
		public bool ClipExists(string clipName)
		{
			return !string.IsNullOrEmpty(clipName) && _clips.Contains(clipName);
		}

		/// <summary>Returns the animation name for a given action set + action type (null when the set or the type is unknown).</summary>
		public string GetAnimationOfAction(string actionSetId, string actionType)
		{
			return !string.IsNullOrEmpty(actionSetId) && !string.IsNullOrEmpty(actionType)
				&& _actionsByActionSet.TryGetValue(actionSetId, out Dictionary<string, string> actions)
				&& actions.TryGetValue(actionType, out string animation) ? animation : null;
		}

		/// <summary>Resolves an action type to a clip like <see cref="GetAnimationOfAction"/>, then
		/// falls back to any other action set built for the same skeleton - action types are spread
		/// across compatible sets (e.g. "act_childhood_toddler_vigor" is declared by
		/// "as_human_facegen", not by the warrior set, but both animate the human skeleton).
		/// Returns null when no compatible set defines the action.</summary>
		public string GetAnimationOfActionCompatible(string actionSetId, string actionType)
		{
			string clip = GetAnimationOfAction(actionSetId, actionType);
			if (clip != null || string.IsNullOrEmpty(actionSetId) || string.IsNullOrEmpty(actionType)) return clip;
			if (!_skeletonOfActionSet.TryGetValue(actionSetId, out string skeleton)
				|| !_actionSetIdsBySkeleton.TryGetValue(skeleton, out List<string> compatibleIds))
			{
				return null;
			}
			foreach (string compatibleId in compatibleIds)
			{
				if (_actionsByActionSet.TryGetValue(compatibleId, out Dictionary<string, string> actions)
					&& actions.TryGetValue(actionType, out string compatibleClip))
				{
					return compatibleClip;
				}
			}
			return null;
		}

		/// <summary>Native duration (seconds) of an animation clip, queried directly from the engine.
		/// Cached - the native query runs at most once per clip. 0 when unknown.</summary>
		public float GetClipDuration(string clipName)
		{
			if (string.IsNullOrEmpty(clipName)) return 0f;
			if (_clipDurations.TryGetValue(clipName, out float cached)) return cached;
			float duration = 0f;
			try
			{
				float queried = MBAnimation.GetAnimationDuration(clipName);
				if (queried > 0.01f) duration = queried;
			}
			catch
			{
				duration = 0f;
			}
			_clipDurations[clipName] = duration;
			return duration;
		}

		public static bool TryGetNativeSoundLength(string name, out float length)
		{
			length = 0f;
			if (string.IsNullOrEmpty(name)) return false;
			return Instance._nativeSoundEventLengths != null && Instance._nativeSoundEventLengths.TryGetValue(name, out length);
		}

		public MonsterDefinition GetMonster(string monsterId)
		{
			return string.IsNullOrEmpty(monsterId) ? null : _monsters.TryGetValue(monsterId, out MonsterDefinition monster) ? monster : null;
		}

		public MonsterUsageDefinition GetMonsterUsage(string usageId)
		{
			return string.IsNullOrEmpty(usageId) ? null : _monsterUsages.TryGetValue(usageId, out MonsterUsageDefinition usage) ? usage : null;
		}

		public HolsterDefinition GetHolster(string holsterId)
		{
			return string.IsNullOrEmpty(holsterId) ? null : _holsters.TryGetValue(holsterId, out HolsterDefinition holster) ? holster : null;
		}

		/// <summary>Every holster of a visual anchor group (group_name), empty when unknown.</summary>
		public List<HolsterDefinition> GetHolstersOfGroup(string group)
		{
			return string.IsNullOrEmpty(group) || !_holstersByGroup.TryGetValue(group, out List<HolsterDefinition> holsters)
				? new List<HolsterDefinition>()
				: holsters;
		}

		/// <summary>Global usage count of a holster rig (holster_skeleton) across all holsters.</summary>
		public int GetHolsterRigUsage(string rig)
		{
			return string.IsNullOrEmpty(rig) || !_holsterRigUsage.TryGetValue(rig, out int usage) ? 0 : usage;
		}

		/// <summary>Bone a holster hangs on, resolved through its base_set chain (e.g. shield_round -> thorax_back_far -> thorax -> "biped_thorax").</summary>
		public string GetHolsterBone(string holsterId)
		{
			string current = holsterId;
			for (int depth = 0; depth < 16 && !string.IsNullOrEmpty(current); depth++)
			{
				if (_holsters.TryGetValue(current, out HolsterDefinition holster) && !string.IsNullOrEmpty(holster.Bone))
				{
					return holster.Bone;
				}
				if (!_holsterBaseIds.TryGetValue(current, out string baseId)) break;
				current = baseId;
			}
			return null;
		}

		/// <summary>Initializes the native MP class divisions when the current context hasn't (game servers/clients load them natively; SP and the editor don't).</summary>
		private void InitMPClassDivisions()
		{
			try
			{
				if (MultiplayerClassDivisions.MultiplayerHeroClassGroups == null)
				{
					GameTextManager textManager = new GameTextManager();
					textManager.LoadGameTexts();
					GameTexts.Initialize(textManager);
					MultiplayerClassDivisions.Initialize();
				}
			}
			catch (Exception ex)
			{
				Log($"[AllianceData] Failed to initialize MultiplayerClassDivisions ({ex.Message}).", LogLevel.Warning);
			}
		}

		/// <summary>Reads the MPClassDivisions merged XML to know every class type each character
		/// is registered as, then applies the mapping onto the parsed character definitions.</summary>
		private void LoadMPClassDivisions()
		{
			_characterClassTypes.Clear();
			try
			{
				if (!MBObjectManager.Instance.HasType(typeof(MultiplayerClassDivisions.MPHeroClass)))
				{
					MBObjectManager.Instance.RegisterType<MultiplayerClassDivisions.MPHeroClass>("MPClassDivision", "MPClassDivisions", 45U, true, false);
				}
				XmlDocument mpClassDivisionsXml = MBObjectManager.GetMergedXmlForManaged("MPClassDivisions", false);
				if (mpClassDivisionsXml == null)
				{
					Log("[AllianceData] MPClassDivisions XML not found - characters get no class types.", LogLevel.Warning);
				}
				else
				{
					foreach (XmlNode node in mpClassDivisionsXml.SelectNodes("//MPClassDivision"))
					{
						string heroCharacter = node.Attributes["hero"]?.Value;
						string troopCharacter = node.Attributes["troop"]?.Value;
						string bannerBearerCharacter = node.Attributes["banner_bearer"]?.Value;

						if (!string.IsNullOrEmpty(heroCharacter)) RegisterRole(heroCharacter, AgentExtensions.ClassType.Hero);
						if (!string.IsNullOrEmpty(troopCharacter)) RegisterRole(troopCharacter, AgentExtensions.ClassType.Troop);
						if (!string.IsNullOrEmpty(bannerBearerCharacter)) RegisterRole(bannerBearerCharacter, AgentExtensions.ClassType.BannerBearer);
					}
				}
			}
			catch (Exception ex)
			{
				Log($"[AllianceData] Error loading MPClassDivisions XML: {ex.Message}.", LogLevel.Warning);
			}

			foreach (CharacterDefinition definition in _characters.Values)
			{
				definition.ClassTypes = _characterClassTypes.TryGetValue(definition.Id, out HashSet<AgentExtensions.ClassType> found)
					? new HashSet<AgentExtensions.ClassType>(found)
					: new HashSet<AgentExtensions.ClassType> { AgentExtensions.ClassType.None };
			}
		}

		private void RegisterRole(string characterId, AgentExtensions.ClassType role)
		{
			if (!_characterClassTypes.TryGetValue(characterId, out HashSet<AgentExtensions.ClassType> roles))
			{
				roles = new HashSet<AgentExtensions.ClassType>();
				_characterClassTypes[characterId] = roles;
			}
			roles.Add(role);
		}

		private void RebuildCultureIndex()
		{
			_charactersByCultureId.Clear();
			foreach (CharacterDefinition definition in _characters.Values)
			{
				if (!definition.IsMultiplayer || string.IsNullOrEmpty(definition.CultureId)) continue;
				if (!_charactersByCultureId.TryGetValue(definition.CultureId, out List<CharacterDefinition> byCulture))
				{
					_charactersByCultureId[definition.CultureId] = byCulture = new List<CharacterDefinition>();
				}
				byCulture.Add(definition);
			}
		}

		/// <summary>
		/// Returns the merged XML the game's own loading pipeline would read for a managed type (in all active modules files).
		/// Null when the type is not declared by any active module.
		/// </summary>
		private static XmlDocument GetMergedXml(string managedTypeId)
		{
			try
			{
				XmlDocument xml = MBObjectManager.GetMergedXmlForManaged(managedTypeId, false);
				if (xml?.DocumentElement != null) return xml;
			}
			catch (Exception ex)
			{
				Log($"[AllianceData] Failed to load merged XML for '{managedTypeId}' ({ex.Message}).", LogLevel.Warning);
			}
			return null;
		}

		private void LoadCharacters(string managedTypeId)
		{
			XmlDocument xml = GetMergedXml(managedTypeId);
			if (xml == null) return;
			bool multiplayer = string.Equals(managedTypeId, "MPCharacters", StringComparison.OrdinalIgnoreCase);
			foreach (XmlNode node in xml.SelectNodes("//NPCCharacter"))
			{
				string id = node.Attributes?["id"]?.Value;
				if (string.IsNullOrEmpty(id)) continue;
				CharacterDefinition definition = ParseCharacter(node);
				definition.IsMultiplayer = multiplayer;
				// An MP declaration wins when the same id exists in both SP and MP merged XMLs.
				if (multiplayer || !_characters.ContainsKey(id)) _characters[id] = definition;
			}
		}

		private void LoadItems(string managedTypeId)
		{
			XmlDocument xml = GetMergedXml(managedTypeId);
			if (xml == null) return;
			// Crafted weapons live in <CraftedItem> nodes (piece-composed, no authored mesh).
			foreach (XmlNode node in xml.SelectNodes("//Item | //CraftedItem"))
			{
				string id = node.Attributes?["id"]?.Value;
				if (string.IsNullOrEmpty(id)) continue;
				_items[id] = ParseItem(node);
			}
		}

		/// <summary>Engine-consumed files (skins, monsters, holsters, action sets, voices...) are not declared in SubModule.xml.
		/// Each module lists them in ModuleData/project.mbproj, and their paths are module-specific (e.g. "ModuleData/Skins/lotr_skins_uruk.xml").
		/// Returns every declared file of the given type across active modules;</summary>
		private static List<string> GetNativeXmlPaths(string fileType)
		{
			List<string> paths = new List<string>();
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				try
				{
					List<string> declared = new List<string>();
					string mbprojPath = Path.Combine(module.FolderPath, "ModuleData", "project.mbproj");
					if (File.Exists(mbprojPath))
					{
						XmlDocument mbproj = new XmlDocument();
						mbproj.Load(mbprojPath);
						foreach (XmlNode file in mbproj.SelectNodes($"//file[@type='{fileType}']"))
						{
							string name = file.Attributes?["name"]?.Value;
							if (string.IsNullOrEmpty(name)) continue;
							string path = Path.Combine(module.FolderPath, name.Replace('/', Path.DirectorySeparatorChar));
							if (File.Exists(path)) declared.Add(path);
						}
					}
					if (declared.Count > 0)
					{
						paths.AddRange(declared);
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to read project.mbproj of module '{module.Id}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			return paths;
		}

		/// <summary>The active module owning the given file path (module root prefix match), or null.</summary>
		private static ModuleInfo GetModuleOfPath(string filePath)
		{
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				if (!string.IsNullOrEmpty(module.FolderPath) && filePath.StartsWith(module.FolderPath, StringComparison.OrdinalIgnoreCase)) return module;
			}
			return null;
		}

		/// <summary>item_holsters.xml is consumed by the native engine (no SubModule.xml managed-xml entry),
		/// so parse every active module's file directly - it defines where holstered items hang (bone + local frame)</summary>
		private void LoadHolsters()
		{
			foreach (string path in GetNativeXmlPaths("item_holster"))
			{
				try
				{
					XmlDocument xml = new XmlDocument();
					xml.Load(path);
					foreach (XmlNode node in xml.SelectNodes("//item_holster"))
				{
						string id = node.Attributes?["id"]?.Value;
						if (string.IsNullOrEmpty(id)) continue;
						_holsters[id] = new HolsterDefinition
						{
							Id = id,
							Bone = node.Attributes?["holster_bone"]?.Value,
							Skeleton = node.Attributes?["holster_skeleton"]?.Value,
							Group = node.Attributes?["group_name"]?.Value,
							Position = ParseVec3(node.Attributes?["holster_position"]?.Value),
							RotationYawPitchRoll = ParseVec3(node.Attributes?["holster_rotation_yaw_pitch_roll"]?.Value),
							ShowHolsterWhenDrawn = ParseBool(node.Attributes?["show_holster_when_drawn"]?.Value)
						};
						string baseId = node.Attributes?["base_set"]?.Value;
						if (!string.IsNullOrEmpty(baseId)) _holsterBaseIds[id] = baseId;
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to load item_holsters.xml '{path}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			ResolveHolsterInheritance();

			// Holster group/rig indexes for FakeAgent.HolsterRigCorrection.
			foreach (HolsterDefinition holster in _holsters.Values)
			{
				if (!string.IsNullOrEmpty(holster.Group))
				{
					if (!_holstersByGroup.TryGetValue(holster.Group, out List<HolsterDefinition> group)) _holstersByGroup[holster.Group] = group = new List<HolsterDefinition>();
					group.Add(holster);
				}
				if (!string.IsNullOrEmpty(holster.Skeleton)) _holsterRigUsage[holster.Skeleton] = _holsterRigUsage.TryGetValue(holster.Skeleton, out int usage) ? usage + 1 : 1;
			}
		}

		/// <summary>Holster entries without their own bone inherit it through their base_set chain
		/// (e.g. shield -> thorax_back_far -> thorax -> biped_thorax).</summary>
		private void ResolveHolsterInheritance()
		{
			foreach (string id in _holsters.Keys.ToList())
			{
				string current = id;
				int depth = 0;
				while (_holsters.TryGetValue(current, out HolsterDefinition holster) && string.IsNullOrEmpty(holster.Bone) && depth++ < 16)
				{
					if (!_holsterBaseIds.TryGetValue(current, out string baseId)
						|| string.Equals(baseId, id, StringComparison.OrdinalIgnoreCase)
						|| !_holsters.TryGetValue(baseId, out HolsterDefinition baseHolster)) break;
					if (!string.IsNullOrEmpty(baseHolster.Bone)) holster.Bone = baseHolster.Bone;
					if (string.IsNullOrEmpty(holster.Skeleton) && !string.IsNullOrEmpty(baseHolster.Skeleton)) holster.Skeleton = baseHolster.Skeleton;
					current = baseId;
				}
			}
		}

		/// <summary>skins.xml is consumed by the native engine (no SubModule.xml managed-xml entry),
		/// so it can't be merged through MBObjectManager - parse every active module's files instead.</summary>
		private void LoadSkins()
		{
			foreach (string path in GetNativeXmlPaths("skin"))
			{
				try
				{
					XmlDocument xml = new XmlDocument();
					xml.Load(path);
					foreach (XmlNode raceNode in xml.SelectNodes("//race"))
					{
						string raceId = raceNode.Attributes?["id"]?.Value;
						if (string.IsNullOrEmpty(raceId)) continue;
						foreach (XmlNode skinNode in raceNode.SelectNodes("skin"))
						{
							bool female = skinNode.Attributes?["gender"]?.Value == "1";
							string maturity = skinNode.Attributes?["mesh_maturity_type"]?.Value;
							var key = (raceId, female);
							// Prefer the adult entry; keep the first seen otherwise.
							if (_skins.ContainsKey(key) && !string.Equals(maturity, "adult", StringComparison.OrdinalIgnoreCase)) continue;
							_skins[key] = ParseSkin(skinNode);
						}
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to load skins file '{path}' ({ex.Message}).", LogLevel.Warning);
				}
			}
		}

		/// <summary>
		/// Parse every active module's action set files (paths from project.mbproj). Two things are kept:
		/// - the action-set -> skeleton mapping (to resolve the mount's skeleton model)
		/// - the action type -> animation mapping per set + the global clip catalog (fake agents play raw clips, resolved through their action set).
		/// </summary>
		private void LoadActionSets()
		{
			foreach (string path in GetNativeXmlPaths("action_set"))
			{
				try
				{
					XmlDocument xml = new XmlDocument();
					xml.Load(path);
					foreach (XmlNode setNode in xml.SelectNodes("//action_set"))
					{
						string id = setNode.Attributes?["id"]?.Value;
						string skeleton = setNode.Attributes?["skeleton"]?.Value;
						string baseSet = setNode.Attributes?["base_set"]?.Value;
						if (!string.IsNullOrEmpty(id))
						{
							if (!string.IsNullOrEmpty(skeleton) && !_skeletonOfActionSet.ContainsKey(id))
								_skeletonOfActionSet[id] = skeleton;
							if (!string.IsNullOrEmpty(baseSet))
								_baseSetOfActionSet[id] = baseSet;
							// Load order is kept (roots are declared before their derived sets) for
							// the post-parse passes and deterministic compatible-set resolution.
							if (!_actionSetIdsInOrder.Contains(id)) _actionSetIdsInOrder.Add(id);
						}
						foreach (XmlNode actionNode in setNode.SelectNodes("action"))
					{
						string clip = actionNode.Attributes?["animation"]?.Value;
						string type = actionNode.Attributes?["type"]?.Value;
						if (string.IsNullOrEmpty(clip)) continue;
						_clips.Add(clip);
						if (string.IsNullOrEmpty(id)) continue;
						if (!string.IsNullOrEmpty(type))
						{
							if (!_actionsByActionSet.TryGetValue(id, out Dictionary<string, string> actions))
							{
								_actionsByActionSet[id] = actions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
							}
							actions[type] = clip;
						}
					}
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to load action set file '{path}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			MergeBaseActionSets();
			ResolveActionSetSkeletons();
			BuildCompatibleActionSetIndex();
		}

		/// <summary>Native derived action sets (base_set="...") only declare overrides or
		/// additions: merge each base chain into the derived set so actions defined up the
		/// chain resolve locally. Roots load before their derived sets, so one pass in load
		/// order is enough.</summary>
		private void MergeBaseActionSets()
		{
			foreach (string id in _actionSetIdsInOrder)
			{
				if (!_actionsByActionSet.TryGetValue(id, out Dictionary<string, string> actions)) continue;
				_baseSetOfActionSet.TryGetValue(id, out string baseId);
				HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
				while (!string.IsNullOrEmpty(baseId) && visited.Add(baseId))
				{
					if (_actionsByActionSet.TryGetValue(baseId, out Dictionary<string, string> baseActions))
					{
						foreach (KeyValuePair<string, string> entry in baseActions)
						{
							if (!actions.ContainsKey(entry.Key)) actions[entry.Key] = entry.Value;
						}
					}
					_baseSetOfActionSet.TryGetValue(baseId, out baseId);
				}
			}
		}

		/// <summary>Only root action sets declare a skeleton - derived sets inherit it through
		/// their base chain (as_human_facegen -> as_human_warrior -> human_skeleton). Must run
		/// after parsing, in load order (parents first).</summary>
		private void ResolveActionSetSkeletons()
		{
			foreach (string id in _actionSetIdsInOrder)
			{
				if (_skeletonOfActionSet.ContainsKey(id)) continue;
				_baseSetOfActionSet.TryGetValue(id, out string baseId);
				HashSet<string> visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { id };
				while (!string.IsNullOrEmpty(baseId) && visited.Add(baseId))
				{
					if (_skeletonOfActionSet.TryGetValue(baseId, out string skeleton))
					{
						_skeletonOfActionSet[id] = skeleton;
						break;
					}
					_baseSetOfActionSet.TryGetValue(baseId, out baseId);
				}
			}
		}

		/// <summary>Indexes every action set under its (resolved) skeleton, in load order, for
		/// <see cref="GetAnimationOfActionCompatible"/>.</summary>
		private void BuildCompatibleActionSetIndex()
		{
			foreach (string id in _actionSetIdsInOrder)
			{
				if (!_skeletonOfActionSet.TryGetValue(id, out string skeleton)) continue;
				if (!_actionSetIdsBySkeleton.TryGetValue(skeleton, out List<string> ids))
					_actionSetIdsBySkeleton[skeleton] = ids = new List<string>();
				if (!ids.Contains(id)) ids.Add(id);
			}
		}

		private void LoadMonsters()
		{
			foreach (string path in GetNativeXmlPaths("monster"))
			{
				try
				{
					XmlDocument xml = new XmlDocument();
					xml.Load(path);
					foreach (XmlNode node in xml.SelectNodes("//Monster"))
					{
						string id = node.Attributes?["id"]?.Value;
						if (string.IsNullOrEmpty(id)) continue;
						if (!_monsters.ContainsKey(id))
						{
							_monsters[id] = new MonsterDefinition
							{
								Id = id,
								ActionSetId = node.Attributes?["action_set"]?.Value,
								MonsterUsage = node.Attributes?["monster_usage"]?.Value,
								MainHandItemBone = node.Attributes?["main_hand_item_bone"]?.Value,
								OffHandItemBone = node.Attributes?["off_hand_item_bone"]?.Value,
								OffHandItemSecondaryBone = node.Attributes?["off_hand_item_secondary_bone"]?.Value,
								ThoraxBone = node.Attributes?["thorax_look_direction_bone"]?.Value,
								SpineLowerBone = node.Attributes?["spine_lower_bone"]?.Value,
								RiderSitBone = node.Attributes?["rider_sit_bone"]?.Value,
								WalkingSpeed = ParseFloat(node.Attributes?["walking_speed_limit"]?.Value)
							};
						}
						string baseId = node.Attributes?["base_monster"]?.Value;
						if (!string.IsNullOrEmpty(baseId)) _monsterBaseIds[id] = baseId;
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to load monster file '{path}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			ResolveMonsterInheritance();
		}

		/// <summary>monster_usage_sets.xml is consumed by the native engine (no SubModule.xml managed-xml entry).
		/// Parse every active module's files (paths from project.mbproj).
		/// It defines the exact movement action of each pace per monster family.</summary>
		private void LoadMonsterUsages()
		{
			foreach (string path in GetNativeXmlPaths("monster_usage_set"))
			{
				try
				{
					XmlDocument xml = new XmlDocument();
					xml.Load(path);
					foreach (XmlNode setNode in xml.SelectNodes("//monster_usage_set"))
					{
						string id = setNode.Attributes?["id"]?.Value;
						if (string.IsNullOrEmpty(id) || _monsterUsages.ContainsKey(id)) continue;
						MonsterUsageDefinition usage = _monsterUsages[id] = new MonsterUsageDefinition { Id = id };
						foreach (XmlNode movement in setNode.SelectNodes("monster_usage_movements/monster_usage_movement"))
						{
							if (movement.Attributes?["direction"]?.Value != "front") continue;
							if (!IsStraightDirection(movement.Attributes?["turn_direction"]?.Value)) continue;
							if (ParseBool(movement.Attributes?["is_left_foot"]?.Value)) continue;
							string action = movement.Attributes?["action"]?.Value;
							if (string.IsNullOrEmpty(action)) continue;
							if (int.TryParse(movement.Attributes?["pace"]?.Value, out int pace)) usage.MovementActions[pace] = action;
						}
						foreach (XmlNode upperBody in setNode.SelectNodes("monster_usage_upper_body_movements/monster_usage_upper_body_movement"))
						{
							if (upperBody.Attributes?["direction"]?.Value != "none") continue;
							if (!IsStraightDirection(upperBody.Attributes?["turn_direction"]?.Value)) continue;
							string action = upperBody.Attributes?["action"]?.Value;
							if (string.IsNullOrEmpty(action)) continue;
							if (int.TryParse(upperBody.Attributes?["pace"]?.Value, out int pace)) usage.UpperBodyActions[pace] = action;
						}
					}
				}
				catch (Exception ex)
				{
					Log($"[AllianceData] Failed to load monster usage file '{path}' ({ex.Message}).", LogLevel.Warning);
				}
			}
		}

		private static bool IsStraightDirection(string turnDirection) => string.IsNullOrEmpty(turnDirection) || string.Equals(turnDirection, "none", StringComparison.OrdinalIgnoreCase);

		private void ResolveMonsterInheritance()
		{
			foreach (string id in _monsters.Keys.ToList())
			{
				MonsterDefinition monster = _monsters[id];
				string current = id;
				int depth = 0;
				while (_monsterBaseIds.TryGetValue(current, out string baseId) && depth++ < 16)
				{
					if (string.Equals(baseId, id, StringComparison.OrdinalIgnoreCase)) break;
					if (!_monsters.TryGetValue(baseId, out MonsterDefinition baseMonster)) break;
					// The base fills the gaps; the derived values already set win.
					if (string.IsNullOrEmpty(monster.ActionSetId)) monster.ActionSetId = baseMonster.ActionSetId;
					if (string.IsNullOrEmpty(monster.MonsterUsage)) monster.MonsterUsage = baseMonster.MonsterUsage;
					if (string.IsNullOrEmpty(monster.MainHandItemBone)) monster.MainHandItemBone = baseMonster.MainHandItemBone;
					if (string.IsNullOrEmpty(monster.OffHandItemBone)) monster.OffHandItemBone = baseMonster.OffHandItemBone;
					if (string.IsNullOrEmpty(monster.OffHandItemSecondaryBone)) monster.OffHandItemSecondaryBone = baseMonster.OffHandItemSecondaryBone;
					if (string.IsNullOrEmpty(monster.ThoraxBone)) monster.ThoraxBone = baseMonster.ThoraxBone;
					if (string.IsNullOrEmpty(monster.SpineLowerBone)) monster.SpineLowerBone = baseMonster.SpineLowerBone;
					if (string.IsNullOrEmpty(monster.RiderSitBone)) monster.RiderSitBone = baseMonster.RiderSitBone;
					if (monster.WalkingSpeed <= 0f) monster.WalkingSpeed = baseMonster.WalkingSpeed;
					current = baseId;
				}
			}
		}

		private static float ParseFloat(string value)
		{
			return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result) ? result : 0f;
		}

		/// <summary>Parses "x,y,z" triples (optionally "x,y,z,w" like native Vec3.Parse; spaces
		/// ignored, a failed component reads 0 like the native weapon parser). Zero when absent.</summary>
		private static Vec3 ParseVec3(string value)
		{
			if (string.IsNullOrEmpty(value)) return Vec3.Zero;
			string[] parts = value.Replace(" ", "").Split(',');
			if (parts.Length < 3 || parts.Length > 4) return Vec3.Zero;
			Vec3 result = Vec3.Zero;
			float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result.x);
			float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result.y);
			float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result.z);
			return result;
		}

		/// <summary>Strips a "{=abc}" localization prefix from a raw XML name (null-safe).</summary>
		private static string StripLocalizationPrefix(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;
			if (name.StartsWith("{=", StringComparison.Ordinal) && name.IndexOf('}') is int end && end >= 0 && end < name.Length - 1)
			{
				return name.Substring(end + 1);
			}
			return name;
		}

		private static SkinDefinition ParseSkin(XmlNode node)
		{
			return new SkinDefinition
			{
				Skeleton = node.Attributes?["skeleton"]?.Value,
				Head = node.Attributes?["face_meta_mesh"]?.Value,
				Body = node.Attributes?["body_meta_mesh"]?.Value,
				BodyShoulders = node.Attributes?["body_meta_mesh_shoulders"]?.Value,
				Legs = node.Attributes?["legs_mesh"]?.Value,
				Hands = node.Attributes?["hands_mesh"]?.Value,
				UnderwearBottom = node.Attributes?["underwear_bottom_mesh"]?.Value,
				UnderwearTop = node.Attributes?["underwear_top_mesh"]?.Value,
				MinScale = ParseFloat(node.Attributes?["min_scale"]?.Value) is float scale && scale > 0f ? scale : 1f
			};
		}

		private static ItemDefinition ParseItem(XmlNode node)
		{
			ItemDefinition item = new ItemDefinition
			{
				Id = node.Attributes?["id"]?.Value,
				MeshName = node.Attributes?["mesh"]?.Value,
				ItemType = node.Attributes?["Type"]?.Value ?? node.Attributes?["crafting_template"]?.Value,
				IsCrafted = !string.IsNullOrEmpty(node.Attributes?["crafting_template"]?.Value),
				HolsterMesh = node.Attributes?["holster_mesh"]?.Value,
				HolsterWithWeaponMesh = node.Attributes?["holster_mesh_with_weapon"]?.Value,
				HolsterPositionShift = ParseVec3(node.Attributes?["holster_position_shift"]?.Value),
				HasLowerHolsterPriority = ParseBool(node.Attributes?["has_lower_holster_priority"]?.Value),
				UsingTableau = ParseBool(node.Attributes?["using_tableau"]?.Value)
			};
			XmlNode armor = node.SelectSingleNode("ItemComponent/Armor");
			if (armor != null)
			{
				item.HasArmor = true;
				item.CoversHead = ParseBool(armor.Attributes?["covers_head"]?.Value);
				item.CoversBody = ParseBool(armor.Attributes?["covers_body"]?.Value);
				item.CoversHands = ParseBool(armor.Attributes?["covers_hands"]?.Value);
				item.CoversLegs = ParseBool(armor.Attributes?["covers_legs"]?.Value);
				item.ManeCoverType = armor.Attributes?["mane_cover_type"]?.Value;
			}
			XmlNode weapon = node.SelectSingleNode("ItemComponent/Weapon");
			if (weapon != null)
			{
				item.WeaponPosition = ParseVec3(weapon.Attributes?["position"]?.Value);
				item.WeaponRotation = ParseVec3(weapon.Attributes?["rotation"]?.Value);
				item.WeaponClass = weapon.Attributes?["weapon_class"]?.Value;
			}
			XmlNode bannerComponent = node.SelectSingleNode("ItemComponent/Banner");
			if (bannerComponent != null)
			{
				item.WeaponPosition = ParseVec3(bannerComponent.Attributes?["position"]?.Value);
				item.WeaponRotation = ParseVec3(bannerComponent.Attributes?["rotation"]?.Value);
				item.WeaponClass = bannerComponent.Attributes?["weapon_class"]?.Value;
			}
			item.MountMonsterId = StripPrefix(node.SelectSingleNode("ItemComponent/Horse")?.Attributes?["monster"]?.Value);
			foreach (string holsterBone in (node.Attributes?["item_holsters"]?.Value ?? "").Split(':'))
			{
				if (!string.IsNullOrEmpty(holsterBone)) item.Holsters.Add(holsterBone);
			}
			foreach (XmlNode meshNode in node.SelectNodes("ItemComponent/Horse/AdditionalMeshes/Mesh"))
			{
				string meshName = meshNode.Attributes?["name"]?.Value;
				if (string.IsNullOrEmpty(meshName)) continue;
				item.AdditionalMeshes.Add(new KeyValuePair<string, bool>(meshName, ParseBool(meshNode.Attributes?["affected_by_cover"]?.Value)));
			}
			foreach (XmlNode flags in node.SelectNodes("Flags"))
			{
				foreach (XmlAttribute attribute in flags.Attributes?.Cast<XmlAttribute>() ?? Enumerable.Empty<XmlAttribute>())
				{
					if (string.Equals(attribute.Value, "false", StringComparison.OrdinalIgnoreCase)) continue;
					if (Enum.TryParse(attribute.LocalName, ignoreCase: true, out ItemFlags nativeFlag))
					{
						item.NativeFlags |= nativeFlag;
					}
				}
			}
			item.IsOffHand = string.Equals(item.ItemType, "Shield", StringComparison.OrdinalIgnoreCase) || item.NativeFlags.HasFlag(ItemFlags.HeldInOffHand);
			item.Kind = ClassifyItem(item);
			return item;
		}

		// Rigid item types: their mesh is not skinned and must be worn (holster) or hand-bound.
		private static readonly HashSet<string> RigidWeaponTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"OneHandedWeapon", "TwoHandedWeapon", "Polearm", "Bow", "Crossbow", "Thrown", "Shield", "Pistol", "Musket"
		};

		// Ammo item types: their mesh is the projectile - only the quiver is worn.
		private static readonly HashSet<string> AmmoItemTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"Arrows", "Bolts", "SlingStones", "Bullets"
		};

		private static ItemKind ClassifyItem(ItemDefinition item)
		{
			// Crafted items carry no "Type" attribute (their type is the crafting template) - they
			// are always weapons.
			if (item.IsCrafted) return ItemKind.Weapon;
			string type = item.ItemType;
			if (string.Equals(type, "Horse", StringComparison.OrdinalIgnoreCase)) return ItemKind.Mount;
			if (string.Equals(type, "HorseHarness", StringComparison.OrdinalIgnoreCase)) return ItemKind.MountHarness;
			if (string.Equals(type, "Banner", StringComparison.OrdinalIgnoreCase)) return ItemKind.Banner;
			if (AmmoItemTypes.Contains(type)) return ItemKind.Ammo;
			if (RigidWeaponTypes.Contains(type)) return ItemKind.Weapon;
			return ItemKind.Garment;
		}

		private static CharacterDefinition ParseCharacter(XmlNode node)
		{
			string id = node.Attributes?["id"]?.Value;
			CharacterDefinition definition = new CharacterDefinition
			{
				Id = id,
				Name = node.Attributes?["name"]?.Value,
				CultureId = StripPrefix(node.Attributes?["culture"]?.Value),
				RaceId = StripPrefix(node.Attributes?["race"]?.Value) ?? "human",
				IsFemale = ParseBool(node.Attributes?["is_female"]?.Value)
			};
			// Every battle roster is kept: the game randomizes between them at spawn.
			foreach (XmlNode roster in node.SelectNodes("Equipments/EquipmentRoster"))
			{
				Dictionary<int, string> slots = new Dictionary<int, string>();
				int nextSlot = 0;
				foreach (XmlNode equipment in roster.SelectNodes("equipment"))
				{
					string itemId = StripPrefix(equipment.Attributes?["id"]?.Value);
					if (string.IsNullOrEmpty(itemId)) continue;
					int slot = ParseSlot(equipment.Attributes?["slot"]?.Value, nextSlot);
					nextSlot = Math.Max(nextSlot, slot + 1);
					slots[slot] = itemId;
				}
				if (slots.Count > 0) definition.EquipmentRosters.Add(slots);
			}
			return definition;
		}

		/// <summary>Resolves a slot attribute like native Equipment.FromXml: "Item0".."Item4" map to
		/// Weapon0..ExtraWeaponSlot, other names are EquipmentIndex members ("Head", "Body"...
		/// "HorseHarness"). Unset or unknown names fall back to <paramref name="fallbackSlot"/>.</summary>
		private static int ParseSlot(string slotName, int fallbackSlot)
		{
			if (string.IsNullOrEmpty(slotName)) return fallbackSlot;
			string mapped = slotName switch
			{
				"Item0" => "Weapon0",
				"Item1" => "Weapon1",
				"Item2" => "Weapon2",
				"Item3" => "Weapon3",
				"Item4" => "ExtraWeaponSlot",
				_ => slotName
			};
			return Enum.TryParse(mapped, out EquipmentIndex index) ? (int)index : fallbackSlot;
		}

		private static bool ParseBool(string value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";

		private static string StripPrefix(string value)
		{
			if (string.IsNullOrEmpty(value)) return value;
			int separator = value.IndexOf('.');
			return separator >= 0 && separator < value.Length - 1 ? value.Substring(separator + 1) : value;
		}
	}
}
