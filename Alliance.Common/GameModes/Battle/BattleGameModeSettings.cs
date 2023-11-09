using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Utilities;
using System.Collections.Generic;
using System.Linq;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.Battle
{
    public class BattleGameModeSettings : GameModeSettings
    {
        public BattleGameModeSettings() : base("BattleX", "Battle", "Battle mode.")
        {
        }

        public override void SetDefaultNativeOptions()
        {
            base.SetDefaultNativeOptions();
            SetNativeOption(OptionType.NumberOfBotsPerFormation, 0);
        }

        public override void SetDefaultModOptions()
        {
            base.SetDefaultModOptions();
        }

        public override List<string> GetAvailableMaps()
        {
            return SceneList.Scenes
                    .Where(scene => new[] { "battle_", "tdm_", "skirmish_", "captain_", "sergeant_" }
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
                OptionType.UnlimitedGold,
                OptionType.FriendlyFireDamageMeleeFriendPercent,
                OptionType.FriendlyFireDamageMeleeSelfPercent,
                OptionType.FriendlyFireDamageRangedFriendPercent,
                OptionType.FriendlyFireDamageRangedSelfPercent,
            };
        }

        public override List<string> GetAvailableModOptions()
        {
            return new List<string>
            {
                nameof(Config.AllowCustomBody),
                nameof(Config.RandomizeAppearance),
                nameof(Config.ShowFlagMarkers),
                nameof(Config.ShowScore),
                nameof(Config.ShowOfficers),
                nameof(Config.ShowWeaponTrail),
                nameof(Config.KillFeedEnabled),
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