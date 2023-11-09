using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Reflection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionMultiplayerGameModeBase
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionMultiplayerGameModeBase));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionMultiplayerGameModeBase).GetMethod(nameof(MissionMultiplayerGameModeBase.ChangeCurrentGoldForPeer),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionMultiplayerGameModeBase).GetMethod(
                        nameof(Prefix_ChangeCurrentGoldForPeer), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionMultiplayerGameModeBase)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Remove gold limit on ChangeCurrentGoldForPeer
        public static bool Prefix_ChangeCurrentGoldForPeer(MissionMultiplayerGameModeBase __instance,
                                                           MissionMultiplayerGameModeBaseClient ___GameModeBaseClient,
                                                            MissionPeer peer,
                                                            int newAmount)
        {
            newAmount = MBMath.ClampInt(newAmount, 0, CompressionBasic.RoundGoldAmountCompressionInfo.GetMaximumValue());

            if (peer.Peer.Communicator.IsConnectionActive)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncGoldsForSkirmish(peer.Peer, newAmount));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            if (___GameModeBaseClient != null)
            {
                ___GameModeBaseClient.OnGoldAmountChangedForRepresentative(peer.Representative, newAmount);
            }

            // Skip original method
            return false;
        }


        /* Original method 
         * 		
        public void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
        {
            if (newAmount >= 0)
            {
                newAmount = MBMath.ClampInt(newAmount, 0, 2000);
            }

            if (peer.Peer.Communicator.IsConnectionActive)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncGoldsForSkirmish(peer.Peer, newAmount));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            if (GameModeBaseClient != null)
            {
                GameModeBaseClient.OnGoldAmountChangedForRepresentative(peer.Representative, newAmount);
            }
        }*/
    }
}
