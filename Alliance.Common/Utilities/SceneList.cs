using Alliance.Common.Extensions.SAE.Models;
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
        // These maps are causing unidentified crash and should not be used
        public static List<string> InvalidMaps = new List<string>() { "mp_skirmish_map_004", "mp_compact", "benchmark_battle_11", "mp_duel_mode_map_004_night" };

        public static List<SceneInfo> Scenes => _scenes;

        private static Dictionary<string, MultiplayerGameTypeInfo> _multiplayerGameTypeInfos;
        private static List<SceneInfo> _scenes;

        public struct SceneInfo
        {
            public string Module;
            public string Name;
            public bool HasGenericSpawn;
            public bool HasSpawnForDefender;
            public bool HasSpawnForAttacker;
            public bool HasSpawnVisual;
            public bool HasNavmesh;
            public bool HasSAEPos;
        }

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
            _scenes = new List<SceneInfo>();
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
                    SceneInfo sceneInfo = new SceneInfo();
                    sceneInfo.Module = modName;
                    sceneInfo.Name = sceneNode.Attributes["name"].Value;
                    sceneInfo.HasSpawnForAttacker = xmlDoc.SelectNodes("//game_entity[.//tag[@name='attacker'] and @name='mp_spawnpoint' or @prefab='mp_spawnpoint_attacker']").Count > 0;
                    sceneInfo.HasSpawnForDefender = xmlDoc.SelectNodes("//game_entity[.//tag[@name='defender'] and @name='mp_spawnpoint' or @prefab='mp_spawnpoint_defender']").Count > 0;
                    sceneInfo.HasGenericSpawn = sceneInfo.HasSpawnForAttacker || sceneInfo.HasSpawnForDefender || xmlDoc.SelectNodes("//game_entity[@name='mp_spawnpoint' or @prefab='mp_spawnpoint'] | //tag[@name='spawnpoint']").Count > 0;
                    sceneInfo.HasSpawnVisual = xmlDoc.SelectNodes("//game_entity[@name='spawn_visual'] | //game_entity[@prefab='spawn_visual']").Count > 0;
                    sceneInfo.HasNavmesh = File.Exists(Path.Combine(dir, "navmesh.bin"));
                    sceneInfo.HasSAEPos = xmlDoc.SelectNodes($"//game_entity[@prefab='{SaeCommonConstants.FDC_QUICK_PLACEMENT_POS_PREFAB_NAME}']").Count > 0;
                    _scenes.Add(sceneInfo);
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
    }
}
