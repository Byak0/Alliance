using Alliance.Common.GameModes.Lobby;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Conditions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Objectives;
using System.Collections.Generic;
using TaleWorlds.Core;
using static Alliance.Common.GameModes.Story.Conditions.Condition;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Story.Scenarios
{
	/// <summary>
	/// Example scenarios for testing and demonstration purposes.
	/// </summary>
	public class ExampleScenarios
	{
		public static Scenario BFHD()
		{
			string scenarioId = nameof(BFHD);
			Scenario bfhd = new Scenario(
				name: new LocalizedString("BFHD name"),
				desc: new LocalizedString("BFHD description")
				);

			// Act 1 - Defend the fortress (night)
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.TWOptions[OptionType.Map] = "bfhd_helms_deep_act_I";
			act1Settings.TWOptions[OptionType.CultureTeam1] = "vlandia"; // Attacker
			act1Settings.TWOptions[OptionType.CultureTeam2] = "battania"; // Defender		
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "", "" },
				maxLives: new int[] { 1, 1 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Spawnpoint },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.NoRespawn }
				);

			LobbyGameModeSettings lobbyGameModeSettings = new LobbyGameModeSettings();
			lobbyGameModeSettings.TWOptions[OptionType.Map] = "bfhd_helms_deep_lobby";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam1] = "battania";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam2] = "vlandia";

			List<ActionBase> act1ActionsDisplayResults = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new IfElseAction(
					new VictoryCondition(BattleSideEnum.Attacker),
					new StartGameAction("bfhd_helms_deep_lobby",lobbyGameModeSettings),
					new StartScenarioAction(scenarioId, 1))
			};

			VictoryLogic act1VictoryLogic = new VictoryLogic(act1ActionsDisplayResults);
			Act act1 = new Act(
				name: new LocalizedString("BFHD act 1 name"),
				desc: new LocalizedString("BFHD act 1 description"),
				loadMap: true,
				mapId: "bfhd_helms_deep_act_I",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
		Objective act1objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("BFHD act 1 timer objective"),
			new LocalizedString("BFHD act 1 timer objective desc"),
			true, true, new TimerCondition { WaitTime = 3600, TypeOfTimer = TimerCondition.TimerType.TriggerOnce });
		Objective act1objective2 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("BFHD act 1 killcount objective"),
			new LocalizedString("BFHD act 1 killcount objective desc"),
			true, true, new KillCountCondition(SideType.Attacker, 10000));
		Objective act1objective3 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("BFHD act 1 KillAllObjective"),
			new LocalizedString("BFHD act 1 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Defender));
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			act1.Objectives.Add(act1objective3);
			bfhd.Acts.Add(act1);

			// Act 2 - Sally out (day)
			actId = "_2";
			ScenarioGameModeSettings act2Settings = new ScenarioGameModeSettings();
			act2Settings.TWOptions[OptionType.Map] = "bfhd_helms_deep_act_II";
			act2Settings.TWOptions[OptionType.CultureTeam1] = "vlandia"; // Attacker
			act2Settings.TWOptions[OptionType.CultureTeam2] = "battania"; // Defender
			SpawnLogic act2SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "mp_light_cavalry_battania", "mp_heavy_cavalry_vlandia" },
				spawnTags: new string[] { "reinforcement", "" },
				maxLives: new int[] { 3, 1 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Spawnpoint },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.MaxLivesPerPlayer,
					RespawnStrategy.NoRespawn }
				);

			List<ActionBase> act2ActionsDisplayResults = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new StartGameAction("bfhd_helms_deep_lobby", lobbyGameModeSettings)
			};
			VictoryLogic act2VictoryLogic = new VictoryLogic(act2ActionsDisplayResults);
			Act act2 = new Act(
				name: new LocalizedString("BFHD act2 name"),
				desc: new LocalizedString("BFHD act2 desc"),
				loadMap: true,
				mapId: "bfhd_helms_deep_act_II",
				actSettings: act2Settings,
				spawnLogic: act2SpawnLogic,
				victoryLogic: act2VictoryLogic
				);
		Objective act2objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("BFHD act 2 KillAllObjective"),
			new LocalizedString("BFHD act 2 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Attacker));
		Objective act2objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("BFHD act 2 KillAllObjective2"),
			new LocalizedString("BFHD act 2 KillAllObjective2 desc"),
			true, false, new SideEliminatedCondition(SideType.Defender));
			act2.Objectives.Add(act2objective1);
			act2.Objectives.Add(act2objective2);
			bfhd.Acts.Add(act2);

			return bfhd;
		}

		public static Scenario GP()
		{
			string scenarioId = nameof(GP);
			Scenario Test = new Scenario(
				name: new LocalizedString("BFHD name"),
				desc: new LocalizedString("BFHD description")
				);

			// Act 1 - Occupy the village
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.TWOptions[OptionType.Map] = "FrenchCastlePathEdC";
			act1Settings.TWOptions[OptionType.CultureTeam1] = "empire"; // Attacker
			act1Settings.TWOptions[OptionType.CultureTeam2] = "battania"; // Defender
			act1Settings.TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			act1Settings.TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			act1Settings.TWOptions[OptionType.RoundPreparationTimeLimit] = 20;
			act1Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act1_def_village", "act1_att_village" },
				maxLives: new int[] { 1, 1500 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Zones },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);

			LobbyGameModeSettings lobbyGameModeSettings = new LobbyGameModeSettings();
			lobbyGameModeSettings.TWOptions[OptionType.Map] = "FrenchCastlePathEdC";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam1] = "empire";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam2] = "battania";

			List<ActionBase> act1DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new IfElseAction(
					new VictoryCondition(BattleSideEnum.Defender),
					new StartGameAction("FrenchCastlePathEdC", lobbyGameModeSettings),
					new StartScenarioAction(scenarioId, 1))
			};
			VictoryLogic act1VictoryLogic = new VictoryLogic(act1DisplayResultsActions);
			Act act1 = new Act(
				name: new LocalizedString("GP act 1 name"),
				desc: new LocalizedString("GP act 1 description"),
				loadMap: true,
				mapId: "scn_lobby_v01",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
		Objective act1objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("GP act 1 KillAllObjective"),
			new LocalizedString("GP act 1 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Attacker));
		Objective act1objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("GP act 1 CaptureObjective"),
			new LocalizedString("GP act 1 CaptureObjective desc"),
			true, false, new ZoneCapturedCondition("farm", SideType.Attacker));
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			Test.Acts.Add(act1);


			// Act 2 - Attack the english camp
			actId = "_2";
			ScenarioGameModeSettings act2Settings = new ScenarioGameModeSettings();
			act2Settings.TWOptions[OptionType.Map] = "FrenchCastlePathEdC";
			act2Settings.TWOptions[OptionType.CultureTeam1] = "empire"; // Attacker
			act2Settings.TWOptions[OptionType.CultureTeam2] = "battania"; // Defender
			act2Settings.TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			act2Settings.TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			act2Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act2SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act2_def_camp", "act1_att_village" },
				maxLives: new int[] { 1, 0 },
				keepLivesFromPreviousAct: true,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Banners },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);

			List<ActionBase> act2DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new IfElseAction(
					new VictoryCondition(BattleSideEnum.Defender),
					new StartGameAction("FrenchCastlePathEdC", lobbyGameModeSettings),
					new StartScenarioAction(scenarioId, 2))
			};

			VictoryLogic act2VictoryLogic = new VictoryLogic(act2DisplayResultsActions);
			Act act2 = new Act(
				name: new LocalizedString("GP act 2 name"),
				desc: new LocalizedString("GP act 2 description"),
				loadMap: false,
				mapId: "scn_lobby_v01",
				actSettings: act2Settings,
				spawnLogic: act2SpawnLogic,
				victoryLogic: act2VictoryLogic
				);
		Objective act2objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("GP act 2 KillAllObjective"),
			new LocalizedString("GP act 2 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Attacker));
		Objective act2objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("GP act 2 CaptureObjective"),
			new LocalizedString("GP act 2 CaptureObjective desc"),
			true, false, new ZoneCapturedCondition("camp", SideType.Attacker));
			act2.Objectives.Add(act2objective1);
			act2.Objectives.Add(act2objective2);
			Test.Acts.Add(act2);

			// Act 3 - Attack the castle
			actId = "_3";
			ScenarioGameModeSettings act3Settings = new ScenarioGameModeSettings();
			act3Settings.TWOptions[OptionType.Map] = "FrenchCastlePathEdC";
			act3Settings.TWOptions[OptionType.CultureTeam1] = "empire"; // Attacker
			act3Settings.TWOptions[OptionType.CultureTeam2] = "battania"; // Defender
			act3Settings.TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			act3Settings.TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			act3Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act3SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act3_def_castle", "act1_att_village" },
				maxLives: new int[] { 1, 0 },
				keepLivesFromPreviousAct: true,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Banners },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);

			List<ActionBase> act3DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new IfElseAction(
					new VictoryCondition(BattleSideEnum.Defender),
					new StartGameAction("FrenchCastlePathEdC", lobbyGameModeSettings),
					new StartScenarioAction(scenarioId, 3))
			};

			VictoryLogic act3VictoryLogic = new VictoryLogic(act3DisplayResultsActions);
			Act act3 = new Act(
				name: new LocalizedString("GP act 3 name"),
				desc: new LocalizedString("GP act 3 description"),
				loadMap: false,
				mapId: "scn_lobby_v01",
				actSettings: act3Settings,
				spawnLogic: act3SpawnLogic,
				victoryLogic: act3VictoryLogic
				);
		Objective act3objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("GP act 3 KillAllObjective"),
			new LocalizedString("GP act 3 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Attacker));
		Objective act3objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("GP act 3 CaptureObjective"),
			new LocalizedString("GP act 3 CaptureObjective desc"),
			true, false, new ZoneCapturedCondition("castle", SideType.Attacker));
			act3.Objectives.Add(act3objective1);
			act3.Objectives.Add(act3objective2);
			Test.Acts.Add(act3);

			// Act 4 - Defend the castle
			actId = "_4";
			ScenarioGameModeSettings act4Settings = new ScenarioGameModeSettings();
			act4Settings.TWOptions[OptionType.Map] = "FrenchCastlePathEdC";
			act4Settings.TWOptions[OptionType.CultureTeam1] = "empire"; // Attacker
			act4Settings.TWOptions[OptionType.CultureTeam2] = "battania"; // Defender
			act4Settings.TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			act4Settings.TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			act4Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 30;
			SpawnLogic act4SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "act4_def_castle", "act1_att_village" },
				maxLives: new int[] { 1, 0 },
				keepLivesFromPreviousAct: true,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Banners },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);

			List<ActionBase> act4DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new StartGameAction("FrenchCastlePathEdC", lobbyGameModeSettings)
			};

			VictoryLogic act4VictoryLogic = new VictoryLogic(act4DisplayResultsActions);
			Act act4 = new Act(
				name: new LocalizedString("GP act 4 name"),
				desc: new LocalizedString("GP act 4 description"),
				loadMap: false,
				mapId: "scn_lobby_v01",
				actSettings: act4Settings,
				spawnLogic: act4SpawnLogic,
				victoryLogic: act4VictoryLogic
				);
		Objective act4objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("GP act 4 KillAllObjective"),
			new LocalizedString("GP act 4 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Attacker));
		Objective act4objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("GP act 4 KillAllObjective2"),
			new LocalizedString("GP act 4 KillAllObjective2 desc"),
			true, false, new SideEliminatedCondition(SideType.Defender));
			act4.Objectives.Add(act4objective1);
			act4.Objectives.Add(act4objective2);
			Test.Acts.Add(act4);

			return Test;
		}

		public static Scenario GdCFinal()
		{
			Scenario scenarGdCFinal = new Scenario(
				name: new LocalizedString("GdCFinal name"),
				desc: new LocalizedString("GdCFinal description")
				);

			// Act 1 - Defend the fortress
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.TWOptions[OptionType.Map] = "bfhd_helms_deep_gdc";
			act1Settings.TWOptions[OptionType.CultureTeam1] = "isengard"; // Attacker
			act1Settings.TWOptions[OptionType.CultureTeam2] = "rohan"; // Defender
			act1Settings.TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			act1Settings.TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			act1Settings.TWOptions[OptionType.RoundPreparationTimeLimit] = 20;
			act1Settings.TWOptions[OptionType.RoundTimeLimit] = 4000;
			act1Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 60;
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "defender", "attacker" },
				maxLives: new int[] { 200, 4000 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Spawnpoint },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.MaxLivesPerTeam,
					RespawnStrategy.MaxLivesPerTeam }
				);

			LobbyGameModeSettings lobbyGameModeSettings = new LobbyGameModeSettings();
			lobbyGameModeSettings.TWOptions[OptionType.Map] = "bfhd_helms_deep_v2";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam1] = "isengard";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam2] = "rohan";

			LobbyGameModeSettings lobbyGameModeSettings2 = new LobbyGameModeSettings();
			lobbyGameModeSettings2.TWOptions[OptionType.Map] = "bfhd_helms_deep_v2";
			lobbyGameModeSettings2.TWOptions[OptionType.CultureTeam1] = "rohan";
			lobbyGameModeSettings2.TWOptions[OptionType.CultureTeam2] = "isengard";

			List<ActionBase> act1DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new IfElseAction(
					new VictoryCondition(BattleSideEnum.Attacker),
					new StartGameAction("bfhd_helms_deep_v2", lobbyGameModeSettings),
					new StartGameAction("bfhd_helms_deep_v2", lobbyGameModeSettings2))
			};

			VictoryLogic act1VictoryLogic = new VictoryLogic(act1DisplayResultsActions);
			Act act1 = new Act(
				name: new LocalizedString("GdCFinal act 1 name"),
				desc: new LocalizedString("GdCFinal act 1 description"),
				loadMap: true,
				mapId: "bfhd_helms_deep_gdc",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
		Objective act1objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("GdCFinal act 1 TimerObjective"),
			new LocalizedString("GdCFinal act 1 TimerObjective desc"),
			true, false, new TimerCondition { WaitTime = 3600, TypeOfTimer = TimerCondition.TimerType.TriggerOnce });
		Objective act1objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("GdCFinal act 1 KillAllObjective"),
			new LocalizedString("GdCFinal act 1 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Defender));
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			scenarGdCFinal.Acts.Add(act1);

			return scenarGdCFinal;
		}

		public static Scenario OrgaDefault()
		{
			string scenarioId = nameof(OrgaDefault);
			Scenario scenarOrgaDefault = new Scenario(
				name: new LocalizedString("OrgaDefault name"),
				desc: new LocalizedString("OrgaDefault description")
				);

			// Act 1
			string actId = "_1";
			ScenarioGameModeSettings act1Settings = new ScenarioGameModeSettings();
			act1Settings.TWOptions[OptionType.Map] = "Amazonia";
			act1Settings.TWOptions[OptionType.CultureTeam1] = "explorator"; // Attacker
			act1Settings.TWOptions[OptionType.CultureTeam2] = "autochtone"; // Defender
			act1Settings.TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			act1Settings.TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			act1Settings.TWOptions[OptionType.RoundPreparationTimeLimit] = 20;
			act1Settings.TWOptions[OptionType.RoundTimeLimit] = 4000;
			act1Settings.ModOptions.ActivateSAE = true;
			act1Settings.ModOptions.SAERange = 120;
			SpawnLogic act1SpawnLogic = new SpawnLogic(
				defaultCharacters: new string[] { "", "" },
				spawnTags: new string[] { "defender", "attacker" },
				maxLives: new int[] { 0, 1000 },
				keepLivesFromPreviousAct: false,
				locationStrategies: new LocationStrategy[] {
					LocationStrategy.Spawnpoint,
					LocationStrategy.Zones },
				respawnStrategies: new RespawnStrategy[] {
					RespawnStrategy.NoRespawn,
					RespawnStrategy.MaxLivesPerTeam }
				);

			LobbyGameModeSettings lobbyGameModeSettings = new LobbyGameModeSettings();
			lobbyGameModeSettings.TWOptions[OptionType.Map] = "Amazonia";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam1] = "explorator";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam2] = "autochtone";

			LobbyGameModeSettings lobbyGameModeSettings2 = new LobbyGameModeSettings();
			lobbyGameModeSettings.TWOptions[OptionType.Map] = "Amazonia";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam1] = "autochtone";
			lobbyGameModeSettings.TWOptions[OptionType.CultureTeam2] = "explorator";

			List<ActionBase> act1DisplayResultsActions = new List<ActionBase>()
			{
				new ShowResultScreenAction(),
				new WaitAction() { Duration = new LiteralValue<float>(15f) },
				new IfElseAction(
					new VictoryCondition(BattleSideEnum.Attacker),
					new StartGameAction("Amazonia", lobbyGameModeSettings),
					new StartGameAction("Amazonia", lobbyGameModeSettings2))
			};

			VictoryLogic act1VictoryLogic = new VictoryLogic(act1DisplayResultsActions);
			Act act1 = new Act(
				name: new LocalizedString("OrgaDefault act 1 name"),
				desc: new LocalizedString("OrgaDefault act 1 description"),
				loadMap: true,
				mapId: "Amazonia",
				actSettings: act1Settings,
				spawnLogic: act1SpawnLogic,
				victoryLogic: act1VictoryLogic
				);
		Objective act1objective1 = new Objective(
			BattleSideEnum.Defender,
			new LocalizedString("OrgaDefault act 1 KillAllObjective"),
			new LocalizedString("OrgaDefault act 1 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Attacker));
		Objective act1objective2 = new Objective(
			BattleSideEnum.Attacker,
			new LocalizedString("OrgaDefault act 2 KillAllObjective"),
			new LocalizedString("OrgaDefault act 2 KillAllObjective desc"),
			true, false, new SideEliminatedCondition(SideType.Defender));
			act1.Objectives.Add(act1objective1);
			act1.Objectives.Add(act1objective2);
			scenarOrgaDefault.Acts.Add(act1);

			return scenarOrgaDefault;
		}
	}
}
