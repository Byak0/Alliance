using Alliance.Common.Core.Configuration.Models;
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
			return new List<OptionType>
			{
				OptionType.CultureTeam1,
				OptionType.CultureTeam2,
				OptionType.NumberOfBotsTeam1,
				OptionType.NumberOfBotsTeam2,
				OptionType.AutoTeamBalanceThreshold,
				OptionType.FriendlyFireDamageMeleeFriendPercent,
				OptionType.FriendlyFireDamageMeleeSelfPercent,
				OptionType.FriendlyFireDamageRangedFriendPercent,
				OptionType.FriendlyFireDamageRangedSelfPercent
			};
		}

		public override List<string> GetAvailableModOptions()
		{
			// Otherwise return only following options
			return new List<string>
			{
				nameof(Config.AllowCustomBody),
				nameof(Config.RandomizeAppearance),
				nameof(Config.ShowFlagMarkers),
				nameof(Config.ShowOfficers),
				nameof(Config.ShowWeaponTrail),
				nameof(Config.KillFeedEnabled),
				nameof(Config.PlayerHPMultiplier),
				nameof(Config.BotHPMultiplier),
				nameof(Config.ActivateSAE),
				nameof(Config.SAERange),
				nameof(Config.BotDifficulty),
				nameof(Config.EnableFormation),
				nameof(Config.MinPlayer),
				nameof(Config.MaxPlayer),
				nameof(Config.FormRadMin),
				nameof(Config.FormRadMax),
				nameof(Config.SkirmRadMin),
				nameof(Config.SkirmRadMax),
				nameof(Config.NbFormMin),
				nameof(Config.NbFormMax),
				nameof(Config.NbSkirmMin),
				nameof(Config.NbSkirmMax),
				nameof(Config.MinPlayerForm),
				nameof(Config.CavFormationRadiusMultiplier),
				nameof(Config.RangedFormationRadiusMultiplier),
				nameof(Config.CavSkirmishRadiusMultiplier),
				nameof(Config.RangedSkirmishRadiusMultiplier),
				nameof(Config.MeleeDebuffRambo),
				nameof(Config.DistDebuffRambo),
				nameof(Config.AccDebuffRambo),
				nameof(Config.MeleeDebuffSkirm),
				nameof(Config.DistDebuffSkirm),
				nameof(Config.AccDebuffSkirm),
				nameof(Config.MeleeDebuffForm),
				nameof(Config.DistDebuffForm),
				nameof(Config.AccDebuffForm),
			};
		}
	}
}