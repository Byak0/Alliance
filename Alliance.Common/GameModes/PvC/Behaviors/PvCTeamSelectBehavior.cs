using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.PlatformService;
using TaleWorlds.PlayerServices;
using static Alliance.Common.GameModes.PvC.Behaviors.PvCRepresentative;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.PvC.Behaviors
{
    /// <summary>
    /// TODO : rework this so we don't have to use this custom behavior (handle team assignation in game mode own behavior)
    /// Custom TeamSelectBehavior based on native MultiplayerTeamSelectComponent.
    /// Handle team selection/assignation.
    /// </summary>
    public class PvCTeamSelectBehavior : MissionNetwork
    {
        public delegate void OnSelectingTeamDelegate(List<Team> disableTeams);

        private MissionNetworkComponent _missionNetworkComponent;

        private MissionMultiplayerGameModeBase _gameModeServer;

        private HashSet<PlayerId> _platformFriends;

        private Dictionary<Team, IEnumerable<VirtualPlayer>> _friendsPerTeam;

        public bool TeamSelectionEnabled { get; private set; }

        public event OnSelectingTeamDelegate OnSelectingTeam;

        public event Action OnMyTeamChange;

        public event Action OnUpdateTeams;

        public event Action OnUpdateFriendsPerTeam;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _missionNetworkComponent = Mission.GetMissionBehavior<MissionNetworkComponent>();
            _gameModeServer = Mission.GetMissionBehavior<MissionMultiplayerGameModeBase>();
            if (BannerlordNetwork.LobbyMissionType == LobbyMissionType.Matchmaker)
            {
                TeamSelectionEnabled = false;
            }
            else
            {
                TeamSelectionEnabled = true;
            }
        }

        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            if (GameNetwork.IsServer)
            {
                registerer.Register<TeamChange>(HandleClientEventTeamChange);
            }
        }

        /// <summary>
        /// TODO : move this to game mode own behavior?
        /// Auto select team based on player role.
        /// </summary>
        private void OnMyClientSynchronized()
        {
            // Ask for server to auto assign player's team
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new TeamChange(autoAssign: true, -1));
            GameNetwork.EndModuleEventAsClient();

            //Mission.GetMissionBehavior<MissionNetworkComponent>().OnMyClientSynchronized -= OnMyClientSynchronized;                        
            //Team spectator = Mission.Teams.FirstOrDefault((t) => t.Side == BattleSideEnum.None);
            //Team commanderSide = Mission.Teams.FirstOrDefault((t) => (int)t.Side == PvCConfig.Instance.CommanderSide);
            //Team playerSide = Mission.Teams.FirstOrDefault((t) => (int)t.Side == PvCConfig.Instance.CommanderSide);
            //if (PvCRoles.Instance.Commanders.Contains(GameNetwork.MyPeer.UserName))
            //{
            //    ChangeTeam(commanderSide);
            //}
            //else if (PvCRoles.Instance.Officers.Contains(GameNetwork.MyPeer.UserName))
            //{
            //    ChangeTeam(playerSide);
            //}
            //else
            //{
            //    ChangeTeam(playerSide);
            //}
        }

        public override void AfterStart()
        {
            _platformFriends = new HashSet<PlayerId>();
            foreach (PlayerId allFriendsInAllPlatform in FriendListService.GetAllFriendsInAllPlatforms())
            {
                _platformFriends.Add(allFriendsInAllPlatform);
            }
            _friendsPerTeam = new Dictionary<Team, IEnumerable<VirtualPlayer>>();

            MissionPeer.OnTeamChanged += UpdateTeams;
            MissionPeer.OnTeamChanged += UpdateRepresentative;
            if (GameNetwork.IsClient)
            {
                MissionNetworkComponent missionBehavior = Mission.GetMissionBehavior<MissionNetworkComponent>();
                if (TeamSelectionEnabled)
                {
                    missionBehavior.OnMyClientSynchronized += OnMyClientSynchronized;
                }
            }
        }

        public override void OnRemoveBehavior()
        {
            MissionPeer.OnTeamChanged -= UpdateTeams;
            MissionPeer.OnTeamChanged -= UpdateRepresentative;
            OnMyTeamChange = null;
            base.OnRemoveBehavior();
        }

        private bool HandleClientEventTeamChange(NetworkCommunicator peer, TeamChange message)
        {
            if (TeamSelectionEnabled)
            {
                if (message.AutoAssign)
                {
                    //AutoAssignTeam(peer);
                    // TODO fix ugly for event
                    BattleSideEnum playerSide = Config.Instance.CommanderSide == (int)BattleSideEnum.Defender ? BattleSideEnum.Attacker : BattleSideEnum.Defender;
                    Team spectator = Mission.Teams.FirstOrDefault((t) => t.Side == BattleSideEnum.None);
                    Team commanderTeam = Mission.Teams.FirstOrDefault((t) => (int)t.Side == Config.Instance.CommanderSide);
                    Team playerTeam = Mission.Teams.FirstOrDefault((t) => t.Side == playerSide);
                    if (peer.IsCommander())
                    {
                        ChangeTeamServer(peer, commanderTeam);
                    }
                    else if (peer.IsOfficer())
                    {
                        ChangeTeamServer(peer, playerTeam);
                    }
                    else
                    {
                        ChangeTeamServer(peer, playerTeam);
                    }
                }
                else
                {
                    Team teamFromTeamIndex = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(message.TeamIndex);
                    ChangeTeamServer(peer, teamFromTeamIndex);
                }

            }
            return true;
        }

        // Todo : move this to game mode own behavior
        public void UpdateRepresentative(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            MissionPeer component = peer.GetComponent<MissionPeer>();

            if ((int)newTeam.Side == Config.Instance.CommanderSide)
            {
                ((PvCRepresentative)component.Representative).Side = Sides.Commander;
            }
            else
            {
                ((PvCRepresentative)component.Representative).Side = Sides.Player;
            }

            Log($"{peer.UserName} joined the {((PvCRepresentative)component.Representative).Side}s' side.", LogLevel.Debug);
        }

        public void SelectTeam()
        {
            if (OnSelectingTeam != null)
            {
                List<Team> disabledTeams = GetDisabledTeams();
                OnSelectingTeam(disabledTeams);
            }
        }

        public void UpdateTeams(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            if (OnUpdateTeams != null)
            {
                OnUpdateTeams();
            }
            if (GameNetwork.IsMyPeerReady)
            {
                CacheFriendsForTeams();
            }
            if (newTeam.Side != BattleSideEnum.None)
            {
                MissionPeer component = peer.GetComponent<MissionPeer>();
                component.SelectedTroopIndex = 0;
                component.NextSelectedTroopIndex = 0;
                component.OverrideCultureWithTeamCulture();
            }
        }

        public List<Team> GetDisabledTeams()
        {
            List<Team> list = new List<Team>();
            int autoTeamBalanceDiff = MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue();
            if (autoTeamBalanceDiff == 0)
            {
                return list;
            }
            Team myTeam = GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>().Team : null;
            Team[] array = (from q in Mission.Teams
                            where q != Mission.SpectatorTeam
                            orderby myTeam != null ? q != myTeam ? GetPlayerCountForTeam(q) : GetPlayerCountForTeam(q) - 1 : GetPlayerCountForTeam(q)
                            select q).ToArray();
            Team[] array2 = array;
            foreach (Team team in array2)
            {
                int num = GetPlayerCountForTeam(team);
                int num2 = GetPlayerCountForTeam(array[0]);
                if (myTeam == team)
                {
                    num--;
                }
                if (myTeam == array[0])
                {
                    num2--;
                }
                if (num - num2 >= autoTeamBalanceDiff)
                {
                    list.Add(team);
                }
            }
            return list;
        }

        public void ChangeTeamServer(NetworkCommunicator networkPeer, Team team)
        {
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            Team team2 = component.Team;
            if (team2 != null && team2 != Mission.SpectatorTeam && team2 != team && component.ControlledAgent != null)
            {
                Blow b = new Blow(component.ControlledAgent.Index);
                b.DamageType = DamageTypes.Invalid;
                b.BaseMagnitude = 10000f;
                b.GlobalPosition = component.ControlledAgent.Position;
                b.DamagedPercentage = 1f;
                component.ControlledAgent.Die(b, Agent.KillInfo.TeamSwitch);
            }
            component.Team = team;

            if (team != team2)
            {
                if (component.HasSpawnedAgentVisuals)
                {
                    component.HasSpawnedAgentVisuals = false;
                    MBDebug.Print("HasSpawnedAgentVisuals = false for peer: " + component.Name + " because he just changed his team");
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
        }

        public void ChangeTeam(Team team)
        {
            if (team == GameNetwork.MyPeer.GetComponent<MissionPeer>().Team)
            {
                return;
            }
            if (GameNetwork.IsServer)
            {
                Mission.Current.PlayerTeam = team;
                ChangeTeamServer(GameNetwork.MyPeer, team);
            }
            else
            {
                foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                {
                    networkPeer.GetComponent<MissionPeer>()?.ClearAllVisuals();
                }
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new TeamChange(autoAssign: false, team.TeamIndex));
                GameNetwork.EndModuleEventAsClient();
            }
            if (OnMyTeamChange != null)
            {
                OnMyTeamChange();
            }
        }

        public int GetPlayerCountForTeam(Team team)
        {
            int num = 0;
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component?.Team != null && component.Team == team)
                {
                    num++;
                }
            }
            return num;
        }

        private void CacheFriendsForTeams()
        {
            _friendsPerTeam.Clear();
            if (_platformFriends.Count <= 0)
            {
                return;
            }
            List<MissionPeer> list = new List<MissionPeer>();
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component != null && _platformFriends.Contains(networkPeer.VirtualPlayer.Id))
                {
                    list.Add(component);
                }
            }
            foreach (Team team in Mission.Teams)
            {
                if (team != null)
                {
                    _friendsPerTeam.Add(team, from x in list
                                              where x.Team == team
                                              select x.Peer);
                }
            }
            if (OnUpdateFriendsPerTeam != null)
            {
                OnUpdateFriendsPerTeam();
            }
        }

        public IEnumerable<VirtualPlayer> GetFriendsForTeam(Team team)
        {
            if (_friendsPerTeam.ContainsKey(team))
            {
                return _friendsPerTeam[team];
            }
            return new List<VirtualPlayer>();
        }

        public void BalanceTeams()
        {
            if (MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue() == 0)
            {
                return;
            }
            int num = GetPlayerCountForTeam(Mission.Current.AttackerTeam);
            int autoTeamBalanceDiff = MultiplayerOptions.OptionType.AutoTeamBalanceThreshold.GetIntValue();
            int i;
            for (i = GetPlayerCountForTeam(Mission.Current.DefenderTeam); num > i + autoTeamBalanceDiff; i++)
            {
                MissionPeer missionPeer = null;
                foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                {
                    if (networkPeer.IsSynchronized)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component?.Team != null && component.Team == Mission.AttackerTeam && (missionPeer == null || component.JoinTime >= missionPeer.JoinTime))
                        {
                            missionPeer = component;
                        }
                    }
                }
                ChangeTeamServer(missionPeer.GetNetworkPeer(), Mission.Current.DefenderTeam);
                num--;
            }
            while (i > num + autoTeamBalanceDiff)
            {
                MissionPeer missionPeer2 = null;
                foreach (NetworkCommunicator networkPeer2 in GameNetwork.NetworkPeers)
                {
                    if (networkPeer2.IsSynchronized)
                    {
                        MissionPeer component2 = networkPeer2.GetComponent<MissionPeer>();
                        if (component2?.Team != null && component2.Team == Mission.DefenderTeam && (missionPeer2 == null || component2.JoinTime >= missionPeer2.JoinTime))
                        {
                            missionPeer2 = component2;
                        }
                    }
                }
                ChangeTeamServer(missionPeer2.GetNetworkPeer(), Mission.Current.AttackerTeam);
                num++;
                i--;
            }
        }

        public void AutoAssignTeam(NetworkCommunicator peer)
        {
            if (GameNetwork.IsServer)
            {
                List<Team> disabledTeams = GetDisabledTeams();
                List<Team> list = Mission.Teams.Where((x) => !disabledTeams.Contains(x) && x.Side != BattleSideEnum.None).ToList();
                Team team;
                if (list.Count > 1)
                {
                    int[] array = new int[list.Count];
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component?.Team == null)
                        {
                            continue;
                        }
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (component.Team == list[i])
                            {
                                array[i]++;
                            }
                        }
                    }
                    int num = -1;
                    int num2 = -1;
                    for (int j = 0; j < array.Length; j++)
                    {
                        if (num2 < 0 || array[j] < num)
                        {
                            num2 = j;
                            num = array[j];
                        }
                    }
                    team = list[num2];
                }
                else
                {
                    team = list[0];
                }
                if (!peer.IsMine)
                {
                    ChangeTeamServer(peer, team);
                }
                else
                {
                    ChangeTeam(team);
                }
            }
            else
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new TeamChange(autoAssign: true, -1));
                GameNetwork.EndModuleEventAsClient();
                if (OnMyTeamChange != null)
                {
                    OnMyTeamChange();
                }
            }
        }
    }
}