using Alliance.Common.GameModes.Story.Models;
using Alliance.Server.GameModes.Story.Behaviors;
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
                //Harmony.Patch(
                //    typeof(MultiplayerRoundController).GetMethod("BeginNewRound",
                //        BindingFlags.Instance | BindingFlags.NonPublic),
                //    prefix: new HarmonyMethod(typeof(Patch_MultiplayerRoundController).GetMethod(
                //        nameof(Prefix_BeginNewRound), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerRoundController)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        private static DateTime lastCheck = DateTime.MinValue;

        // Wait for at least one player or bot in each team before starting the game
        public static bool Prefix_CheckForNewRound(MultiplayerRoundController __instance, MissionMultiplayerGameModeBase ____gameModeServer)
        {
            // Only do the check once every second to not spam messages.
            if (DateTime.Now <= lastCheck.AddSeconds(1))
            {
                return false;
            }
            lastCheck = DateTime.Now;


            if (__instance.CurrentRoundState == MultiplayerRoundState.WaitingForPlayers || ____gameModeServer.TimerComponent.CheckIfTimerPassed())
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

                if (array.Sum() < MultiplayerOptions.OptionType.MinNumberOfPlayersForMatchStart.GetIntValue() && __instance.RoundCount == 0)
                {
                    typeof(MultiplayerRoundController).GetProperty("IsMatchEnding")
                        .SetValue(__instance, true);
                    return false;
                }

                // Check for at least one player or bot in each team
                array[(int)BattleSideEnum.Attacker] += MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
                array[(int)BattleSideEnum.Defender] += MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
                if (array[(int)BattleSideEnum.Defender] >= 1 && array[(int)BattleSideEnum.Attacker] >= 1)
                {
                    return true;
                }

                SendMessageToAll($"Waiting for players to join... ({array[(int)BattleSideEnum.Defender]} defenders VS {array[(int)BattleSideEnum.Attacker]} attackers)");
            }

            return false;
        }

        // Prevent BeginNewRound from calling TeamSelectComponent and causing a NullPointerException (we removed it duh)
        // Also wait for players to load before starting the round
        public static bool Prefix_BeginNewRound(MultiplayerRoundController __instance)
        {
            // If we are in scenario and waiting for players, do not begin round yet
            ScenarioBehavior scenario = Mission.Current.GetMissionBehavior<ScenarioBehavior>();
            if (scenario != null && scenario.State == ActState.AwaitingPlayerJoin)
            {
                //Log("PvC - Skipped BeginNewRound (waiting for players)", 0, Debug.DebugColor.Purple);
                // Return false to skip original method
                return false;
            }


            MissionMultiplayerGameModeBase _gameModeServer = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();

            if (__instance.CurrentRoundState == MultiplayerRoundState.WaitingForPlayers)
            {
                _gameModeServer.ClearPeerCounts();
            }

            //ChangeRoundState(MultiplayerRoundState.Preparation);
            typeof(MultiplayerRoundController).GetMethod("ChangeRoundState",
                        BindingFlags.Instance | BindingFlags.NonPublic)?
                        .Invoke(__instance, new object[] { MultiplayerRoundState.Preparation });

            __instance.RoundCount++;
            Mission.Current.ResetMission();
            //_gameModeServer.MultiplayerTeamSelectComponent.BalanceTeams(); << Removed this            
            _gameModeServer.TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue());

            //__instance.OnRoundStarted?.Invoke();
            // Reflection way of invoking an event (not sure of this black magic)
            var eventField = typeof(MultiplayerRoundController).GetEvent(nameof(MultiplayerRoundController.OnRoundStarted), BindingFlags.Instance | BindingFlags.Public);
            FieldInfo fi = __instance.GetType().GetField(eventField.Name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            Delegate del = (Delegate)fi.GetValue(__instance);
            var list = del?.GetInvocationList();
            if (list != null)
            {
                foreach (var invocationMethod in list)
                {
                    invocationMethod.DynamicInvoke(new object[] { });
                }
            }

            _gameModeServer.SpawnComponent.ToggleUpdatingSpawnEquipment(canUpdate: true);

            // Return false to skip original method
            return false;
        }

        /* Original method
         * 
        private void BeginNewRound()
        {
            if (CurrentRoundState == MultiplayerRoundState.WaitingForPlayers)
            {
                _gameModeServer.ClearPeerCounts();
            }

            ChangeRoundState(MultiplayerRoundState.Preparation);
            RoundCount++;
            Mission.Current.ResetMission();
            _gameModeServer.MultiplayerTeamSelectComponent.BalanceTeams();
            _gameModeServer.TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue());
            this.OnRoundStarted?.Invoke();
            _gameModeServer.SpawnComponent.ToggleUpdatingSpawnEquipment(canUpdate: true);
        }*/
    }
}
