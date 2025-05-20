using Alliance.Common.Extensions.AdvancedCombat.Behaviors;
using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.GameModels;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Scenarios;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Patch;
using Alliance.Common.Patch.HarmonyPatch;
using Alliance.Common.Utilities;
using Alliance.Editor.GameModes.Story.Utilities;
using Alliance.Editor.GameModes.Story.Views;
using Alliance.Editor.Patch;
using System.IO;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using Path = System.IO.Path;

namespace Alliance.Editor
{
	public class SubModule : MBSubModuleBase
	{
		public const string ModuleId = "Alliance.Editor";
		private ScenarioEditorWindow _scenarioEditorWindow;

		protected override void OnSubModuleLoad()
		{
			Common.SubModule.CurrentModuleName = ModuleId;

			ActionFactory.Initialize();
			ScenarioManager.Instance = new ScenarioManager();
			SceneList.Initialize();

			EditorToolsManager.EditorTools = new EditorTools();

			// Need to force load MaterialDesign dlls for obscure reasons
			Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignThemes.Wpf.dll"));
			Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignColors.dll"));

			GenerateScenarioExamples();

			// Apply Harmony patches
			DirtyCommonPatcher.Patch();

			DirtyEditorPatcher.Patch();

			Log("Alliance.Editor initialized", LogLevel.Debug);
		}

		public override void OnBeforeMissionBehaviorInitialize(Mission mission)
		{
			// Initialize animation system and all the game animations
			AnimationSystem.Instance.Init();

			mission.AddMissionBehavior(new AdvancedCombatBehavior());
		}

		protected override void OnGameStart(Game game, IGameStarter gameStarter)
		{
			// Late patching, patching earlier causes issues with Voice type
			Patch_AdvancedCombat.LatePatch();

			// Add our custom GameModels 
			gameStarter.AddModel(new ExtendedAgentStatCalculateModel());
			gameStarter.AddModel(new ExtendedAgentApplyDamageModel());
		}

		private void GenerateScenarioExamples()
		{
			// Generate example scenario XML files
			string directoryPath = Path.Combine(ModuleHelper.GetModuleFullPath(Common.SubModule.CurrentModuleName), ScenarioManager.SCENARIO_FOLDER_NAME);
			if (!Directory.Exists(directoryPath) || Directory.GetFiles(directoryPath).Length == 0)
			{
				GenerateExampleScenarioXML(ExampleScenarios.GdCFinal(), directoryPath);
				GenerateExampleScenarioXML(ExampleScenarios.BFHD(), directoryPath);
				GenerateExampleScenarioXML(ExampleScenarios.OrgaDefault(), directoryPath);
				GenerateExampleScenarioXML(ExampleScenarios.GP(), directoryPath);
			}
		}

		private void GenerateExampleScenarioXML(Scenario scenario, string directory)
		{
			// Clean the scenario name to make it filename-safe (remove illegal characters)
			string safeScenarioName = string.Join("_", scenario.Name.GetText().Split(Path.GetInvalidFileNameChars()));

			// Construct the full file path
			string filename = Path.Combine(directory, $"EXAMPLE_{safeScenarioName}_{scenario.Id}.xml");

			ScenarioSerializer.SerializeScenarioToXML(scenario, filename);
		}

		protected override void OnApplicationTick(float dt)
		{
			EditZoneView.Tick(dt);
			if (Input.IsKeyPressed(InputKey.O))
			{
				OpenScenarioEditor();
			}
		}

		private void OpenScenarioEditor()
		{
			// Create and show the editor window
			Scenario scenario = new Scenario(new LocalizedString("New scenario"), new LocalizedString());
			if (_scenarioEditorWindow == null || !_scenarioEditorWindow.IsLoaded)
			{
				_scenarioEditorWindow = new ScenarioEditorWindow(scenario);
				_scenarioEditorWindow.Show();
				_scenarioEditorWindow.Closed += (s, e) =>
				{
					_scenarioEditorWindow = null;
				};
			}
			else
			{
				_scenarioEditorWindow.Focus();
			}
		}
	}
}