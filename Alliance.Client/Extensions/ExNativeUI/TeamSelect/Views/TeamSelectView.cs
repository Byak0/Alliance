using Alliance.Client.Extensions.ExNativeUI.TeamSelect.ViewModels;
using Alliance.Client.Extensions.TroopSpawner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.GauntletUI.Mission;
using TaleWorlds.MountAndBlade.Multiplayer.View.MissionViews;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;

namespace Alliance.Client.Extensions.ExNativeUI.TeamSelect.Views
{
    /// <summary>
    /// Override native team selection screen.
    /// Generate banner using the culture bannerkey instead of a sprite.
    /// Customized the visuals.
    /// </summary>
    [OverrideView(typeof(MultiplayerTeamSelectUIHandler))]
    public class TeamSelectView : MissionView
    {
        public TeamSelectView()
        {
            ViewOrderPriority = 22;
        }

        public override void OnMissionScreenInitialize()
        {
            base.OnMissionScreenInitialize();
            _missionNetworkComponent = Mission.GetMissionBehavior<MissionNetworkComponent>();
            _multiplayerTeamSelectComponent = Mission.GetMissionBehavior<MultiplayerTeamSelectComponent>();
            _classLoadoutGauntletComponent = Mission.GetMissionBehavior<MissionGauntletClassLoadout>();
            _lobbyComponent = Mission.GetMissionBehavior<MissionLobbyComponent>();
            _missionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
            _lobbyComponent.OnPostMatchEnded += OnClose;
            _multiplayerTeamSelectComponent.OnSelectingTeam += MissionLobbyComponentOnSelectingTeam;
            _multiplayerTeamSelectComponent.OnUpdateTeams += MissionLobbyComponentOnUpdateTeams;
            _multiplayerTeamSelectComponent.OnUpdateFriendsPerTeam += MissionLobbyComponentOnFriendsUpdated;
            _scoreboardGauntletComponent = Mission.GetMissionBehavior<MissionGauntletMultiplayerScoreboard>();
            if (_scoreboardGauntletComponent != null)
            {
                MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
                scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Combine(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
            }
            _multiplayerTeamSelectComponent.OnMyTeamChange += OnMyTeamChanged;
        }

        public override void OnMissionScreenFinalize()
        {
            _missionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;
            _lobbyComponent.OnPostMatchEnded -= OnClose;
            _multiplayerTeamSelectComponent.OnSelectingTeam -= MissionLobbyComponentOnSelectingTeam;
            _multiplayerTeamSelectComponent.OnUpdateTeams -= MissionLobbyComponentOnUpdateTeams;
            _multiplayerTeamSelectComponent.OnUpdateFriendsPerTeam -= MissionLobbyComponentOnFriendsUpdated;
            _multiplayerTeamSelectComponent.OnMyTeamChange -= OnMyTeamChanged;
            if (_gauntletLayer != null)
            {
                _gauntletLayer.InputRestrictions.ResetInputRestrictions();
                MissionScreen.RemoveLayer(_gauntletLayer);
                _gauntletLayer = null;
            }
            if (_dataSource != null)
            {
                _dataSource.OnFinalize();
                _dataSource = null;
            }
            if (_scoreboardGauntletComponent != null)
            {
                MissionGauntletMultiplayerScoreboard scoreboardGauntletComponent = _scoreboardGauntletComponent;
                scoreboardGauntletComponent.OnScoreboardToggled = (Action<bool>)Delegate.Remove(scoreboardGauntletComponent.OnScoreboardToggled, new Action<bool>(OnScoreboardToggled));
            }
            base.OnMissionScreenFinalize();
        }

        public override bool OnEscape()
        {
            if (_isActive && !_dataSource.IsCancelDisabled)
            {
                OnClose();
                return true;
            }
            return base.OnEscape();
        }

        private void OnClose()
        {
            if (!_isActive)
            {
                return;
            }
            _isActive = false;
            _disabledTeams = null;
            MissionScreen.RemoveLayer(_gauntletLayer);
            MissionScreen.SetCameraLockState(false);
            MissionScreen.SetDisplayDialog(false);
            _gauntletLayer.InputRestrictions.ResetInputRestrictions();
            _gauntletLayer = null;
            _dataSource.OnFinalize();
            _dataSource = null;
            if (_classLoadoutGauntletComponent != null && _classLoadoutGauntletComponent.IsForceClosed)
            {
                _classLoadoutGauntletComponent.OnTryToggle(true);
            }
        }

        private void OnOpen()
        {
            if (_isActive)
            {
                return;
            }
            _isActive = true;
            string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            SpriteData spriteData = UIResourceManager.SpriteData;
            TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
            ResourceDepot uiresourceDepot = UIResourceManager.UIResourceDepot;
            _dataSource = new TeamSelectVM(Mission, new Action<Team>(OnChangeTeamTo), new Action(OnAutoassign), new Action(OnClose), Mission.Teams, strValue);
            _dataSource.RefreshDisabledTeams(_disabledTeams);
            _gauntletLayer = new GauntletLayer(ViewOrderPriority, "GauntletLayer", false);
            _gauntletLayer.LoadMovie("AL_MultiplayerTeamSelection", _dataSource);
            _gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.Mouse);
            MissionScreen.AddLayer(_gauntletLayer);
            MissionScreen.SetCameraLockState(true);
            MissionLobbyComponentOnUpdateTeams();
            MissionLobbyComponentOnFriendsUpdated();
        }

