using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Utilities;
using System.Collections.Generic;
using System.Linq;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.CvC
{
    public class CvCGameModeSettings : GameModeSettings
    {
        public CvCGameModeSettings() : base("CvC", "Commanders VS Commanders", "Two armies fight each other.")
        {
        }

        public override void SetDefaultNativeOptions()
        {
            base.SetDefaultNativeOptions();
            SetNativeOption(OptionType.RoundPreparationTimeLimit, 30);
            SetNativeOption(OptionType.RoundTimeLimit, 1200);
            SetNativeOption(OptionType.RoundTotal, 3);
            SetNativeOption(OptionType.FriendlyFireDamageMeleeFriendPercent, 75);
            SetNativeOption(OptionType.FriendlyFireDamageMeleeSelfPercent, 0);
            SetNativeOption(OptionType.FriendlyFireDamageRangedFriendPercent, 75);
            SetNativeOption(OptionType.FriendlyFireDamageRangedSelfPercent, 0);
            SetNativeOption(OptionType.AutoTeamBalanceThreshold, 1);
        }

        public override void SetDefaultModOptions()
        {
            base.SetDefaultModOptions();
            ModOptions = Config.Instance;
            ModOptions.ActivateSAE = true;
            ModOptions.SAERange = 50;
            ModOptions.EnableFormation = false;
            ModOptions.TimeBeforeFlagRemoval = 450;
            ModOptions.MoraleMultiplierForFlag = 1f;
            ModOptions.MoraleMultiplierForLastFlag = 1f;
            ModOptions.UseTroopCost = true;
            ModOptions.UseTroopLimit = false;
            ModOptions.GoldMultiplier = 0f;
            ModOptions.StartingGold = 12000;
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
            return SceneList.Scenes
                    .Where(scene => new[] { "captain_", "sergeant_", "pvc", "cvc" }
                    .Any(prefix => scene.Contains(prefix)))
                    .ToList();
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
                OptionType.FriendlyFireDamageRangedFriendPercent
            };
        }

        public override List<string> GetAvailableModOptions()
        {
            return new List<string>
            {
                nameof(Config.ActivateSAE),
                nameof(Config.SAERange),
                nameof(Config.EnableFormation),
                nameof(Config.StartingGold),
                nameof(Config.TimeBeforeFlagRemoval),
                nameof(Config.MoraleMultiplierForFlag),
                nameof(Config.MoraleMultiplierForLastFlag)
            };
        }
    }
}