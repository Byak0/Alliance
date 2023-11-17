using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromClient;
using System.Linq;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
    public class AdminVM : ViewModel
    {
        private string _Username;
        private string _Health;
        private string _Position;
        private string _Kill;
        private string _Death;
        private string _Assist;
        private string _Score;
        private string _KickCounter;
        private string _BanCounter;
        private string _WarningCounter;
        private string _filterText;
        private CharacterViewModel _unitCharacter;
        private MBBindingList<NetworkPeerVM> _networkCommunicators;
        private MBBindingList<ServerMessageVM> _serverMessage;
        private RolesVM _roles;
        private NetworkPeerVM _selectedPeer;
        private bool _isSudo;

        public AdminVM()
        {
            _isSudo = GameNetwork.MyPeer.IsDev();
            _unitCharacter = new CharacterViewModel();
            _serverMessage = new MBBindingList<ServerMessageVM>();
            _roles = new RolesVM();
            RefreshPlayerList();
        }

        [DataSourceProperty]
        public bool IsSudo
        {
            get
            {
                return _isSudo;
            }
            set
            {
                if (value != _isSudo)
                {
                    _isSudo = value;
                    OnPropertyChangedWithValue(value, "IsSudo");
                }
            }
        }

        [DataSourceProperty]
        public string Username
        {
            get
            {
                return _Username;
            }
            set
            {
                if (value != _Username)
                {
                    _Username = value;
                    OnPropertyChangedWithValue(value, "Username");
                }
            }
        }

        [DataSourceProperty]
        public RolesVM Roles
        {
            get
            {
                return _roles;
            }
            set
            {
                if (value != _roles)
                {
                    _roles = value;
                    OnPropertyChangedWithValue(value, "Roles");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<ServerMessageVM> ServerMessage
        {
            get
            {
                return _serverMessage;
            }
            set
            {
                if (value != _serverMessage)
                {
                    _serverMessage = value;
                    OnPropertyChangedWithValue(value, "ServerMessage");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<NetworkPeerVM> NetworkPeers
        {
            get
            {
                return _networkCommunicators;
            }
            set
            {
                if (value != _networkCommunicators)
                {
                    _networkCommunicators = value;
                    OnPropertyChangedWithValue(value, "NetworkPeers");
                }
            }
        }

        [DataSourceProperty]
        public CharacterViewModel UnitCharacter
        {
            get
            {
                return _unitCharacter;
            }
            set
            {
                if (value != _unitCharacter)
                {
                    _unitCharacter = value;
                    OnPropertyChangedWithValue(value, "UnitCharacter");
                }
            }
        }

        [DataSourceProperty]
        public string Health
        {
            get
            {
                return _Health;
            }
            set
            {
                if (value != _Health)
                {
                    _Health = value;
                    OnPropertyChangedWithValue(value, "Health");
                }
            }
        }

        [DataSourceProperty]
        public string Position
        {
            get
            {
                return _Position;
            }
            set
            {
                if (value != _Position)
                {
                    _Position = value;
                    OnPropertyChangedWithValue(value, "Position");
                }
            }
        }

        [DataSourceProperty]
        public string Kill
        {
            get
            {
                return _Kill;
            }
            set
            {
                if (value != _Kill)
                {
                    _Kill = value;
                    OnPropertyChangedWithValue(value, "Kill");
                }
            }
        }

        [DataSourceProperty]
        public string Death
        {
            get
            {
                return _Death;
            }
            set
            {
                if (value != _Death)
                {
                    _Death = value;
                    OnPropertyChangedWithValue(value, "Death");
                }
            }
        }

        [DataSourceProperty]
        public string Assist
        {
            get
            {
                return _Assist;
            }
            set
            {
                if (value != _Assist)
                {
                    _Assist = value;
                    OnPropertyChangedWithValue(value, "Assist");
                }
            }
        }

        [DataSourceProperty]
        public string Score
        {
            get
            {
                return _Score;
            }
            set
            {
                if (value != _Score)
                {
                    _Score = value;
                    OnPropertyChangedWithValue(value, "Score");
                }
            }
        }

        [DataSourceProperty]
        public string FilterText
        {
            get
            {
                return _filterText;
            }
            set
            {
                if (value != _filterText)
                {
                    _filterText = value;
                    FilterPlayers(_filterText);
                    OnPropertyChangedWithValue(value, "FilterText");
                }
            }
        }

        /// <summary>
        /// Filter list of players with given text filter
        /// </summary>
        public void FilterPlayers(string filterText)
        {
            foreach (NetworkPeerVM networkPeerVM in _networkCommunicators)
            {
                if (networkPeerVM != null)
                {
                    networkPeerVM.IsFiltered = !networkPeerVM.Username.ToLower().Contains(filterText);
                }
            }
        }

        public void Heal()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { Heal = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void HealAll()
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { HealAll = true, PlayerSelected = null });
            GameNetwork.EndModuleEventAsClient();
        }

        public void GodMod()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { GodMod = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void GodModAll()
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { GodModAll = true, PlayerSelected = null });
            GameNetwork.EndModuleEventAsClient();
        }

        public void KillPlayer()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { Kill = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void KillAll()
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { KillAll = true, PlayerSelected =  null });
            GameNetwork.EndModuleEventAsClient();
        }

        public void KickPlayer()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { Kick = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void BanPlayer()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { Ban = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void TeleportToPlayer()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { TeleportToPlayer = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void TeleportPlayerToYou()
        {
            if (_selectedPeer == null) { return; }
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new AdminClient() { TeleportPlayerToYou = true, PlayerSelected = _selectedPeer.PeerId });
            GameNetwork.EndModuleEventAsClient();
        }

        public void SetAdmin()
        {
            if (_selectedPeer == null) { return; }
            if (GameNetwork.MyPeer.IsDev())
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new AdminClient() { SetAdmin = true, PlayerSelected = _selectedPeer.PeerId });
                GameNetwork.EndModuleEventAsClient();
            }
        }

        public void RefreshPlayerList()
        {
            _networkCommunicators = new MBBindingList<NetworkPeerVM>();
            GameNetwork.NetworkPeers.ToList().ForEach(x =>
            {
                _networkCommunicators.Add(new NetworkPeerVM()
                {
                    Username = x.UserName,
                    AgentIndex = x.ControlledAgent?.Index ?? -1,
                    PeerId = x.VirtualPlayer.Id.ToString(),
                    IsSelected = x.VirtualPlayer.Id.ToString() == _selectedPeer?.PeerId,
                    OnSelect = OnNetworkPeerSelected
                });
            });
            _selectedPeer = _networkCommunicators.FirstOrDefault(x => x.PeerId == _selectedPeer?.PeerId);
        }

        public void SelectTarget(Agent agent)
        {
            NetworkPeerVM peerVM = null;
            // Look for player match
            if (agent.MissionPeer?.Peer != null) peerVM = _networkCommunicators.Where(x => x.PeerId == agent.MissionPeer?.Peer?.Id.ToString()).FirstOrDefault();
            // If nothing was found, look for agent match
            if (peerVM == null && agent.Index != -1) peerVM = _networkCommunicators.Where(x => x.AgentIndex == agent.Index).FirstOrDefault();
            // If still nothing was found, add new VM
            if (peerVM == null)
            {
                peerVM = new NetworkPeerVM()
                {
                    Username = agent.MissionPeer?.Name ?? agent.Name,
                    AgentIndex = agent.Index,
                    PeerId = agent.MissionPeer?.Peer?.Id.ToString(),
                    IsSelected = false,
                    OnSelect = OnNetworkPeerSelected
                };
            }
            OnNetworkPeerSelected(peerVM);
        }

        /// <summary>
        /// Update player related informations.
        /// </summary>
        private void UpdatePlayerVM(NetworkCommunicator networkCommunicator)
        {
            MissionPeer peer = networkCommunicator?.GetComponent<MissionPeer>();
            if (peer != null)
            {
                Kill = peer.KillCount.ToString();
                Assist = peer.AssistCount.ToString();
                Death = peer.DeathCount.ToString();
                Score = peer.Score.ToString();
            }
            if (networkCommunicator?.ControlledAgent != null)
            {
                UpdateAgentVM(networkCommunicator.ControlledAgent);
            }
        }

        /// <summary>
        /// Update agent related informations.
        /// </summary>
        private void UpdateAgentVM(Agent selectedAgent)
        {
            UnitCharacter = new CharacterViewModel();
            if (selectedAgent != null)
            {
                Health = selectedAgent.Health.ToString();
                Position = selectedAgent.Position.ToString();
                if (selectedAgent.Character != null) UnitCharacter.FillFrom(selectedAgent.Character);
            }
        }

        private void OnNetworkPeerSelected(NetworkPeerVM peer)
        {
            if (peer == null) return;
            if (_selectedPeer != null) _selectedPeer.IsSelected = false;
            NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.Where(x => x.VirtualPlayer.Id.ToString() == peer.PeerId).FirstOrDefault();
            // Player informations
            if (networkCommunicator != null)
            {
                Username = networkCommunicator.UserName;
                UpdatePlayerVM(networkCommunicator);
            }
            // Agent informations
            else if (peer.AgentIndex != -1)
            {
                Agent agent = Mission.Current.Agents.Where(x => x.Index == peer.AgentIndex).FirstOrDefault();
                Username = agent?.Name;
                UpdateAgentVM(agent);
            }
            peer.IsSelected = true;
            _selectedPeer = peer;
        }
    }
}
