using System;
using System.Collections.Generic;
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
	/// "Lightweight", managed access to the native object data (characters, items, cultures, skins, monsters, action sets).
	/// Managed types are read through MBObjectManager.GetMergedXmlForManaged (every active module, dependency order); 
	/// engine-consumed files with no SubModule.xml declaration (skins.xml, action_sets.xml, monsters.xml) are parsed 
	/// from each active module's ModuleData directly.
	/// Parsed with plain XML - deliberately NOT through native object deserialization, 
	/// which requires Game.Current / eager ItemObject lookups that don't exist in the modding-kit editing context.
	/// </summary>
	public class NativeMpData
	{
		/// <summary>Definition of a character: culture + equipment item ids per slot.</summary>
		public class CharacterDefinition
		{
			public string Id;
			public string CultureId;
			public string RaceId;
			public bool IsFemale;
			/// <summary>Equipment item ids keyed by <see cref="EquipmentIndex"/> slot (main outfit = first roster).</summary>
			public readonly Dictionary<int, string> SlotItems = new Dictionary<int, string>();
			/// <summary>Every battle equipment roster of the character (the game randomizes between
			/// them at spawn: RandomBattleEquipment). [0] == SlotItems.</summary>
			public readonly List<Dictionary<int, string>> EquipmentRosters = new List<Dictionary<int, string>>();
			/// <summary>Item ids of the first equipment roster, slot order preserved.</summary>
			public readonly List<string> EquipmentItemIds = new List<string>();

			public Dictionary<int, string> SelectRoster(int seed)
			{
				return EquipmentRosters[((seed % EquipmentRosters.Count) + EquipmentRosters.Count) % EquipmentRosters.Count];
			}
		}

		/// <summary>Native item data subset: wielded mesh, type and armor coverage flags.</summary>
		public class ItemDefinition
		{
			public string Id;
			public string MeshName;
			public string ItemType;
			/// <summary>Crafted item (crafting_template + pieces): the mesh is composed from the
			/// pieces at runtime (FakeAgent resolves it through the game's crafting cache).</summary>
			public bool IsCrafted;
			public bool HasArmor;
			public bool CoversHead;
			public bool CoversBody;
			public bool CoversHands;
			public bool CoversLegs;
			/// <summary>Empty sheath / sheath-containing-the-weapon multimeshes (rigid weapons).</summary>
			public string HolsterMesh;
			public string HolsterWithWeaponMesh;
			/// <summary>Native holster_position_shift (x,y,z): extra offset applied to the holster
			/// frame - crossbows, quivers and shields carry one.</summary>
			public Vec3 HolsterPositionShift;
			/// <summary>Weapon component position/rotation (x,y,z / degrees) - the item's authored
			/// frame, composed into its placement (each shield carries its own).</summary>
			public Vec3 WeaponPosition;
			public Vec3 WeaponRotation;
			/// <summary>Holster names (item_holsters) - resolved to placement frames via the native holster registry.</summary>
			public readonly List<string> Holsters = new List<string>();
			/// <summary>Monster id of a mount item (Horse component), e.g. "horse".</summary>
			public string MountMonsterId;
			/// <summary>Mane/additional meshes of a mount item (Horse component): mesh name + whether
			/// it is affected by cover (hidden when a harness covers it).</summary>
			public readonly List<KeyValuePair<string, bool>> AdditionalMeshes = new List<KeyValuePair<string, bool>>();
			public readonly HashSet<string> Flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			public bool HasFlag(string flag) => Flags.Contains(flag);

			/// <summary>Mirrors ArmorComponent.MeshesMask: which skin parts this armor leaves visible.
			/// Only meaningful for items with an Armor component.</summary>
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
		}

		public class MonsterDefinition
		{
			public string Id;
			public string ActionSetId;
			public float WalkingSpeed;
			public string MainHandItemBone;
			public string OffHandItemBone;
			/// <summary>Bone wielded shields sit on (native off_hand_item_secondary_bone, e.g. "l_foretwist1").</summary>
			public string OffHandItemSecondaryBone;
			/// <summary>Thorax bone (thorax_look_direction_bone) - where back holsters hang.</summary>
			public string ThoraxBone;
			/// <summary>Lower spine bone (spine_lower_bone) - where hip holsters hang.</summary>
			public string SpineLowerBone;
			public string RiderSitBone;
		}

		public class HolsterDefinition
		{
			public string Id;
			/// <summary>Bone the holster hangs on (e.g. "biped_thorax"); inherited through base_set chains.</summary>
			public string Bone;
			/// <summary>Local offset on the holster bone.</summary>
			public Vec3 Position;
			/// <summary>Local rotation on the holster bone, degrees (yaw, pitch, roll).</summary>
			public Vec3 RotationYawPitchRoll;
			/// <summary>Whether the empty holster is shown while the weapon is drawn.</summary>
			public bool ShowHolsterWhenDrawn;
		}

		// Managed type ids whose merged XML contains the data we need. Every module can declare
		// these in its SubModule.xml (Xmls/XmlNode) - all active declarations are merged.
		private static readonly string[] CharacterTypeIds = { "NPCCharacters", "MPCharacters" };
		private static readonly string[] ItemTypeIds = { "Items", "SPItems", "MPItems" };
		private static readonly string[] CultureTypeIds = { "SPCultures", "MPCultures" };

		private static NativeMpData _instance;
		public static NativeMpData Instance => _instance ??= new NativeMpData();
		private static readonly object _loadLock = new object();

		private readonly Dictionary<string, CharacterDefinition> _characters = new Dictionary<string, CharacterDefinition>();
		private readonly Dictionary<string, ItemDefinition> _items = new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, (uint Color1, uint Color2)> _cultureColors = new Dictionary<string, (uint, uint)>();
		private readonly Dictionary<(string Race, bool Female), SkinDefinition> _skins = new Dictionary<(string, bool), SkinDefinition>();
		private readonly Dictionary<string, string> _skeletonOfActionSet = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> _clips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		/// <summary>Resolved clip durations cache - the native query runs at most once per clip.</summary>
		private readonly Dictionary<string, float> _clipDurations = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, MonsterDefinition> _monsters = new Dictionary<string, MonsterDefinition>(StringComparer.OrdinalIgnoreCase);
		private readonly Dictionary<string, HolsterDefinition> _holsters = new Dictionary<string, HolsterDefinition>(StringComparer.OrdinalIgnoreCase);
		/// <summary>item_holsters.xml base_set links: id -> base id (bone inheritance chains).</summary>
		private readonly Dictionary<string, string> _holsterBaseIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private NativeMpData()
		{
			lock (_loadLock)
			{
				foreach (string typeId in CharacterTypeIds) LoadCharacters(typeId);
				foreach (string typeId in ItemTypeIds) LoadItems(typeId);
				foreach (string typeId in CultureTypeIds) LoadCultureColors(typeId);
				LoadSkins();
				LoadActionSets();
				LoadMonsters();
				LoadHolsters();
			}

			Log($"[NativeMpData] Loaded {_characters.Count} characters, {_items.Count} items, {_cultureColors.Count} cultures, {_skins.Count} skins, {_clips.Count} clips, {_monsters.Count} monsters, {_holsters.Count} holsters.", LogLevel.Debug);
		}

		/// <summary>item_holsters.xml is consumed by the native engine (no SubModule.xml managed-xml entry), 
		/// so parse every active module's file directly - it defines where holstered items hang (bone + local frame)</summary>
		private void LoadHolsters()
		{
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				try
				{
					string path = System.IO.Path.Combine(module.FolderPath, "ModuleData", "item_holsters.xml");
					if (!System.IO.File.Exists(path)) continue;
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
					Log($"[NativeMpData] Failed to load item_holsters.xml of module '{module.Id}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			ResolveHolsterInheritance();
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
					current = baseId;
				}
			}
		}

		public HolsterDefinition GetHolster(string holsterId)
		{
			return string.IsNullOrEmpty(holsterId) ? null : _holsters.TryGetValue(holsterId, out HolsterDefinition holster) ? holster : null;
		}

		/// <summary>(Color1, Color2) of a culture (white when unknown).</summary>
		public (uint Color1, uint Color2) GetCultureColors(string cultureId)
		{
			if (!string.IsNullOrEmpty(cultureId) && _cultureColors.TryGetValue(cultureId, out (uint Color1, uint Color2) colors))
				return colors;
			return (uint.MaxValue, uint.MaxValue);
		}

		public ItemDefinition GetItem(string itemId)
		{
			return string.IsNullOrEmpty(itemId) ? null : _items.TryGetValue(itemId, out ItemDefinition item) ? item : null;
		}

		/// <summary>Parsed character definition (race, culture, equipment). Null when unknown.</summary>
		public CharacterDefinition GetCharacter(string characterId)
		{
			return string.IsNullOrEmpty(characterId) ? null : _characters.TryGetValue(characterId, out CharacterDefinition character) ? character : null;
		}

		/// <summary>Body mesh recipe for a race/gender, from the engine-consumed ModuleData/skins.xml
		/// files (female entry first, male as fallback for a missing female entry). Null when unknown.</summary>
		public SkinDefinition GetSkin(string raceId, bool female)
		{
			if (string.IsNullOrEmpty(raceId)) return null;
			if (_skins.TryGetValue((raceId, female), out SkinDefinition skin)) return skin;
			return _skins.TryGetValue((raceId, false), out skin) ? skin : null;
		}

		/// <summary>Skeleton model declared by an action set (e.g. "as_horse" -> "horse_skeleton").
		/// Only used to resolve the mount's skeleton model; FakeAgents never use action sets for
		/// playback. Null when unknown.</summary>
		public string GetSkeletonForActionSet(string actionSetId)
		{
			return string.IsNullOrEmpty(actionSetId) ? null : _skeletonOfActionSet.TryGetValue(actionSetId, out string skeleton) ? skeleton : null;
		}

		/// <summary>Every animation clip registered by any action set (the editor's clip catalog).</summary>
		public IReadOnlyCollection<string> GetClips()
		{
			return _clips;
		}

		/// <summary>Native duration (seconds) of an animation clip, queried directly from the
		/// engine (<see cref="MBAnimation.GetAnimationDuration(string)"/>). Cached - the native
		/// query runs at most once per clip. 0 when unknown (the clip cannot be loop-restarted).</summary>
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

		/// <summary>A monsters.xml entry by id (null when unknown).</summary>
		public MonsterDefinition GetMonster(string monsterId)
		{
			return string.IsNullOrEmpty(monsterId) ? null : _monsters.TryGetValue(monsterId, out MonsterDefinition monster) ? monster : null;
		}

		/// <summary>
		/// Returns the merged XML the game's own loading pipeline would read for a managed type
		/// (base file + per-module part files, every active module, dependency order). Null when
		/// the type is not declared by any active module.
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
				Log($"[NativeMpData] Failed to load merged XML for '{managedTypeId}' ({ex.Message}).", LogLevel.Warning);
			}
			return null;
		}

		private void LoadCharacters(string managedTypeId)
		{
			XmlDocument xml = GetMergedXml(managedTypeId);
			if (xml == null) return;
			foreach (XmlNode node in xml.SelectNodes("//NPCCharacter"))
			{
				string id = node.Attributes?["id"]?.Value;
				if (string.IsNullOrEmpty(id)) continue;
				_characters[id] = ParseCharacter(node);
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

		private void LoadCultureColors(string managedTypeId)
		{
			XmlDocument xml = GetMergedXml(managedTypeId);
			if (xml == null) return;
			foreach (XmlNode node in xml.SelectNodes("//Culture"))
			{
				string id = node.Attributes?["id"]?.Value;
				string color1 = node.Attributes?["color"]?.Value;
				string color2 = node.Attributes?["color2"]?.Value;
				if (string.IsNullOrEmpty(id)) continue;
				_cultureColors[id] = (ParseColor(color1), ParseColor(color2));
			}
		}

		/// <summary>skins.xml is consumed by the native engine (no SubModule.xml managed-xml entry),
		/// so it can't be merged through MBObjectManager - parse every active module's file instead.</summary>
		private void LoadSkins()
		{
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				try
				{
					string path = System.IO.Path.Combine(module.FolderPath, "ModuleData", "skins.xml");
					if (!System.IO.File.Exists(path)) continue;
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
					Log($"[NativeMpData] Failed to load skins.xml of module '{module.Id}' ({ex.Message}).", LogLevel.Warning);
				}
			}
		}

		/// <summary>
		/// Parse every active module's action_sets.xml. Only two things are kept: 
		/// - the action-set -> skeleton mapping (to resolve the mount's skeleton model)
		/// - the catalog of animation clip names (editor suggestions).
		/// </summary>
		private void LoadActionSets()
		{
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				try
				{
					string path = System.IO.Path.Combine(module.FolderPath, "ModuleData", "action_sets.xml");
					if (!System.IO.File.Exists(path)) continue;
					XmlDocument xml = new XmlDocument();
					xml.Load(path);
					foreach (XmlNode setNode in xml.SelectNodes("//action_set"))
					{
						string id = setNode.Attributes?["id"]?.Value;
						string skeleton = setNode.Attributes?["skeleton"]?.Value;
						if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(skeleton) && !_skeletonOfActionSet.ContainsKey(id))
							_skeletonOfActionSet[id] = skeleton;
						foreach (XmlNode actionNode in setNode.SelectNodes("action"))
						{
							string clip = actionNode.Attributes?["animation"]?.Value;
							if (!string.IsNullOrEmpty(clip)) _clips.Add(clip);
						}
					}
				}
				catch (Exception ex)
				{
					Log($"[NativeMpData] Failed to load action_sets.xml of module '{module.Id}' ({ex.Message}).", LogLevel.Warning);
				}
			}
		}

		private void LoadMonsters()
		{
			foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
			{
				try
				{
					string path = System.IO.Path.Combine(module.FolderPath, "ModuleData", "monsters.xml");
					if (!System.IO.File.Exists(path)) continue;
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
					Log($"[NativeMpData] Failed to load monsters.xml of module '{module.Id}' ({ex.Message}).", LogLevel.Warning);
				}
			}
			ResolveMonsterInheritance();
		}

		private readonly Dictionary<string, string> _monsterBaseIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private void ResolveMonsterInheritance()
		{
			foreach (string id in _monsters.Keys.ToList())
			{
				MonsterDefinition monster = _monsters[id];
				string current = id;
				int depth = 0;
				while (_monsterBaseIds.TryGetValue(current, out string baseId) && depth++ < 16)
				{
					if (string.Equals(baseId, id, StringComparison.OrdinalIgnoreCase)) break; // cycle
					if (!_monsters.TryGetValue(baseId, out MonsterDefinition baseMonster)) break;
				// The base fills the gaps; the derived values already set win.
				string actionSet = monster.ActionSetId, main = monster.MainHandItemBone, off = monster.OffHandItemBone, offSec = monster.OffHandItemSecondaryBone, thorax = monster.ThoraxBone, spineLower = monster.SpineLowerBone, sit = monster.RiderSitBone;
				float speed = monster.WalkingSpeed;
				if (string.IsNullOrEmpty(actionSet)) monster.ActionSetId = baseMonster.ActionSetId;
				if (string.IsNullOrEmpty(main)) monster.MainHandItemBone = baseMonster.MainHandItemBone;
				if (string.IsNullOrEmpty(off)) monster.OffHandItemBone = baseMonster.OffHandItemBone;
				if (string.IsNullOrEmpty(offSec)) monster.OffHandItemSecondaryBone = baseMonster.OffHandItemSecondaryBone;
				if (string.IsNullOrEmpty(thorax)) monster.ThoraxBone = baseMonster.ThoraxBone;
				if (string.IsNullOrEmpty(spineLower)) monster.SpineLowerBone = baseMonster.SpineLowerBone;
				if (string.IsNullOrEmpty(sit)) monster.RiderSitBone = baseMonster.RiderSitBone;
				if (speed <= 0f) monster.WalkingSpeed = baseMonster.WalkingSpeed;
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
				HolsterPositionShift = ParseVec3(node.Attributes?["holster_position_shift"]?.Value)
			};
			XmlNode armor = node.SelectSingleNode("ItemComponent/Armor");
			if (armor != null)
			{
				item.HasArmor = true;
				item.CoversHead = ParseBool(armor.Attributes?["covers_head"]?.Value);
				item.CoversBody = ParseBool(armor.Attributes?["covers_body"]?.Value);
				item.CoversHands = ParseBool(armor.Attributes?["covers_hands"]?.Value);
				item.CoversLegs = ParseBool(armor.Attributes?["covers_legs"]?.Value);
			}
			XmlNode weapon = node.SelectSingleNode("ItemComponent/Weapon");
			if (weapon != null)
			{
				item.WeaponPosition = ParseVec3(weapon.Attributes?["position"]?.Value);
				item.WeaponRotation = ParseVec3(weapon.Attributes?["rotation"]?.Value);
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
					item.Flags.Add(attribute.LocalName);
				}
			}
			return item;
		}

		private static CharacterDefinition ParseCharacter(XmlNode node)
		{
			string id = node.Attributes?["id"]?.Value;
			CharacterDefinition definition = new CharacterDefinition
			{
				Id = id,
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

		private static uint ParseColor(string hex)
		{
			return uint.TryParse(string.IsNullOrEmpty(hex) ? null : hex.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out uint value) ? value : uint.MaxValue;
		}
	}
}
