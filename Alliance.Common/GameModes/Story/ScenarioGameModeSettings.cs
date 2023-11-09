using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Utilities;
using System.Collections.Generic;
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

        public override List<string> GetAvailableMaps()
        {
            return SceneList.Scenes;
        }

        public override List<OptionType> GetAvailableNativeOptions()
        {
            return new List<OptionType>
            {
                OptionType.CultureTeam1,
                OptionType.CultureTeam2,
                OptionType.NumberOfBotsTeam1,
                OptionType.NumberOfBotsTeam2,
                OptionType.RoundPreparationTimeLimit,
                OptionType.MapTimeLimit,
                OptionType.FriendlyFireDamageMeleeFriendPercent,
                OptionType.FriendlyFireDamageMeleeSelfPercent,
                OptionType.FriendlyFireDamageRangedFriendPercent,
                OptionType.FriendlyFireDamageRangedSelfPercent
            };
        }

        public override List<string> GetAvailableModOptions()
        {
            return new List<string>
            {
                nameof(Config.ActivateSAE),
                nameof(Config.SAERange),
                nameof(Config.AllowSpawnInRound),
                nameof(Config.FreeRespawnTimer),
                nameof(Config.AllowCustomBody),
                nameof(Config.RandomizeAppearance),
                nameof(Config.ShowFlagMarkers),
                nameof(Config.ShowScore),
                nameof(Config.ShowOfficers),
                nameof(Config.ShowWeaponTrail),
                nameof(Config.KillFeedEnabled),
                nameof(Config.UseTroopCost),
                nameof(Config.StartingGold),
                nameof(Config.MinTroopCost),
                nameof(Config.MaxTroopCost),
                nameof(Config.GoldPerKill),
                nameof(Config.GoldPerAssist),
                nameof(Config.GoldPerAllyDead),
                nameof(Config.UseTroopLimit),
                nameof(Config.CommanderSide),
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
                nameof(Config.MeleeDebuffRambo),
                nameof(Config.DistDebuffRambo),
                nameof(Config.AccDebuffRambo),
                nameof(Config.MeleeDebuffSkirm),
                nameof(Config.DistDebuffSkirm),
                nameof(Config.AccDebuffSkirm),
                nameof(Config.OfficerHPMultip)
            };
        }
    }
}