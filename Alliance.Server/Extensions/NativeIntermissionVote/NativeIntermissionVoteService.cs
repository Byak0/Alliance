using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Server.Core;
using Alliance.Server.Extensions.NativeIntermissionVote.Behaviors;
using Alliance.Server.Extensions.NativeIntermissionVote.NetworkMessages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Server.Extensions.NativeIntermissionVote
{
	/// <summary>
	/// Handles the custom timeline for looping the current game mode through TaleWorlds native intermission votes.
	/// </summary>
	internal static class NativeIntermissionVoteService
	{
		private const float MapVoteDurationSeconds = 15f;
		private const float CultureVoteDurationSeconds = 15f;
		private const float IntermissionUpdateTickSeconds = 0.5f;
		private const int WaitForLobbyStateMilliseconds = 2000;
		private const int WaitBeforeMissionStartMilliseconds = 1000;
		private const string ScenarioGameType = "Scenario";

		private static readonly Dictionary<string, ScenarioVoteCandidate> ScenarioVoteCandidatesByVoteId = new Dictionary<string, ScenarioVoteCandidate>();
		private static bool _currentVoteIsScenario;

		/// <summary>
		/// Tries to start the native intermission vote flow.
		/// Returns false when the flow cannot be prepared and caller should use its fallback transition.
		/// </summary>
		internal static bool TryStart(GameModeStarter gameModeStarter, GameModeSettings currentGameModeSettings)
		{
			MissionLobbyComponent missionLobby = Mission.Current?.GetMissionBehavior<MissionLobbyComponent>();
			if (missionLobby == null)
			{
				Log("Native intermission loop requested but MissionLobbyComponent is missing.", LogLevel.Warning);
				return false;
			}

			if (missionLobby.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.Ending)
			{
				return true;
			}

			_currentVoteIsScenario = IsScenario(currentGameModeSettings);

			if (!PrepareVote(currentGameModeSettings, _currentVoteIsScenario))
			{
				return false;
			}

			gameModeStarter.EndMissionWithListener(new NativeIntermissionVoteMissionListener());
			Log("Ending mission — native vote will run after mission closes, when clients are in lobby state.");
			return true;
		}

		/// <summary>
		/// Prepares the native voting manager before mission end, without broadcasting vote items yet.
		/// Clients receive items only after returning to LobbyGameStateCustomGameClient.
		/// </summary>
		private static bool PrepareVote(GameModeSettings currentGameModeSettings, bool isScenarioVote)
		{
			if (currentGameModeSettings == null)
			{
				Log("Native intermission vote preparation failed: current game mode settings are null.", LogLevel.Error);
				return false;
			}

			MultiplayerIntermissionVotingManager votingManager = MultiplayerIntermissionVotingManager.Instance;
			if (votingManager == null)
			{
				Log("Native intermission vote preparation failed: MultiplayerIntermissionVotingManager is missing.", LogLevel.Error);
				return false;
			}

			votingManager.InitialGameType = OptionType.GameType.GetStrValue();
			votingManager.IsAutomatedBattleSwitchingEnabled = true;
			votingManager.IsMapVoteEnabled = true;
			votingManager.IsCultureVoteEnabled = !isScenarioVote;
			votingManager.IsDisableMapVoteOverride = false;
			votingManager.IsDisableCultureVoteOverride = false;
			votingManager.IsMapSelectedByAdmin = false;

			return PrepareVoteItems(votingManager, currentGameModeSettings, isScenarioVote);
		}


		/// <summary>
		/// Runs after the mission has closed. At this point clients are in lobby state and can display the native vote UI.
		/// </summary>
		public static void RunVoteAndRestartMission()
		{
			// Give clients time to finish mission unload and enter lobby state.
			Thread.Sleep(WaitForLobbyStateMilliseconds);

			MultiplayerIntermissionVotingManager votingManager = MultiplayerIntermissionVotingManager.Instance;
			bool isScenarioVote = _currentVoteIsScenario;

			// Re-enable voting and sync native manager values.
			votingManager.IsMapVoteEnabled = true;
			votingManager.IsCultureVoteEnabled = !isScenarioVote;
			NativeIntermissionVoteMsg.SyncVotingManagerValuesToAllPeers();

			NativeIntermissionVoteMsg.SendVoteItemsToAllPeers(votingManager);

			votingManager.CurrentVoteState = MultiplayerIntermissionState.CountingForMapVote;
			Log($"Native vote timeline - state -> CountingForMapVote for {MapVoteDurationSeconds}s");
			RunIntermissionCountdown(MultiplayerIntermissionState.CountingForMapVote, MapVoteDurationSeconds);

			if (!isScenarioVote)
			{
				votingManager.CurrentVoteState = MultiplayerIntermissionState.CountingForCultureVote;
				Log($"Native vote timeline - state -> CountingForCultureVote for {CultureVoteDurationSeconds}s");
				RunIntermissionCountdown(MultiplayerIntermissionState.CountingForCultureVote, CultureVoteDurationSeconds);
			}

			Log($"Native vote timeline - sorting votes. Maps={votingManager.MapVoteItems.Count} Cultures={votingManager.CultureVoteItems.Count}", LogLevel.Debug);
			if (isScenarioVote)
			{
				StartVotedScenario(votingManager);
				return;
			}

			votingManager.SortVotesAndPickBest();
			ApplyDynamicCultureVoteResult(votingManager);
			votingManager.CurrentVoteState = MultiplayerIntermissionState.CountingForMission;

			string selectedMap = OptionType.Map.GetStrValue();
			string selectedCulture1 = OptionType.CultureTeam1.GetStrValue();
			string selectedCulture2 = OptionType.CultureTeam2.GetStrValue();
			Log($"Native vote timeline - state -> CountingForMission | selectedMap={selectedMap} | culture1={selectedCulture1} | culture2={selectedCulture2}");
			NativeIntermissionVoteMsg.SendIntermissionUpdateToAllPeers(MultiplayerIntermissionState.CountingForMission, 0f);

			GameModeSettings nextSettings = GameModeStarter.Instance.CreateSettingsFromCurrentOptions();
			nextSettings.TWOptions[OptionType.Map] = selectedMap;
			nextSettings.TWOptions[OptionType.CultureTeam1] = selectedCulture1;
			nextSettings.TWOptions[OptionType.CultureTeam2] = selectedCulture2;

			Thread.Sleep(WaitBeforeMissionStartMilliseconds);
			GameModeStarter.Instance.EndingCurrentMissionThenStartingNewMission = false;
			GameModeStarter.Instance.StartMissionOnly(nextSettings);
		}

		private static bool PrepareVoteItems(MultiplayerIntermissionVotingManager votingManager, GameModeSettings currentGameModeSettings, bool isScenarioVote)
		{
			Log($"Vote setup - gameType={OptionType.GameType.GetStrValue()} | currentState={votingManager.CurrentVoteState} | mapsEnabled={votingManager.IsMapVoteEnabled} | culturesEnabled={votingManager.IsCultureVoteEnabled}", LogLevel.Debug);
			Log($"Vote setup - current map={OptionType.Map.GetStrValue()} | cultures={OptionType.CultureTeam1.GetStrValue()} vs {OptionType.CultureTeam2.GetStrValue()}", LogLevel.Debug);

			votingManager.ClearVotes();
			votingManager.ClearItems();
			ScenarioVoteCandidatesByVoteId.Clear();

			if (isScenarioVote)
			{
				return PrepareScenarioVoteItems(votingManager);
			}

			List<string> cultureVotePool = GetDynamicCultureVotePool();

			List<string> availableMaps = currentGameModeSettings.GetAvailableMaps().Select(scene => scene.Name).Distinct().Take(MultiplayerIntermissionVotingManager.MaxAllowedMapCount).ToList();
			Log($"Vote setup - preparing {availableMaps.Count} map candidates.", LogLevel.Debug);
			for (int i = 0; i < availableMaps.Count; i++)
			{
				votingManager.MapVoteItems.Add(new IntermissionVoteItem(availableMaps[i], i));
				Log($"Vote setup - map[{i}]={availableMaps[i]}", LogLevel.Debug);
			}

			Log($"Vote setup - preparing {cultureVotePool.Count} culture candidates from Factions.", LogLevel.Debug);
			for (int i = 0; i < cultureVotePool.Count; i++)
			{
				votingManager.CultureVoteItems.Add(new IntermissionVoteItem(cultureVotePool[i], i));
				Log($"Vote setup - culture[{i}]={cultureVotePool[i]}", LogLevel.Debug);
			}

			return availableMaps.Count > 0 && cultureVotePool.Count > 0;
		}

		private static List<string> GetDynamicCultureVotePool()
		{
			Factions factions;
			try
			{
				factions = Factions.Instance;
				factions.RefreshAvailablecultures();
			}
			catch (Exception exception)
			{
				Log($"Vote setup - failed to refresh available cultures through Factions: {exception.Message}", LogLevel.Error);
				return GetCurrentCultureVotePoolFallback();
			}

			IEnumerable<string> cultureIds = null;
			if (factions.OrderedCultureKeys != null && factions.OrderedCultureKeys.Count > 0)
			{
				cultureIds = factions.OrderedCultureKeys;
			}
			else if (factions.AvailableCultures != null)
			{
				cultureIds = factions.AvailableCultures.Keys;
			}

			List<string> cultureVotePool = new List<string>();
			if (cultureIds != null)
			{
				foreach (string cultureId in cultureIds)
				{
					if (string.IsNullOrWhiteSpace(cultureId))
					{
						continue;
					}

					if (factions.AvailableCultures != null && !factions.AvailableCultures.ContainsKey(cultureId))
					{
						continue;
					}

					AddCultureToVotePool(cultureVotePool, cultureId);
				}
			}

			if (cultureVotePool.Count == 0)
			{
				Log("Vote setup - Factions returned no available culture; falling back to current team cultures.", LogLevel.Warning);
				return GetCurrentCultureVotePoolFallback();
			}

			return cultureVotePool;
		}

		private static List<string> GetCurrentCultureVotePoolFallback()
		{
			List<string> cultureVotePool = new List<string>();
			AddCultureToVotePool(cultureVotePool, OptionType.CultureTeam1.GetStrValue());
			AddCultureToVotePool(cultureVotePool, OptionType.CultureTeam2.GetStrValue());

			if (cultureVotePool.Count == 0)
			{
				Log("Vote setup - no culture candidate could be loaded for native intermission vote.", LogLevel.Error);
			}

			return cultureVotePool;
		}

		private static void AddCultureToVotePool(List<string> cultureVotePool, string cultureId)
		{
			if (!string.IsNullOrWhiteSpace(cultureId) && !cultureVotePool.Contains(cultureId))
			{
				cultureVotePool.Add(cultureId);
			}
		}

		private static void ApplyDynamicCultureVoteResult(MultiplayerIntermissionVotingManager votingManager)
		{
			if (!votingManager.IsCultureVoteEnabled)
			{
				return;
			}

			List<IntermissionVoteItem> cultureVoteItems = votingManager.CultureVoteItems.ToList();
			if (cultureVoteItems.Count == 0)
			{
				Log("Native vote timeline - no culture vote item available; keeping current culture options.", LogLevel.Warning);
				return;
			}

			cultureVoteItems.Sort((culture1, culture2) => -culture1.VoteCount.CompareTo(culture2.VoteCount));

			string selectedCulture1;
			string selectedCulture2;
			if (cultureVoteItems[0].VoteCount > 0)
			{
				selectedCulture1 = cultureVoteItems[0].Id;
				selectedCulture2 = cultureVoteItems.Count > 1 ? cultureVoteItems[1].Id : cultureVoteItems[0].Id;

				int totalVoteCount = cultureVoteItems.Select(item => item.VoteCount).Sum();
				if (totalVoteCount > 0 && 10 * cultureVoteItems[0].VoteCount >= 7 * totalVoteCount)
				{
					selectedCulture2 = selectedCulture1;
				}
			}
			else
			{
				Random random = new Random();
				selectedCulture1 = cultureVoteItems[random.Next(0, cultureVoteItems.Count)].Id;
				selectedCulture2 = cultureVoteItems[random.Next(0, cultureVoteItems.Count)].Id;
			}

			// TaleWorlds' no-vote culture fallback is hardcoded to the six native cultures.
			// Re-apply the final culture result from CultureVoteItems so Factions-loaded cultures stay valid.
			OptionType.CultureTeam1.SetValue(selectedCulture1);
			OptionType.CultureTeam2.SetValue(selectedCulture2);
			Log($"Native vote timeline - dynamic culture selection | culture1={selectedCulture1} | culture2={selectedCulture2}", LogLevel.Debug);
		}

		private static bool PrepareScenarioVoteItems(MultiplayerIntermissionVotingManager votingManager)
		{
			ScenarioManager.Instance.RefreshAvailableScenarios();

			HashSet<string> usedVoteIds = new HashSet<string>();
			List<Scenario> scenarios = ScenarioManager.Instance.AvailableScenario ?? new List<Scenario>();
			foreach (Scenario scenario in scenarios)
			{
				if (scenario?.Acts == null)
				{
					continue;
				}

				for (int actIndex = 0; actIndex < scenario.Acts.Count; actIndex++)
				{
					Act act = scenario.Acts[actIndex];
					if (act == null || string.IsNullOrEmpty(act.MapID))
					{
						continue;
					}

					string voteId = CreateScenarioVoteId(scenario, act, actIndex, usedVoteIds);
					int itemIndex = votingManager.MapVoteItems.Count;
					votingManager.MapVoteItems.Add(new IntermissionVoteItem(voteId, itemIndex));
					ScenarioVoteCandidatesByVoteId[voteId] = new ScenarioVoteCandidate(scenario, act);
					Log($"Vote setup - scenario[{itemIndex}]={voteId} | map={act.MapID}", LogLevel.Debug);
				}
			}

			Log($"Vote setup - culture vote disabled for Scenario game mode. Prepared {ScenarioVoteCandidatesByVoteId.Count} scenario candidates.", LogLevel.Debug);
			return ScenarioVoteCandidatesByVoteId.Count > 0;
		}

		private static string CreateScenarioVoteId(Scenario scenario, Act act, int actIndex, HashSet<string> usedVoteIds)
		{
			string baseVoteId = $"{scenario.Name.LocalizedText} - {act.Name.LocalizedText}";
			if (string.IsNullOrWhiteSpace(baseVoteId) || baseVoteId == " - ")
			{
				baseVoteId = $"{scenario.Id} - Act {actIndex + 1}";
			}

			string voteId = baseVoteId;
			if (usedVoteIds.Contains(voteId))
			{
				voteId = $"{baseVoteId} [{scenario.Id}:{actIndex + 1}]";
			}

			int duplicateIndex = 2;
			while (usedVoteIds.Contains(voteId))
			{
				voteId = $"{baseVoteId} [{scenario.Id}:{actIndex + 1}:{duplicateIndex++}]";
			}

			usedVoteIds.Add(voteId);
			return voteId;
		}

		private static void StartVotedScenario(MultiplayerIntermissionVotingManager votingManager)
		{
			ScenarioVoteCandidate candidate = GetWinningScenarioCandidate(votingManager);
			if (candidate == null)
			{
				Log("Native scenario vote failed: no winning scenario candidate found. Falling back to Lobby.", LogLevel.Error);
				GameModeStarter.Instance.EndingCurrentMissionThenStartingNewMission = false;
				GameModeStarter.Instance.StartLobby(OptionType.Map.GetStrValue(), OptionType.CultureTeam1.GetStrValue(), OptionType.CultureTeam2.GetStrValue());
				return;
			}

			votingManager.CurrentVoteState = MultiplayerIntermissionState.CountingForMission;
			OptionType.Map.SetValue(candidate.Act.MapID);
			Log($"Native vote timeline - state -> CountingForMission | selectedScenario={candidate.Scenario.Name.LocalizedText} | act={candidate.Act.Name.LocalizedText} | map={candidate.Act.MapID}");
			NativeIntermissionVoteMsg.SendIntermissionUpdateToAllPeers(MultiplayerIntermissionState.CountingForMission, 0f);

			Thread.Sleep(WaitBeforeMissionStartMilliseconds);
			GameModeStarter.Instance.EndingCurrentMissionThenStartingNewMission = false;
			candidate.Act.ActSettings.TWOptions[OptionType.Map] = candidate.Act.MapID;
			ScenarioManager.Instance.StartScenario(candidate.Scenario, candidate.Act);
		}

		private static ScenarioVoteCandidate GetWinningScenarioCandidate(MultiplayerIntermissionVotingManager votingManager)
		{
			IntermissionVoteItem winningItem = votingManager.MapVoteItems.OrderByDescending(item => item.VoteCount).FirstOrDefault();
			if (winningItem == null)
			{
				return null;
			}

			if (winningItem.VoteCount <= 0)
			{
				winningItem = votingManager.MapVoteItems[MBRandom.RandomInt(0, votingManager.MapVoteItems.Count)];
			}

			ScenarioVoteCandidatesByVoteId.TryGetValue(winningItem.Id, out ScenarioVoteCandidate candidate);
			return candidate;
		}

		private static void RunIntermissionCountdown(MultiplayerIntermissionState state, float durationSeconds)
		{
			float remaining = durationSeconds;
			while (remaining > 0f)
			{
				NativeIntermissionVoteMsg.SendIntermissionUpdateToAllPeers(state, remaining);
				float step = remaining > IntermissionUpdateTickSeconds ? IntermissionUpdateTickSeconds : remaining;
				Thread.Sleep((int)(step * 1000f));
				remaining -= step;
			}

			NativeIntermissionVoteMsg.SendIntermissionUpdateToAllPeers(state, 0f);
		}

		private static bool IsScenario(GameModeSettings gameModeSettings)
		{
			return gameModeSettings?.TWOptions[OptionType.GameType]?.ToString() == ScenarioGameType;
		}

		private sealed class ScenarioVoteCandidate
		{
			public readonly Scenario Scenario;
			public readonly Act Act;

			public ScenarioVoteCandidate(Scenario scenario, Act act)
			{
				Scenario = scenario;
				Act = act;
			}
		}
	}
}



