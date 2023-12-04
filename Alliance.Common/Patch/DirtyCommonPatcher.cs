using Alliance.Common.Patch.HarmonyPatch;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch
{
    /// <summary>
    /// This class is responsible for applying Harmony patches and fixing native code in "roundabout" ways.
    /// Must be used as last resort.
    /// </summary>
    public static class DirtyCommonPatcher
    {
        public static void IncreaseNativeLimits()
        {
            // Increase the limit for number of bots in captain (255 to 16384)            
            typeof(CompressionMission).GetField(nameof(CompressionMission.AgentOffsetCompressionInfo)).
                SetValue(null, new CompressionInfo.Integer(0, 16384, true));

            // Increase map time limit (in minutes)
            typeof(CompressionBasic).GetField(nameof(CompressionBasic.MapTimeLimitCompressionInfo)).
                SetValue(null, new CompressionInfo.Integer(0, 360, true));

            // Increase network message size for round time (in seconds)
            typeof(CompressionMission).GetField(nameof(CompressionMission.RoundTimeCompressionInfo)).
                SetValue(null, new CompressionInfo.Integer(0, 3600, true));

            // Increase formation max
            typeof(CompressionBasic).GetField(nameof(CompressionBasic.NumberOfBotsPerFormationCompressionInfo)).
                SetValue(null, new CompressionInfo.Integer(0, 4096, true));

            // Increase gold max
            typeof(CompressionBasic).GetField(nameof(CompressionBasic.RoundGoldAmountCompressionInfo)).
                SetValue(null, new CompressionInfo.Integer(-1, 50000, true));

            // TODO : Check if still necessary with 1.2 optimisation
            // Fix native lag when lot of arrows
            //typeof(CompressionBasic).GetField(nameof(CompressionBasic.BigRangeLowResLocalPositionCompressionInfo), BindingFlags.Static).
            //    SetValue(null, new CompressionInfo.Float(-2000f, 2000f, 16));

            // Test :))) Increase max number of players
            //typeof(CompressionBasic).GetField(nameof(CompressionBasic.PlayerCompressionInfo), BindingFlags.Static).
            //    SetValue(null, new CompressionInfo.Integer(-1, 20000, true));
        }

        public static bool Patch()
        {
            bool patchSuccess = true;
            //patchSuccess &= Patch_MultiplayerTeamSelectComponent.Patch();
            patchSuccess &= Patch_MultiplayerOptionsImmediate.Patch();
            patchSuccess &= Patch_MultiplayerOptionsInitial.Patch();
            //patchSuccess &= Patch_MultiplayerClassDivisions.Patch();
            if (patchSuccess) Log(SubModule.ModuleId + " - Patches successful", LogLevel.Information);
            return patchSuccess;
        }
    }
}
