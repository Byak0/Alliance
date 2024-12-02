using System.Collections.Generic;
using System.Linq;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Duel
{
	public class DuelGameModeSettings : GameModeSettings
	{
		public DuelGameModeSettings() : base("DuelX", "Duel", "Duel mode.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
			TWOptions[OptionType.NumberOfBotsTeam1] = 0;
			TWOptions[OptionType.NumberOfBotsTeam2] = 0;
			TWOptions[OptionType.NumberOfBotsPerFormation] = 0;
			TWOptions[OptionType.MinScoreToWinDuel] = 3;
			TWOptions[OptionType.MinNumberOfPlayersForMatchStart] = 0;
			TWOptions[OptionType.MapTimeLimit] = 360;
		}

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
			ModOptions.EnableFormation = false;
		}

		public override List<SceneInfo> GetAvailableMaps()
		{
			return base.GetAvailableMaps().Where(scene => scene.Name.ToLower().Contains("duel")).ToList();
		}

		public override List<OptionType> GetAvailableNativeOptions()
		{
			return new List<OptionType>
			{
				OptionType.CultureTeam1,
				OptionType.CultureTeam2,
				OptionType.MinScoreToWinDuel
			};
		}

		public override List<string> GetAvailableModOptions()
		{
			return base.GetAvailableModOptions();
		}
	}
}