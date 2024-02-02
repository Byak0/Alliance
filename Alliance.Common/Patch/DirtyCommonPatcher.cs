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
        public const int MAX_MISSION_OBJECTS = 10000;

        public static void IncreaseNativeLimits()
        {
            // Increase the limit for number of bots in captain (255 to 16384)
            CompressionMission.AgentOffsetCompressionInfo = new CompressionInfo.Integer(0, 16384, true);

            // Increase map time limit (in minutes)
            CompressionBasic.MapTimeLimitCompressionInfo = new CompressionInfo.Integer(0, 360, true);

            // Increase network message size for round time (in seconds)
            CompressionMission.RoundTimeCompressionInfo = new CompressionInfo.Integer(0, 3600, true);

            // Increase formation max
            CompressionBasic.NumberOfBotsPerFormationCompressionInfo = new CompressionInfo.Integer(0, 2048, true);

            // Increase gold max
            CompressionBasic.RoundGoldAmountCompressionInfo = new CompressionInfo.Integer(-1, 50000, true);

            // Increase max number of mission object
            CompressionBasic.MissionObjectIDCompressionInfo = new CompressionInfo.Integer(-1, MAX_MISSION_OBJECTS, maximumValueGiven: true);

            // TODO : Check if still necessary with 1.2 optimisation
            // Fix native lag when lot of arrows
            //typeof(CompressionBasic).GetField(nameof(CompressionBasic.BigRangeLowResLocalPositionCompressionInfo), BindingFlags.Static).
            //    SetValue(null, new CompressionInfo.Float(-2000f, 2000f, 16));

            // (For testing purpose only) Increase max number of players on server
            //typeof(CompressionBasic).GetField(nameof(CompressionBasic.PlayerCompressionInfo), BindingFlags.Static).
            //    SetValue(null, new CompressionInfo.Integer(-1, 20000, true));
        }

        public static bool Patch()
        {
            bool patchSuccess = true;

            //TODO : Following 1.2 -> Check if any patch can be removed
            //patchSuccess &= Patch_MultiplayerTeamSelectComponent.Patch();
            patchSuccess &= Patch_MultiplayerOptionsImmediate.Patch();
            patchSuccess &= Patch_MultiplayerOptionsInitial.Patch();
            patchSuccess &= Patch_MultiplayerClassDivisions.Patch();

            if (patchSuccess) Log(SubModule.ModuleId + " - Patches successful", LogLevel.Information);
            return patchSuccess;
        }
    }
}
