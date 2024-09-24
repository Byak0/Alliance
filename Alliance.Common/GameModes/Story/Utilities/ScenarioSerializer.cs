using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
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

		private static XmlSerializer XmlSerializer
		{
			get
			{
				_xmlSerializer ??= CreateSerializer(
					rootType: typeof(Scenario),
					typeof(ObjectiveBase), typeof(ActionBase), typeof(Condition), typeof(GameModeSettings));
				return _xmlSerializer;
			}
		}

		private static XmlSerializer ConditionalActionSerializer
		{
			get
			{
				_conditionalActionSerializer ??= CreateSerializer(
					rootType: typeof(ConditionalActionStruct),
					typeof(Condition), typeof(ActionBase));
				return _conditionalActionSerializer;
			}
		}

		/// <summary>
		/// Creates an XmlSerializer that can serialize a given root type and include all derived types of specified base types.
		/// </summary>
		/// <param name="rootType">The type of the root object to serialize.</param>
		/// <param name="baseTypes">The base types for which all derived types should be included.</param>
		/// <returns>A configured XmlSerializer.</returns>
		private static XmlSerializer CreateSerializer(Type rootType, params Type[] baseTypes)
		{
			var derivedTypes = new List<Type>();
			foreach (var baseType in baseTypes)
			{
				derivedTypes.AddRange(Assembly.GetAssembly(baseType)
					.GetTypes()
					.Where(t => t.IsSubclassOf(baseType) && !t.IsAbstract));
			}

			return new XmlSerializer(rootType, derivedTypes.Distinct().ToArray());
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
			else
			{
				Log($"The scenario file '{filename}' does not exist.", LogLevel.Error);
				return null;
			}
		}

		/// <summary>
		/// Serialize a ConditionalActionStruct into a base64 string.
		/// </summary>
		public static string SerializeConditionalActionStruct(ConditionalActionStruct conditionalActionStruct)
		{
			// Serialize the struct to XML
			using (StringWriter stringWriter = new StringWriter())
			{
				// Serialize the object to XML
				ConditionalActionSerializer.Serialize(stringWriter, conditionalActionStruct);
				string xmlString = stringWriter.ToString();

				return CompressString(xmlString);
			}
		}

		/// <summary>
		/// Deserialize a base64 string into a ConditionalActionStruct.
		/// </summary>
		public static ConditionalActionStruct DeserializeConditionalActionStruct(string serializedConditionalAction)
		{
			try
			{
				string xmlString = DecompressString(serializedConditionalAction);

				if (string.IsNullOrEmpty(xmlString))
				{
					return new ConditionalActionStruct();
				}

				ConditionalActionStruct conditionalActionStruct;

				// Deserialize the XML string back into the object
				using (StringReader stringReader = new StringReader(xmlString))
				{
					conditionalActionStruct = (ConditionalActionStruct)ConditionalActionSerializer.Deserialize(stringReader);
				}
				RecursiveActionReplace(conditionalActionStruct);
				RecursiveSerializationCallBack(conditionalActionStruct, obj => obj.OnAfterDeserialize());
				return conditionalActionStruct;
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

			return new ConditionalActionStruct();
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
		/// Recursively replaces all ActionBase objects with their correct version from Server or Client ActionFactory.
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
		/// Creates the correct instance of an ActionBase object by invoking the corresponding method in the ActionFactory.
		/// </summary>
		private static object CreateCorrectActionInstance(object obj)
		{
			if (obj == null) return null;

			// Get the corresponding method in the ActionFactory (e.g., StartGameAction)
			MethodInfo actionMethod = ActionFactory.Instance.GetType().GetMethod(obj.GetType().Name);

			if (actionMethod == null)
			{
				Log($"Action method '{obj.GetType().Name}' not found in ActionFactory.", LogLevel.Error);
				return obj;
			}

			// Invoke the method on the ActionFactory to get the correct action instance
			object newAction = actionMethod.Invoke(ActionFactory.Instance, null);
			return newAction;
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
				var propertyValue = property.GetValue(obj);
				RecursiveSerializationCallBack(propertyValue, callbackAction);
			}
		}
	}
}
