using Alliance.Common.Core.Configuration.Models;
using System.Collections.Generic;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.BattleRoyale
{
	public class BRGameModeSettings : GameModeSettings
	{
		public BRGameModeSettings() : base("BattleRoyale", "BattleRoyale", "Last man standing.")
		{
		}

		public override void SetDefaultNativeOptions()
		{
			base.SetDefaultNativeOptions();
			TWOptions[OptionType.CultureTeam1] = "empire";
			TWOptions[OptionType.NumberOfBotsTeam1] = 5;
			TWOptions[OptionType.NumberOfBotsTeam2] = 0;
		}

		public override void SetDefaultModOptions()
		{
			base.SetDefaultModOptions();
			ModOptions.BRZoneLifeTime = 300;
		}

		public override List<SceneInfo> GetAvailableMaps()
		{
			return base.GetAvailableMaps();
		}

		public override List<OptionType> GetAvailableNativeOptions()
		{
			return new List<OptionType>
			{
				OptionType.CultureTeam1,
				OptionType.NumberOfBotsTeam1
			};
		}

		public override List<string> GetAvailableModOptions()
		{
			return new List<string>
			{
				nameof(Config.BRZoneLifeTime),
				nameof(Config.AllowCustomBody),
				nameof(Config.RandomizeAppearance),
				nameof(Config.ShowFlagMarkers),
				nameof(Config.ShowOfficers),
				nameof(Config.ShowWeaponTrail),
				nameof(Config.KillFeedEnabled),
				nameof(Config.OfficerHPMultip)
			};
		}
	}
}