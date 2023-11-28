using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.ViewModels;
using Alliance.Common.GameModes.PvC.Behaviors;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.TwoDimension;

namespace Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views
{
    // TODO : Seems unused ?
    //[OverrideView(typeof(MissionLobbyEquipmentUIHandler))]
    public class LobbyEquipmentView : MissionView
    {
        public bool IsForceClosed { get; private set; }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            ViewOrderPriority = 20;
            _missionLobbyEquipmentNetworkComponent = Mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
            _gameModeClient = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            _teamSelectComponent = Mission.GetMissionBehavior<PvCTeamSelectBehavior>();
            if (_teamSelectComponent != null)
            {
                _teamSelectComponent.OnSelectingTeam += OnSelectingTeam;
            }
            _scoreboardGauntletComponent = Mission.GetMissionBehavior<MissionGauntletMultiplayerScoreboard>();
            if (_scoreboardGauntletComponent != null)
            {
                MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
                scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Combine(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
            }
            _missionNetworkComponent = Mission.GetMissionBehavior<MissionNetworkComponent>();
            if (_missionNetworkComponent != null)
            {
                _missionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
            }
            MissionPeer.OnTeamChanged += OnTeamChanged;
            _missionLobbyEquipmentNetworkComponent.OnToggleLoadout += OnTryToggle;
            _missionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed += OnPeerEquipmentRefreshed;
        }

        private void OnMyClientSynchronized()
        {
            NetworkCommunicator myPeer = GameNetwork.MyPeer;
            _myRepresentative = myPeer != null ? myPeer.VirtualPlayer.GetComponent<MissionRepresentativeBase>() : null;
            if (_myRepresentative != null)
            {
                _myRepresentative.OnGoldUpdated += OnGoldUpdated;
            }
        }

        private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
        {
            if (peer.IsMine && newTeam != null && (newTeam.IsAttacker || newTeam.IsDefender))
            {
                if (_isActive)
                {
                    OnTryToggle(false);
                }
                OnTryToggle(true);
            }
        }

        private void OnRefreshSelection(MultiplayerClassDivisions.MPHeroClass heroClass)
        {
            _lastSelectedHeroClass = heroClass;
        }

        public override void OnMissionScreenFinalize()
        {
            if (_gauntletLayer != null)
            {
                MissionScreen.RemoveLayer(_gauntletLayer);
                _gauntletLayer = null;
            }
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
                _dataSource = null;
            }
            if (_teamSelectComponent != null)
            {
                _teamSelectComponent.OnSelectingTeam -= OnSelectingTeam;
            }
            if (_scoreboardGauntletComponent != null)
            {
                MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
                scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Remove(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
            }
            if (_missionNetworkComponent != null)
            {
                _missionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
                if (_myRepresentative != null)
                {
                    _myRepresentative.OnGoldUpdated -= OnGoldUpdated;
                }
            }
            _missionLobbyEquipmentNetworkComponent.OnToggleLoadout -= OnTryToggle;
            _missionLobbyEquipmentNetworkComponent.OnEquipmentRefreshed -= OnPeerEquipmentRefreshed;
            MissionPeer.OnTeamChanged -= OnTeamChanged;
            base.OnMissionScreenFinalize();
        }

        private void CreateView()
        {
            MissionMultiplayerGameModeBaseClient missionBehavior = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            _dataSource = new LobbyEquipmentVM(missionBehavior, new Action<MultiplayerClassDivisions.MPHeroClass>(OnRefreshSelection), _lastSelectedHeroClass);
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("MultiplayerClassLoadout", _dataSource);
        }

        public void OnTryToggle(bool isActive)
        {
            if (isActive)
            {
                _tryToInitialize = true;
                return;
            }
            IsForceClosed = false;
            OnToggled(false);
        }

        private bool OnToggled(bool isActive)
        {
            if (_isActive == isActive)
            {
                return true;
            }
            if (!MissionScreen.SetDisplayDialog(isActive))
            {
                return false;
            }
            if (isActive)
            {
                CreateView();
                _dataSource.Tick(1f);
                _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                MissionScreen.AddLayer(_gauntletLayer);
            }
            else
            {
                MissionScreen.RemoveLayer(_gauntletLayer);
                _dataSource.OnFinalize();
                _dataSource = null;
                _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                _gauntletLayer = null;
            }
            _isActive = isActive;
            return true;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            MissionPeer peer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
            if (_tryToInitialize && GameNetwork.IsMyPeerReady && peer != null && peer.HasSpawnedAgentVisuals && OnToggled(true))
            {
                _tryToInitialize = false;
            }
            if (_isActive)
            {
                _dataSource.Tick(dt);
                MissionMultiplayerGameModeFlagDominationClient missionMultiplayerGameModeFlagDominationClient;
                if (Input.IsHotKeyReleased("ForfeitSpawn") && (missionMultiplayerGameModeFlagDominationClient = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>() as MissionMultiplayerGameModeFlagDominationClient) != null)
                {
                    missionMultiplayerGameModeFlagDominationClient.OnRequestForfeitSpawn();
                }
            }
        }

        private void OnSelectingTeam(List<Team> disableTeams)
        {
            IsForceClosed = true;
            OnToggled(false);
        }

        private void OnScoreboardToggled(bool isEnabled)
        {
            if (isEnabled)
            {
                GauntletLayer gauntletLayer = _gauntletLayer;
                if (gauntletLayer == null)
                {
                    return;
                }
                gauntletLayer.InputRestrictions.ResetInputRestrictions();
                return;
            }
            else
            {
                GauntletLayer gauntletLayer2 = _gauntletLayer;
                if (gauntletLayer2 == null)
                {
                    return;
                }
                gauntletLayer2.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
                return;
            }
        }

        private void OnPeerEquipmentRefreshed(MissionPeer peer)
        {
            if (_gameModeClient.GameType == MultiplayerGameType.Skirmish || _gameModeClient.GameType == MultiplayerGameType.Captain)
            {
                LobbyEquipmentVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.OnPeerEquipmentRefreshed(peer);
            }
        }

        private void OnGoldUpdated()
        {
            LobbyEquipmentVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnGoldUpdated();
        }

        public LobbyEquipmentView()
        {
        }

        private LobbyEquipmentVM _dataSource;

        private GauntletLayer _gauntletLayer;

        private MissionRepresentativeBase _myRepresentative;

        private MissionNetworkComponent _missionNetworkComponent;

        private MissionLobbyEquipmentNetworkComponent _missionLobbyEquipmentNetworkComponent;

        private MissionMultiplayerGameModeBaseClient _gameModeClient;

        private PvCTeamSelectBehavior _teamSelectComponent;

        private MissionGauntletMultiplayerScoreboard _scoreboardGauntletComponent;

        private MultiplayerClassDivisions.MPHeroClass _lastSelectedHeroClass;

        private bool _tryToInitialize;

        private bool _isActive;
    }
}
