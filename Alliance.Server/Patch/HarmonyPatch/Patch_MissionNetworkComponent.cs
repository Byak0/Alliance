using Alliance.Common.Extensions.TroopSpawner.Models;
using HarmonyLib;
using NetworkMessages.FromClient;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionNetworkComponent
    {
        private static readonly Harmony Harmony = new Harmony(nameof(Patch_MissionNetworkComponent));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionNetworkComponent).GetMethod("HandleClientEventApplyOrderWithFormationAndNumber",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionNetworkComponent).GetMethod(
                        nameof(Prefix_HandleClientEventApplyOrderWithFormationAndNumber), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionNetworkComponent)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Fix for transfer order in multiplayer
        public static bool Prefix_HandleClientEventApplyOrderWithFormationAndNumber(MissionNetworkComponent __instance, ref bool __result, NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
        {
            ApplyOrderWithFormationAndNumber message = (ApplyOrderWithFormationAndNumber)baseMessage;
            Team teamOfPeer = (Team)(typeof(MissionNetworkComponent).GetMethod("GetTeamOfPeer",
                        BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(__instance, new object[] { networkPeer }));
            OrderController orderController = teamOfPeer != null ? teamOfPeer.GetOrderControllerOf(networkPeer.ControlledAgent) : null;

            // Remove check on CountOfUnits to allow transfer to empty formations
            Formation formation = teamOfPeer != null ? teamOfPeer.FormationsIncludingEmpty.SingleOrDefault((f) => /*f.CountOfUnits > 0 && */f.Index == message.FormationIndex) : null;

            // Give control to player if formation was empty
            if (formation.CountOfUnits == 0)
            {
                Log($"Formation {formation.Index} was empty, giving control to {networkPeer.UserName}", LogLevel.Information);
                FormationControlModel.Instance.AssignControlToPlayer(networkPeer.GetComponent<MissionPeer>(), formation.FormationIndex, true);
            }

            int number = message.Number;
            if (teamOfPeer != null && orderController != null && formation != null)
            {
                orderController.SetOrderWithFormationAndNumber(message.OrderType, formation, number);
            }
            __result = true;

            // Return false to skip original method
            return false;
        }



        // Original method
        //private bool HandleClientEventApplyOrderWithFormationAndNumber(NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
        //{
        //    ApplyOrderWithFormationAndNumber message = (ApplyOrderWithFormationAndNumber)baseMessage;
        //    Team teamOfPeer = this.GetTeamOfPeer(networkPeer);
        //    OrderController orderController = ((teamOfPeer != null) ? teamOfPeer.GetOrderControllerOf(networkPeer.ControlledAgent) : null);
        //    Formation formation = ((teamOfPeer != null) ? teamOfPeer.FormationsIncludingEmpty.SingleOrDefault((Formation f) => f.CountOfUnits > 0 && f.Index == message.FormationIndex) : null);
        //    int number = message.Number;
        //    if (teamOfPeer != null && orderController != null && formation != null)
        //    {
        //        orderController.SetOrderWithFormationAndNumber(message.OrderType, formation, number);
        //    }
        //    return true;
        //}
    }
}
