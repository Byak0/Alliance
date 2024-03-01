using Alliance.Server.GameModes.CaptainX.Behaviors;
using Alliance.Server.GameModes.PvC.Behaviors;
using Alliance.Server.GameModes.SiegeX.Behaviors;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_SpawnComponent
    {
        private static readonly Harmony Harmony = new Harmony(nameof(Patch_SpawnComponent));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(SpawnComponent).GetMethod(nameof(SpawnComponent.SetWarmupSpawningBehavior),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_SpawnComponent).GetMethod(
                        nameof(Prefix_SetWarmupSpawningBehavior), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(SpawnComponent).GetMethod(nameof(SpawnComponent.SetSiegeSpawningBehavior),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_SpawnComponent).GetMethod(
                        nameof(Prefix_SetSiegeSpawningBehavior), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(SpawnComponent).GetMethod(nameof(SpawnComponent.SetFlagDominationSpawningBehavior),
                        BindingFlags.Static | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_SpawnComponent).GetMethod(
                        nameof(Prefix_SetFlagDominationSpawningBehavior), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_SpawnComponent)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Call correct spawning behavior when warmup starts
        public static bool Prefix_SetWarmupSpawningBehavior()
        {
            Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new PvCFFASpawnFrameBehavior());
            Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new PvCWarmupSpawningBehavior());

            // Return false to skip original method
            return false;
        }

        // Call correct spawning behavior when warmup ends
        public static bool Prefix_SetSiegeSpawningBehavior()
        {
            string gameType = MultiplayerOptions.OptionType.GameType.GetStrValue();
            if (gameType == "SiegeX")
            {
                Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new SiegeXSpawnFrameBehavior());
                Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new SiegeXSpawningBehavior());
                // Return false to skip original method
                return false;
            }
            else
            {
                return true;
            }
        }

        // Call correct spawning behavior when warmup ends
        public static bool Prefix_SetFlagDominationSpawningBehavior()
        {
            string gameType = MultiplayerOptions.OptionType.GameType.GetStrValue();
            if (gameType == "CaptainX")
            {
                Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new PvCFlagDominationSpawnFrameBehavior());
                Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new PvCFlagDominationSpawningBehavior());
                // Return false to skip original method
                return false;
            }
            else if (gameType == "BattleX" || gameType == "PvC" || gameType == "CvC")
            {
                Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new PvCSpawnFrameBehavior());
                Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new PvCSpawningBehavior());
                // Return false to skip original method
                return false;
            }
            else
            {
                return true;
            }
        }

        /* Original method
         * 
		public static void SetFlagDominationSpawningBehavior()
		{
			Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawnFrameBehavior(new FlagDominationSpawnFrameBehavior());
			Mission.Current.GetMissionBehavior<SpawnComponent>().SetNewSpawningBehavior(new FlagDominationSpawningBehavior());
		}*/
    }
}
