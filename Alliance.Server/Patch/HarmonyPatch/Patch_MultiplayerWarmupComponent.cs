using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MultiplayerWarmupComponent
    {
        private static readonly Harmony Harmony = new Harmony(nameof(Patch_MultiplayerWarmupComponent));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MultiplayerWarmupComponent).GetMethod(nameof(MultiplayerWarmupComponent.CanMatchStartAfterWarmup),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerWarmupComponent).GetMethod(
                        nameof(Prefix_CanMatchStartAfterWarmup), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log("Alliance - ERROR in {nameof(Patch_MultiplayerWarmupComponent)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Allow match start even if not all teams has players
        public static bool Prefix_CanMatchStartAfterWarmup(ref bool __result)
        {
            int[] array = new int[2];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (networkPeer.IsSynchronized && component?.Team != null && component.Team.Side != BattleSideEnum.None)
                {
                    array[(int)component.Team.Side]++;
                }
            }
            if (array.Sum() >= MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions))
            {
                __result = true;
            }
            else
            {
                __result = false;
            }

            // Return false to skip original method
            return false;
        }

        /* Original method
         * 
        public bool CanMatchStartAfterWarmup()
        {
            bool[] array = new bool[2];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
                {
                    array[(int)component.Team.Side] = true;
                }

                if (array[1] && array[0])
                {
                    return true;
                }
            }

            return false;
        }*/
    }
}
