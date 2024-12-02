using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static XmlDocument LoadAndMergeXML(List<string> xmlList, string nodesName)
        {
            XmlDocument xmlDoc = new();
            xmlDoc.Load(xmlList.First());
            xmlList.RemoveAt(0);
            foreach (string path in xmlList)
            {
                XmlDocument xmlDocX = new();
                xmlDocX.Load(path);
                XmlNodeList nodesToImport = xmlDocX.SelectNodes(nodesName);
                foreach (XmlNode node in nodesToImport)
                {
                    xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(node, true));
                }
            }
            return xmlDoc;
        }
    }
}
