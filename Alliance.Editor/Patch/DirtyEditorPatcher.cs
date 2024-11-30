using Alliance.Common.Patch.HarmonyPatch;
using Alliance.Editor.Patch.HarmonyPatch;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Editor.Patch
{
	/// <summary>
	/// This class is responsible for applying Harmony patches and fixing native code in "roundabout" ways.
	/// Must be used as last resort.
	/// </summary>
	public static class DirtyEditorPatcher
	{
		public static bool Patch()
		{
			bool patchSuccess = true;

			patchSuccess &= Patch_CustomBattleData.Patch();
			patchSuccess &= Patch_BannerlordConfig.Patch();
			patchSuccess &= Patch_BasicCultureObject.Patch();
			patchSuccess &= Patch_LessUselessErrors.Patch();
			patchSuccess &= Patch_BodyGeneratorView.Patch();

			if (patchSuccess) Log(SubModule.ModuleId + " - Patches successful", LogLevel.Information);
			return patchSuccess;
		}
	}
}
