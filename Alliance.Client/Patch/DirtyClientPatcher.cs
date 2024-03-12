using Alliance.Client.Patch.HarmonyPatch;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch
{
    /// <summary>
    /// This class is responsible for applying Harmony patches and fixing native code in "roundabout" ways.
    /// Must be used as last resort.    
    /// </summary>
    public static class DirtyClientPatcher
    {
        public static bool Patch()
        {
            bool patchSuccess = true;

            //TODO : Following 1.2 -> Check if any patch can be removed
            //patchSuccess &= Patch_MissionMultiplayerGameModeFlagDominationClient.Patch();
            patchSuccess &= Patch_KeyBinder.Patch();
            patchSuccess &= Patch_MissionNetworkComponent.Patch();
            patchSuccess &= Patch_DefaultAdminPanelOptionProvider.Patch();


            if (patchSuccess) Log(SubModule.ModuleId + " - Patches successful", LogLevel.Information);
            return patchSuccess;
        }
    }
}
