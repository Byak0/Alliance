using Alliance.Client.Extensions.BuildSystem.Views;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Audio.NetworkMessages.FromClient;
using Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromClient;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages;
using Alliance.Common.GameModes.Story.NetworkMessages.FromClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.PlayerServices;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Core.Console
{
	public static class ConsoleCommands
	{
		/// <summary>Check every physics shape reference: 
		/// Current scene, items, prefabs...
		/// Broken body/collision references are suspected to corrupt the native physics shape cache 
		/// and crash mission finalize after repeated same-map loads (rglPhysX_physics_engine_shape).
		/// Engine-generated runtime shapes (__scene_barrier__ etc.) are skipped.</summary>
		[CommandLineFunctionality.CommandLineArgumentFunction("check_shapes", "alliance")]
		public static string CheckShapes(List<string> args)
		{
			Dictionary<string, List<string>> missing = new Dictionary<string, List<string>>();

			int entityCount = 0;
			if (Mission.Current?.Scene != null)
			{
				List<GameEntity> entities = new List<GameEntity>();
				Mission.Current.Scene.GetEntities(ref entities);
				foreach (GameEntity entity in entities)
				{
					entityCount += AuditEntityShapes(entity, missing);
				}
			}

			foreach (ItemObject item in MBObjectManager.Instance.GetObjectTypeList<ItemObject>())
			{
				CheckShape(missing, item.CollisionBodyName, "item:" + item.StringId);
				CheckShape(missing, item.BodyName, "item:" + item.StringId);
				CheckShape(missing, item.HolsterBodyName, "item:" + item.StringId);
			}

			int xmlFiles = 0;
			try
			{
				foreach (ModuleInfo module in ModuleHelper.GetActiveModules() ?? new List<ModuleInfo>())
				{
					if (module.Id == "Native" || module.Id == "Sandbox" || module.Id == "SandBoxCore" || module.Id == "StoryMode") continue;
					foreach (string root in new[] { "Prefabs", "SceneObj" })
					{
						string dir = System.IO.Path.Combine(module.FolderPath, root);
						if (!System.IO.Directory.Exists(dir)) continue;
						foreach (string file in System.IO.Directory.GetFiles(dir, "*.xml", System.IO.SearchOption.AllDirectories))
						{
							xmlFiles++;
							string text = System.IO.File.ReadAllText(file);
							foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
								text, "(?:body_name|collision_body)\\s*=\\s*\"([^\"]+)\""))
							{
								CheckShape(missing, m.Groups[1].Value, "xml:" + module.Id + "/" + System.IO.Path.GetFileName(file));
							}
						}
					}
				}
			}
			catch (System.Exception ex)
			{
				Log($"Shape audit XML scan failed: {ex.Message}", LogLevel.Warning);
			}

			if (missing.Count == 0)
			{
				string result = $"Shape audit over: {entityCount} scene entities, all items, {xmlFiles} module XMLs checked.";
				Log(result, LogLevel.Information);
				return result;
			}	

			StringBuilder sb = new StringBuilder();
			sb.AppendLine($"Shape audit: {missing.Count} missing physics shape(s) [{entityCount} entities, {xmlFiles} XMLs checked]:");
			foreach (KeyValuePair<string, List<string>> kv in missing)
			{
				sb.AppendLine($"  '{kv.Key}' used by {kv.Value.Count} reference(s): {string.Join(", ", kv.Value.Take(4))}"
					+ (kv.Value.Count > 4 ? $" (+{kv.Value.Count - 4} more)" : ""));
			}
			Log($"Shape audit over: {entityCount} scene entities, all items, {xmlFiles} module XMLs checked.", LogLevel.Error);
			Log(sb.ToString(), LogLevel.Error);
			return sb.ToString();
		}

		private static int AuditEntityShapes(GameEntity entity, Dictionary<string, List<string>> missing)
		{
			int count = 1;
			string shapeName = entity.GetBodyShape()?.GetName();
			CheckShape(missing, shapeName, "scene:" + (string.IsNullOrEmpty(entity.Name) ? "?" : entity.Name));
			foreach (GameEntity child in entity.GetChildren())
			{
				count += AuditEntityShapes(child, missing);
			}
			return count;
		}

		private static void CheckShape(Dictionary<string, List<string>> missing, string shapeName, string where)
		{
			// "__...__" names are engine-generated runtime shapes (scene barrier etc.), not resources.
			if (string.IsNullOrEmpty(shapeName) || shapeName.StartsWith("__")) return;
			if (PhysicsShape.GetFromResource(shapeName, mayReturnNull: true) == null)
			{
				AddMissing(missing, shapeName, where);
			}
		}

		private static void AddMissing(Dictionary<string, List<string>> missing, string shapeName, string where)
		{
			if (!missing.TryGetValue(shapeName, out List<string> whereList)) missing[shapeName] = whereList = new List<string>();
			if (!whereList.Contains(where)) whereList.Add(where);
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("get_player_id", "alliance")]
		public static string GetPlayerId(List<string> playerName)
		{
			string name = playerName.ElementAtOrValue(0, "");
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			List<NetworkCommunicator> matchingPlayers = GameNetwork.NetworkPeers?.ToMBList().FindAll(x => x.UserName.Contains(name));
			string result = "";
			foreach (NetworkCommunicator player in matchingPlayers)
			{
				result += player.UserName + " : " + player.VirtualPlayer?.Id + "\n";
			}
			return result;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("play_sound_native", "alliance")]
		public static string PlaySoundNative(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			else if (args.ElementAtOrValue(0, "") == "")
			{
				return "Usage: alliance.play_sound_native sound_name sound_duration\n" +
					"Example: alliance.play_sound_native LOTR/OST/Flaming Red Hair.wav 30";
			}

			int soundDuration = 300;
			if (int.TryParse(args[args.Count - 1], out int duration))
			{
				soundDuration = duration;
				args.RemoveAt(args.Count - 1);
			}
			int soundIndex = SoundEvent.GetEventIdFromString(ConcatenateString(args));

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new NativeSoundRequest(soundIndex, soundDuration));
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to play " + soundIndex + " for " + soundDuration + " seconds.";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("play_sound", "alliance")]
		public static string PlaySound(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			else if (args.ElementAtOrValue(0, "") == "")
			{
				return "Usage: alliance.play_sound sound_name\n" +
					"Example: alliance.play_sound LOTR/Rohan/Voice/Theoden/This will be a day to remember.wav";
			}

			string soundName = string.Join(" ", args);
			soundName = soundName.Trim('"');
			int soundIndex = AudioPlayer.Instance.GetAudioId(soundName);

			if (soundIndex == -1)
			{
				return "Sound not found.";
			}

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AudioRequest(soundIndex));
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to play " + soundName;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("play_music", "alliance")]
		public static string PlayMusic(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			else if (args.ElementAtOrValue(0, "") == "")
			{
				return "Usage: alliance.play_music sound_name\n" +
					"Example: alliance.play_music LOTR/OST/Flaming Red Hair.wav";
			}

			string soundName = string.Join(" ", args);
			soundName = soundName.Trim('"');
			int soundIndex = AudioPlayer.Instance.GetAudioId(soundName);

			if (soundIndex == -1)
			{
				return "Sound not found.";
			}

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new MusicRequest(soundIndex));
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to play " + soundName;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("toggle_invulnerable", "alliance")]
		public static string ToggleInvulnerable(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			else if (args.Count() == 0)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new AdminClient() { ToggleInvulnerable = true, PlayerSelected = PlayerId.Empty });
				GameNetwork.EndModuleEventAsClient();
				return "Requested server to toggle invulnerable for all";
			}

			string name = ConcatenateString(args);
			NetworkCommunicator playerSelected = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.UserName.ToString() == name).FirstOrDefault();
			if (playerSelected?.VirtualPlayer == null) return "No valid player found with name " + name;

			PlayerId playerId = playerSelected.VirtualPlayer.Id;

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new AdminClient() { ToggleInvulnerable = true, PlayerSelected = playerId });
			GameNetwork.EndModuleEventAsClient();
			return "Requested server to toggle invulnerable for " + name;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("send_notification", "alliance")]
		public static string SendNotification(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}

			string text = ConcatenateString(args);

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestNotification(text, 0));
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to send notification : " + text;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("send_information", "alliance")]
		public static string SendInformation(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}

			string text = ConcatenateString(args);

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestNotification(text, 1));
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to send information : " + text;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("send_message", "alliance")]
		public static string SendMessage(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}

			string text = ConcatenateString(args);

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestNotification(text, 2));
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to send message : " + text;
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("spawn_horse", "alliance")]
		public static string SpawnHorse(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new SpawnHorseRequest());
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to spawn hrose";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("toggle_entities", "alliance")]
		public static string ToggleEntities(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0 || Mission.Current?.Scene == null)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			if (args.Count < 2 || !bool.TryParse(args[1], out bool show))
			{
				return "Usage: alliance.toggle_entities entities_tag true/false";
			}
			string entities_tag = args[0];

			ToggleEntitiesMsg.RequestToggleEntities(entities_tag, show);

			return $"Requested server to {(show ? "show" : "hide")} entities with tag {entities_tag}";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("build_set_prefab_list", "alliance")]
		public static string BuildSetPrefabList(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0 || Mission.Current?.Scene == null)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			if (args.Count < 2)
			{
				return "Usage: alliance.build_set_prefab_list prefabName1 prefabName2...";
			}

			BuildSystemView buildSystemView = Mission.Current.GetMissionBehavior<BuildSystemView>();
			buildSystemView.SetAvailablePrefabs(args);

			return $"Updated available prefabs in Build system";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("scenario_stop", "alliance")]
		public static string StopScenario(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0 || Mission.Current?.Scene == null)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestStopScenario());
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to stop current scenario";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("scenario_set_winner", "alliance")]
		public static string SetWinner(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0 || Mission.Current?.Scene == null)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}
			string[] availableSides = new string[] { "None", "Attacker", "Defender" };
			if (args.Count < 1 || !availableSides.Contains(args[0]))
			{
				return "Usage: alliance.scenario_set_winner None/Attacker/Defender";
			}

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestSetWinner(args[0]));
			GameNetwork.EndModuleEventAsClient();

			return $"Requested server to set winner of current scenario to {args[0]}";
		}

		[CommandLineFunctionality.CommandLineArgumentFunction("start_fake_army", "alliance")]
		public static string SpawnFakeArmy(List<string> args)
		{
			if (GameNetwork.NetworkPeerCount == 0 || Mission.Current?.Scene == null)
			{
				return "Log into a server to use this command.";
			}
			else if (!GameNetwork.MyPeer.IsAdmin())
			{
				return "You need to be admin to use this command.";
			}

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new StartFakeArmyMessage());
			GameNetwork.EndModuleEventAsClient();

			return "Requested server to set start fake army";
		}

		public static string ConcatenateString(List<string> strings)
		{
			if (strings == null || strings.IsEmpty())
			{
				return string.Empty;
			}
			string text = strings[0];
			if (strings.Count > 1)
			{
				for (int i = 1; i < strings.Count; i++)
				{
					text = text + " " + strings[i];
				}
			}
			return text;
		}
	}
}
