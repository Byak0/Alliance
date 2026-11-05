using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Core.Utils.AllianceData;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Change the atmosphere of the scene (lighting, weather, time of day).
	/// The atmosphere is applied on the server and every client.
	/// </summary>
	[Serializable]
	[PhrasePreview("Set atmosphere to {AtmosphereName}")]
	public class SetAtmosphereAction : ActionBase
	{
		[ConfigProperty(label: "Atmosphere Name", tooltip: "Name of the atmosphere to apply. Examples:\n- TOD_06_00_Foggy\n- TOD_12_00_HeavyRain\n- night_clear\nAny module can provide more of them in its Atmospheres folder.", dataType: DataTypes.Atmosphere)]
		public string AtmosphereName;

		public SetAtmosphereAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			ApplyCurrentAtmosphere();
			ExecuteOnClient(context);
			return ActionTask.CompletedTask;
		}

		public override void ExecuteClient()
		{
			ApplyCurrentAtmosphere();
		}

		private void ApplyCurrentAtmosphere()
		{
			if (!ApplyAtmosphere(AtmosphereName)) return;
			// Track the mission's live atmosphere: the server replays it to players joining mid mission
			ScenarioManager.Instance.CurrentAtmosphereName = AtmosphereName;
		}

		/// <summary>Applies an atmosphere to the current scene (mission or editor).
		/// Returns false when the name is empty, unknown, or there is no scene.</summary>
		public static bool ApplyAtmosphere(string atmosphereName)
		{
			if (string.IsNullOrEmpty(atmosphereName)) return false;
			Scene scene = Mission.Current?.Scene ?? MBEditor._editorScene;
			if (scene == null) return false;
			// Check atmosphere validity before applying it, might crash otherwise.
			if (!AvailableAtmospheres().Contains(atmosphereName))
			{
				Log($"Atmosphere '{atmosphereName}' doesn't exist in any active module - skipping.", LogLevel.Warning);
				return false;
			}
			scene.SetAtmosphereWithName(atmosphereName);
			Log($"Atmosphere changed to {atmosphereName}", LogLevel.Debug);
			return true;
		}
	}
}
