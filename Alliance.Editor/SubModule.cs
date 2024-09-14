using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Actions;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Scenarios;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Utilities;
using Alliance.Editor.Extensions.ScenarioMaker.State;
using Alliance.Editor.Extensions.Story.Views;
using System.IO;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
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
			ActionFactory.Initialize();
			ScenarioManager.Instance = new ScenarioManager();
			ScenarioManager.Instance.RefreshAvailableScenarios("Alliance.Editor");
			SceneList.Initialize();

			// Need to force load MaterialDesign dlls for obscure reasons
			Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignThemes.Wpf.dll"));
			Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "MaterialDesignColors.dll"));

			GenerateScenarioExamples();

			Log("Alliance.Editor initialized", LogLevel.Debug);
		}

		private void GenerateScenarioExamples()
		{
			// Generate example scenario XML files
			string directoryPath = Path.Combine(ModuleHelper.GetModuleFullPath("Alliance.Editor"), "Scenarios");
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

		public void OpenScenarioMakerMenu(Scene scene)
		{
			ScenarioMakerState scenarioMakerState = GameStateManager.Current.CreateState<ScenarioMakerState>(new object[] { scene });
			GameStateManager.Current.PushState(scenarioMakerState, 0);
		}

		protected override void OnApplicationTick(float dt)
		{
			if (Input.IsKeyPressed(InputKey.O))
			{
				OpenScenarioEditor();
			}
		}

		private void OpenScenarioEditor()
		{
			// Create and show the editor window
			Scenario scenario = ScenarioManager.Instance.AvailableScenario.FirstOrDefault();
			scenario ??= new Scenario(new LocalizedString("New scenario"), new LocalizedString());
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