using System.Collections.Generic;
using System.IO;
using System.Xml;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Utilities
{
    /// <summary>
    /// Store all available scenes depending on module loaded.
    /// </summary>
    public static class SceneList
    {
        public static void Initialize()
        {
            CreateGameTypeInformations();
            LoadMultiplayerSceneInformations();
            LoadAllScenes();
        }

        public static bool CheckGameTypeInfoExists(string gameType)
        {
            return _multiplayerGameTypeInfos.ContainsKey(gameType);
        }

        public static MultiplayerGameTypeInfo GetGameTypeInfo(string gameType)
        {
            if (_multiplayerGameTypeInfos.ContainsKey(gameType))
            {
                return _multiplayerGameTypeInfos[gameType];
            }
            Log("Cannot find game type:" + gameType, LogLevel.Error);
            return null;
        }

        private static void LoadAllScenes()
        {
            _scenes = new List<string>();
            foreach (string modName in TaleWorlds.Engine.Utilities.GetModulesNames())
            {
                string scenesPath = ModuleHelper.GetModuleFullPath(modName) + "SceneObj/";

                if (!Directory.Exists(scenesPath)) continue;

                foreach (string dir in Directory.GetDirectories(scenesPath))
                {
                    string scenePath = dir + "/scene.xscene";

                    if (!File.Exists(scenePath)) continue;

                    XmlDocument xmlDoc = new();
                    xmlDoc.Load(scenePath);
                    XmlNode sceneNode = xmlDoc.SelectSingleNode("/scene");
                    _scenes.Add(sceneNode.Attributes["name"].Value);
                }
            }
        }

        private static void LoadMultiplayerSceneInformations()
        {
            XmlDocument xmlDocument = new();
            xmlDocument.Load(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/Multiplayer/MultiplayerScenes.xml");
            foreach (object obj in xmlDocument.FirstChild)
            {
                XmlNode xmlNode = (XmlNode)obj;
                if (xmlNode.NodeType != XmlNodeType.Comment)
                {
                    string innerText = xmlNode.Attributes["name"].InnerText;
                    foreach (object obj2 in xmlNode.ChildNodes)
                    {
                        XmlNode xmlNode2 = (XmlNode)obj2;
                        if (xmlNode2.NodeType != XmlNodeType.Comment)
                        {
                            string innerText2 = xmlNode2.Attributes["name"].InnerText;
                            if (_multiplayerGameTypeInfos.ContainsKey(innerText2))
                            {
                                _multiplayerGameTypeInfos[innerText2].Scenes.Add(innerText);
                            }
                        }
                    }
                }
            }
        }

        private static void CreateGameTypeInformations()
        {
            _multiplayerGameTypeInfos = new Dictionary<string, MultiplayerGameTypeInfo>();
            foreach (MultiplayerGameTypeInfo multiplayerGameTypeInfo in Module.CurrentModule.GetMultiplayerGameTypes())
            {
                _multiplayerGameTypeInfos.Add(multiplayerGameTypeInfo.GameType, multiplayerGameTypeInfo);
            }
        }

        private static Dictionary<string, MultiplayerGameTypeInfo> _multiplayerGameTypeInfos;
        private static List<string> _scenes;

        public static List<string> Scenes => _scenes;
    }
}
