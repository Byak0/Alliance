using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
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
			ApplyAtmosphere();
			ExecuteOnClient(context);
			return ActionTask.CompletedTask;
		}

		public override void ExecuteClient()
		{
			ApplyAtmosphere();
		}

		private void ApplyAtmosphere()
		{
			if (string.IsNullOrEmpty(AtmosphereName)) return;
			Scene scene = Mission.Current?.Scene ?? MBEditor._editorScene;
			if (scene == null) return;
			scene.SetAtmosphereWithName(AtmosphereName);
			Log($"Atmosphere changed to {AtmosphereName}", LogLevel.Debug);
		}
	}
}
