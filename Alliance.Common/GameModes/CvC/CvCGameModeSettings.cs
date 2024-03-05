using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.SceneList;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Common.GameModes.CvC
{
    public class CvCGameModeSettings : GameModeSettings
    {
        // Don't show these maps in CvC
        public static List<string> InvalidMaps = new List<string>() { "helms_deep", "bilbo" };

        public CvCGameModeSettings() : base("CvC", "Commanders VS Commanders", "Two armies fight each other.")
        {
        }

        public override void SetDefaultNativeOptions()
        {
            base.SetDefaultNativeOptions();
            SetNativeOption(OptionType.RoundPreparationTimeLimit, 30);
            SetNativeOption(OptionType.RoundTimeLimit, 1200);
            SetNativeOption(OptionType.RoundTotal, 3);
            SetNativeOption(OptionType.NumberOfBotsTeam1, 0);
            SetNativeOption(OptionType.NumberOfBotsTeam2, 0);
            SetNativeOption(OptionType.NumberOfBotsPerFormation, 0);
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
            ModOptions.TimeBeforeFlagRemoval = 300;
            ModOptions.MoraleMultiplierForFlag = 1f;
            ModOptions.MoraleMultiplierForLastFlag = 1f;
            ModOptions.UseTroopCost = true;
            ModOptions.UseTroopLimit = false;
            ModOptions.GoldMultiplier = 0f;
            ModOptions.StartingGold = 5000;
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
            return base.GetAvailableMaps().Where(scene => scene.Module != "Native" && InvalidMaps.All(str => !scene.Name.Contains(str)) && scene.HasSpawnForAttacker && scene.HasSpawnForDefender && scene.HasSpawnVisual && scene.HasNavmesh).ToList();
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
                OptionType.FriendlyFireDamageMeleeFriendPercent,
                OptionType.FriendlyFireDamageRangedFriendPercent
            };
        }

        public override List<string> GetAvailableModOptions()
        {
            // Return full list of options for admins
            if (GameNetwork.MyPeer.IsAdmin())
            {
                return base.GetAvailableModOptions();
            }
            // Otherwise return only following options
            return new List<string>
            {
                nameof(Config.ActivateSAE),
                nameof(Config.SAERange),
                nameof(Config.EnableFormation),
                nameof(Config.BotDifficulty),
                nameof(Config.StartingGold),
                nameof(Config.TimeBeforeFlagRemoval),
                nameof(Config.MoraleMultiplierForFlag),
                nameof(Config.MoraleMultiplierForLastFlag)
            };
        }
    }
}