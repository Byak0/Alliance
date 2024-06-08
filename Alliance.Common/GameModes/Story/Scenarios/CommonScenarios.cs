using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Models.Objectives;
using TaleWorlds.Core;
using static Alliance.Common.GameModes.Story.Models.VictoryLogic;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Story.Scenarios
{
	public class CommonScenarios
	{
		public static Scenario BFHD(
			OnActVictoryDelegate onActShowResults, OnActVictoryDelegate onActCompleted,
			OnActVictoryDelegate onActShowResults2, OnActVictoryDelegate onActCompleted2)
		{
			string scenarioId = nameof(BFHD);
			Scenario bfhd = new Scenario(
				id: scenarioId,
				name: GameTexts.FindText("str_alliance_scenario_name", scenarioId).ToString(),
				desc: GameTexts.FindText("str_alliance_scenario_description", scenarioId).ToString()
				);

			// Act 1 - Defend the fortress (night)
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.SetNativeOption(OptionType.Map, "bfhd_helms_deep_act_I");
			act1Settings.SetNativeOption(OptionType.CultureTeam1, "vlandia"); // Attacker
			act1Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "", "" },
				storeAgentsInfo: true,
				usePreviousActAgents: false,
				maxLives: new int[] { 1, 1 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyTags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.NoRespawn }
				);
			VictoryLogic act1VictoryLogic = new VictoryLogic(onActShowResults, onActCompleted);
			Act act1 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: true,
				mapId: "bfhd_helms_deep_act_I",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
			TimerObjective act1objective1 = new TimerObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, false, 3600);
			KillCountObjective act1objective2 = new KillCountObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_2").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_2").ToString(),
				true, false, 10000);
			KillAllObjective act1objective3 = new KillAllObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_3").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_3").ToString(),
				true, true);
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			act1.Objectives.Add(act1objective3);
			bfhd.Acts.Add(act1);

			// Act 2 - Sally out (day)
			actId = "_2";
			ScenarioGameModeSettings act2Settings = new ScenarioGameModeSettings();
			act1Settings.SetNativeOption(OptionType.Map, "bfhd_helms_deep_act_II");
			act2Settings.SetNativeOption(OptionType.CultureTeam1, "vlandia"); // Attacker
			act2Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			SpawnLogic act2SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "mp_light_cavalry_battania", "mp_heavy_cavalry_vlandia" },
				spawnTags: new string[] { "reinforcement", "" },
				storeAgentsInfo: false,
				usePreviousActAgents: true,
				maxLives: new int[] { 3, 1 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyTags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.MaxLivesPerPlayer,
					RespawnStrategy.NoRespawn }
				);
			VictoryLogic act2VictoryLogic = new VictoryLogic(onActShowResults2, onActCompleted2);
			Act act2 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: true,
				mapId: "bfhd_helms_deep_act_II",
				actSettings: act2Settings,
				spawnLogic: act2SpawnLogic,
				victoryLogic: act2VictoryLogic
				);
			KillAllObjective act2objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true);
			KillAllObjective act2objective2 = new KillAllObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true);
			act2.Objectives.Add(act2objective1);
			act2.Objectives.Add(act2objective2);
			bfhd.Acts.Add(act2);

			return bfhd;
		}

		public static Scenario TestGiant(OnActVictoryDelegate onActShowResults, OnActVictoryDelegate onActCompleted)
		{
			string scenarioId = nameof(TestGiant);
			Scenario Test = new Scenario(scenarioId, "test", "test desc");

			// Act 1 - Test giant
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.SetNativeOption(OptionType.Map, "bfhd_helms_deep_act_II");
			act1Settings.SetNativeOption(OptionType.CultureTeam1, "sturgia"); // Attacker
			act1Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "mp_heavy_ranged_battania", "mp_light_infantry_sturgia" },
				spawnTags: new string[] { "reinforcement", "" },
				storeAgentsInfo: false,
				usePreviousActAgents: false,
				maxLives: new int[] { 5, 1 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyTags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.MaxLivesPerPlayer,
					RespawnStrategy.NoRespawn }
				);
			VictoryLogic act1VictoryLogic = new VictoryLogic(onActShowResults, onActCompleted);
			Act act1 = new Act(
				name: "Test",
				desc: "TestDesc",
				loadMap: true,
				mapId: "bfhd_helms_deep_act_II",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
			KillAllObjective act1objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				"Test obj",
				"Test obj desc",
				true, true);
			KillAllObjective act1objective2 = new KillAllObjective(
				BattleSideEnum.Attacker,
				"Test obj",
				"Test obj desc",
				true, true);
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			Test.Acts.Add(act1);

			return Test;
		}

		public static Scenario GP(
			OnActVictoryDelegate onActShowResults, OnActVictoryDelegate onActCompleted,
			OnActVictoryDelegate onActShowResults2, OnActVictoryDelegate onActCompleted2,
			OnActVictoryDelegate onActShowResults3, OnActVictoryDelegate onActCompleted3,
			OnActVictoryDelegate onActShowResults4, OnActVictoryDelegate onActCompleted4)
		{
			string scenarioId = nameof(GP);
			Scenario Test = new Scenario(
				id: scenarioId,
				name: GameTexts.FindText("str_alliance_scenario_name", scenarioId).ToString(),
				desc: GameTexts.FindText("str_alliance_scenario_description", scenarioId).ToString()
				);

			// Act 1 - Occupy the village
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.SetNativeOption(OptionType.Map, "FrenchCastlePathEdC");
			act1Settings.SetNativeOption(OptionType.CultureTeam1, "empire"); // Attacker
			act1Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			act1Settings.SetNativeOption(OptionType.NumberOfBotsTeam1, 0);
			act1Settings.SetNativeOption(OptionType.NumberOfBotsTeam2, 0);
			act1Settings.SetNativeOption(OptionType.RoundPreparationTimeLimit, 20);
			act1Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act1_def_village", "act1_att_village" },
				storeAgentsInfo: false,
				usePreviousActAgents: false,
				maxLives: new int[] { 1, 1500 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.TagsThenFlags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);
			VictoryLogic act1VictoryLogic = new VictoryLogic(onActShowResults, onActCompleted);
			Act act1 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: true,
				mapId: "scn_lobby_v01",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
			KillAllObjective act1objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true);
			CaptureObjective act1objective2 = new CaptureObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_2").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_2").ToString(),
				true, true, "farm");
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			Test.Acts.Add(act1);


			// Act 2 - Attack the english camp
			actId = "_2";
			ScenarioGameModeSettings act2Settings = new ScenarioGameModeSettings();
			act2Settings.SetNativeOption(OptionType.Map, "FrenchCastlePathEdC");
			act2Settings.SetNativeOption(OptionType.CultureTeam1, "empire"); // Attacker
			act2Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			act2Settings.SetNativeOption(OptionType.NumberOfBotsTeam1, 0);
			act2Settings.SetNativeOption(OptionType.NumberOfBotsTeam2, 0);
			act2Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act2SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act2_def_camp", "act1_att_village" },
				storeAgentsInfo: false,
				usePreviousActAgents: false,
				maxLives: new int[] { 1, 0 },
				keepLivesFromPreviousAct: true,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyFlags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);
			VictoryLogic act2VictoryLogic = new VictoryLogic(onActShowResults2, onActCompleted2);
			Act act2 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: false,
				mapId: "scn_lobby_v01",
				actSettings: act2Settings,
				spawnLogic: act2SpawnLogic,
				victoryLogic: act2VictoryLogic
				);
			KillAllObjective act2objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true);
			CaptureObjective act2objective2 = new CaptureObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_2").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_2").ToString(),
				true, true, "camp");
			act2.Objectives.Add(act2objective1);
			act2.Objectives.Add(act2objective2);
			Test.Acts.Add(act2);

			// Act 3 - Attack the castle
			actId = "_3";
			ScenarioGameModeSettings act3Settings = new ScenarioGameModeSettings();
			act3Settings.SetNativeOption(OptionType.Map, "FrenchCastlePathEdC");
			act3Settings.SetNativeOption(OptionType.CultureTeam1, "empire"); // Attacker
			act3Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			act3Settings.SetNativeOption(OptionType.NumberOfBotsTeam1, 0);
			act3Settings.SetNativeOption(OptionType.NumberOfBotsTeam2, 0);
			act3Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act3SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act3_def_castle", "act1_att_village" },
				storeAgentsInfo: false,
				usePreviousActAgents: false,
				maxLives: new int[] { 1, 0 },
				keepLivesFromPreviousAct: true,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyFlags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);
			VictoryLogic act3VictoryLogic = new VictoryLogic(onActShowResults3, onActCompleted3);
			Act act3 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: false,
				mapId: "scn_lobby_v01",
				actSettings: act3Settings,
				spawnLogic: act3SpawnLogic,
				victoryLogic: act3VictoryLogic
				);
			KillAllObjective act3objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true);
			CaptureObjective act3objective2 = new CaptureObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_2").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_2").ToString(),
				true, true, "castle");
			act3.Objectives.Add(act3objective1);
			act3.Objectives.Add(act3objective2);
			Test.Acts.Add(act3);

			// Act 4 - Defend the castle
			actId = "_4";
			ScenarioGameModeSettings act4Settings = new ScenarioGameModeSettings();
			act4Settings.SetNativeOption(OptionType.Map, "FrenchCastlePathEdC");
			act4Settings.SetNativeOption(OptionType.CultureTeam1, "empire"); // Attacker
			act4Settings.SetNativeOption(OptionType.CultureTeam2, "battania"); // Defender
			act4Settings.SetNativeOption(OptionType.NumberOfBotsTeam1, 0);
			act4Settings.SetNativeOption(OptionType.NumberOfBotsTeam2, 0);
			act4Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act4SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act4_def_castle", "act1_att_village" },
				storeAgentsInfo: false,
				usePreviousActAgents: false,
				maxLives: new int[] { 1, 0 },
				keepLivesFromPreviousAct: true,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyFlags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);
			VictoryLogic act4VictoryLogic = new VictoryLogic(onActShowResults4, onActCompleted4);
			Act act4 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: false,
				mapId: "scn_lobby_v01",
				actSettings: act4Settings,
				spawnLogic: act4SpawnLogic,
				victoryLogic: act4VictoryLogic
				);
			KillAllObjective act4objective1 = new KillAllObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true);
			KillAllObjective act4objective2 = new KillAllObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_2").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_2").ToString(),
				true, true);
			act4.Objectives.Add(act4objective1);
			act4.Objectives.Add(act4objective2);
			Test.Acts.Add(act4);

			return Test;
		}

		public static Scenario GdCFinal(OnActVictoryDelegate onActShowResults, OnActVictoryDelegate onActCompleted)
		{
			string scenarioId = nameof(GdCFinal);
			Scenario scenarGdCFinal = new Scenario(
				id: scenarioId,
				name: GameTexts.FindText("str_alliance_scenario_name", scenarioId).ToString(),
				desc: GameTexts.FindText("str_alliance_scenario_description", scenarioId).ToString()
				);

			// Act 1 - Defend the fortress
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.SetNativeOption(OptionType.Map, "bfhd_helms_deep_v2");
			act1Settings.SetNativeOption(OptionType.CultureTeam1, "isengard"); // Attacker
			act1Settings.SetNativeOption(OptionType.CultureTeam2, "rohan"); // Defender
			act1Settings.SetNativeOption(OptionType.NumberOfBotsTeam1, 20);
			act1Settings.SetNativeOption(OptionType.NumberOfBotsTeam2, 20);
			act1Settings.SetNativeOption(OptionType.RoundPreparationTimeLimit, 20);
			act1Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 60;
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "defender", "attacker" },
				storeAgentsInfo: false,
				usePreviousActAgents: false,
				maxLives: new int[] { 200, 10000 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.OnlyTags,
					LocationStrategy.OnlyTags },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.MaxLivesPerTeam,
					RespawnStrategy.MaxLivesPerTeam }
				);
			VictoryLogic act1VictoryLogic = new VictoryLogic(onActShowResults, onActCompleted);
			Act act1 = new Act(
				name: GameTexts.FindText("str_alliance_act_name", scenarioId + actId).ToString(),
				desc: GameTexts.FindText("str_alliance_act_description", scenarioId + actId).ToString(),
				loadMap: true,
				mapId: "bfhd_helms_deep_v2",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
			TimerObjective act1objective1 = new TimerObjective(
				BattleSideEnum.Defender,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_1").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_1").ToString(),
				true, true, 3600);
			KillAllObjective act1objective2 = new KillAllObjective(
				BattleSideEnum.Attacker,
				GameTexts.FindText("str_alliance_obj_name", scenarioId + actId + "_2").ToString(),
				GameTexts.FindText("str_alliance_obj_description", scenarioId + actId + "_2").ToString(),
				true, true);
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			scenarGdCFinal.Acts.Add(act1);

			return scenarGdCFinal;
		}
	}
}
