using Alliance.Common.Extensions.TroopSpawner.Models;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace Alliance.Client.Extensions.ExNativeUI.HUDExtension.ViewModels
{
    public class SpectatorHUDVM : ViewModel
    {
        private readonly Mission _mission;

        private readonly bool _isTeamsEnabled;

        private readonly bool _isFlagDominationMode;

        private Agent _spectatedAgent;

        private string _spectatedPlayerName;

        private string _takeControlText;

        private int _spectatedPlayerNeutrality = -1;

        private bool _isSpectatingPlayer;

        private bool _canTakeControlOfSpectatedAgent;

        private bool _agentHasMount;

        private bool _agentHasShield;

        private bool _showAgentHealth;

        private bool _agentHasRangedWeapon;

        private bool _agentHasCompassElement;

        private float _spectatedPlayerHealthLimit;

        private float _spectatedPlayerCurrentHealth;

        private float _spectatedPlayerMountCurrentHealth;

        private float _spectatedPlayerMountHealthLimit;

        private float _spectatedPlayerShieldCurrentHealth;

        private float _spectatedPlayerShieldHealthLimit;

        private int _spectatedPlayerAmmoAmount;

        private MPTeammateCompassTargetVM _compassElement;

        [DataSourceProperty]
        public int SpectatedPlayerNeutrality
        {
            get
            {
                return _spectatedPlayerNeutrality;
            }
            set
            {
                if (value != _spectatedPlayerNeutrality)
                {
                    _spectatedPlayerNeutrality = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerNeutrality");
                    IsSpectatingAgent = value >= 0;
                }
            }
        }

        [DataSourceProperty]
        public MPTeammateCompassTargetVM CompassElement
        {
            get
            {
                return _compassElement;
            }
            set
            {
                if (value != _compassElement)
                {
                    _compassElement = value;
                    OnPropertyChangedWithValue(value, "CompassElement");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSpectatingAgent
        {
            get
            {
                return _isSpectatingPlayer;
            }
            set
            {
                if (value != _isSpectatingPlayer)
                {
                    _isSpectatingPlayer = value;
                    OnPropertyChangedWithValue(value, "IsSpectatingAgent");
                }
            }
        }

        [DataSourceProperty]
        public bool AgentHasCompassElement
        {
            get
            {
                return _agentHasCompassElement;
            }
            set
            {
                if (value != _agentHasCompassElement)
                {
                    _agentHasCompassElement = value;
                    OnPropertyChangedWithValue(value, "AgentHasCompassElement");
                }
            }
        }

        [DataSourceProperty]
        public bool AgentHasMount
        {
            get
            {
                return _agentHasMount;
            }
            set
            {
                if (value != _agentHasMount)
                {
                    _agentHasMount = value;
                    OnPropertyChangedWithValue(value, "AgentHasMount");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowAgentHealth
        {
            get
            {
                return _showAgentHealth;
            }
            set
            {
                if (value != _showAgentHealth)
                {
                    _showAgentHealth = value;
                    OnPropertyChangedWithValue(value, "ShowAgentHealth");
                }
            }
        }

        [DataSourceProperty]
        public bool AgentHasRangedWeapon
        {
            get
            {
                return _agentHasRangedWeapon;
            }
            set
            {
                if (value != _agentHasRangedWeapon)
                {
                    _agentHasRangedWeapon = value;
                    OnPropertyChangedWithValue(value, "AgentHasRangedWeapon");
                }
            }
        }

        [DataSourceProperty]
        public bool AgentHasShield
        {
            get
            {
                return _agentHasShield;
            }
            set
            {
                if (value != _agentHasShield)
                {
                    _agentHasShield = value;
                    OnPropertyChangedWithValue(value, "AgentHasShield");
                }
            }
        }

        [DataSourceProperty]
        public bool CanTakeControlOfSpectatedAgent
        {
            get
            {
                return _canTakeControlOfSpectatedAgent;
            }
            set
            {
                if (value != _canTakeControlOfSpectatedAgent)
                {
                    _canTakeControlOfSpectatedAgent = value;
                    OnPropertyChangedWithValue(value, "CanTakeControlOfSpectatedAgent");
                }
            }
        }

        [DataSourceProperty]
        public string SpectatedPlayerName
        {
            get
            {
                return _spectatedPlayerName;
            }
            set
            {
                if (value != _spectatedPlayerName)
                {
                    _spectatedPlayerName = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerName");
                }
            }
        }

        [DataSourceProperty]
        public string TakeControlText
        {
            get
            {
                return _takeControlText;
            }
            set
            {
                if (value != _takeControlText)
                {
                    _takeControlText = value;
                    OnPropertyChangedWithValue(value, "TakeControlText");
                }
            }
        }

        [DataSourceProperty]
        public float SpectatedPlayerHealthLimit
        {
            get
            {
                return _spectatedPlayerHealthLimit;
            }
            set
            {
                if (value != _spectatedPlayerHealthLimit)
                {
                    _spectatedPlayerHealthLimit = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerHealthLimit");
                }
            }
        }

        [DataSourceProperty]
        public float SpectatedPlayerCurrentHealth
        {
            get
            {
                return _spectatedPlayerCurrentHealth;
            }
            set
            {
                if (value != _spectatedPlayerCurrentHealth)
                {
                    _spectatedPlayerCurrentHealth = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerCurrentHealth");
                }
            }
        }

        [DataSourceProperty]
        public float SpectatedPlayerMountCurrentHealth
        {
            get
            {
                return _spectatedPlayerMountCurrentHealth;
            }
            set
            {
                if (value != _spectatedPlayerMountCurrentHealth)
                {
                    _spectatedPlayerMountCurrentHealth = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerMountCurrentHealth");
                }
            }
        }

        [DataSourceProperty]
        public float SpectatedPlayerMountHealthLimit
        {
            get
            {
                return _spectatedPlayerMountHealthLimit;
            }
            set
            {
                if (value != _spectatedPlayerMountHealthLimit)
                {
                    _spectatedPlayerMountHealthLimit = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerMountHealthLimit");
                }
            }
        }

        [DataSourceProperty]
        public float SpectatedPlayerShieldCurrentHealth
        {
            get
            {
                return _spectatedPlayerShieldCurrentHealth;
            }
            set
            {
                if (value != _spectatedPlayerShieldCurrentHealth)
                {
                    _spectatedPlayerShieldCurrentHealth = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerShieldCurrentHealth");
                }
            }
        }

        [DataSourceProperty]
        public float SpectatedPlayerShieldHealthLimit
        {
            get
            {
                return _spectatedPlayerShieldHealthLimit;
            }
            set
            {
                if (value != _spectatedPlayerShieldHealthLimit)
                {
                    _spectatedPlayerShieldHealthLimit = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerShieldHealthLimit");
                }
            }
        }

        [DataSourceProperty]
        public int SpectatedPlayerAmmoAmount
        {
            get
            {
                return _spectatedPlayerAmmoAmount;
            }
            set
            {
                if (value != _spectatedPlayerAmmoAmount)
                {
                    _spectatedPlayerAmmoAmount = value;
                    OnPropertyChangedWithValue(value, "SpectatedPlayerAmmoAmount");
                }
            }
        }

        public SpectatorHUDVM(Mission mission)
        {
            _mission = mission;
            MissionLobbyComponent missionBehavior = mission.GetMissionBehavior<MissionLobbyComponent>();
            _isTeamsEnabled = missionBehavior.MissionType != 0 && missionBehavior.MissionType != MultiplayerGameType.Duel;
            _isFlagDominationMode = Mission.Current.HasMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
            GameTexts.SetVariable("USE_KEY", keyHyperlinkText);
            TakeControlText = GameTexts.FindText("str_sergeant_battle_press_action_to_control_bot_2").ToString();
        }

        public void Tick(float dt)
        {
            if (_mission.MainAgent != null)
            {
                SpectatedPlayerNeutrality = -1;
            }

            UpdateDynamicProperties();
        }

        private void UpdateDynamicProperties()
        {
            AgentHasShield = false;
            AgentHasMount = false;
            ShowAgentHealth = false;
            AgentHasRangedWeapon = false;
            if (SpectatedPlayerNeutrality <= 0 || _spectatedAgent == null)
            {
                return;
            }

            ShowAgentHealth = true;
            SpectatedPlayerHealthLimit = _spectatedAgent.HealthLimit;
            SpectatedPlayerCurrentHealth = _spectatedAgent.Health;
            AgentHasMount = _spectatedAgent.MountAgent != null;
            if (AgentHasMount)
            {
                SpectatedPlayerMountCurrentHealth = _spectatedAgent.MountAgent.Health;
                SpectatedPlayerMountHealthLimit = _spectatedAgent.MountAgent.HealthLimit;
            }

            EquipmentIndex wieldedItemIndex = _spectatedAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            EquipmentIndex wieldedItemIndex2 = _spectatedAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            int num = -1;
            if (wieldedItemIndex != EquipmentIndex.None && _spectatedAgent.Equipment[wieldedItemIndex].CurrentUsageItem != null)
            {
                if (_spectatedAgent.Equipment[wieldedItemIndex].CurrentUsageItem.IsRangedWeapon && _spectatedAgent.Equipment[wieldedItemIndex].CurrentUsageItem.IsConsumable)
                {
                    int ammoAmount = _spectatedAgent.Equipment.GetAmmoAmount(wieldedItemIndex);
                    if (_spectatedAgent.Equipment[wieldedItemIndex].ModifiedMaxAmount == 1 || ammoAmount > 0)
                    {
                        num = _spectatedAgent.Equipment[wieldedItemIndex].ModifiedMaxAmount == 1 ? -1 : ammoAmount;
                    }
                }
                else if (_spectatedAgent.Equipment[wieldedItemIndex].CurrentUsageItem.IsRangedWeapon)
                {
                    bool flag = _spectatedAgent.Equipment[wieldedItemIndex].CurrentUsageItem.WeaponClass == WeaponClass.Crossbow;
                    num = _spectatedAgent.Equipment.GetAmmoAmount(wieldedItemIndex) + (flag ? _spectatedAgent.Equipment[wieldedItemIndex].Ammo : 0);
                }
            }

            if (wieldedItemIndex2 != EquipmentIndex.None && _spectatedAgent.Equipment[wieldedItemIndex2].CurrentUsageItem != null)
            {
                MissionWeapon missionWeapon = _spectatedAgent.Equipment[wieldedItemIndex2];
                AgentHasShield = missionWeapon.CurrentUsageItem.IsShield;
                if (AgentHasShield)
                {
                    SpectatedPlayerShieldHealthLimit = missionWeapon.ModifiedMaxHitPoints;
                    SpectatedPlayerShieldCurrentHealth = missionWeapon.HitPoints;
                }
            }

            AgentHasRangedWeapon = num >= 0;
            SpectatedPlayerAmmoAmount = num;
        }

        internal void OnSpectatedAgentFocusIn(Agent followedAgent)
        {
            MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();

            if (component?.Team == null)
            {
                return;
            }

            _spectatedAgent = followedAgent;
            int spectatedPlayerNeutrality = 0;
            if (component.Team != _mission.SpectatorTeam && component.Team == followedAgent.Team && _isTeamsEnabled)
            {
                spectatedPlayerNeutrality = 1;
            }

            SpectatedPlayerNeutrality = spectatedPlayerNeutrality;
            SpectatedPlayerName = followedAgent.MissionPeer?.DisplayedName ?? followedAgent.Name.ToString();
            CanTakeControlOfSpectatedAgent = followedAgent.Team != null && followedAgent.Team.Side == component.Team.Side && followedAgent.Formation != null && FormationControlModel.Instance.GetControlledFormations(component).Contains(followedAgent.Formation.FormationIndex);
            CompassElement = null;
            AgentHasCompassElement = false;
            MissionPeer missionPeer = followedAgent.MissionPeer ?? followedAgent.Formation?.PlayerOwner?.MissionPeer;
            if (missionPeer != null)
            {
                TargetIconType iconType = MultiplayerClassDivisions.GetMPHeroClassForPeer(missionPeer)?.IconType ?? TargetIconType.None;
                BannerCode bannercode = BannerCode.CreateFrom(new Banner(missionPeer.Peer.BannerCode, missionPeer.Team.Color, missionPeer.Team.Color2));
                CompassElement = new MPTeammateCompassTargetVM(iconType, missionPeer.Team.Color, missionPeer.Team.Color2, bannercode, missionPeer.Team.IsPlayerAlly);
                AgentHasCompassElement = true;
            }
        }

        internal void OnSpectatedAgentFocusOut(Agent followedPeer)
        {
            _spectatedAgent = null;
            SpectatedPlayerNeutrality = -1;
        }
    }
}