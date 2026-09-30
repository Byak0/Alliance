using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.BuildSystem.Configuration;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.ModuleManager;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Core.Configuration.Models
{
	/// <summary>
	/// Data for Alliance : available maps, cultures, battlesides, characters, etc.
	/// Used in editor and in-game to display options, dropdown lists...
	/// </summary>
	public static class AllianceData
	{
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
			Difficulty,
			Prefab,
			Font,
			Color,
			Animation,
			FacialAnimation
		}

		public enum Difficulty
		{
			Easy = 0,
			Normal = 1,
			Hard = 2,
			VeryHard = 3,
			Bannerlord = 4
		}

		public static string[] AvailableMaps() => SceneList.Scenes.ConvertAll(s => s.Name).ToArray();

		public static string[] AvailableCultures() => Factions.Instance.OrderedCultureKeys.ToArray();

		public static string[] AvailableCharacters() => Characters.Instance.CharacterStubs.ConvertAll(c => c.StringId).ToArray();

		public static string[] AvailableItems()
		{
			// Not usable when in Editor, xml not loaded...
			//return MBObjectManager.Instance.GetObjectTypeList<ItemObject>().ConvertAll(c => c.Name.ToString()).ToArray();
			return new string[] { "TODO" };
		}

		public static string[] AvailableSounds() => AudioPlayer.Instance.GetAvailableSounds();

		public static string[] AvailablePrefabs() => BuildPrefabCatalogManager.AllPrefabNames;

		/// <summary>Action names known by the AnimationSystem (empty when it has not been initialized).</summary>
		public static string[] AvailableAnimations() => AnimationSystem.Instance.DefaultAnimations?
			.Select(a => a.Action.GetName()).ToArray() ?? Array.Empty<string>();

		/// <summary>Animation clip names registered by any action set (native action_sets.xml data,
		/// parsed by NativeMpData). Used by fake agents, which play raw clips.</summary>
		public static string[] AvailableClips() => NativeMpData.Instance.GetClips().OrderBy(c => c).ToArray();

		/// <summary>Facial animation ids from the voices.xml files (face_animation_record entries).
		/// These are consumed by the native engine and are NOT registered as managed Xmls in any
		/// SubModule.xml, so MBObjectManager merging can't see them - parse each active module's
		/// ModuleData/voices.xml directly instead.</summary>
		public static string[] AvailableFacialAnimations()
		{
			try
			{
				List<string> ids = new List<string>();
				HashSet<string> known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				IEnumerable<ModuleInfo> modules = ModuleHelper.GetActiveModules();
				if (modules == null || !modules.Any()) modules = ModuleHelper.GetAllModules();
				foreach (ModuleInfo module in modules)
				{
					string path = Path.Combine(module.FolderPath, "ModuleData", "voices.xml");
					if (!File.Exists(path)) continue;
					XmlDocument voices = new XmlDocument();
					voices.Load(path);
					foreach (XmlNode node in voices.SelectNodes("//face_animation_record"))
					{
						string id = node.Attributes?["id"]?.Value;
						if (!string.IsNullOrEmpty(id) && known.Add(id)) ids.Add(id);
					}
				}
				return ids.ToArray();
			}
			catch
			{
				return Array.Empty<string>();
			}
		}

		public static string[] AvailableFonts()
		{
#if !SERVER
			return TaleWorlds.Engine.GauntletUI.UIResourceManager.FontFactory.GetFonts().Select(TaleWorlds.Engine.GauntletUI.UIResourceManager.FontFactory.GetFontName).Where(n => !string.IsNullOrEmpty(n)).OrderBy(n => n).ToArray();
#else
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

		public static string[] GetData(DataTypes dataType)
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
				DataTypes.Difficulty => AvailableDifficulties,
				DataTypes.Prefab => AvailablePrefabs(),
				DataTypes.Font => AvailableFonts(),
				DataTypes.Color => Array.Empty<string>(),
				DataTypes.Animation => AvailableAnimations(),
				DataTypes.FacialAnimation => AvailableFacialAnimations(),
				_ => Array.Empty<string>(),
			};
		}
	}
}
