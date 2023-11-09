using Alliance.Common.GameModes.Story.Models;
using Alliance.Server.GameModes.Story.Behaviors;
using HarmonyLib;
using System;
using System.Reflection;
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
                    typeof(MultiplayerRoundController).GetMethod("BeginNewRound",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerRoundController).GetMethod(
                        nameof(Prefix_BeginNewRound), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerRoundController)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
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
