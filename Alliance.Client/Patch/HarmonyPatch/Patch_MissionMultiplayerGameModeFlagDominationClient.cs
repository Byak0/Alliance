using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_MissionMultiplayerGameModeFlagDominationClient
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionMultiplayerGameModeFlagDominationClient));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionMultiplayerGameModeFlagDominationClient).GetMethod(nameof(MissionMultiplayerGameModeFlagDominationClient.OnBehaviorInitialize),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionMultiplayerGameModeFlagDominationClient).GetMethod(
                        nameof(Prefix_OnBehaviorInitialize), BindingFlags.Static | BindingFlags.Public)));
                Harmony.ReversePatch(
                    typeof(MissionMultiplayerGameModeBaseClient).GetMethod(
                        nameof(MissionMultiplayerGameModeBaseClient.OnBehaviorInitialize), BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(Patch_MissionMultiplayerGameModeBaseClient).GetMethod(nameof(Patch_MissionMultiplayerGameModeBaseClient.OnBehaviorInitialize))));

            }
            catch (Exception e)
            {
                Log("Alliance - ERROR in " + nameof(Patch_MissionMultiplayerGameModeFlagDominationClient), LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Replace original OnBehaviorInitialize to prevent crash when capture points > 3 (ie in siege)
        public static bool Prefix_OnBehaviorInitialize(MissionMultiplayerGameModeFlagDominationClient __instance,
                                                       ref MissionScoreboardComponent ____scoreboardComponent,
                                                       ref MissionLobbyComponent.MultiplayerGameType ____currentGameType,
                                                       ref Team[] ____capturePointOwners)
        {
            Patch_MissionMultiplayerGameModeBaseClient.OnBehaviorInitialize(__instance);

            ____scoreboardComponent = Mission.Current.GetMissionBehavior<MissionScoreboardComponent>();

            if (MultiplayerOptions.OptionType.SingleSpawn.GetBoolValue())
            {
                ____currentGameType = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() > 0 ? MissionLobbyComponent.MultiplayerGameType.Captain : MissionLobbyComponent.MultiplayerGameType.Battle;
            }
            else
            {
                ____currentGameType = MissionLobbyComponent.MultiplayerGameType.Skirmish;
            }

            MethodInfo ResetTeamPowers = __instance.GetType().GetMethod("ResetTeamPowers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (ResetTeamPowers == null) ResetTeamPowers = __instance.GetType().BaseType.GetMethod("ResetTeamPowers", BindingFlags.NonPublic | BindingFlags.Instance);
            ResetTeamPowers.Invoke(__instance, new object[] { 1 });

            typeof(MissionMultiplayerGameModeFlagDominationClient).GetProperty("AllCapturePoints")
                .SetValue(__instance, Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>());

            ____capturePointOwners = new Team[__instance.AllCapturePoints.CountQ()];

            __instance.RoundComponent.OnPreparationEnded += __instance.OnPreparationEnded;

            MethodInfo OnMyClientSynchronized = __instance.GetType().GetMethod("OnMyClientSynchronized", BindingFlags.NonPublic | BindingFlags.Instance);
            if (OnMyClientSynchronized == null) OnMyClientSynchronized = __instance.GetType().BaseType.GetMethod("OnMyClientSynchronized", BindingFlags.NonPublic | BindingFlags.Instance);
            __instance.MissionNetworkComponent.OnMyClientSynchronized += (Action)OnMyClientSynchronized.CreateDelegate(typeof(Action), __instance);

            return false;
        }
    }

    // Reverse patch to access base implementation of Patch_MissionMultiplayerGameModeBaseClient.OnBehaviorInitialize
    class Patch_MissionMultiplayerGameModeBaseClient
    {
        public static void OnBehaviorInitialize(MissionMultiplayerGameModeBaseClient instance)
        {
            Console.WriteLine($"Patch.OnBehaviorInitialize({instance})");
        }
    }
}

