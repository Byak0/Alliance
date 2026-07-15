using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using StringReader = System.IO.StringReader;
using StringWriter = System.IO.StringWriter;

namespace Alliance.Common.Utilities
{
    public static class SerializeHelper
    {
        /// <summary>
        /// Try to deserialize file at specified path into requested class.
        /// If file doesn't exist, create it with the defaultInstance.
        /// </summary>
        /// <param name="pathConfig">Path of the file to deserialize</param>
        /// <param name="defaultInstance">Default instance to create file</param>
        /// <returns>Deserialized instance or default one if file doesn't exist</returns>
        public static T LoadClassFromFile<T>(string pathConfig, T defaultInstance) where T : new()
        {
            // If file doesn't exist, create a default one
            if (!File.Exists(pathConfig))
            {
                TextWriter writer = null;
                try
                {
                    var serializer = new XmlSerializer(typeof(T));
                    writer = new StreamWriter(pathConfig, false);
                    serializer.Serialize(writer, defaultInstance);
                    return defaultInstance;
                }
                finally
                {
                    if (writer != null)
                        writer.Close();
                }
            }
            // Else load config from file
            else
            {
                TextReader reader = null;
                try
                {
                    using var fs = new FileStream(pathConfig, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var serializer = new XmlSerializer(typeof(T));
                    reader = new StreamReader(fs);
                    return (T)serializer.Deserialize(reader);
                }
                finally
                {
                    if (reader != null)
                        reader.Close();
                }
            }
        }

        /// <summary>
        /// Try to save instance to specified file path.
        /// </summary>
        public static bool SaveClassToFile<T>(string pathConfig, T instance) where T : new()
        {
            TextWriter writer = null;
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                writer = new StreamWriter(pathConfig, false);
                serializer.Serialize(writer, instance);
                return true;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
        }

        public static T DeserializeXml<T>(this string toDeserialize)
        {
            XmlSerializer xmlSerializer = new(typeof(T));
            using (StringReader textReader = new(toDeserialize))
            {
                return (T)xmlSerializer.Deserialize(textReader);
            }
        }

        public static string SerializeXml<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new(typeof(T));
            using (StringWriter textWriter = new Utf8StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }

        public class Utf8StringWriter : StringWriter
        {
            public override Encoding Encoding => Encoding.UTF8;
        }

        public static FileSystemWatcher CreateFileWatcher(string path, FileSystemEventHandler OnChanged)
        {
            string dir = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            // Create a new FileSystemWatcher and set its properties
            FileSystemWatcher watcher = new();
            watcher.Path = dir;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = fileName;

            // Add event handlers
            watcher.Changed += OnChanged;

            // Begin watching
            watcher.EnableRaisingEvents = true;

            return watcher;
        }

		/// <summary>
		/// Creates an XmlSerializer that can serialize a given root type and include all derived types of specified base types.
		/// </summary>
		/// <param name="rootType">The type of the root object to serialize.</param>
		/// <param name="baseTypes">The base types for which all derived types should be included.</param>
		/// <returns>A configured XmlSerializer.</returns>
		public static XmlSerializer CreateSerializer(Type rootType, params Type[] baseTypes)
		{
			Type[] derivedTypes = GetSerializableDerivedTypes(baseTypes)
				.Distinct()
				.ToArray();

			return new XmlSerializer(rootType, derivedTypes);
		}

		public static IEnumerable<Type> GetSerializableDerivedTypes(params Type[] baseTypes)
		{
			IEnumerable<Type> allTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a =>
				{
					try
					{
						return a.GetTypes();
					}
					catch (ReflectionTypeLoadException ex)
					{
						return ex.Types.Where(t => t != null);
					}
					catch
					{
						return Enumerable.Empty<Type>();
					}
				});

			return allTypes
				.Where(t => t != null && !t.IsAbstract && baseTypes.Any(t.IsSubclassOf));
		}

		public static T LoadAbstractClassFromFile<T>(string pathConfig, T defaultInstance) where T : new()
		{
			TextReader reader = null;

			try
			{
				
				var serializer =  CreateSerializer(rootType: typeof(T), typeof(T));
				using var fs = new FileStream(pathConfig, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				reader = new StreamReader(fs);
				return (T)serializer.Deserialize(reader);
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
			
		}

		/// <summary>
		/// Try to save instance to specified file path.
		/// </summary>
		public static bool SaveAbstractClassToFile<T>(string pathConfig, T instance) where T : new()
		{
			TextWriter writer = null;
			try
			{
				var serializer = CreateSerializer(rootType: typeof(T), typeof(T));
				writer = new StreamWriter(pathConfig, false);
				serializer.Serialize(writer, instance);
				return true;
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}

	}
}
