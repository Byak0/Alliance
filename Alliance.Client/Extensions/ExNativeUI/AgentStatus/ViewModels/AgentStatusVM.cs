using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using System;
using System.ComponentModel;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD;
using TaleWorlds.MountAndBlade.ViewModelCollection.HUD.DamageFeed;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Client.Extensions.ExNativeUI.AgentStatus.ViewModels
{
    public class AgentStatusVM : ViewModel
    {
        public bool IsInDeployement { get; set; }

        public AgentStatusVM(Mission mission, Camera missionCamera, Func<float> getCameraToggleProgress)
        {
            _missionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();
            InteractionInterface = new AgentInteractionInterfaceVM(mission);
            _mission = mission;
            _missionCamera = missionCamera;
            _getCameraToggleProgress = getCameraToggleProgress;
            PrimaryWeapon = new ImageIdentifierVM(ImageIdentifierType.Item);
            OffhandWeapon = new ImageIdentifierVM(ImageIdentifierType.Item);
            TakenDamageFeed = new MissionAgentDamageFeedVM();
            TakenDamageController = new MissionAgentTakenDamageVM(_missionCamera);
            IsInteractionAvailable = true;
            RefreshValues();
        }

        public void InitializeMainAgentPropterties()
        {
            Mission.Current.OnMainAgentChanged += OnMainAgentChanged;
            OnMainAgentChanged(_mission, null);
            OnMainAgentWeaponChange();
            _mpGameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            CameraToggleText = GameTexts.FindText("str_toggle_camera", null).ToString();
        }

        private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Agent.Main != null)
            {
                Agent main = Agent.Main;
                main.OnMainAgentWieldedItemChange = (Agent.OnMainAgentWieldedItemChangeDelegate)Delegate.Combine(main.OnMainAgentWieldedItemChange, new Agent.OnMainAgentWieldedItemChangeDelegate(OnMainAgentWeaponChange));
                OnMainAgentWeaponChange();
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            Mission.Current.OnMainAgentChanged -= OnMainAgentChanged;
            TakenDamageFeed.OnFinalize();
        }

        public void Tick(float dt)
        {
            if (_mission == null) return;

            // Native TW way of setting attributes...
            if (_missionPeer == null) _missionPeer = GameNetwork.MyPeer.GetComponent<MissionPeer>();

            CouchLanceState = GetCouchLanceState();
            SpearBraceState = GetSpearBraceState();
            Func<float> getCameraToggleProgress = _getCameraToggleProgress;
            CameraToggleProgress = getCameraToggleProgress != null ? getCameraToggleProgress() : 0f;
            if (_mission.MainAgent != null && !IsInDeployement)
            {
                ShowAgentHealthBar = true;

                //InteractionInterface.Tick();
                InteractionInterface.GetType().GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InteractionInterface, null);

                if (_mission.Mode == MissionMode.Battle && !_mission.IsFriendlyMission && _missionPeer != null)
                {
                    MissionPeer myMissionPeer = _missionPeer;
                    IsTroopsActive = (myMissionPeer != null ? myMissionPeer.ControlledFormation : null) != null;
                    if (IsTroopsActive)
                    {
                        TroopCount = _missionPeer.ControlledFormation.CountOfUnits;
                        FormationClass defaultFormationGroup = (FormationClass)MultiplayerClassDivisions.GetMPHeroClassForPeer(_missionPeer, false).TroopCharacter.DefaultFormationGroup;
                        TroopsAmmoAvailable = defaultFormationGroup == FormationClass.Ranged || defaultFormationGroup == FormationClass.HorseArcher;
                        if (TroopsAmmoAvailable)
                        {
                            int totalCurrentAmmo = 0;
                            int totalMaxAmmo = 0;
                            _missionPeer.ControlledFormation.ApplyActionOnEachUnit(delegate (Agent agent)
                            {
                                if (!agent.IsMainAgent)
                                {
                                    int num;
                                    int num2;
                                    GetMaxAndCurrentAmmoOfAgent(agent, out num, out num2);
                                    totalCurrentAmmo += num;
                                    totalMaxAmmo += num2;
                                }
                            });
                            TroopsAmmoPercentage = totalCurrentAmmo / (float)totalMaxAmmo;
                        }
                    }
                }
                UpdateWeaponStatuses();
                UpdateAgentAndMountStatuses();
                IsPlayerActive = true;
                IsCombatUIActive = true;
            }
            else
            {
                AgentHealth = 0;
                ShowMountHealthBar = false;
                ShowShieldHealthBar = false;
                if (IsCombatUIActive)
                {
                    _combatUIRemainTimer += dt;
                    if (_combatUIRemainTimer >= 3f)
                    {
                        IsCombatUIActive = false;
                    }
                }
            }
            MissionMultiplayerGameModeBaseClient mpGameMode = _mpGameMode;

            // Activate gold for commanders only
            IsGoldActive = GameNetwork.MyPeer.IsCommander() && Config.Instance.UseTroopCost;
            if (IsGoldActive) GoldAmount = GameNetwork.MyPeer.GetComponent<MissionRepresentativeBase>()?.Gold ?? 0;

            MissionAgentTakenDamageVM takenDamageController = TakenDamageController;
            if (takenDamageController == null)
            {
                return;
            }
            //takenDamageController.Tick(dt);
            takenDamageController.GetType().GetMethod("Tick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(takenDamageController, new object[] { dt });
        }

        private void UpdateWeaponStatuses()
        {
            bool flag = false;
            if (_mission.MainAgent != null)
            {
                int num = -1;
                EquipmentIndex wieldedItemIndex = _mission.MainAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
                EquipmentIndex wieldedItemIndex2 = _mission.MainAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
                if (wieldedItemIndex != EquipmentIndex.None && _mission.MainAgent.Equipment[wieldedItemIndex].CurrentUsageItem != null)
                {
                    if (_mission.MainAgent.Equipment[wieldedItemIndex].CurrentUsageItem.IsRangedWeapon && _mission.MainAgent.Equipment[wieldedItemIndex].CurrentUsageItem.IsConsumable)
                    {
                        int ammoAmount = _mission.MainAgent.Equipment.GetAmmoAmount(wieldedItemIndex);
                        if (_mission.MainAgent.Equipment[wieldedItemIndex].ModifiedMaxAmount == 1 || ammoAmount > 0)
                        {
                            num = ammoAmount;
                        }
                    }
                    else if (_mission.MainAgent.Equipment[wieldedItemIndex].CurrentUsageItem.IsRangedWeapon)
                    {
                        bool flag2 = _mission.MainAgent.Equipment[wieldedItemIndex].CurrentUsageItem.WeaponClass == WeaponClass.Crossbow;
                        num = _mission.MainAgent.Equipment.GetAmmoAmount(wieldedItemIndex) + (flag2 ? _mission.MainAgent.Equipment[wieldedItemIndex].Ammo : 0);
                    }
                    if (!_mission.MainAgent.Equipment[wieldedItemIndex].IsEmpty)
                    {
                        int maxAmmo = _mission.MainAgent.Equipment.GetMaxAmmo(wieldedItemIndex);
                        float num2 = maxAmmo * 0.2f;
                        flag = maxAmmo != AmmoCount && AmmoCount <= MathF.Ceiling(num2);
                    }
                }
                if (wieldedItemIndex2 != EquipmentIndex.None && _mission.MainAgent.Equipment[wieldedItemIndex2].CurrentUsageItem != null)
                {
                    MissionWeapon missionWeapon = _mission.MainAgent.Equipment[wieldedItemIndex2];
                    ShowShieldHealthBar = missionWeapon.CurrentUsageItem.IsShield;
                    if (ShowShieldHealthBar)
                    {
                        ShieldHealthMax = missionWeapon.ModifiedMaxHitPoints;
                        ShieldHealth = missionWeapon.HitPoints;
                    }
                }
                AmmoCount = num;
            }
            else
            {
                ShieldHealth = 0;
                AmmoCount = 0;
                ShowShieldHealthBar = false;
            }
            IsAmmoCountAlertEnabled = flag;
        }

        public void OnEquipmentInteractionViewToggled(bool isActive)
        {
            IsInteractionAvailable = !isActive;
        }

        private void UpdateAgentAndMountStatuses()
        {
            if (_mission.MainAgent == null)
            {
                AgentHealthMax = 1;
                AgentHealth = (int)_mission.MainAgent.Health;
                HorseHealthMax = 1;
                HorseHealth = 0;
                ShowMountHealthBar = false;
                return;
            }
            AgentHealthMax = (int)_mission.MainAgent.HealthLimit;
            AgentHealth = (int)_mission.MainAgent.Health;
            if (_mission.MainAgent.MountAgent != null)
            {
                HorseHealthMax = (int)_mission.MainAgent.MountAgent.HealthLimit;
                HorseHealth = (int)_mission.MainAgent.MountAgent.Health;
                ShowMountHealthBar = true;
                return;
            }
            ShowMountHealthBar = false;
        }

        public void OnMainAgentWeaponChange()
        {
            if (_mission.MainAgent == null)
            {
                return;
            }
            MissionWeapon missionWeapon = MissionWeapon.Invalid;
            MissionWeapon missionWeapon2 = MissionWeapon.Invalid;
            EquipmentIndex equipmentIndex = _mission.MainAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            if (equipmentIndex > EquipmentIndex.None && equipmentIndex < EquipmentIndex.NumAllWeaponSlots)
            {
                missionWeapon = _mission.MainAgent.Equipment[equipmentIndex];
            }
            equipmentIndex = _mission.MainAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            if (equipmentIndex > EquipmentIndex.None && equipmentIndex < EquipmentIndex.NumAllWeaponSlots)
            {
                missionWeapon2 = _mission.MainAgent.Equipment[equipmentIndex];
            }
            WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
            ShowShieldHealthBar = currentUsageItem != null && currentUsageItem.IsShield;
            PrimaryWeapon = missionWeapon2.IsEmpty ? new ImageIdentifierVM(ImageIdentifierType.Null) : new ImageIdentifierVM(missionWeapon2.Item, "");
            OffhandWeapon = missionWeapon.IsEmpty ? new ImageIdentifierVM(ImageIdentifierType.Null) : new ImageIdentifierVM(missionWeapon.Item, "");
        }

        public void OnAgentRemoved(Agent agent)
        {
            //InteractionInterface.CheckAndClearFocusedAgent(agent);
            InteractionInterface.GetType().GetMethod("CheckAndClearFocusedAgent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InteractionInterface, new object[] { agent });
        }

        public void OnAgentDeleted(Agent agent)
        {
            //InteractionInterface.CheckAndClearFocusedAgent(agent);
            InteractionInterface.GetType().GetMethod("CheckAndClearFocusedAgent", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InteractionInterface, new object[] { agent });
        }

        public void OnMainAgentHit(int damage, float distance)
        {
            //TakenDamageController.OnMainAgentHit(damage, distance);
            TakenDamageController.GetType().GetMethod("OnMainAgentHit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(TakenDamageController, new object[] { damage, distance });
        }

        public void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
        {
            //InteractionInterface.OnFocusGained(mainAgent, focusableObject, isInteractable);
            InteractionInterface.GetType().GetMethod("OnFocusGained", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InteractionInterface, new object[] { mainAgent, focusableObject, isInteractable });
        }

        public void OnFocusLost(Agent agent, IFocusable focusableObject)
        {
            //InteractionInterface.OnFocusLost(agent, focusableObject);
            InteractionInterface.GetType().GetMethod("OnFocusLost", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InteractionInterface, new object[] { agent, focusableObject });
        }

        public void OnSecondaryFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
        {
        }

        public void OnSecondaryFocusLost(Agent agent, IFocusable focusableObject)
        {
        }

        public void OnAgentInteraction(Agent userAgent, Agent agent)
        {
            //InteractionInterface.OnAgentInteraction(userAgent, agent);
            InteractionInterface.GetType().GetMethod("OnAgentInteraction", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InteractionInterface, new object[] { userAgent, agent });
        }

        private void GetMaxAndCurrentAmmoOfAgent(Agent agent, out int currentAmmo, out int maxAmmo)
        {
            currentAmmo = 0;
            maxAmmo = 0;
            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.ExtraWeaponSlot; equipmentIndex++)
            {
                if (!agent.Equipment[equipmentIndex].IsEmpty && agent.Equipment[equipmentIndex].CurrentUsageItem.IsRangedWeapon)
                {
                    currentAmmo = agent.Equipment.GetAmmoAmount(equipmentIndex);
                    maxAmmo = agent.Equipment.GetMaxAmmo(equipmentIndex);
                    return;
                }
            }
        }

        private int GetCouchLanceState()
        {
            int num = 0;
            if (Agent.Main != null)
            {
                MissionWeapon wieldedWeapon = Agent.Main.WieldedWeapon;
                if (Agent.Main.HasMount && IsWeaponCouchable(wieldedWeapon))
                {
                    if (IsPassiveUsageActiveWithCurrentWeapon(wieldedWeapon))
                    {
                        num = 3;
                    }
                    else if (IsConditionsMetForCouching())
                    {
                        num = 2;
                    }
                }
            }
            return num;
        }

        private bool IsWeaponCouchable(MissionWeapon weapon)
        {
            if (weapon.IsEmpty)
            {
                return false;
            }
            foreach (WeaponComponentData weaponComponentData in weapon.Item.Weapons)
            {
                string weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
                if (weaponDescriptionId != null && weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsConditionsMetForCouching()
        {
            return Agent.Main.HasMount && Agent.Main.IsPassiveUsageConditionsAreMet;
        }

        private int GetSpearBraceState()
        {
            int num = 0;
            if (Agent.Main != null)
            {
                MissionWeapon wieldedWeapon = Agent.Main.WieldedWeapon;
                if (!Agent.Main.HasMount && Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand) == EquipmentIndex.None && IsWeaponBracable(wieldedWeapon))
                {
                    if (IsPassiveUsageActiveWithCurrentWeapon(wieldedWeapon))
                    {
                        num = 3;
                    }
                    else if (IsConditionsMetForBracing())
                    {
                        num = 2;
                    }
                }
            }
            return num;
        }

        private bool IsWeaponBracable(MissionWeapon weapon)
        {
            if (weapon.IsEmpty)
            {
                return false;
            }
            foreach (WeaponComponentData weaponComponentData in weapon.Item.Weapons)
            {
                string weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
                if (weaponDescriptionId != null && weaponDescriptionId.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsConditionsMetForBracing()
        {
            return !Agent.Main.HasMount && !Agent.Main.WalkMode && Agent.Main.IsPassiveUsageConditionsAreMet;
        }

        private bool IsPassiveUsageActiveWithCurrentWeapon(MissionWeapon weapon)
        {
            return !weapon.IsEmpty && MBItem.GetItemIsPassiveUsage(weapon.CurrentUsageItem.ItemUsage);
        }

        [DataSourceProperty]
        public MissionAgentTakenDamageVM TakenDamageController
        {
            get
            {
                return _takenDamageController;
            }
            set
            {
                if (value != _takenDamageController)
                {
                    _takenDamageController = value;
                    OnPropertyChangedWithValue(value, "TakenDamageController");
                }
            }
        }

        [DataSourceProperty]
        public AgentInteractionInterfaceVM InteractionInterface
        {
            get
            {
                return _interactionInterface;
            }
            set
            {
                if (value != _interactionInterface)
                {
                    _interactionInterface = value;
                    OnPropertyChangedWithValue(value, "InteractionInterface");
                }
            }
        }

        [DataSourceProperty]
        public int AgentHealth
        {
            get
            {
                return _agentHealth;
            }
            set
            {
                if (value != _agentHealth)
                {
                    if (value <= 0)
                    {
                        _agentHealth = 0;
                        OffhandWeapon = new ImageIdentifierVM(ImageIdentifierType.Null);
                        PrimaryWeapon = new ImageIdentifierVM(ImageIdentifierType.Null);
                        AmmoCount = -1;
                        ShieldHealth = 100;
                        IsPlayerActive = false;
                    }
                    else
                    {
                        _agentHealth = value;
                    }
                    OnPropertyChangedWithValue(value, "AgentHealth");
                }
            }
        }

        [DataSourceProperty]
        public int AgentHealthMax
        {
            get
            {
                return _agentHealthMax;
            }
            set
            {
                if (value != _agentHealthMax)
                {
                    _agentHealthMax = value;
                    OnPropertyChangedWithValue(value, "AgentHealthMax");
                }
            }
        }

        [DataSourceProperty]
        public int HorseHealth
        {
            get
            {
                return _horseHealth;
            }
            set
            {
                if (value != _horseHealth)
                {
                    _horseHealth = value;
                    OnPropertyChangedWithValue(value, "HorseHealth");
                }
            }
        }

        [DataSourceProperty]
        public int HorseHealthMax
        {
            get
            {
                return _horseHealthMax;
            }
            set
            {
                if (value != _horseHealthMax)
                {
                    _horseHealthMax = value;
                    OnPropertyChangedWithValue(value, "HorseHealthMax");
                }
            }
        }

        [DataSourceProperty]
        public int ShieldHealth
        {
            get
            {
                return _shieldHealth;
            }
            set
            {
                if (value != _shieldHealth)
                {
                    _shieldHealth = value;
                    OnPropertyChangedWithValue(value, "ShieldHealth");
                }
            }
        }

        [DataSourceProperty]
        public int ShieldHealthMax
        {
            get
            {
                return _shieldHealthMax;
            }
            set
            {
                if (value != _shieldHealthMax)
                {
                    _shieldHealthMax = value;
                    OnPropertyChangedWithValue(value, "ShieldHealthMax");
                }
            }
        }

        [DataSourceProperty]
        public bool IsPlayerActive
        {
            get
            {
                return _isPlayerActive;
            }
            set
            {
                if (value != _isPlayerActive)
                {
                    _isPlayerActive = value;
                    OnPropertyChangedWithValue(value, "IsPlayerActive");
                }
            }
        }

        public bool IsCombatUIActive
        {
            get
            {
                return _isCombatUIActive;
            }
            set
            {
                if (value != _isCombatUIActive)
                {
                    _isCombatUIActive = value;
                    OnPropertyChangedWithValue(value, "IsCombatUIActive");
                    _combatUIRemainTimer = 0f;
                }
            }
        }

        [DataSourceProperty]
        public bool ShowAgentHealthBar
        {
            get
            {
                return _showAgentHealthBar;
            }
            set
            {
                if (value != _showAgentHealthBar)
                {
                    _showAgentHealthBar = value;
                    OnPropertyChangedWithValue(value, "ShowAgentHealthBar");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowMountHealthBar
        {
            get
            {
                return _showMountHealthBar;
            }
            set
            {
                if (value != _showMountHealthBar)
                {
                    _showMountHealthBar = value;
                    OnPropertyChangedWithValue(value, "ShowMountHealthBar");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowShieldHealthBar
        {
            get
            {
                return _showShieldHealthBar;
            }
            set
            {
                if (value != _showShieldHealthBar)
                {
                    _showShieldHealthBar = value;
                    OnPropertyChangedWithValue(value, "ShowShieldHealthBar");
                }
            }
        }

        [DataSourceProperty]
        public bool IsInteractionAvailable
        {
            get
            {
                return _isInteractionAvailable;
            }
            set
            {
                if (value != _isInteractionAvailable)
                {
                    _isInteractionAvailable = value;
                    OnPropertyChangedWithValue(value, "IsInteractionAvailable");
                }
            }
        }

        [DataSourceProperty]
        public bool IsAgentStatusAvailable
        {
            get
            {
                return _isAgentStatusAvailable;
            }
            set
            {
                if (value != _isAgentStatusAvailable)
                {
                    _isAgentStatusAvailable = value;
                    OnPropertyChangedWithValue(value, "IsAgentStatusAvailable");
                }
            }
        }

        [DataSourceProperty]
        public int CouchLanceState
        {
            get
            {
                return _couchLanceState;
            }
            set
            {
                if (value != _couchLanceState)
                {
                    _couchLanceState = value;
                    OnPropertyChangedWithValue(value, "CouchLanceState");
                }
            }
        }

        [DataSourceProperty]
        public int SpearBraceState
        {
            get
            {
                return _spearBraceState;
            }
            set
            {
                if (value != _spearBraceState)
                {
                    _spearBraceState = value;
                    OnPropertyChangedWithValue(value, "SpearBraceState");
                }
            }
        }

        [DataSourceProperty]
        public int TroopCount
        {
            get
            {
                return _troopCount;
            }
            set
            {
                if (value != _troopCount)
                {
                    _troopCount = value;
                    OnPropertyChangedWithValue(value, "TroopCount");
                }
            }
        }

        [DataSourceProperty]
        public bool IsTroopsActive
        {
            get
            {
                return _isTroopsActive;
            }
            set
            {
                if (value != _isTroopsActive)
                {
                    _isTroopsActive = value;
                    OnPropertyChangedWithValue(value, "IsTroopsActive");
                }
            }
        }

        [DataSourceProperty]
        public bool IsGoldActive
        {
            get
            {
                return _isGoldActive;
            }
            set
            {
                if (value != _isGoldActive)
                {
                    _isGoldActive = value;
                    OnPropertyChangedWithValue(value, "IsGoldActive");
                }
            }
        }

        [DataSourceProperty]
        public int GoldAmount
        {
            get
            {
                return _goldAmount;
            }
            set
            {
                if (value != _goldAmount)
                {
                    _goldAmount = value;
                    OnPropertyChangedWithValue(value, "GoldAmount");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowAmmoCount
        {
            get
            {
                return _showAmmoCount;
            }
            set
            {
                if (value != _showAmmoCount)
                {
                    _showAmmoCount = value;
                    OnPropertyChangedWithValue(value, "ShowAmmoCount");
                }
            }
        }

        [DataSourceProperty]
        public int AmmoCount
        {
            get
            {
                return _ammoCount;
            }
            set
            {
                if (value != _ammoCount)
                {
                    _ammoCount = value;
                    OnPropertyChangedWithValue(value, "AmmoCount");
                    ShowAmmoCount = value >= 0;
                }
            }
        }

        [DataSourceProperty]
        public float TroopsAmmoPercentage
        {
            get
            {
                return _troopsAmmoPercentage;
            }
            set
            {
                if (value != _troopsAmmoPercentage)
                {
                    _troopsAmmoPercentage = value;
                    OnPropertyChangedWithValue(value, "TroopsAmmoPercentage");
                }
            }
        }

        [DataSourceProperty]
        public bool TroopsAmmoAvailable
        {
            get
            {
                return _troopsAmmoAvailable;
            }
            set
            {
                if (value != _troopsAmmoAvailable)
                {
                    _troopsAmmoAvailable = value;
                    OnPropertyChangedWithValue(value, "TroopsAmmoAvailable");
                }
            }
        }

        [DataSourceProperty]
        public bool IsAmmoCountAlertEnabled
        {
            get
            {
                return _isAmmoCountAlertEnabled;
            }
            set
            {
                if (value != _isAmmoCountAlertEnabled)
                {
                    _isAmmoCountAlertEnabled = value;
                    OnPropertyChangedWithValue(value, "IsAmmoCountAlertEnabled");
                }
            }
        }

        [DataSourceProperty]
        public float CameraToggleProgress
        {
            get
            {
                return _cameraToggleProgress;
            }
            set
            {
                if (value != _cameraToggleProgress)
                {
                    _cameraToggleProgress = value;
                    OnPropertyChangedWithValue(value, "CameraToggleProgress");
                }
            }
        }

        [DataSourceProperty]
        public string CameraToggleText
        {
            get
            {
                return _cameraToggleText;
            }
            set
            {
                if (value != _cameraToggleText)
                {
                    _cameraToggleText = value;
                    OnPropertyChangedWithValue(value, "CameraToggleText");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM OffhandWeapon
        {
            get
            {
                return _offhandWeapon;
            }
            set
            {
                if (value != _offhandWeapon)
                {
                    _offhandWeapon = value;
                    OnPropertyChangedWithValue(value, "OffhandWeapon");
                }
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM PrimaryWeapon
        {
            get
            {
                return _primaryWeapon;
            }
            set
            {
                if (value != _primaryWeapon)
                {
                    _primaryWeapon = value;
                    OnPropertyChangedWithValue(value, "PrimaryWeapon");
                }
            }
        }

        [DataSourceProperty]
        public MissionAgentDamageFeedVM TakenDamageFeed
        {
            get
            {
                return _takenDamageFeed;
            }
            set
            {
                if (value != _takenDamageFeed)
                {
                    _takenDamageFeed = value;
                    OnPropertyChangedWithValue(value, "TakenDamageFeed");
                }
            }
        }

        private const string _couchLanceUsageString = "couch";

        private const string _spearBraceUsageString = "spear";

        private readonly Mission _mission;

        private readonly Camera _missionCamera;

        private float _combatUIRemainTimer;

        private MissionPeer _missionPeer;

        private MissionMultiplayerGameModeBaseClient _mpGameMode;

        private readonly Func<float> _getCameraToggleProgress;

        private int _agentHealth;

        private int _agentHealthMax;

        private int _horseHealth;

        private int _horseHealthMax;

        private int _shieldHealth;

        private int _shieldHealthMax;

        private bool _isPlayerActive = true;

        private bool _isCombatUIActive;

        private bool _showAgentHealthBar;

        private bool _showMountHealthBar;

        private bool _showShieldHealthBar;

        private bool _troopsAmmoAvailable;

        private bool _isAgentStatusAvailable;

        private bool _isInteractionAvailable;

        private float _troopsAmmoPercentage;

        private int _troopCount;

        private int _goldAmount;

        private bool _isTroopsActive;

        private bool _isGoldActive;

        private AgentInteractionInterfaceVM _interactionInterface;

        private ImageIdentifierVM _offhandWeapon;

        private ImageIdentifierVM _primaryWeapon;

        private MissionAgentTakenDamageVM _takenDamageController;

        private MissionAgentDamageFeedVM _takenDamageFeed;

        private int _ammoCount;

        private int _couchLanceState = -1;

        private int _spearBraceState = -1;

        private bool _showAmmoCount;

        private bool _isAmmoCountAlertEnabled;

        private float _cameraToggleProgress;

        private string _cameraToggleText;

        private enum PassiveUsageStates
        {
            NotPossible,
            ConditionsNotMet,
            Possible,
            Active
        }
    }
}
