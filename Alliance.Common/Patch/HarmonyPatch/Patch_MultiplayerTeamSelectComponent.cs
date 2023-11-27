using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Patch.HarmonyPatch
{
    public class Patch_MultiplayerTeamSelectComponent
    {
        private static readonly Harmony Harmony = new Harmony(nameof(Patch_MultiplayerTeamSelectComponent));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MultiplayerTeamSelectComponent).GetMethod(nameof(MultiplayerTeamSelectComponent.ChangeTeamServer),
                        BindingFlags.Instance | BindingFlags.Public),
                    prefix: new HarmonyMethod(typeof(Patch_MultiplayerTeamSelectComponent).GetMethod(
                        nameof(Prefix_ChangeTeamServer), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MultiplayerTeamSelectComponent)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Assign a different team to each captain
        public static bool Prefix_ChangeTeamServer(MultiplayerTeamSelectComponent __instance, NetworkCommunicator networkPeer, Team team, MissionMultiplayerGameModeBase ____gameModeServer, MissionNetworkComponent ____missionNetworkComponent)
        {
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            Team team2 = component.Team;

            // Create new team instead of using existing ones
            //Mission.Current.Teams.Add(team.Side, team.Color, team.Color2, team.Banner, isPlayerGeneral: false, isPlayerSergeant: true);
            Team newTeam = team;// Mission.Current.Teams.FindLast(t => t.Side == team.Side);

            if (team2 != null && team2 != __instance.Mission.SpectatorTeam && team2 != newTeam && component.ControlledAgent != null)
            {
                Blow b = new Blow(component.ControlledAgent.Index);
                b.DamageType = DamageTypes.Invalid;
                b.BaseMagnitude = 10000f;
                b.GlobalPosition = component.ControlledAgent.Position;
                b.DamagedPercentage = 1f;
                component.ControlledAgent.Die(b, Agent.KillInfo.TeamSwitch);
            }

            component.Team = newTeam;
            BasicCultureObject basicCultureObject2 = component.Culture = component.Team.IsAttacker ? MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue()) : MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            if (newTeam != team2)
            {
                if (component.HasSpawnedAgentVisuals)
                {
                    component.HasSpawnedAgentVisuals = false;
                    Log("HasSpawnedAgentVisuals = false for peer: " + component.Name + " because he just changed his team", LogLevel.Debug);
                    component.SpawnCountThisRound = 0;
                    Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component, sync: true);
                }

                if (!____gameModeServer.IsGameModeHidingAllAgentVisuals && !networkPeer.IsServerPeer)
                {
                    ____missionNetworkComponent?.OnPeerSelectedTeam(component);
                }

                ____gameModeServer.OnPeerChangedTeam(networkPeer, team2, newTeam);
                component.SpawnTimer.Reset(Mission.Current.CurrentTime, 0.1f);
                component.WantsToSpawnAsBot = false;
                component.HasSpawnTimerExpired = false;
            }

            __instance.UpdateTeams(networkPeer, team2, newTeam);

            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new ServerMessage(networkPeer.UserName + " set to team " + newTeam.Side + "/" + newTeam.TeamIndex));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

            // Return false to skip original method
            return false;
        }

        /* Original method
         * 
        public void ChangeTeamServer(NetworkCommunicator networkPeer, Team team)
        {
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            Team team2 = component.Team;
            if (team2 != null && team2 != base.Mission.SpectatorTeam && team2 != team && component.ControlledAgent != null)
            {
                Blow b = new Blow(component.ControlledAgent.Index);
                b.DamageType = DamageTypes.Invalid;
                b.BaseMagnitude = 10000f;
                b.Position = component.ControlledAgent.Position;
                b.DamagedPercentage = 1f;
                component.ControlledAgent.Die(b, Agent.KillInfo.TeamSwitch);
            }

            component.Team = team;
            BasicCultureObject basicCultureObject2 = (component.Culture = (component.Team.IsAttacker ? MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue()) : MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue())));
            if (team != team2)
            {
                if (component.HasSpawnedAgentVisuals)
                {
                    component.HasSpawnedAgentVisuals = false;
                    MBLog("HasSpawnedAgentVisuals = false for peer: " + component.Name + " because he just changed his team");
                    component.SpawnCountThisRound = 0;
                    Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().RemoveAgentVisuals(component, sync: true);
                }

                if (!_gameModeServer.IsGameModeHidingAllAgentVisuals && !networkPeer.IsServerPeer)
                {
                    _missionNetworkComponent?.OnPeerSelectedTeam(component);
                }

                _gameModeServer.OnPeerChangedTeam(networkPeer, team2, team);
                component.SpawnTimer.Reset(Mission.Current.CurrentTime, 0.1f);
                component.WantsToSpawnAsBot = false;
                component.HasSpawnTimerExpired = false;
            }

            UpdateTeams(networkPeer, team2, team);
        }*/
    }
}
