using Alliance.Common.Core.Configuration.Models;
using System.Collections.Generic;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.PvC
{
    public class PvCGameModeSettings : GameModeSettings
    {
        public PvCGameModeSettings() : base("PvC", "Players VS Commanders", "A team of players face of against a team of commanders controlling bots.")
        {
        }

        public override void SetDefaultNativeOptions()
        {
            base.SetDefaultNativeOptions();
            SetNativeOption(OptionType.RoundPreparationTimeLimit, 40);
            SetNativeOption(OptionType.RoundTimeLimit, 1200);
            SetNativeOption(OptionType.RoundTotal, 3);
            SetNativeOption(OptionType.FriendlyFireDamageMeleeFriendPercent, 75);
            SetNativeOption(OptionType.FriendlyFireDamageMeleeSelfPercent, 0);
            SetNativeOption(OptionType.FriendlyFireDamageRangedFriendPercent, 75);
            SetNativeOption(OptionType.FriendlyFireDamageRangedSelfPercent, 0);
        }

        public override void SetDefaultModOptions()
        {
            base.SetDefaultModOptions();
            ModOptions = Config.Instance;
            ModOptions.ActivateSAE = true;
            ModOptions.SAERange = 50;
            ModOptions.TimeBeforeFlagRemoval = 450;
            ModOptions.MoraleMultiplierForFlag = 1f;
            ModOptions.MoraleMultiplierForLastFlag = 1f;
            ModOptions.UseTroopCost = true;
            ModOptions.UseTroopLimit = false;
            ModOptions.GoldMultiplier = 3f;
            ModOptions.StartingGold = 0;
            ModOptions.GoldPerKill = 0;
            ModOptions.GoldPerAssist = 0;
            ModOptions.GoldPerAllyDead = 0;
            ModOptions.AllowSpawnInRound = false;
            ModOptions.ShowFlagMarkers = true;
            ModOptions.ShowScore = true;
            ModOptions.ShowOfficers = true;
        }

        public override List<string> GetAvailableMaps()
        {
            return base.GetAvailableMaps();
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
                OptionType.RoundTimeLimit,
                OptionType.RoundTotal,
                OptionType.WarmupTimeLimit,
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
                nameof(Config.AllowSpawnInRound),
                nameof(Config.FreeRespawnTimer),
                nameof(Config.AllowCustomBody),
                nameof(Config.RandomizeAppearance),
                nameof(Config.ShowFlagMarkers),
                nameof(Config.ShowScore),
                nameof(Config.ShowOfficers),
                nameof(Config.ShowWeaponTrail),
                nameof(Config.KillFeedEnabled),
                nameof(Config.TimeBeforeFlagRemoval),
                nameof(Config.MoraleMultiplierForFlag),
                nameof(Config.MoraleMultiplierForLastFlag),
                nameof(Config.UseTroopCost),
                nameof(Config.GoldMultiplier),
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
                nameof(Config.OfficerHPMultip),
                nameof(Config.ActivateSAE),
                nameof(Config.SAERange)
            };
        }
    }
}