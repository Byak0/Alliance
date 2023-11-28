using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Common.Extensions.TroopSpawner.Models;
using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_MissionMultiplayerFlagDomination
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionMultiplayerFlagDomination));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionMultiplayerFlagDomination).GetMethod("TickFlags",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionMultiplayerFlagDomination).GetMethod(
                        nameof(Prefix_TickFlags), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionMultiplayerFlagDomination).GetMethod("CheckForPlayersSpawningAsBots",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionMultiplayerFlagDomination).GetMethod(
                        nameof(Prefix_CheckForPlayersSpawningAsBots), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionMultiplayerFlagDomination).GetMethod("CheckPlayerBeingDetached",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionMultiplayerFlagDomination).GetMethod(
                        nameof(Prefix_CheckPlayerBeingDetached), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionMultiplayerFlagDomination).GetMethod(nameof(MissionMultiplayerFlagDomination.OnBehaviorInitialize),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MissionMultiplayerFlagDomination).GetMethod(
                        nameof(Prefix_OnBehaviorInitialize), BindingFlags.Static | BindingFlags.Public)));

                Harmony.ReversePatch(
                    typeof(MissionMultiplayerGameModeBase).GetMethod(
                        nameof(MissionMultiplayerGameModeBase.OnBehaviorInitialize), BindingFlags.Instance | BindingFlags.Public),
                    new HarmonyMethod(typeof(ReversePatch_MissionMultiplayerGameModeBase).GetMethod(nameof(ReversePatch_MissionMultiplayerGameModeBase.OnBehaviorInitialize))));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionMultiplayerFlagDomination)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Skip native method CheckPlayerBeingDetached to prevent troops following far away commander
        public static bool Prefix_CheckPlayerBeingDetached()
        {
            return false;
        }

        // Replace original OnBehaviorInitialize to prevent crash when capture points > 3 (ie in siege)
        public static bool Prefix_OnBehaviorInitialize(MissionMultiplayerFlagDomination __instance,
                                                       ref MissionMultiplayerGameModeFlagDominationClient ____gameModeFlagDominationClient,
                                                       ref float ____morale,
                                                       ref Team[] ____capturePointOwners,
                                                       ref ValueTuple<int, int>[] ____defenderAttackerCountsInFlagArea)
        {
            ReversePatch_MissionMultiplayerGameModeBase.OnBehaviorInitialize(__instance);

            ____gameModeFlagDominationClient = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();

            ____morale = 0f;

            typeof(MissionMultiplayerFlagDomination).GetProperty("AllCapturePoints")
                .SetValue(__instance, new MBReadOnlyList<FlagCapturePoint>(Mission.Current.MissionObjects.FindAllWithType<FlagCapturePoint>().ToListQ()));

            ____capturePointOwners = new Team[__instance.AllCapturePoints.Count];

            foreach (FlagCapturePoint capturePoint in __instance.AllCapturePoints)
            {
                capturePoint.SetTeamColorsWithAllSynched(4284111450u, uint.MaxValue);
                ____capturePointOwners[capturePoint.FlagIndex] = null;
            }

            ____defenderAttackerCountsInFlagArea = new ValueTuple<int, int>[__instance.AllCapturePoints.Count];

            return false;
        }

        // Patch to prevent rambos from capturing flags        
        public static bool Prefix_TickFlags(
            MissionMultiplayerFlagDomination __instance,
            int[] ____agentCountsOnSide,
            Team[] ____capturePointOwners,
            (int, int)[] ____defenderAttackerCountsInFlagArea,
            MissionMultiplayerGameModeFlagDominationClient ____gameModeFlagDominationClient,
            MultiplayerGameNotificationsComponent ___NotificationsComponent)
        {
            foreach (FlagCapturePoint allCapturePoint in __instance.AllCapturePoints)
            {
                if (allCapturePoint.IsDeactivated)
                {
                    continue;
                }

                for (int i = 0; i < 2; i++)
                {
                    ____agentCountsOnSide[i] = 0;
                }

                Team team = ____capturePointOwners[allCapturePoint.FlagIndex];
                Agent agent = null;
                float num = 16f;
                AgentProximityMap.ProximityMapSearchStruct searchStruct = AgentProximityMap.BeginSearch(Mission.Current, allCapturePoint.Position.AsVec2, 6f);
                while (searchStruct.LastFoundAgent != null)
                {
                    Agent lastFoundAgent = searchStruct.LastFoundAgent;
                    // Check if capturing agent is not in rambo
                    FormationComponent formationComponent = lastFoundAgent?.MissionPeer?.GetComponent<FormationComponent>();
                    bool agentIsAuthorizedToCap = !(formationComponent != null && formationComponent.State == FormationComponent.States.Rambo);
                    if (lastFoundAgent.IsHuman && lastFoundAgent.IsActive() && agentIsAuthorizedToCap)
                    {
                        ____agentCountsOnSide[(int)lastFoundAgent.Team.Side]++;
                        float num2 = lastFoundAgent.Position.DistanceSquared(allCapturePoint.Position);
                        if (num2 <= num)
                        {
                            agent = lastFoundAgent;
                            num = num2;
                        }
                    }

                    AgentProximityMap.FindNext(Mission.Current, ref searchStruct);
                }

                (int, int) tuple = ValueTuple.Create(____agentCountsOnSide[0], ____agentCountsOnSide[1]);
                bool flag = tuple.Item1 != ____defenderAttackerCountsInFlagArea[allCapturePoint.FlagIndex].Item1 || tuple.Item2 != ____defenderAttackerCountsInFlagArea[allCapturePoint.FlagIndex].Item2;
                ____defenderAttackerCountsInFlagArea[allCapturePoint.FlagIndex] = tuple;
                bool isContested = allCapturePoint.IsContested;
                float speedMultiplier = 1f;
                if (agent != null)
                {
                    BattleSideEnum side = agent.Team.Side;
                    BattleSideEnum oppositeSide = side.GetOppositeSide();
                    if (____agentCountsOnSide[(int)oppositeSide] != 0)
                    {
                        int num3 = Math.Min(____agentCountsOnSide[(int)side], 200);
                        int num4 = Math.Min(____agentCountsOnSide[(int)oppositeSide], 200);
                        float val = (MathF.Log10(num3) + 1f) / (2f * (MathF.Log10(num4) + 1f)) - 0.09f;
                        speedMultiplier = Math.Min(1f, val);
                    }
                }

                if (team == null)
                {
                    if (!isContested && agent != null)
                    {
                        allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Down, speedMultiplier);
                    }
                    else if (agent == null && isContested)
                    {
                        allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Up, speedMultiplier);
                    }
                    else if (flag)
                    {
                        allCapturePoint.ChangeMovementSpeed(speedMultiplier);
                    }
                }
                else if (agent != null)
                {
                    if (agent.Team != team && !isContested)
                    {
                        allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Down, speedMultiplier);
                    }
                    else if (agent.Team == team && isContested)
                    {
                        allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Up, speedMultiplier);
                    }
                    else if (flag)
                    {
                        allCapturePoint.ChangeMovementSpeed(speedMultiplier);
                    }
                }
                else if (isContested)
                {
                    allCapturePoint.SetMoveFlag(CaptureTheFlagFlagDirection.Up, speedMultiplier);
                }
                else if (flag)
                {
                    allCapturePoint.ChangeMovementSpeed(speedMultiplier);
                }

                allCapturePoint.OnAfterTick(agent != null, out var ownerTeamChanged);
                if (ownerTeamChanged)
                {
                    Team team2 = agent.Team;
                    uint color = (uint)(((int?)team2?.Color) ?? (-10855846));
                    uint color2 = (uint)(((int?)team2?.Color2) ?? (-1));
                    allCapturePoint.SetTeamColorsWithAllSynched(color, color2);
                    ____capturePointOwners[allCapturePoint.FlagIndex] = team2;
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new FlagDominationCapturePointMessage(allCapturePoint.FlagIndex, team2.TeamIndex));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    ____gameModeFlagDominationClient?.OnCapturePointOwnerChanged(allCapturePoint, team2);
                    ___NotificationsComponent.FlagXCapturedByTeamX(allCapturePoint, agent.Team);
                    MPPerkObject.RaiseEventForAllPeers(MPPerkCondition.PerkEventFlags.FlagCapture);
                }
            }
            return false;
        }

        // Fix CheckForPlayersSpawningAsBots to allow commanders to take control of all their troops
        public static bool Prefix_CheckForPlayersSpawningAsBots()
        {
            string gameMode = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);

            // Use native method for native game modes
            if (gameMode != "Scenario" && gameMode != "PvC")
            {
                return true;
            }

            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                if (networkCommunicator.IsSynchronized)
                {
                    MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                    // Removed check on controlled formation to allow control when changing team
                    if (component != null && /*component.ControlledFormation != null && */component.ControlledAgent == null && component.Team != null && component.SpawnCountThisRound > 0)
                    {
                        if (!component.HasSpawnTimerExpired && component.SpawnTimer.Check(Mission.Current.CurrentTime))
                        {
                            component.HasSpawnTimerExpired = true;
                        }
                        if (component.HasSpawnTimerExpired && component.WantsToSpawnAsBot)
                        {
                            Agent followedAgent = component.FollowedAgent;

                            // Check if the followed agent is in a formation controlled by the player
                            if (followedAgent != null && followedAgent.IsActive() && followedAgent.IsAIControlled && followedAgent.Formation != null && followedAgent.Health > 0
                                && FormationControlModel.Instance.GetControlledFormations(component).Contains(followedAgent.Formation.FormationIndex))
                            {
                                // Update player controlled formation to target
                                component.ControlledFormation = followedAgent.Formation;
                                Mission.Current.ReplaceBotWithPlayer(followedAgent, component);
                                component.WantsToSpawnAsBot = false;
                                component.HasSpawnTimerExpired = false;
                            }
                        }
                    }
                }
            }

            return false;
        }

        // Original method
        //private void CheckForPlayersSpawningAsBots()
        //{
        //    foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
        //    {
        //        if (networkCommunicator.IsSynchronized)
        //        {
        //            MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
        //            if (component != null && component.ControlledAgent == null && component.Team != null && component.ControlledFormation != null && component.SpawnCountThisRound > 0)
        //            {
        //                if (!component.HasSpawnTimerExpired && component.SpawnTimer.Check(base.Mission.CurrentTime))
        //                {
        //                    component.HasSpawnTimerExpired = true;
        //                }
        //                if (component.HasSpawnTimerExpired && component.WantsToSpawnAsBot)
        //                {
        //                    if (component.ControlledFormation.HasUnitsWithCondition((Agent agent) => agent.IsActive() && agent.IsAIControlled))
        //                    {
        //                        Agent newAgent = null;
        //                        Agent followingAgent = component.FollowedAgent;
        //                        if (followingAgent != null && followingAgent.IsActive() && followingAgent.IsAIControlled && component.ControlledFormation.HasUnitsWithCondition((Agent agent) => agent == followingAgent))
        //                        {
        //                            newAgent = followingAgent;
        //                        }
        //                        else
        //                        {
        //                            float maxHealth = 0f;
        //                            component.ControlledFormation.ApplyActionOnEachUnit(delegate (Agent agent)
        //                            {
        //                                if (agent.Health > maxHealth)
        //                                {
        //                                    maxHealth = agent.Health;
        //                                    newAgent = agent;
        //                                }
        //                            }, null);
        //                        }
        //                        Mission.Current.ReplaceBotWithPlayer(newAgent, component);
        //                        component.WantsToSpawnAsBot = false;
        //                        component.HasSpawnTimerExpired = false;
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
    }

    // Reverse patch to access base implementation of MissionMultiplayerGameModeBase.OnBehaviorInitialize
    class ReversePatch_MissionMultiplayerGameModeBase
    {
        public static void OnBehaviorInitialize(MissionMultiplayerGameModeBase instance)
        {
            Console.WriteLine($"Patch.OnBehaviorInitialize({instance})");
        }
    }
}