        private void OnChangeTeamTo(Team targetTeam)
        {
            _multiplayerTeamSelectComponent.ChangeTeam(targetTeam);
        }

        private void OnMyTeamChanged()
        {
            if (GameNetwork.MyPeer.GetComponent<MissionPeer>()?.Team?.Side != null)
            {
                MissionPeer player = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                // Update culture selected in SpawnTroopModel
                BasicCultureObject culture1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
                BasicCultureObject culture2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
                SpawnTroopsModel.Instance.SelectedFaction = player.Team?.Side == BattleSideEnum.Attacker ? culture1 : culture2;
                // TODO : Bug seems to be : you switch team, your agent dies but stays as the owner of the controller,
                // yet by dying he is not in a team anymore, so team returns null and it crashes
                // Try to assign a valid agent as the controller owner
                // Or assign a null owner ?
                //InformationManager.DisplayMessage(new InformationMessage("Agent = " + player.Name, Colors.Yellow));
                //InformationManager.DisplayMessage(new InformationMessage(player.Team?.PlayerOrderController?.Owner?.Name, Colors.Yellow));
                if (player.Team?.PlayerOrderController?.Owner != null)
                {
                    player.Team.PlayerOrderController.Owner = null;
                    //Agent newController = player.Team.GeneralAgent != null ? player.Team.GeneralAgent : player.Team.ActiveAgents.FirstOrDefault();
                    //player.Team.PlayerOrderController.Owner = newController;
                }
            }

            OnClose();
        }

        private void OnAutoassign()
        {
            _multiplayerTeamSelectComponent.AutoAssignTeam(GameNetwork.MyPeer);
        }

        public override void OnMissionScreenTick(float dt)
        {
            base.OnMissionScreenTick(dt);
            if (_isSynchronized && _toOpen && MissionScreen.SetDisplayDialog(true))
            {
                _toOpen = false;
                OnOpen();
            }
            TeamSelectVM dataSource = _dataSource;
            if (dataSource == null)
            {
                return;
            }
            dataSource.Tick(dt);
        }

        private void MissionLobbyComponentOnSelectingTeam(List<Team> disabledTeams)
        {
            _disabledTeams = disabledTeams;

            // Do not open Team Select menu. Instead assign players based on PvC roles.
            // TODO : make this work with other cases (CvC / PvP)
            // Also should make a way for admin to switch teams
            _toOpen = true;
            //Team spectator = Mission.Teams.FirstOrDefault((t) => t.Side == BattleSideEnum.None);
            //Team attacker = Mission.Teams.FirstOrDefault((t) => t.Side == BattleSideEnum.Attacker);
            //Team defender = Mission.Teams.FirstOrDefault((t) => t.Side == BattleSideEnum.Defender);
            //if (PvCRoles.Instance.Commanders.Contains(GameNetwork.MyPeer.UserName))
            //{
            //    OnChangeTeamTo(attacker);
            //} else if (PvCRoles.Instance.Officers.Contains(GameNetwork.MyPeer.UserName))
            //{
            //    OnChangeTeamTo(defender);
            //} else
            //{
            //    OnChangeTeamTo(defender);
            //}
        }

        private void MissionLobbyComponentOnFriendsUpdated()
        {
            if (!_isActive)
            {
                return;
            }
            IEnumerable<MissionPeer> enumerable = from x in _multiplayerTeamSelectComponent.GetFriendsForTeam(Mission.AttackerTeam)
                                                  select x.GetComponent<MissionPeer>();
            IEnumerable<MissionPeer> enumerable2 = from x in _multiplayerTeamSelectComponent.GetFriendsForTeam(Mission.DefenderTeam)
                                                   select x.GetComponent<MissionPeer>();
            _dataSource.RefreshFriendsPerTeam(enumerable, enumerable2);
        }

        private void MissionLobbyComponentOnUpdateTeams()
        {
            if (!_isActive)
            {
                return;
            }
            List<Team> disabledTeams = _multiplayerTeamSelectComponent.GetDisabledTeams();
            _dataSource.RefreshDisabledTeams(disabledTeams);
            int playerCountForTeam = _multiplayerTeamSelectComponent.GetPlayerCountForTeam(Mission.AttackerTeam);
            int playerCountForTeam2 = _multiplayerTeamSelectComponent.GetPlayerCountForTeam(Mission.DefenderTeam);
            int intValue = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            int intValue2 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            _dataSource.RefreshPlayerAndBotCount(playerCountForTeam, playerCountForTeam2, intValue, intValue2);
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

        private void OnMyClientSynchronized()
        {
            _isSynchronized = true;
        }

        private GauntletLayer _gauntletLayer;

        private TeamSelectVM _dataSource;

        private MissionNetworkComponent _missionNetworkComponent;

        private MultiplayerTeamSelectComponent _multiplayerTeamSelectComponent;

        private MissionGauntletMultiplayerScoreboard _scoreboardGauntletComponent;

        private MissionGauntletClassLoadout _classLoadoutGauntletComponent;

        private MissionLobbyComponent _lobbyComponent;

        private List<Team> _disabledTeams;

        private bool _toOpen;

        private bool _isSynchronized;

        private bool _isActive;
    }
}
