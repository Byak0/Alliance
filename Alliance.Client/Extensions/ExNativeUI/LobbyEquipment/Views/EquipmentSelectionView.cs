using Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.ViewModels;
using Alliance.Common.Extensions.ClassLimiter.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.Views
{
    /// <summary>
    /// Custom view for the equipment selection menu.
    /// Based on native class MissionGauntletClassLoadout.
    /// </summary>
    public class EquipmentSelectionView : MissionView
    {
        public bool IsActive { get; private set; }

        public bool IsForceClosed { get; private set; }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            ViewOrderPriority = 20;
            _missionLobbyComponent = Mission.GetMissionBehavior<MissionLobbyComponent>();
            _missionLobbyEquipmentNetworkComponent = Mission.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
            _gameModeClient = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            _teamSelectComponent = Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
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
            _myRepresentative = (myPeer?.VirtualPlayer.GetComponent<MissionRepresentativeBase>());
            if (_myRepresentative != null)
            {
                _myRepresentative.OnGoldUpdated += OnGoldUpdated;
            }
            _missionLobbyComponent.OnClassRestrictionChanged += OnGoldUpdated;
            ClassLimiterModel.Instance.CharacterAvailabilityChanged += OnCharacterAvailabilityUpdated;
        }

        private void OnCharacterAvailabilityUpdated(BasicCharacterObject character, bool available)
        {
            Log($"Updating availability of {character} to {available}", LogLevel.Debug);
            if (_dataSource != null && _dataSource.CharacterToVM.TryGetValue(character, out ALHeroClassVM classVM))
            {
                classVM.UpdateEnabled();
            }
        }

        private void OnTeamChanged(NetworkCommunicator peer, Team previousTeam, Team newTeam)
        {
            if (peer.IsMine && newTeam != null && (newTeam.IsAttacker || newTeam.IsDefender))
            {
                if (IsActive)
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
                    _missionLobbyComponent.OnClassRestrictionChanged -= OnGoldUpdated;
                }
            }
            ClassLimiterModel.Instance.CharacterAvailabilityChanged -= OnCharacterAvailabilityUpdated;
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
            _dataSource = new EquipmentSelectionVM(missionBehavior, new Action<MultiplayerClassDivisions.MPHeroClass>(OnRefreshSelection), _lastSelectedHeroClass);
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
            if (IsActive == isActive)
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
            IsActive = isActive;
            return true;
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
            if (_tryToInitialize && GameNetwork.IsMyPeerReady && GameNetwork.MyPeer.GetComponent<MissionPeer>().HasSpawnedAgentVisuals && OnToggled(true))
            {
                _tryToInitialize = false;
            }
            if (IsActive)
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
                EquipmentSelectionVM dataSource = _dataSource;
                if (dataSource == null)
                {
                    return;
                }
                dataSource.OnPeerEquipmentRefreshed(peer);
            }
        }

        private void OnGoldUpdated()
        {
            EquipmentSelectionVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.OnGoldUpdated();
        }

        private EquipmentSelectionVM _dataSource;

        private GauntletLayer _gauntletLayer;

        private MissionRepresentativeBase _myRepresentative;

        private MissionNetworkComponent _missionNetworkComponent;

        private MissionLobbyComponent _missionLobbyComponent;

        private MissionLobbyEquipmentNetworkComponent _missionLobbyEquipmentNetworkComponent;

        private MissionMultiplayerGameModeBaseClient _gameModeClient;

        private MultiplayerTeamSelectComponent _teamSelectComponent;

        private MissionGauntletMultiplayerScoreboard _scoreboardGauntletComponent;

        private MultiplayerClassDivisions.MPHeroClass _lastSelectedHeroClass;

        private bool _tryToInitialize;
    }
}
