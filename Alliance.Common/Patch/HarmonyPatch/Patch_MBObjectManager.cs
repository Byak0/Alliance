using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
	/// <summary>
	/// Patch MBObjectManager to provide more useful infos on failure.
	/// </summary>
	class Patch_MBObjectManager
	{
		private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MBObjectManager));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;

				_patched = true;

				Harmony.Patch(
					typeof(ManagedParameters).GetMethod("LoadFromXml", BindingFlags.Instance | BindingFlags.NonPublic),
					prefix: new HarmonyMethod(typeof(Patch_MBObjectManager).GetMethod(nameof(Prefix_LoadFromXml), BindingFlags.Static | BindingFlags.Public)));

				Harmony.Patch(
					typeof(MBObjectManager).GetMethod("CreateDocumentFromXmlFile", BindingFlags.Static | BindingFlags.NonPublic),
					prefix: new HarmonyMethod(typeof(Patch_MBObjectManager).GetMethod(
						nameof(Prefix_CreateDocumentFromXmlFile), BindingFlags.Static | BindingFlags.Public)));

				Harmony.Patch(
					typeof(MBObjectManager).GetMethod(nameof(MBObjectManager.ToXDocument), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_MBObjectManager).GetMethod(
						nameof(Prefix_ToXDocument), BindingFlags.Static | BindingFlags.Public)));

				Harmony.Patch(
					typeof(MBObjectManager).GetMethod(nameof(MBObjectManager.MergeElements), BindingFlags.Static | BindingFlags.Public),
					prefix: new HarmonyMethod(typeof(Patch_MBObjectManager).GetMethod(
						nameof(Prefix_MergeElements), BindingFlags.Static | BindingFlags.Public)));

			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Patch_MBObjectManager)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Add debug info on merge failure.
		/// </summary>
		public static bool Prefix_LoadFromXml(ManagedParameters __instance, XmlNode doc)
		{
			Debug.Print("loading managed_core_parameters.xml");
			if (doc.ChildNodes.Count < 1)
			{
				//throw new TWXmlLoadException("Incorrect XML document format.");
			}

			if (doc.ChildNodes[0].Name != "base")
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}

			if (doc.ChildNodes[0].ChildNodes[0].Name != "managed_core_parameters")
			{
				throw new TWXmlLoadException("Incorrect XML document format.");
			}

			XmlNode xmlNode = null;
			if (doc.ChildNodes[0].ChildNodes[0].Name == "managed_core_parameters")
			{
				xmlNode = doc.ChildNodes[0].ChildNodes[0].ChildNodes[0];
			}

			while (xmlNode != null)
			{
				if (xmlNode.Name == "managed_core_parameter" && xmlNode.NodeType != XmlNodeType.Comment && Enum.TryParse<ManagedParametersEnum>(xmlNode.Attributes["id"].Value, ignoreCase: true, out var result))
				{
					FieldInfo mPA = __instance.GetType().GetField("_managedParametersArray", BindingFlags.Instance | BindingFlags.NonPublic);
					float[] _managedParametersArray = (float[])mPA.GetValue(__instance);
					_managedParametersArray[(int)result] = float.Parse(xmlNode.Attributes["value"].Value);
					mPA.SetValue(__instance, _managedParametersArray);
				}

				xmlNode = xmlNode.NextSibling;
			}

			return false; // Skip original method
		}

		/// <summary>
		/// Add debug info on merge failure.
		/// </summary>
		public static bool Prefix_CreateDocumentFromXmlFile(string xmlPath, string xsdPath, bool forceSkipValidation, ref XmlDocument __result)
		{
			// Load directly as XDocument with line info
			var xdoc = XDocument.Load(xmlPath, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);

			// Convert back into XmlDocument so callers don’t notice
			var xmldoc = new XmlDocument();
			using (var reader = xdoc.CreateReader())
			{
				xmldoc.Load(reader);
			}

			__result = xmldoc;
			return false; // skip original
		}

		/// <summary>
		/// Add debug info on merge failure.
		/// </summary>
		public static bool Prefix_ToXDocument(XmlDocument xmlDocument, ref XDocument __result)
		{
			using var reader = new XmlNodeReader(xmlDocument);
			try
			{
				reader.MoveToContent();
				// Force line info + base URI
				__result = XDocument.Load(reader, LoadOptions.SetBaseUri | LoadOptions.SetLineInfo);
			}
			catch (Exception ex)
			{
				Log(ex.Message, LogLevel.Debug);
			}

			return false; // Skip original method
		}

		/// <summary>
		/// Add debug info on merge failure, concise format.
		/// </summary>
		public static bool Prefix_MergeElements(XElement element1, XElement element2, string xsdPath)
		{
			try
			{
				bool merged = MBObjectManager.MergeElementAttributes(element1, element2);
				if (element1.Value != "" && element2.Value != "")
				{
					element1.Value = element2.Value;
				}

				if (!string.IsNullOrEmpty(element1.Value) && !string.IsNullOrEmpty(element2.Value))
					element1.Value = element2.Value;

				if (!XmlResource.XsdElementDictionary.ContainsKey(xsdPath))
				{
					Log($"No schema found for {xsdPath}", LogLevel.Warning);
					element1.Add(element2.Elements());
					return false;
				}
				else if (merged)
				{
					element1.Elements().Remove();
				}

				Dictionary<string, XmlResource.XsdElement> elementSchema = XmlResource.XsdElementDictionary[xsdPath];
				IEnumerable<XElement> enumerable = element2.Elements() ?? Enumerable.Empty<XElement>();

				var dictionary =
					(from element in element1.Elements()
					 group element by element.Name)
					.ToDictionary(
						el => el.Key,
						el =>
						{
							XElement element4 = el.First();
							string path = XmlResource.GetFullXPathOfElement(element4, isXsd: false);

							if (!elementSchema.TryGetValue(path, out var schema))
							{
								Log(FormatError(element4, elementSchema), LogLevel.Debug);
								throw new KeyNotFoundException(
									$"Schema lookup failed for element '{element4.Name}'"
								);
							}

							var uniqueAttributes = schema.UniqueAttributes;
							var dictionary2 = new Dictionary<string, XElement>();
							foreach (XElement element3 in el)
							{
								string key = string.Concat(uniqueAttributes.Select(attr => element3?.Attribute(attr)?.Value ?? string.Empty));
								dictionary2[key] = element3;
							}

							return dictionary2;
						});

				foreach (XElement element2Element in enumerable)
				{
					if (dictionary.TryGetValue(element2Element.Name, out var value))
					{
						XElement value2 = value.First().Value;
						string path = XmlResource.GetFullXPathOfElement(value2, isXsd: false);

						if (!elementSchema.TryGetValue(path, out var schema))
						{
							Log(FormatError(value2, elementSchema), LogLevel.Debug);
							throw new KeyNotFoundException(
								$"Schema lookup failed for element '{value2.Name}'"
							);
						}

						if (schema.AlwaysPreferMerge)
						{
							MBObjectManager.MergeElements(value2, element2Element, xsdPath);
							continue;
						}

						var uniqueAttributes = schema.UniqueAttributes;
						string text = string.Concat(uniqueAttributes.Select(attr => element2Element?.Attribute(attr)?.Value ?? string.Empty));

						if (value.TryGetValue(text, out var value3) && text != "")
						{
							MBObjectManager.MergeElements(value3, element2Element, xsdPath);
						}
						else
						{
							element1.Add(element2Element);
						}
					}
					else
					{
						element1.Add(element2Element);
					}
				}

				return false; // Skip original method
			}
			catch (Exception ex)
			{
				string file1 = Path.GetFileName(element1.BaseUri ?? "");
				string file2 = Path.GetFileName(element2.BaseUri ?? "");
				Log(
		$@"[Alliance_Patch_MBObjectManager] ERROR while merging:
root1=<{element1.Name.LocalName}> (file={file1})
root2=<{element2.Name.LocalName}> (file={file2})
xsdPath='{xsdPath}'
Exception: {ex.GetType().Name} - {ex.Message}",
					LogLevel.Debug
				);
				throw;
			}
		}

		/// <summary>
		/// Get file/line info for an XElement if available.
		/// </summary>
		private static string GetElementLocation(XElement element)
		{
			if (element == null) return "unknown element";
			string parent = element.Parent?.Name.LocalName ?? "no parent";
			string file = element.BaseUri;

			if (element is IXmlLineInfo li && li.HasLineInfo())
			{
				return (string.IsNullOrEmpty(file) ? "unknown file" : file) +
					   $" (line {li.LineNumber}, pos {li.LinePosition}), inside <{parent}>";
			}

			if (!string.IsNullOrEmpty(file))
				return file + $", inside <{parent}>";

			return $"inside <{parent}>";
		}

		/// <summary>
		/// Finds expected parent(s) from schema for a given element name.
		/// </summary>
		private static string GetExpectedParents(Dictionary<string, XmlResource.XsdElement> schema, string elementName, out string exampleXPath)
		{
			var parents = new HashSet<string>(StringComparer.Ordinal);
			string example = null;

			foreach (var key in schema.Keys)
			{
				if (!key.EndsWith("/" + elementName, StringComparison.Ordinal))
					continue;

				var parts = key.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2)
				{
					string parent = parts[parts.Length - 2];
					parents.Add(parent);
					if (example == null) example = key;
				}
			}

			exampleXPath = example ?? "(none)";
			if (parents.Count == 0) return "unknown";

			if (parents.Count == 1) return parents.First();
			return string.Join(", ", parents.Take(3));
		}

		/// <summary>
		/// Formats a concise error message with file, line and suggestion.
		/// </summary>
		private static string FormatError(XElement element, Dictionary<string, XmlResource.XsdElement> schema)
		{
			string file = Path.GetFileName(element.BaseUri ?? "");
			string loc = "";
			if (element is IXmlLineInfo li && li.HasLineInfo())
				loc = $" (line {li.LineNumber}, pos {li.LinePosition})";

			string snippet = element.ToString(SaveOptions.DisableFormatting);
			if (snippet.Length > 200) snippet = snippet.Substring(0, 200) + "...";

			string examplePath;
			string expected = GetExpectedParents(schema, element.Name.LocalName, out examplePath);

			return
		$@"[Alliance_Patch_MBObjectManager] ERROR !! Invalid XML structure in {file}{loc}:
{snippet}
>> Check that <{element.Name.LocalName}> is nested under <{expected}>";
		}
	}
}