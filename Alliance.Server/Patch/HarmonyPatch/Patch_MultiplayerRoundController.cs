using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MultiplayerRoundController
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MultiplayerRoundController));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MultiplayerRoundController).GetMethod("CheckForNewRound",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerRoundController).GetMethod(
                        nameof(Prefix_CheckForNewRound), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MultiplayerRoundController).GetMethod("HasEnoughCharactersOnBothSides",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerRoundController).GetMethod(
                        nameof(Prefix_HasEnoughCharactersOnBothSides), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerRoundController)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Start the round even if not all teams have a player (Prevent ending match if alone)
        public static bool Prefix_HasEnoughCharactersOnBothSides(ref bool __result)
        {
            int[] array = new int[2];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (networkPeer.IsSynchronized && component?.Team != null && (component.Team.Side == BattleSideEnum.Attacker || component.Team.Side == BattleSideEnum.Defender))
                {
                    array[(int)component.Team.Side]++;
                }
            }
            __result = array.Any(count => count > 0);

            return false; // Skip original method
        }

        private static DateTime lastCheck = DateTime.MinValue;
        private static DateTime? startWaitTime = null;

        // Wait for at least one player or bot in each team before starting the game
        public static bool Prefix_CheckForNewRound(MultiplayerRoundController __instance, MissionMultiplayerGameModeBase ____gameModeServer, ref bool __result)
        {
            // Only do the check once every second to not spam messages.
            if (DateTime.Now <= lastCheck.AddSeconds(1))
            {
                __result = false;
                return false;
            }
            lastCheck = DateTime.Now;

            // Store number of players per team
            int[] array = new int[2];
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (networkPeer.IsSynchronized && component?.Team != null && (component.Team.Side == BattleSideEnum.Attacker || component.Team.Side == BattleSideEnum.Defender))
                {
                    array[(int)component.Team.Side]++;
                }
            }

            // Initialize or reset the start wait time at the beginning of each round if players are present.
            if ((__instance.CurrentRoundState == MultiplayerRoundState.WaitingForPlayers || ____gameModeServer.TimerComponent.CheckIfTimerPassed()) && array.Any(count => count > 0) && !startWaitTime.HasValue)
            {
                startWaitTime = DateTime.Now;
            }

            if (startWaitTime.HasValue && array.All(count => count == 0))
            {
                SendMessageToAll($"Nobody is ready. Waiting for players...");
                startWaitTime = null; // Reset the start time as no player are ready.
                __result = false;
                return false;
            }

            if (__instance.CurrentRoundState == MultiplayerRoundState.WaitingForPlayers || ____gameModeServer.TimerComponent.CheckIfTimerPassed())
            {
                if (array.Sum() < MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue() && __instance.RoundCount == 0)
                {
                    typeof(MultiplayerRoundController).GetProperty("IsMatchEnding")
                        .SetValue(__instance, true);
                    __result = false;
                    return false;
                }

                // Check for at least one player or bot in each team
                array[(int)BattleSideEnum.Attacker] += MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
                array[(int)BattleSideEnum.Defender] += MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
                if (array[(int)BattleSideEnum.Defender] >= 1 && array[(int)BattleSideEnum.Attacker] >= 1)
                {
                    startWaitTime = null; // Reset the start time as the condition to start the game is met.
                    __result = true; // Proceed with starting the game.
                    return false;
                }
            }

            // Check if the maximum wait time has been exceeded.
            if (startWaitTime.HasValue)
            {
                TimeSpan remainingTime = startWaitTime.Value.AddSeconds(20) - DateTime.Now;
                int remainingSeconds = (int)Math.Max(0, remainingTime.TotalSeconds);

                // Send message with remaining time
                if (remainingSeconds > 0)
                {
                    SendMessageToAll($"Waiting for players to join... ({array[(int)BattleSideEnum.Defender]} defenders VS {array[(int)BattleSideEnum.Attacker]} attackers). Starting in {remainingSeconds} seconds.");
                }
                else
                {
                    SendMessageToAll("Starting the game...");
                    startWaitTime = null; // Reset the start time for the next round.
                    __result = true; // Proceed with starting the game.
                    return false;
                }
            }

            return false;
        }
    }
}
