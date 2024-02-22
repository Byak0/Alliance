using System.Collections.Generic;
using System.Linq;
using static Alliance.Common.Utilities.SceneList;
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

        public override List<SceneInfo> GetAvailableMaps()
        {
            return base.GetAvailableMaps().Where(scene => scene.HasSpawnForAttacker && scene.HasSpawnForDefender && scene.HasSpawnVisual).ToList();
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
            return base.GetAvailableModOptions();
        }
    }
}