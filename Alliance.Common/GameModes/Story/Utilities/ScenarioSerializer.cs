using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Functions;
using Alliance.Common.GameModes.Story.Interfaces;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using Alliance.Common.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Utility class for serializing and deserializing scenarios to and from XML files.
	/// Allow for polymorphic serialization of derived types.
	/// Use the ISerializationCallback interface to implement custom serialization and deserialization logic.
	/// </summary>
	public class ScenarioSerializer
	{
		private static XmlSerializer _xmlSerializer;
		private static XmlSerializer _conditionalActionSerializer;
		private static readonly object _knownTypeNamesCacheLock = new object();
		private static readonly Dictionary<Type, HashSet<string>> _knownTypeNamesCache = new Dictionary<Type, HashSet<string>>();

		private static XmlSerializer XmlSerializer
		{
			get
			{
				_xmlSerializer ??= CreateScenarioSerializer(
					rootType: typeof(Scenario),
					typeof(Objective), typeof(ProgressElement),
					typeof(ActionBase), typeof(Condition), typeof(GameModeSettings),
					typeof(Function), typeof(Zone), typeof(ZoneShape), typeof(ZoneAnchor), typeof(ValueSource<>));
				return _xmlSerializer;
			}
		}

		private static XmlSerializer ConditionalActionSerializer
		{
			get
			{
				_conditionalActionSerializer ??= SerializeHelper.CreateSerializer(
					rootType: typeof(ScriptedEvent),
					typeof(Condition), typeof(ActionBase), typeof(ProgressElement),
					typeof(Function), typeof(Zone), typeof(ZoneShape), typeof(ZoneAnchor), typeof(ValueSource<>));
				return _conditionalActionSerializer;
			}
		}

		private static XmlSerializer CreateScenarioSerializer(Type rootType, params Type[] baseTypes)
		{
			List<Type> derivedTypes = SerializeHelper.GetSerializableDerivedTypes(baseTypes)
				.Distinct()
				.ToList();
			// Add ValueSource<T> and its derived types
			derivedTypes.AddRange(GetClosedValueSourceTypes(rootType, derivedTypes));

			return new XmlSerializer(rootType, derivedTypes.Distinct().ToArray());
		}

		/// <summary>
		/// Creates an XmlSerializer that can serialize a given root type and include all derived types of specified base types.
		/// </summary>
		/// <param name="rootType">The type of the root object to serialize.</param>
		/// <param name="baseTypes">The base types for which all derived types should be included.</param>
		/// <returns>A configured XmlSerializer.</returns>
		private static XmlSerializer CreateSerializer(Type rootType, params Type[] baseTypes)
		{
			List<Type> derivedTypes = SerializeHelper.GetSerializableDerivedTypes(baseTypes)
				.Distinct()
				.ToList();
			derivedTypes.AddRange(GetClosedValueSourceTypes(rootType, derivedTypes));

			return new XmlSerializer(rootType, derivedTypes.Distinct().ToArray());
		}

		/// <summary>
		/// XmlSerializer cannot register open generic types such as <c>LiteralValue&lt;&gt;</c>. Discover
		/// each closed <c>ValueSource&lt;T&gt;</c> field reachable from the scenario graph and add its valid
		/// concrete node types explicitly.
		/// </summary>
		private static IEnumerable<Type> GetClosedValueSourceTypes(Type rootType, IEnumerable<Type> knownTypes)
		{
			HashSet<Type> valueSourceTypes = new HashSet<Type>();
			HashSet<Type> visited = new HashSet<Type>();
			Queue<Type> pending = new Queue<Type>();
			pending.Enqueue(rootType);
			foreach (Type type in knownTypes) pending.Enqueue(type);

			while (pending.Count > 0)
			{
				Type type = UnwrapCollectionType(pending.Dequeue());
				if (type == null || !visited.Add(type)) continue;
				if (ValueSourceHelper.IsValueSourceType(type))
				{
					valueSourceTypes.Add(type);
					continue;
				}

				if (!IsScenarioGraphType(type)) continue;
				foreach (FieldInfo field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
				{
					Type fieldType = UnwrapCollectionType(field.FieldType);
					if (ValueSourceHelper.IsValueSourceType(fieldType))
					{
						valueSourceTypes.Add(fieldType);
					}
					else if (IsScenarioGraphType(fieldType))
					{
						pending.Enqueue(fieldType);
					}
				}
			}

			return valueSourceTypes.SelectMany(ValueSourceHelper.GetConcreteTypes);
		}

		private static Type UnwrapCollectionType(Type type)
		{
			if (type != null && type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type))
			{
				return type.GetGenericArguments()[0];
			}
			return type;
		}

		private static bool IsScenarioGraphType(Type type)
		{
			return type != null
				&& type.Namespace != null
				&& type.Namespace.Contains(".GameModes.Story");
		}

		public static void SerializeScenarioToXML(Scenario scenarioToSerialize, string filePath)
		{
			// Ensure the directory exists
			if (!Directory.Exists(Path.GetDirectoryName(filePath)))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			}

			RecursiveSerializationCallBack(scenarioToSerialize, obj => obj.OnBeforeSerialize());

			// Serialize the scenario to XML
			using (TextWriter writer = new StreamWriter(filePath))
			{
				XmlSerializer.Serialize(writer, scenarioToSerialize);
			}
		}

		public static List<Scenario> DeserializeAllScenarios(string directoryPath)
		{
			List<Scenario> scenarios = new List<Scenario>();

			if (!Directory.Exists(directoryPath))
			{
				Log($"Directory '{directoryPath}' does not exist.", LogLevel.Warning);
				return scenarios;
			}

			// Get all XML files in the specified directory
			string[] files = Directory.GetFiles(directoryPath, "*.xml");

			// Deserialize each scenario
			foreach (string file in files)
			{
				scenarios.Add(DeserializeScenarioFromPath(file));
			}

			return scenarios;
		}

		public static Scenario DeserializeScenarioFromPath(string filename)
		{
			// Check if the file exists before deserializing
			if (File.Exists(filename))
			{
				try
				{
					using (TextReader reader = new StreamReader(filename))
					{
						Scenario scenario = (Scenario)XmlSerializer.Deserialize(reader);

						if (scenario is ISerializationCallback deserializationCallback)
						{
							deserializationCallback.OnAfterDeserialize();
						}

						RecursiveActionReplace(scenario);

						RecursiveSerializationCallBack(scenario, obj => obj.OnAfterDeserialize());

						return scenario;
					}
				}
				catch (Exception ex)
				{
					Log($"Failed to deserialize scenario from '{filename}': {ex.Message}", LogLevel.Error);
					return null;
				}
			}
			else
			{
				Log($"The scenario file '{filename}' does not exist.", LogLevel.Error);
				return null;
			}
		}

		/// <summary>
		/// Serialize a ScriptedEvent into a base64 string.
		/// </summary>
		public static string SerializeScriptedEvent(ScriptedEvent scriptedEvent)
		{
			// Serialize the struct to XML
			using (StringWriter stringWriter = new StringWriter())
			{
				// Serialize the object to XML
				ConditionalActionSerializer.Serialize(stringWriter, scriptedEvent);
				string xmlString = stringWriter.ToString();

				return CompressString(xmlString);
			}
		}

		/// <summary>
		/// Deserialize a base64 string into a ScriptedEvent.
		/// </summary>
		public static ScriptedEvent DeserializeScriptedEvent(string serializedConditionalAction, string ownerEntityContext = null)
		{
			if (string.IsNullOrEmpty(serializedConditionalAction))
			{
				return new ScriptedEvent();
			}

			try
			{
				string xmlString = DecompressString(serializedConditionalAction);

				if (string.IsNullOrEmpty(xmlString))
				{
					return new ScriptedEvent();
				}

				xmlString = RemoveObsoleteScriptedEventEntries(xmlString, ownerEntityContext);

				ScriptedEvent scriptedEvent;

				// Deserialize the XML string back into the object
				using (StringReader stringReader = new StringReader(xmlString))
				{
					scriptedEvent = (ScriptedEvent)ConditionalActionSerializer.Deserialize(stringReader);
				}
				RecursiveActionReplace(scriptedEvent);
				RecursiveSerializationCallBack(scriptedEvent, obj => obj.OnAfterDeserialize());
				return scriptedEvent;
			}
			catch (FormatException ex)
			{
				// Log and handle any Base64 decoding issues
				Log($"Base64 decoding failed: {ex.Message}", LogLevel.Error);
			}
			catch (Exception ex)
			{
				// Log and handle any XML deserialization issues
				Log($"XML deserialization failed: {ex.Message}", LogLevel.Error);
			}

			return new ScriptedEvent();
		}

		private static string RemoveObsoleteScriptedEventEntries(string xmlString, string ownerEntityContext)
		{
			try
			{
				XDocument document = XDocument.Parse(xmlString, LoadOptions.PreserveWhitespace);
				bool hasChanges = false;
				HashSet<string> knownConditionTypeNames = GetKnownDerivedTypeNames(typeof(Condition));
				HashSet<string> knownActionTypeNames = GetKnownDerivedTypeNames(typeof(ActionBase));

				hasChanges |= RemoveObsoleteEntries(document, "Conditions", "Condition", knownConditionTypeNames, "condition", ownerEntityContext);
				hasChanges |= RemoveObsoleteEntries(document, "Actions", "ActionBase", knownActionTypeNames, "action", ownerEntityContext);

				return hasChanges ? document.ToString(SaveOptions.DisableFormatting) : xmlString;
			}
			catch (Exception ex)
			{
				Log($"Could not pre-filter obsolete conditional action entries: {ex.Message}", LogLevel.Warning);
				return xmlString;
			}
		}

		private static bool RemoveObsoleteEntries(
			XDocument document,
			string containerName,
			string fallbackTypeName,
			HashSet<string> knownTypeNames,
			string kind,
			string ownerEntityContext)
		{
			bool hasChanges = false;
			XNamespace xsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

			foreach (XElement container in document.Descendants().Where(e => e.Name.LocalName == containerName))
			{
				foreach (XElement child in container.Elements().ToList())
				{
					string elementTypeName = child.Name.LocalName;
					if (elementTypeName == fallbackTypeName)
					{
						XAttribute xsiTypeAttribute = child.Attribute(xsiNamespace + "type");
						string xsiTypeName = GetTypeNameFromXsiType(xsiTypeAttribute?.Value);
						if (!string.IsNullOrEmpty(xsiTypeName))
						{
							elementTypeName = xsiTypeName;
						}
					}

					if (knownTypeNames.Contains(elementTypeName))
					{
						continue;
					}

					child.Remove();
					hasChanges = true;
					string entityContextSuffix = string.IsNullOrWhiteSpace(ownerEntityContext)
						? string.Empty
						: $" on entity '{ownerEntityContext}'";
					Log($"Skipped obsolete {kind} '{elementTypeName}' while deserializing ScriptedEvent{entityContextSuffix}.", LogLevel.Warning);
				}
			}

			return hasChanges;
		}

		private static string GetTypeNameFromXsiType(string xsiType)
		{
			if (string.IsNullOrEmpty(xsiType))
			{
				return null;
			}

			int separatorIndex = xsiType.IndexOf(':');
			return separatorIndex >= 0 ? xsiType.Substring(separatorIndex + 1) : xsiType;
		}

		private static HashSet<string> GetKnownDerivedTypeNames(Type baseType)
		{
			lock (_knownTypeNamesCacheLock)
			{
				if (_knownTypeNamesCache.TryGetValue(baseType, out HashSet<string> cachedTypeNames))
				{
					return cachedTypeNames;
				}

				HashSet<string> computedTypeNames = SerializeHelper.GetSerializableDerivedTypes(baseType)
					.SelectMany(GetXmlTypeNames)
					.ToHashSet();
				_knownTypeNamesCache[baseType] = computedTypeNames;
				return computedTypeNames;
			}
		}


		private static IEnumerable<string> GetXmlTypeNames(Type type)
		{
			yield return type.Name;

			XmlTypeAttribute xmlTypeAttribute = type.GetCustomAttribute<XmlTypeAttribute>();
			if (!string.IsNullOrWhiteSpace(xmlTypeAttribute?.TypeName))
			{
				yield return xmlTypeAttribute.TypeName;
			}
		}

		public static string CompressString(string text)
		{
			byte[] buffer = Encoding.UTF8.GetBytes(text);
			var memoryStream = new MemoryStream();
			using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
			{
				gZipStream.Write(buffer, 0, buffer.Length);
			}

			memoryStream.Position = 0;

			var compressedData = new byte[memoryStream.Length];
			memoryStream.Read(compressedData, 0, compressedData.Length);

			var gZipBuffer = new byte[compressedData.Length + 4];
			Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
			Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
			return Convert.ToBase64String(gZipBuffer);
		}

		public static string DecompressString(string compressedText)
		{
			byte[] gZipBuffer = Convert.FromBase64String(compressedText);
			using (var memoryStream = new MemoryStream())
			{
				int dataLength = BitConverter.ToInt32(gZipBuffer, 0);
				memoryStream.Write(gZipBuffer, 4, gZipBuffer.Length - 4);

				var buffer = new byte[dataLength];

				memoryStream.Position = 0;
				using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
				{
					gZipStream.Read(buffer, 0, buffer.Length);
				}

				return Encoding.UTF8.GetString(buffer);
			}
		}

		/// <summary>
		/// Recursively replaces all ActionBase objects with their optional override (Client or Server) from ActionOverrideRegistry.
		/// </summary>
		private static void RecursiveActionReplace(object obj, object parentObj = null, FieldInfo fi = null)
		{
			if (obj == null) return;


			// If the object is an ActionBase, replace it with the correct action type
			if (parentObj != null && fi != null && obj is ActionBase)
			{
				object correctAction = CreateCorrectActionInstance(obj);
				CopyActionState(obj, correctAction);

				// Replace the parent list with the new element
				if (typeof(IList).IsAssignableFrom(fi.FieldType))
				{
					IList list = fi.GetValue(parentObj) as IList;
					list[list.IndexOf(obj)] = correctAction;
					fi.SetValue(parentObj, list);
				}
				else
				{
					fi.SetValue(parentObj, correctAction);
				}

				// Set obj to the newly created action for further recursive inspection
				obj = correctAction;
			}

			// Recursively check fields of the object
			foreach (FieldInfo field in obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				// Skip fields marked with [XmlIgnore] (e.g. WeakGameEntity runtime caches on Zone/anchors)
				if (Attribute.IsDefined(field, typeof(XmlIgnoreAttribute))) continue;

				// Handle lists
				if (typeof(IList).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(string))
				{
					if (field.GetValue(obj) is IList collection)
					{
						for (int i = 0; i < collection.Count; i++)
						{
							RecursiveActionReplace(collection[i], obj, field);
						}
					}
				}
				// Ignore primitive types, strings, enums, and other simple types
				else if (field.FieldType.IsPrimitive || field.FieldType.IsEnum || field.FieldType == typeof(string) || field.FieldType == typeof(decimal) || field.FieldType == typeof(DateTime))
				{
					continue;
				}
				else
				{
					var fieldValue = field.GetValue(obj);
					RecursiveActionReplace(fieldValue, obj, field);
				}
			}
		}

		/// <summary>
		/// Creates the correct instance of an ActionBase object using optional overrides from the ActionOverrideRegistry
		/// </summary>
		private static object CreateCorrectActionInstance(object obj)
		{
			if (obj == null) return null;

			Type overrideType = ActionOverrideRegistry.GetOverrideType(obj.GetType());

			// No override found: run the Common implementation as-is.
			if (overrideType == null)
			{
				return obj;
			}

			try
			{
				return Activator.CreateInstance(overrideType);
			}
			catch (Exception ex)
			{
				Log($"Failed to create runtime override '{overrideType.Name}' for action '{obj.GetType().Name}': {ex.Message}", LogLevel.Error);
				return obj;
			}
		}

		/// <summary>
		/// Copies the state of an object to another object.
		/// </summary>
		private static void CopyActionState(object source, object target)
		{
			var sourceType = source.GetType();
			var targetType = target.GetType();

			// Copy fields
			foreach (var field in sourceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				var value = field.GetValue(source);
				var targetField = targetType.GetField(field.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (targetField != null && targetField.FieldType == field.FieldType)
				{
					targetField.SetValue(target, value);
				}
			}

			// Copy properties
			foreach (var property in sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				if (property.CanRead && property.CanWrite)
				{
					var value = property.GetValue(source);
					var targetProperty = targetType.GetProperty(property.Name, BindingFlags.Instance | BindingFlags.Public);
					if (targetProperty != null && targetProperty.PropertyType == property.PropertyType)
					{
						targetProperty.SetValue(target, value);
					}
				}
			}
		}

		/// <summary>
		/// Recursively calls the OnBeforeSerialize or OnAfterDeserialize method on all objects that implement ISerializationCallback.
		/// </summary>
		private static void RecursiveSerializationCallBack(object obj, Action<ISerializationCallback> callbackAction)
		{
			if (obj == null) return;

			// Check if the object itself implements ISerializationCallback
			if (obj is ISerializationCallback callback)
			{
				callbackAction(callback);
			}

			// Get the type of the object
			Type type = obj.GetType();

			// Skip primitive types, strings, enums, and other simple types
			if (type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime))
			{
				return;
			}

			// Check if the object is a collection (IEnumerable)
			if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
			{
				foreach (var item in (System.Collections.IEnumerable)obj)
				{
					RecursiveSerializationCallBack(item, callbackAction);
				}
				return;
			}

			// Check all fields and properties
			foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				// Skip fields marked with [XmlIgnore]
				if (Attribute.IsDefined(field, typeof(XmlIgnoreAttribute)))
					continue;

				var fieldValue = field.GetValue(obj);
				RecursiveSerializationCallBack(fieldValue, callbackAction);
			}

			foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
			{
				// Skip properties that are indexers or cannot be read
				if (!property.CanRead || property.GetIndexParameters().Length > 0)
				{
					continue;
				}
				// Skip properties marked with [XmlIgnore]
				if (Attribute.IsDefined(property, typeof(XmlIgnoreAttribute)))
					continue;

				var propertyValue = property.GetValue(obj);
				RecursiveSerializationCallBack(propertyValue, callbackAction);
			}
		}
	}
}
