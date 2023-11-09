using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TaleWorlds.ModuleManager;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AnimationPlayer.Models
{
    /// <summary>
    /// Store default animations durations (to avoid crash when initializing list of animations).
    /// </summary>
    public class AnimationDefaultStore
    {
        private static AnimationDefaultStore _instance;
        public static AnimationDefaultStore Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AnimationDefaultStore();
                }
                return _instance;
            }
        }

        public List<float> DefaultDurations { get; set; }

        private string _filePath;

        private AnimationDefaultStore()
        {
        }

        public void Init()
        {
            DefaultDurations = new List<float>();
            _filePath = Path.Combine(ModuleHelper.GetModuleFullPath("Alliance"), "Animations/DefaultAnimations.xml");
            if (File.Exists(_filePath))
            {
                Deserialize();
            }
            else
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        /// <summary>
        /// Serialize current instance and save it to module.
        /// </summary>
        public void Serialize()
        {
            try
            {
                using (FileStream fs = new FileStream(_filePath, FileMode.Create))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AnimationDefaultStore));
                    serializer.Serialize(fs, this);
                }
            }
            catch (Exception ex)
            {
                Log("Failed to save default animations.", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        /// <summary>
        /// Deserialize file into current instance.
        /// </summary>
        public void Deserialize()
        {
            try
            {
                using (FileStream fs = new FileStream(_filePath, FileMode.Open))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(AnimationDefaultStore));
                    AnimationDefaultStore deserializedStore = (AnimationDefaultStore)serializer.Deserialize(fs);
                    DefaultDurations = new List<float>(deserializedStore.DefaultDurations);
                }
            }
            catch (Exception ex)
            {
                Log("Failed to load default animations.", LogLevel.Error);
                Log(ex.Message, LogLevel.Error);
            }
        }
    }
}