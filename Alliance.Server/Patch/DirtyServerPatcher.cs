using Alliance.Server.Patch.Behaviors;
using Alliance.Server.Patch.HarmonyPatch;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch
{
    /// <summary>
    /// This class is responsible for applying Harmony patches and fixing native code in "roundabout" ways.
    /// Must be used as last resort.    
    /// </summary>
    public static class DirtyServerPatcher
    {
        public static bool Patch()
        {
            bool patchSuccess = true;
            patchSuccess &= Patch_MissionPeer.Patch();
            //patchSuccess &= Patch_MissionLobbyComponent.Patch();
            //patchSuccess &= Patch_MissionMultiplayerFlagDomination.Patch();
            //patchSuccess &= Patch_MissionMultiplayerGameModeBase.Patch();
            //patchSuccess &= Patch_SpawnedItemEntity.Patch();
            //patchSuccess &= Patch_MultiplayerRoundController.Patch();
            //patchSuccess &= Patch_SpawnComponent.Patch();
            //patchSuccess &= Patch_MultiplayerWarmupComponent.Patch();
            //patchSuccess &= Patch_MultiplayerTeamSelectComponent.Patch();
            //patchSuccess &= Patch_MissionNetworkComponent.Patch();
            if (patchSuccess) Log(SubModule.ModuleId + " - Patches successful", LogLevel.Information);
            return patchSuccess;
        }

        public static void AddFixBehaviors(Mission mission)
        {
            mission.AddMissionBehavior(new NotAllPlayersJoinFixBehavior());
        }
    }
}
