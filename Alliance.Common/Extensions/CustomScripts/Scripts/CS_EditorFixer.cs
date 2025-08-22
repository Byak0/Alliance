using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using Path = System.IO.Path;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// A script to fix some editor issues...
	/// </summary>
	public class CS_EditorFixer : ScriptComponentBehavior
	{
		private static Dictionary<string, List<PrefabArg2>> _prefabToArg2 = new();
		private static GameEntity _lastSelectedEntity;

		public string REMINDER = "Save often, can crash anytime";

		public SimpleButton I_APPLY_ARG2_README;
		public string PrefabFile = "al_nat_deco.xml";
		public SimpleButton LOAD_PREFABS;
		public SimpleButton APPLY_ARG2_FROM_PREFABS;
		public bool AutoApplyOnSelect = false;
		public bool AutoBreak = false;

		public SimpleButton II_SAVE_ARG2_README;
		public bool EnableArg2Saving = false;

		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == nameof(I_APPLY_ARG2_README)) { DisplayApplyArg2Readme(); return; }
			if (variableName == nameof(LOAD_PREFABS)) { LoadPrefabs(); return; }
			if (variableName == nameof(APPLY_ARG2_FROM_PREFABS)) { ApplyArg2FromPrefabs(); return; }
			if (variableName == nameof(II_SAVE_ARG2_README)) { DisplaySaveArg2Readme(); return; }
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);

			if (!AutoApplyOnSelect && !EnableArg2Saving) return;

			// If the last selected entity is still selected, no need to check again
			if (_lastSelectedEntity != null && _lastSelectedEntity.Scene != null && _lastSelectedEntity.IsSelectedOnEditor()
				&& !(EnableArg2Saving && Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyPressed(InputKey.N)))
			{
				return;
			}

			List<GameEntity> entities = new();
			MBEditor._editorScene?.GetEntities(ref entities);

			foreach (GameEntity entity in entities)
			{
				if (entity.IsSelectedOnEditor())
				{
					if (EnableArg2Saving && Input.IsKeyDown(InputKey.LeftControl) && Input.IsKeyPressed(InputKey.N))
					{
						SaveEntityArg2ToPrefab(entity);
					}
					else if (_lastSelectedEntity != entity && AutoApplyOnSelect)
					{
						CheckVectorArgument(entity);
					}
					_lastSelectedEntity = entity;
					Log($"Selected {entity.Name}");
					return;
				}
			}
		}

		private void DisplayApplyArg2Readme()
		{
			string message =
				"================ CS_EditorFixer ================\n" +
				"This script intends to help fix flaws in the Modding Kit.\n" +
				"- Missing Argument2 in MetaMesh when importing a prefab :\n" +
				"  1) Set your prefab file name (file must be in Alliance.Editor/Prefabs)\n" +
				"  2) Click 'LOAD_PREFABS' \n" +
				"  3) Then either :\n" +
				"     - Click 'APPLY_ARG2_FROM_PREFABS' to fix all entities in the editor scene\n" +
				"     - Enable AutoApplyOnSelect to fix the selected entity automatically\n" +
				"  (RECOMMENDED) Enable AutoBreak to break prefab after applying.\n" +
				"  Otherwise, the fixed values may not be saved in the scene.\n" +
				"==========================================";
			Log(message, LogLevel.Information);
		}

		private void DisplaySaveArg2Readme()
		{
			string message =
				"================ CS_EditorFixer ================\n" +
				"This script intends to help fix flaws in the Modding Kit.\n" +
				"- Saving Argument2 in MetaMesh when exporting a prefab :\n" +
				"  1) Enable EnableArg2Saving\n" +
				"  2) After saving your prefab, press CTRL+N to also save the Argument2\n" +
				"  (The fixed file is written to PrefabsWithFixedArg2/ and does NOT touch the original.)\n" +
				"==========================================";
			Log(message, LogLevel.Information);
		}

		private void LoadPrefabs()
		{
			string modulePath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
			_prefabToArg2 = PrefabArg2Helper.IndexPrefabsWithOverrides(modulePath, PrefabFile);
			string basePath = Path.Combine(modulePath, "Prefabs", PrefabFile);
			string overPath = Path.Combine(modulePath, "PrefabsWithFixedArg2", PrefabFile);
			Log($"CS_EditorFixer - Loaded {_prefabToArg2.Count} prefabs. Base: {basePath} | Override (preferred if present): {overPath}");
		}

		private void ApplyArg2FromPrefabs()
		{
			List<GameEntity> entities = new();
			MBEditor._editorScene?.GetEntities(ref entities);
			Log($"Found {entities.Count} entities in the editor scene. Checking for VectorArgument2 fixes...");
			foreach (GameEntity entity in entities)
				CheckVectorArgument(entity);
		}

		private void SaveEntityArg2ToPrefab(GameEntity entity)
		{
			List<PrefabArg2> entries = PrefabArg2Helper.CollectRuntimeArg2(entity);
			if (entries.Count == 0)
			{
				Log($"[EditorFixer] No argument2 found on '{entity.Name}' hierarchy. Nothing to save.", LogLevel.Information);
				return;
			}

			string modulePath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
			string prefabsRoot = Path.Combine(modulePath, "Prefabs");
			string xmlPath = PrefabArg2Helper.TryFindPrefabXml(prefabsRoot, entity.Name);

			if (string.IsNullOrEmpty(xmlPath))
			{
				Log($"[EditorFixer] Could not find prefab XML for '{entity.Name}' under '{prefabsRoot}'.", LogLevel.Warning);
				return;
			}

			// writes to PrefabsWithFixedArg2/<same file name>.xml
			(int updated, int added, string outPath) = PrefabArg2Helper.UpdatePrefabFile(xmlPath, entity.Name, entries);

			// Refresh in-memory map to immediately use the NEW values for Apply/AutoApply
			List<PrefabArg2> refreshed = PrefabArg2Helper.IndexSinglePrefabFromFile(outPath, entity.Name);
			if (refreshed != null && refreshed.Count > 0)
				_prefabToArg2[entity.Name] = refreshed;

			Log($"[EditorFixer] Wrote fixed copy to '{outPath}' for prefab '{entity.Name}'. Updated: {updated}, Added: {added}.", LogLevel.Information);
		}

		private void CheckVectorArgument(GameEntity entity)
		{
			if (entity == null || string.IsNullOrEmpty(entity.Name) || _prefabToArg2 == null || _prefabToArg2.Count == 0) return;
			if (!_prefabToArg2.TryGetValue(entity.Name, out List<PrefabArg2> entries) || entries.Count == 0) return;

			PrefabArg2Helper.ApplyArg2Recursive(entity, entries);

			if (AutoBreak)
			{
				entity.BreakPrefab();
				Log($"Auto-broke prefab {entity.Name}");
			}
		}
	}

	/// <summary>
	/// Prefab Arg2 utilities: index a prefab file, apply to runtime entities, and write a safe copy with Arg2 injected.
	/// </summary>
	public static class PrefabArg2Helper
	{
		public static Dictionary<string, List<PrefabArg2>> IndexPrefabsWithOverrides(string modulePath, string prefabFile)
		{
			var result = new Dictionary<string, List<PrefabArg2>>(StringComparer.OrdinalIgnoreCase);

			string basePath = Path.Combine(modulePath, "Prefabs", prefabFile);
			string overridePath = Path.Combine(modulePath, "PrefabsWithFixedArg2", prefabFile);

			// 1) load base file (if present)
			if (File.Exists(basePath))
			{
				var baseMap = IndexAllPrefabsInFile(basePath);
				foreach (var kv in baseMap)
					result[kv.Key] = kv.Value;
			}

			// 2) load override file (if present) and PREFER its entries
			if (File.Exists(overridePath))
			{
				var overMap = IndexAllPrefabsInFile(overridePath);
				foreach (var kv in overMap)
					result[kv.Key] = kv.Value; // override wins
			}

			return result;
		}

		/// <summary>Index a single prefab root from a specific file (used to refresh after save).</summary>
		public static List<PrefabArg2> IndexSinglePrefabFromFile(string xmlPath, string prefabName)
		{
			if (!File.Exists(xmlPath)) return new List<PrefabArg2>();
			XDocument doc = XDocument.Load(xmlPath);
			XElement root = doc.Root;
			if (root == null) return new List<PrefabArg2>();

			XElement prefabRoot = root.Elements("game_entity")
				.FirstOrDefault(ge => string.Equals((string)ge.Attribute("name"), prefabName, StringComparison.Ordinal));
			if (prefabRoot == null) return new List<PrefabArg2>();

			var entries = new List<PrefabArg2>();
			CollectEntityArg2Recursive(prefabRoot, new List<int>(), entries);
			return entries;
		}

		// ---------- INDEX ----------
		public static Dictionary<string, List<PrefabArg2>> IndexAllPrefabsInFile(string xmlPath)
		{
			var result = new Dictionary<string, List<PrefabArg2>>(StringComparer.OrdinalIgnoreCase);
			if (!File.Exists(xmlPath)) return result;

			XDocument doc = XDocument.Load(xmlPath);
			XElement root = doc.Root;
			if (root == null) return result;

			foreach (XElement prefabRoot in root.Elements("game_entity"))
			{
				string prefabName = (string)prefabRoot.Attribute("name") ?? string.Empty;
				if (string.IsNullOrEmpty(prefabName)) continue;

				var entries = new List<PrefabArg2>();
				CollectEntityArg2Recursive(prefabRoot, new List<int>(), entries);

				if (entries.Count > 0)
					result[prefabName] = entries;
			}
			return result;
		}

		private static void CollectEntityArg2Recursive(XElement ge, List<int> indices, List<PrefabArg2> outEntries)
		{
			XElement comps = ge.Element("components");
			if (comps != null)
			{
				// include wrapped meta_mesh_component (e.g., cloth_simulator/meta_mesh_component)
				foreach (XElement mm in comps.Descendants("meta_mesh_component"))
				{
					string metaName = (string)mm.Attribute("name") ?? string.Empty;

					foreach (XElement meshNode in mm.Elements("mesh"))
					{
						XAttribute arg2Attr = meshNode.Attribute("argument2");
						if (arg2Attr == null) continue;

						if (TryParseVec3(arg2Attr.Value, out Vec3 v))
						{
							outEntries.Add(new PrefabArg2
							{
								Path = IndicesToPath(indices),
								MetaMeshName = metaName,
								MeshName = (string)meshNode.Attribute("name") ?? string.Empty,
								MaterialName = (string)meshNode.Attribute("material") ?? string.Empty,
								Arg2 = new Vec3(v.X, v.Y, v.Z, v.w)
							});
						}
					}
				}
			}

			XElement children = ge.Element("children");
			if (children == null) return;

			int idx = 0;
			foreach (XElement childGE in children.Elements("game_entity"))
			{
				indices.Add(idx++);
				CollectEntityArg2Recursive(childGE, indices, outEntries);
				indices.RemoveAt(indices.Count - 1);
			}
		}

		// ---------- APPLY ----------
		public static void ApplyArg2Recursive(GameEntity entity, List<PrefabArg2> entries, List<int> indices = null)
		{
			indices ??= new List<int>();
			string path = IndicesToPath(indices);

			for (int i = 0; i < entries.Count; i++)
			{
				PrefabArg2 e = entries[i];
				if (!path.Equals(e.Path, StringComparison.Ordinal)) continue;
				TryApplyToEntity(entity, e);
			}

			List<GameEntity> children = entity.GetChildren()?.ToList() ?? new List<GameEntity>();
			for (int i = 0; i < children.Count; i++)
			{
				indices.Add(i);
				ApplyArg2Recursive(children[i], entries, indices);
				indices.RemoveAt(indices.Count - 1);
			}
		}

		private static void TryApplyToEntity(GameEntity entity, PrefabArg2 entry)
		{
			try
			{
				for (int i = 0; ; i++)
				{
					MetaMesh mm = entity.GetMetaMesh(i);
					mm ??= entity.GetClothSimulator(i)?.GetFirstMetaMesh();
					if (mm == null) break;

					if (!string.IsNullOrEmpty(entry.MetaMeshName) && mm.GetName() != entry.MetaMeshName)
						continue;

					Vec3 cur = mm.GetVectorArgument2();
					if (IsVec3FullyEqual(entry.Arg2, cur)) continue;

					mm.SetVectorArgument2(entry.Arg2.X, entry.Arg2.Y, entry.Arg2.Z, entry.Arg2.w);
					Log($"Fixed {entity.Name}/{entry.MetaMeshName} VectorArgument2 to ({entry.Arg2.X},{entry.Arg2.Y},{entry.Arg2.Z},{entry.Arg2.w})");
				}
			}
			catch (Exception ex)
			{
				Log($"[PrefabArg2] Apply failed on '{entity?.Name}' at path '{entry.Path}': {ex.Message}", LogLevel.Warning);
			}
		}

		// Custom comparison because native ignores w value
		private static bool IsVec3FullyEqual(Vec3 vecA, Vec3 vecB)
		{
			return vecA == vecB && vecA.w == vecB.w;
		}

		// ---------- SAVE (to copy in PrefabsWithFixedArg2) ----------
		/// <summary>
		/// Update (or add) argument2 attributes for a specific prefab root in the source XML,
		/// then save a copy to Module/PrefabsWithFixedArg2/&lt;same file name&gt;.xml.
		/// Returns (updated, added, outPath).
		/// </summary>
		public static (int updated, int added, string outPath) UpdatePrefabFile(string sourceXmlPath, string prefabName, List<PrefabArg2> entries)
		{
			// Paths
			string modulePath = ModuleHelper.GetModuleFullPath(SubModule.CurrentModuleName);
			string outDir = Path.Combine(modulePath, "PrefabsWithFixedArg2");
			Directory.CreateDirectory(outDir);
			string outPath = Path.Combine(outDir, Path.GetFileName(sourceXmlPath));

			// 1) Load NATIVE doc and grab the target prefab (baseline structure)
			XDocument nativeDoc = XDocument.Load(sourceXmlPath);
			XElement nativeRoot = nativeDoc.Root ?? throw new InvalidOperationException("Invalid prefab XML: missing root.");
			XElement nativePrefab = nativeRoot.Elements("game_entity")
				.FirstOrDefault(ge => string.Equals((string)ge.Attribute("name"), prefabName, StringComparison.Ordinal))
				?? throw new InvalidOperationException($"Prefab '{prefabName}' not found in '{sourceXmlPath}'.");

			// 2) Load WORKING doc (override if exists, otherwise start from native)
			XDocument workDoc = File.Exists(outPath) ? XDocument.Load(outPath) : new XDocument(new XElement(nativeRoot));
			XElement workRoot = workDoc.Root ?? throw new InvalidOperationException("Invalid prefab XML: missing root.");

			// 3) Replace ONLY the target prefab in the working doc with the fresh native one
			XElement workPrefab = workRoot.Elements("game_entity")
				.FirstOrDefault(ge => string.Equals((string)ge.Attribute("name"), prefabName, StringComparison.Ordinal));

			XElement nativeClone = new XElement(nativePrefab); // deep clone

			if (workPrefab != null)
				workPrefab.ReplaceWith(nativeClone);
			else
				workRoot.Add(nativeClone);

			// 4) Apply/merge argument2 updates onto the (now native) prefab element
			int updated = 0, added = 0;

			foreach (var e in entries)
			{
				XElement geNode = ResolveGameEntityByPath(nativeClone, e.Path);
				if (geNode == null) continue;

				IEnumerable<XElement> metaMeshNodes = geNode.Element("components")?.Descendants("meta_mesh_component")
					?? Enumerable.Empty<XElement>();

				if (!string.IsNullOrEmpty(e.MetaMeshName))
					metaMeshNodes = metaMeshNodes.Where(mm => string.Equals((string)mm.Attribute("name"), e.MetaMeshName, StringComparison.Ordinal));

				foreach (XElement mm in metaMeshNodes)
				{
					foreach (XElement mesh in mm.Elements("mesh"))
					{
						string newVal = FormatVec(e.Arg2);
						XAttribute a2 = mesh.Attribute("argument2");
						if (a2 == null)
						{
							mesh.Add(new XAttribute("argument2", newVal));
							added++;
						}
						else if (!string.Equals(a2.Value, newVal, StringComparison.Ordinal))
						{
							a2.Value = newVal;
							updated++;
						}
					}
				}
			}

			// 5) Write back atomically to the override file (other prefabs remain untouched)
			AtomicWrite(workDoc, outPath);
			return (updated, added, outPath);
		}

		// ---------- Helpers ----------
		public static List<PrefabArg2> CollectRuntimeArg2(GameEntity root)
		{
			var result = new List<PrefabArg2>();
			void DFS(GameEntity node, List<int> idx)
			{
				string path = IndicesToPath(idx);

				for (int i = 0; ; i++)
				{
					MetaMesh mm = node.GetMetaMesh(i);
					mm ??= node.GetClothSimulator(i)?.GetFirstMetaMesh();
					if (mm == null) break;

					Vec3 v = mm.GetVectorArgument2();
					string metaName = mm.GetName();

					result.Add(new PrefabArg2
					{
						Path = path,
						MetaMeshName = metaName,
						MeshName = string.Empty,
						MaterialName = string.Empty,
						Arg2 = v
					});
				}

				List<GameEntity> children = node.GetChildren()?.ToList() ?? new List<GameEntity>();
				for (int c = 0; c < children.Count; c++)
				{
					idx.Add(c);
					DFS(children[c], idx);
					idx.RemoveAt(idx.Count - 1);
				}
			}

			DFS(root, new List<int>());
			return result;
		}

		/// Find an XML file containing a root <game_entity name="prefabName"> under a directory (recursive).
		public static string TryFindPrefabXml(string rootDir, string prefabName)
		{
			if (!Directory.Exists(rootDir)) return null;

			foreach (string file in Directory.EnumerateFiles(rootDir, "*.xml", SearchOption.AllDirectories))
			{
				try
				{
					XDocument doc = XDocument.Load(file);
					XElement root = doc.Root;
					if (root == null) continue;

					if (root.Elements("game_entity").Any(ge => string.Equals((string)ge.Attribute("name"), prefabName, StringComparison.Ordinal)))
						return file;
				}
				catch { /* ignore unreadable files */ }
			}
			return null;
		}

		private static XElement ResolveGameEntityByPath(XElement prefabRoot, string path)
		{
			if (string.IsNullOrEmpty(path) || path == "/") return prefabRoot;

			XElement cur = prefabRoot;
			string[] parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0; i < parts.Length; i++)
			{
				if (!int.TryParse(parts[i], out int idx)) return null;
				XElement children = cur.Element("children");
				if (children == null) return null;
				XElement child = children.Elements("game_entity").ElementAtOrDefault(idx);
				if (child == null) return null;
				cur = child;
			}
			return cur;
		}

		private static string IndicesToPath(List<int> idx)
		{
			if (idx == null || idx.Count == 0) return "/";
			StringBuilder sb = new StringBuilder(2 * idx.Count + 1);
			sb.Append('/');
			for (int i = 0; i < idx.Count; i++)
			{
				if (i > 0) sb.Append('/');
				sb.Append(idx[i]);
			}
			return sb.ToString();
		}

		private static bool TryParseVec3(string s, out Vec3 v)
		{
			v = default;
			if (string.IsNullOrWhiteSpace(s)) return false;
			string[] parts = s.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length != 4) return false;
			if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return false;
			if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) return false;
			if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) return false;
			if (!float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float w)) return false;
			v = new Vec3(x, y, z, w);
			return true;
		}

		private static string FormatVec(Vec3 v) =>
			string.Format(CultureInfo.InvariantCulture, "{0:0.000}, {1:0.000}, {2:0.000}, {3:0.000}", v.X, v.Y, v.Z, v.w);

		private static void AtomicWrite(XDocument doc, string path)
		{
			Encoding enc = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
			var settings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineChars = "\n",
				NewLineHandling = NewLineHandling.Replace,
				OmitXmlDeclaration = true,
				Encoding = enc
			};

			string tmp = path + ".tmp";
			using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.WriteThrough))
			using (var xw = XmlWriter.Create(fs, settings))
			{
				doc.Save(xw);
				xw.Flush();
				fs.Flush(true);
			}
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			if (File.Exists(path)) File.Replace(tmp, path, null, true);
			else File.Move(tmp, path);
		}
	}

	public class PrefabArg2
	{
		public string Path;          // "/0/1/..."
		public string MetaMeshName;  // <meta_mesh_component name="...">
		public string MeshName;      // <mesh name="...">
		public string MaterialName;  // <mesh material="...">
		public Vec3 Arg2;          // (x,y,z,w) via your Vec3 with .w
	}
}