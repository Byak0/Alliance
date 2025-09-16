using System.Collections.Generic;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Story
{
	public class ScenarioGameModeSettings : GameModeSettings
	{
		public ScenarioGameModeSettings() : base("Scenario", "Scenario", "Play a premade scenario.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
		}

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
			ModOptions.KillFeedEnabled = false;
			ModOptions.ShowScore = false;
			ModOptions.ShowOfficers = false;
		}

		public override List<SceneInfo> GetAvailableMaps()
		{
			return new List<SceneInfo>();
		}

		public override List<OptionType> GetAvailableNativeOptions()
		{
			return new List<OptionType>();
		}

		public override List<string> GetAvailableModOptions()
		{
			return new List<string>();
		}
	}
}