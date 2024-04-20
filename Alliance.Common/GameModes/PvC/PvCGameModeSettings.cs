using System.Collections.Generic;
using System.Linq;
using static Alliance.Common.Utilities.SceneList;
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
            ModOptions.ActivateSAE = true;
            ModOptions.SAERange = 50;
            ModOptions.TimeBeforeFlagRemoval = 300;
            ModOptions.MoraleMultiplierForFlag = 1f;
            ModOptions.MoraleMultiplierForLastFlag = 1f;
            ModOptions.UseTroopCost = true;
            ModOptions.UseTroopLimit = false;
            ModOptions.GoldMultiplier = 1.3f;
            ModOptions.StartingGold = 0;
            ModOptions.GoldPerKill = 0;
            ModOptions.GoldPerAssist = 0;
            ModOptions.GoldPerAllyDead = 0;
            ModOptions.AllowSpawnInRound = false;
            ModOptions.ShowFlagMarkers = true;
            ModOptions.ShowScore = true;
            ModOptions.ShowOfficers = true;
        }

        public override List<SceneInfo> GetAvailableMaps()
        {
            return base.GetAvailableMaps().Where(scene => scene.HasSpawnForAttacker && scene.HasSpawnForDefender && scene.HasSpawnVisual && scene.HasNavmesh).ToList();
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
            return base.GetAvailableModOptions();
        }
    }
}