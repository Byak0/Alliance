using Alliance.Common.Extensions.BuildSystem.Configuration.Models;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TaleWorlds.ModuleManager;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.BuildSystem.Configuration
{
	/// <summary>
	/// Generates the prefab catalog once at startup and keeps it in memory.
	/// Only the preset is persisted on disk.
	/// </summary>
	public static class BuildPrefabCatalogManager
	{
		private const string PresetFileName = "BuildPrefabPreset.xml";

		private static readonly object _lock = new object();
		public static BuildPrefabCatalog Catalog { get; private set; }
		public static string[] AllPrefabNames { get; private set;}

		public static string PresetPath => PathHelper.GetAllianceDocumentFilePath(PresetFileName);

		public static void Initialize()
		{
			if (Catalog != null)
			{
				return;
			}

			lock (_lock)
			{
				if (Catalog == null)
				{
					Catalog = GenerateCatalogFromModules();
					AllPrefabNames = Catalog.Prefabs.Select(x => x.Id).ToArray();
					Log($"[BuildPrefabCatalog] Initialized {Catalog.Prefabs.Count} prefabs in memory.", LogLevel.Information);
				}
			}
		}

		public static BuildPrefabPreset LoadOrCreatePreset()
		{
			EnsureConfigDirectory();

			BuildPrefabPreset defaultPreset = CreateDefaultPreset();
			BuildPrefabPreset preset = SerializeHelper.LoadClassFromFile(PresetPath, defaultPreset);

			preset = NormalizePreset(preset);
			SerializeHelper.SaveClassToFile(PresetPath, preset);

			return preset;
		}

		public static bool SavePreset(BuildPrefabPreset preset)
		{
			EnsureConfigDirectory();

			preset = NormalizePreset(preset);

			return SerializeHelper.SaveClassToFile(PresetPath, preset);
		}

		public static List<string> GetActivePrefabIds()
		{
			BuildPrefabPreset preset = LoadOrCreatePreset();

			HashSet<string> catalogIds = new HashSet<string>(
				AllPrefabNames,
				StringComparer.OrdinalIgnoreCase);

			return preset.AllowedPrefabs
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id) && catalogIds.Contains(x.Id))
				.Select(x => x.Id)
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}

		private static BuildPrefabCatalog GenerateCatalogFromModules()
		{
			Dictionary<string, BuildPrefabDefinition> prefabs = new Dictionary<string, BuildPrefabDefinition>(StringComparer.OrdinalIgnoreCase);

			try
			{
				foreach (string moduleName in TaleWorlds.Engine.Utilities.GetModulesNames())
				{
					IndexModulePrefabs(moduleName, prefabs);
				}
			}
			catch (Exception ex)
			{
				Log($"[BuildPrefabCatalog] Failed to scan modules: {ex}", LogLevel.Error);
			}

			return new BuildPrefabCatalog
			{
				GeneratedAtUtc = DateTime.UtcNow,
				Prefabs = prefabs.Values
					.OrderBy(x => x.Module, StringComparer.OrdinalIgnoreCase)
					.ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
					.ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
					.ToList()
			};
		}

		private static void IndexModulePrefabs(string moduleName, Dictionary<string, BuildPrefabDefinition> prefabs)
		{
			string modulePath = ModuleHelper.GetModuleFullPath(moduleName);
			if (string.IsNullOrWhiteSpace(modulePath))
			{
				return;
			}

			string prefabsPath = Path.Combine(modulePath, "Prefabs");
			if (!Directory.Exists(prefabsPath))
			{
				return;
			}

			foreach (string xmlPath in Directory.EnumerateFiles(prefabsPath, "*.xml", SearchOption.AllDirectories))
			{
				IndexPrefabFile(moduleName, prefabsPath, xmlPath, prefabs);
			}
		}

		private static void IndexPrefabFile(string moduleName, string prefabsRootPath, string xmlPath, Dictionary<string, BuildPrefabDefinition> prefabs)
		{
			try
			{
				XDocument document = XDocument.Load(xmlPath);
				XElement root = document.Root;
				if (root == null)
				{
					return;
				}

				foreach (XElement prefabRoot in root.Elements("game_entity"))
				{
					string prefabId = (string)prefabRoot.Attribute("name");
					if (string.IsNullOrWhiteSpace(prefabId) || prefabs.ContainsKey(prefabId))
					{
						continue;
					}

					prefabs.Add(prefabId, new BuildPrefabDefinition
					{
						Id = prefabId,
						Module = moduleName,
						SourcePath = PathHelper.GetRelativePath(prefabsRootPath, xmlPath)
					});
				}
			}
			catch (Exception ex)
			{
				Log($"[BuildPrefabCatalog] Failed to parse prefab file '{xmlPath}': {ex.Message}", LogLevel.Warning);
			}
		}

		private static BuildPrefabPreset CreateDefaultPreset()
		{
			return new BuildPrefabPreset
			{
				Name = "Default",
				AllowedPrefabs = new List<BuildPrefabReference>()
			};
		}

		private static BuildPrefabPreset NormalizePreset(BuildPrefabPreset preset)
		{
			preset ??= new BuildPrefabPreset();
			preset.Name ??= "Default";
			preset.AllowedPrefabs ??= new List<BuildPrefabReference>();

			HashSet<string> catalogIds = new HashSet<string>(
				AllPrefabNames,
				StringComparer.OrdinalIgnoreCase);

			preset.AllowedPrefabs = preset.AllowedPrefabs
				.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Id) && catalogIds.Contains(x.Id))
				.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
				.Select(x => x.First())
				.ToList();

			return preset;
		}

		private static void EnsureConfigDirectory()
		{
			string directory = Path.GetDirectoryName(PresetPath);
			if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
	}
}