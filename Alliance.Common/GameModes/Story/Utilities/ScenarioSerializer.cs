using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;

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

		public static void SerializeScenarioToXML(Scenario scenarioToSerialize)
		{
			// Construct the full file path
			string directoryPath = Path.Combine(ModuleHelper.GetModuleFullPath("Alliance"), "Scenarios");
			string filename = Path.Combine(directoryPath, $"{scenarioToSerialize.Id}.xml");

			// Ensure the directory exists
			if (!Directory.Exists(directoryPath))
			{
				Directory.CreateDirectory(directoryPath);
			}

			SerializeScenarioToXML(scenarioToSerialize, filename);
		}

		public static void SerializeScenarioToXML(Scenario scenarioToSerialize, string filePath)
		{
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

			// Get all XML files in the specified directory
			string[] files = Directory.GetFiles(directoryPath, "*.xml");

			// Deserialize each scenario
			foreach (string file in files)
			{
				scenarios.Add(DeserializeScenarioFromPath(file));
			}

			return scenarios;
		}

		public static Scenario DeserializeScenarioFromId(string scenarioId)
		{
			string filename = Path.Combine(ModuleHelper.GetModuleFullPath("Alliance"), "Scenarios", $"{scenarioId}.xml");
			return DeserializeScenarioFromPath(filename);
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

					RecursiveSerializationCallBack(scenario, obj => obj.OnAfterDeserialize());

					return scenario;
				}
			}
			else
			{
				// Handle the case where the file does not exist
				throw new FileNotFoundException($"The scenario file '{filename}' does not exist.");
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
