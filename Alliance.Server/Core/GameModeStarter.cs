using Alliance.Common.Core.Configuration;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Lobby;
using NetworkMessages.FromServer;
using System.Threading;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.GameModes.Battle;
using Alliance.Common.GameModes.BattleRoyale;
using Alliance.Common.GameModes.Captain;
using Alliance.Common.GameModes.CvC;
using Alliance.Common.GameModes.Duel;
using Alliance.Common.GameModes.PvC;
using Alliance.Common.GameModes.Siege;
using Alliance.Common.GameModes.Story;
using Alliance.Server.Extensions.NativeIntermissionVote;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Server.Core
{
	/// <summary>
	/// Start custom game modes. Inspired by mentalrob's ChatCommands.
	/// </summary>
	public class GameModeStarter
	{
		private static readonly GameModeStarter instance = new GameModeStarter();
		public static GameModeStarter Instance { get { return instance; } }

		public bool MissionIsRunning
		{
			get
			{
				return Mission.Current != null;
			}
		}
		public bool EndingCurrentMissionThenStartingNewMission;

		public void SyncMultiplayerOptionsToClients()
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new MultiplayerOptionsInitial());
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new MultiplayerOptionsImmediate());
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);
		}

		public void StartMission(GameModeSettings gameModeSettings)
		{
			if (!EndingCurrentMissionThenStartingNewMission)
			{
				if (!MissionIsRunning)
				{
					StartMissionOnly(gameModeSettings);
					return;
				}
				EndMissionThenStartMission(gameModeSettings);
			}
		}

		private void EndMissionThenStartMission(GameModeSettings gameModeSettings)
		{
			MissionListener missionListener = new MissionListener();
			missionListener.SetGameModeSettings(gameModeSettings);
			EndMissionWithListener(missionListener);
		}

		internal void EndMissionWithListener(IMissionListener listener)
		{
			PrepareCurrentMissionForEnd();
			DisableNativeIntermissionVotes();

			Mission.Current.AddListener(listener);
			EndingCurrentMissionThenStartingNewMission = true;
			DedicatedCustomServerSubModule.Instance.ServerSideIntermissionManager.EndMission();
		}

		private void PrepareCurrentMissionForEnd()
		{
			if (Mission.Current == null)
			{
				return;
			}

			// Try to stop everyone from using objects to prevent crash
			Log("Scene=" + Mission.Current.SceneName, LogLevel.Debug);
			Log("NB Agents=" + Mission.Current.Agents.Count, LogLevel.Debug);
			foreach (MissionObject missionObj in Mission.Current.MissionObjects)
			{
				if (missionObj is UsableMachine machine)
				{
					Log($"Disabling {machine.GameEntity.Name} - {machine.IsDisabled}", LogLevel.Debug);
					machine.Disable();
				}
			}

			foreach (Agent agent in Mission.Current.AllAgents)
			{
				agent.SetMortalityState(Agent.MortalityState.Invulnerable);
			}
		}

		private static void DisableNativeIntermissionVotes()
		{
			MultiplayerIntermissionVotingManager votingManager = MultiplayerIntermissionVotingManager.Instance;
			votingManager.IsCultureVoteEnabled = false;
			votingManager.IsMapVoteEnabled = false;
		}

		public void ApplyGameModeSettings(GameModeSettings gameModeSettings)
		{
			PlayerSpawnMenu.Instance.Clear();
			ConfigManager.Instance.ApplyNativeOptions(gameModeSettings.TWOptions);
			ConfigManager.Instance.ApplyModOptions(gameModeSettings.ModOptions);
			SyncMultiplayerOptionsToClients();
		}

		public bool StartMissionOnly(GameModeSettings gameModeSettings)
		{
			if (gameModeSettings == null)
			{
				Log("StartMissionOnly called with null settings.", LogLevel.Error);
				return false;
			}

			if (!MissionIsRunning)
			{
				ApplyGameModeSettings(gameModeSettings);
				DedicatedCustomServerSubModule.Instance.ServerSideIntermissionManager.StartMission();
				return true;
			}
			return false;
		}

		public void StartLobby(string map, string culture1, string culture2, int nbBots = -1)
		{
			LobbyGameModeSettings lobby = new LobbyGameModeSettings();
			lobby.TWOptions[OptionType.Map] = map;
			lobby.TWOptions[OptionType.CultureTeam1] = culture1;
			lobby.TWOptions[OptionType.CultureTeam2] = culture2;
			if (nbBots > -1) lobby.TWOptions[OptionType.NumberOfBotsTeam1] = nbBots;
			StartMission(lobby);
		}

		/// <summary>
		/// Starts the configured post-match transition.
		/// Defaults to Lobby, or runs the configured intermission flow when enabled.
		/// </summary>
		public void StartPostMatchTransition()
		{
			string map = OptionType.Map.GetStrValue();
			string culture1 = OptionType.CultureTeam1.GetStrValue();
			string culture2 = OptionType.CultureTeam2.GetStrValue();

			if (EndingCurrentMissionThenStartingNewMission)
			{
				return;
			}

			if (Config.Instance.LoopCurrentModeWithNativeVote)
			{
				try
				{
					GameModeSettings currentGameModeSettings = CreateSettingsFromCurrentOptions();
					if (NativeIntermissionVoteService.TryStart(this, currentGameModeSettings))
					{
						return;
					}

					Log("Native intermission vote could not be started. Falling back to Lobby.", LogLevel.Warning);
				}
				catch
				{
					Log("Failed to start native intermission vote. Falling back to Lobby.", LogLevel.Warning);
				}
			}

			StartLobby(map, culture1, culture2);
		}


		public GameModeSettings CreateSettingsFromCurrentOptions()
		{
			string gameType = OptionType.GameType.GetStrValue();
			GameModeSettings gameModeSettings;

			switch (gameType)
			{
				case "CaptainX": gameModeSettings = new CaptainGameModeSettings(); break;
				case "BattleX": gameModeSettings = new BattleGameModeSettings(); break;
				case "SiegeX": gameModeSettings = new SiegeGameModeSettings(); break;
				case "DuelX": gameModeSettings = new DuelGameModeSettings(); break;
				case "Scenario": gameModeSettings = new ScenarioGameModeSettings(); break;
				case "PvC": gameModeSettings = new PvCGameModeSettings(); break;
				case "CvC": gameModeSettings = new CvCGameModeSettings(); break;
				case "BattleRoyale": gameModeSettings = new BRGameModeSettings(); break;
				case "Lobby": gameModeSettings = new LobbyGameModeSettings(); break;
				default:
					Log($"Unsupported game type '{gameType}' for loop transition. Falling back to Lobby.", LogLevel.Warning);
					gameModeSettings = new LobbyGameModeSettings();
					break;
			}

			gameModeSettings.TWOptions = ConfigManager.Instance.GetNativeOptionsCopy();
			gameModeSettings.ModOptions = ConfigManager.Instance.GetModOptionsCopy();
			return gameModeSettings;
		}
	}

	internal class MissionListener : IMissionListener
	{
		public void SetGameModeSettings(GameModeSettings gameModeSettings)
		{
			_gameModeSettings = gameModeSettings;
		}

		public void OnEndMission()
		{
			new Thread(StartMissionThread.ThreadProc).Start(new StartMissionThread.StartMissionRequest(_gameModeSettings, _useCurrentOptionsForNextMission));
			Mission.Current.RemoveListener(this);
		}

		public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart)
		{
		}

		public void OnResetMission()
		{
		}

		public void OnEquipItemsFromSpawnEquipmentBegin(Agent agent, Agent.CreationType creationType)
		{
		}

		public void OnEquipItemsFromSpawnEquipment(Agent agent, Agent.CreationType creationType)
		{
		}

		public void OnConversationCharacterChanged()
		{
		}

		public void OnDeploymentPlanMade(Team team, bool isFirstPlan)
		{
		}

		private GameModeSettings _gameModeSettings;
		private bool _useCurrentOptionsForNextMission;
	}

	internal class StartMissionThread
	{
		public class StartMissionRequest
		{
			public readonly GameModeSettings GameModeSettings;
			public readonly bool UseCurrentOptionsForNextMission;

			public StartMissionRequest(GameModeSettings gameModeSettings, bool useCurrentOptionsForNextMission)
			{
				GameModeSettings = gameModeSettings;
				UseCurrentOptionsForNextMission = useCurrentOptionsForNextMission;
			}
		}

		public static void ThreadProc(object requestObj)
		{
			StartMissionRequest request = requestObj as StartMissionRequest;
			Thread.Sleep(1000);
			GameModeStarter.Instance.EndingCurrentMissionThenStartingNewMission = false;

			if (request?.UseCurrentOptionsForNextMission == true)
			{
				GameModeSettings nextSettings = GameModeStarter.Instance.CreateSettingsFromCurrentOptions();
				GameModeStarter.Instance.StartMissionOnly(nextSettings);
				return;
			}

			GameModeStarter.Instance.StartMissionOnly(request?.GameModeSettings);
		}

		public StartMissionThread()
		{
		}
	}
}
