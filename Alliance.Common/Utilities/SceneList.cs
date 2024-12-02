using Alliance.Common.Extensions.SAE.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
		public static List<string> InvalidMaps = new List<string>() { "mp_skirmish_map_004", "mp_compact", "benchmark_battle_11", "mp_duel_mode_map_004_night", "mp_duel_mode_map_004_w" };

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
			_ = LoadAllScenesAsync();
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

		/// <summary>
		/// Scan all scenes from loaded modules and analyze them for GameMode compatibility.
		/// Can be very long (around a minute) so it's done in a separate thread.
		/// </summary>
		private static async Task LoadAllScenesAsync()
		{
			await Task.Run(() =>
			{
				Stopwatch stopwatch = Stopwatch.StartNew();

				// Concurrent list to store scenes safely when using parallel processing
				var scenes = new ConcurrentBag<SceneInfo>();
				// Exclude unnecessary modules
				var modNames = TaleWorlds.Engine.Utilities.GetModulesNames().Except(new string[] { "Sandbox", "SandBoxCore", "StoryMode" });

				Parallel.ForEach(modNames, modName =>
				{
					string scenesPath = ModuleHelper.GetModuleFullPath(modName) + "SceneObj/";

					if (!Directory.Exists(scenesPath)) return;

					foreach (string dir in Directory.GetDirectories(scenesPath))
					{
						string scenePath = dir + "/scene.xscene";

						if (!File.Exists(scenePath)) continue;

						XmlDocument xmlDoc = new XmlDocument();
						xmlDoc.Load(scenePath);
						XmlNode sceneNode = xmlDoc.SelectSingleNode("/scene");

						SceneInfo sceneInfo = new SceneInfo
						{
							Module = modName,
							Name = sceneNode.Attributes["name"].Value,
							HasSpawnForAttacker = xmlDoc.SelectNodes("//game_entity[.//tag[@name='attacker'] and @name='mp_spawnpoint' or .//tag[@name='attacker'] and .//tag[@name='spawnpoint'] or @prefab='mp_spawnpoint_attacker']").Count > 0,
							HasSpawnForDefender = xmlDoc.SelectNodes("//game_entity[.//tag[@name='defender'] and @name='mp_spawnpoint' or .//tag[@name='defender'] and .//tag[@name='spawnpoint'] or @prefab='mp_spawnpoint_defender' or @prefab='skirmish_start_spawn']").Count > 0,
							HasGenericSpawn = xmlDoc.SelectNodes("//game_entity[@name='mp_spawnpoint' or @name='mp_spawnpoint_attacker' or @name='mp_spawnpoint_defender' or @prefab='mp_spawnpoint' or @prefab='mp_spawnpoint_attacker' or @prefab='mp_spawnpoint_defender'] | //tag[@name='spawnpoint']").Count > 0,
							HasSpawnVisual = xmlDoc.SelectNodes("//game_entity[@name='spawn_visual'] | //game_entity[@prefab='spawn_visual']").Count > 0,
							HasNavmesh = File.Exists(Path.Combine(dir, "navmesh.bin")),
							HasSAEPos = xmlDoc.SelectNodes($"//game_entity[@prefab='{SaeCommonConstants.FDC_QUICK_PLACEMENT_POS_PREFAB_NAME}']").Count > 0
						};

						scenes.Add(sceneInfo);
					}
				});

				_scenes = scenes.ToList();

				stopwatch.Stop();
				Log($"LoadAllScenes executed in: {stopwatch.ElapsedMilliseconds} ms", LogLevel.Debug);
			});
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
