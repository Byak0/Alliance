﻿using System.Collections.Generic;
using System.Linq;
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
            return base.GetAvailableMaps().Where(scene => scene.HasSpawnForAttacker && scene.HasSpawnForDefender).ToList();
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
            return base.GetAvailableModOptions();
        }
    }
}