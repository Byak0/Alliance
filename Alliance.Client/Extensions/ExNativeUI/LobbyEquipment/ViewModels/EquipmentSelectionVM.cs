using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

namespace Alliance.Client.Extensions.ExNativeUI.LobbyEquipment.ViewModels
{
    /// <summary>
    /// Custom VM for the Equipment selection menu.
    /// Based on native class MultiplayerClassLoadoutVM.
    /// </summary>
    public class EquipmentSelectionVM : ViewModel
    {
        public const float UPDATE_INTERVAL = 1f;

        public Dictionary<BasicCharacterObject, ALHeroClassVM> CharacterToVM = new();

        private float _updateTimeElapsed;
        private readonly Action<MultiplayerClassDivisions.MPHeroClass> _onRefreshSelection;
        private readonly MissionMultiplayerGameModeBaseClient _missionMultiplayerGameMode;
        private readonly MultiplayerRoundComponent _multiplayerRoundComponent;
        private Dictionary<MissionPeer, MPPlayerVM> _enemyDictionary;
        private readonly Mission _mission;
        private bool _isTeammateAndEnemiesRelevant;
        private const float REMAINING_TIME_WARNING_THRESHOLD = 5f;
        private MissionLobbyEquipmentNetworkComponent _missionLobbyEquipmentNetworkComponent;
        private bool _isInitializing;
        private Dictionary<MissionPeer, MPPlayerVM> _teammateDictionary;
        private int _gold;
        private string _culture;
        private string _cultureId;
        private string _spawnLabelText;
        private string _spawnForfeitLabelText;
        private string _remainingTimeText;
        private bool _warnRemainingTime;
        private bool _isSpawnTimerVisible;
        private bool _isSpawnLabelVisible;
        private bool _isSpawnForfeitLabelVisible;
        private bool _isGoldEnabled;
        private bool _isInWarmup;
        private bool _useSecondary;
        private bool _showAttackerOrDefenderIcons;
        private bool _isAttacker;
        private string _warmupInfoText;
        private Color _cultureColor1;
        private Color _cultureColor2;
        private MBBindingList<ALHeroClassGroupVM> _classes;
        private ALHeroInformationVM _heroInformation;
        private ALHeroClassVM _currentSelectedClass;
        private MBBindingList<MPPlayerVM> _teammates;
        private MBBindingList<MPPlayerVM> _enemies;

        private Team _playerTeam
        {
            get
            {
                if (!GameNetwork.IsMyPeerReady)
                {
                    return null;
                }

                MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                if (component.Team == null || component.Team.Side == BattleSideEnum.None)
                {
                    return null;
                }

                return component.Team;
            }
        }

        [DataSourceProperty]
        public string Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                if (value != _culture)
                {
                    _culture = value;
                    OnPropertyChangedWithValue(value, "Culture");
                }
            }
        }

        [DataSourceProperty]
        public Color CultureColor1
        {
            get
            {
                return _cultureColor1;
            }
            set
            {
                if (value != _cultureColor1)
                {
                    _cultureColor1 = value;
                    OnPropertyChangedWithValue(value, "CultureColor1");
                }
            }
        }

        [DataSourceProperty]
        public Color CultureColor2
        {
            get
            {
                return _cultureColor2;
            }
            set
            {
                if (value != _cultureColor2)
                {
                    _cultureColor2 = value;
                    OnPropertyChangedWithValue(value, "CultureColor2");
                }
            }
        }

        [DataSourceProperty]
        public string CultureId
        {
            get
            {
                return _cultureId;
            }
            set
            {
                if (value != _cultureId)
                {
                    _cultureId = value;
                    OnPropertyChangedWithValue(value, "CultureId");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSpawnTimerVisible
        {
            get
            {
                return _isSpawnTimerVisible;
            }
            set
            {
                if (value != _isSpawnTimerVisible)
                {
                    _isSpawnTimerVisible = value;
                    OnPropertyChangedWithValue(value, "IsSpawnTimerVisible");
                }
            }
        }

        [DataSourceProperty]
        public string SpawnLabelText
        {
            get
            {
                return _spawnLabelText;
            }
            set
            {
                if (value != _spawnLabelText)
                {
                    _spawnLabelText = value;
                    OnPropertyChangedWithValue(value, "SpawnLabelText");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSpawnLabelVisible
        {
            get
            {
                return _isSpawnLabelVisible;
            }
            set
            {
                if (value != _isSpawnLabelVisible)
                {
                    _isSpawnLabelVisible = value;
                    OnPropertyChangedWithValue(value, "IsSpawnLabelVisible");
                }
            }
        }

        [DataSourceProperty]
        public bool UseSecondary
        {
            get
            {
                return _useSecondary;
            }
            set
            {
                if (value != _useSecondary)
                {
                    _useSecondary = value;
                    OnPropertyChangedWithValue(value, "UseSecondary");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowAttackerOrDefenderIcons
        {
            get
            {
                return _showAttackerOrDefenderIcons;
            }
            set
            {
                if (value != _showAttackerOrDefenderIcons)
                {
                    _showAttackerOrDefenderIcons = value;
                    OnPropertyChangedWithValue(value, "ShowAttackerOrDefenderIcons");
                }
            }
        }

        [DataSourceProperty]
        public bool IsAttacker
        {
            get
            {
                return _isAttacker;
            }
            set
            {
                if (value != _isAttacker)
                {
                    _isAttacker = value;
                    OnPropertyChangedWithValue(value, "IsAttacker");
                }
            }
        }

        [DataSourceProperty]
        public string SpawnForfeitLabelText
        {
            get
            {
                return _spawnForfeitLabelText;
            }
            set
            {
                if (value != _spawnForfeitLabelText)
                {
                    _spawnForfeitLabelText = value;
                    OnPropertyChangedWithValue(value, "SpawnForfeitLabelText");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSpawnForfeitLabelVisible
        {
            get
            {
                return _isSpawnForfeitLabelVisible;
            }
            set
            {
                if (value != _isSpawnForfeitLabelVisible)
                {
                    _isSpawnForfeitLabelVisible = value;
                    OnPropertyChangedWithValue(value, "IsSpawnForfeitLabelVisible");
                }
            }
        }

        [DataSourceProperty]
        public int Gold
        {
            get
            {
                return _gold;
            }
            set
            {
                if (value != _gold)
                {
                    _gold = value;
                    OnPropertyChangedWithValue(value, "Gold");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MPPlayerVM> Teammates
        {
            get
            {
                return _teammates;
            }
            set
            {
                if (value != _teammates)
                {
                    _teammates = value;
                    OnPropertyChangedWithValue(value, "Teammates");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MPPlayerVM> Enemies
        {
            get
            {
                return _enemies;
            }
            set
            {
                if (value != _enemies)
                {
                    _enemies = value;
                    OnPropertyChangedWithValue(value, "Enemies");
                }
            }
        }

        [DataSourceProperty]
        public ALHeroInformationVM HeroInformation
        {
            get
            {
                return _heroInformation;
            }
            set
            {
                if (value != _heroInformation)
                {
                    _heroInformation = value;
                    OnPropertyChangedWithValue(value, "HeroInformation");
                }
            }
        }

        [DataSourceProperty]
        public ALHeroClassVM CurrentSelectedClass
        {
            get
            {
                return _currentSelectedClass;
            }
            set
            {
                if (value != _currentSelectedClass)
                {
                    _currentSelectedClass = value;
                    OnPropertyChangedWithValue(value, "CurrentSelectedClass");
                }
            }
        }

        [DataSourceProperty]
        public string RemainingTimeText
        {
            get
            {
                return _remainingTimeText;
            }
            set
            {
                if (value != _remainingTimeText)
                {
                    _remainingTimeText = value;
                    OnPropertyChangedWithValue(value, "RemainingTimeText");
                }
            }
        }

        [DataSourceProperty]
        public bool WarnRemainingTime
        {
            get
            {
                return _warnRemainingTime;
            }
            set
            {
                if (value != _warnRemainingTime)
                {
                    _warnRemainingTime = value;
                    OnPropertyChangedWithValue(value, "WarnRemainingTime");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<ALHeroClassGroupVM> Classes
        {
            get
            {
                return _classes;
            }
            set
            {
                if (value != _classes)
                {
                    _classes = value;
                    OnPropertyChangedWithValue(value, "Classes");
                }
            }
        }

        [DataSourceProperty]
        public bool IsGoldEnabled
        {
            get
            {
                return _isGoldEnabled;
            }
            set
            {
                if (value != _isGoldEnabled)
                {
                    _isGoldEnabled = value;
                    OnPropertyChangedWithValue(value, "IsGoldEnabled");
                }
            }
        }

        [DataSourceProperty]
        public bool IsInWarmup
        {
            get
            {
                return _isInWarmup;
            }
            set
            {
                if (value != _isInWarmup)
                {
                    _isInWarmup = value;
                    OnPropertyChangedWithValue(value, "IsInWarmup");
                }
            }
        }

        [DataSourceProperty]
        public string WarmupInfoText
        {
            get
            {
                return _warmupInfoText;
            }
            set
            {
                if (value != _warmupInfoText)
                {
                    _warmupInfoText = value;
                    OnPropertyChangedWithValue(value, "WarmupInfoText");
                }
            }
        }

        public EquipmentSelectionVM(MissionMultiplayerGameModeBaseClient gameMode, Action<MultiplayerClassDivisions.MPHeroClass> onRefreshSelection, MultiplayerClassDivisions.MPHeroClass initialHeroSelection)
        {
            MBTextManager.SetTextVariable("newline", "\n");
            _isInitializing = true;
            _onRefreshSelection = onRefreshSelection;
            _missionMultiplayerGameMode = gameMode;
            _mission = gameMode.Mission;
            Team team = GameNetwork.MyPeer.GetComponent<MissionPeer>().Team;
            Classes = new MBBindingList<ALHeroClassGroupVM>();
            HeroInformation = new ALHeroInformationVM();
            _enemyDictionary = new Dictionary<MissionPeer, MPPlayerVM>();
            _missionLobbyEquipmentNetworkComponent = Mission.Current.GetMissionBehavior<MissionLobbyEquipmentNetworkComponent>();
            _multiplayerRoundComponent = Mission.Current.GetMissionBehavior<MultiplayerRoundComponent>();

            if (_multiplayerRoundComponent != null)
            {
                _multiplayerRoundComponent.OnRoundStarted += RefreshSelection;
            }

            // Gold usage is defined by the Game Mode client.
            IsGoldEnabled = _missionMultiplayerGameMode.IsGameModeUsingGold;
            if (IsGoldEnabled)
            {
                Gold = _missionMultiplayerGameMode.GetGoldAmount();
            }

            ALHeroClassVM ALHeroClassVM = null;
            UseSecondary = team.Side == BattleSideEnum.Defender;
            foreach (MultiplayerClassDivisions.MPHeroClassGroup multiplayerHeroClassGroup in MultiplayerClassDivisions.MultiplayerHeroClassGroups)
            {
                ALHeroClassGroupVM ALHeroClassGroupVM = new ALHeroClassGroupVM(RefreshCharacter, OnSelectPerk, multiplayerHeroClassGroup, UseSecondary);
                if (ALHeroClassGroupVM.IsValid)
                {
                    Classes.Add(ALHeroClassGroupVM);
                }
            }

            int num = ((initialHeroSelection != null) ? (gameMode.IsGameModeUsingCasualGold ? initialHeroSelection.TroopCasualCost : ((gameMode.GameType == MultiplayerGameType.Battle) ? initialHeroSelection.TroopBattleCost : initialHeroSelection.TroopCost)) : 0);

            if (initialHeroSelection != null)
            {
                if (Config.Instance.UsePlayerLimit)
                {
                    BasicCharacterObject initialChoiceCharacter = GameNetwork.MyPeer.IsOfficer() ? initialHeroSelection.HeroCharacter : initialHeroSelection.TroopCharacter;
                    bool initialChoiceUnavailable = Config.Instance.UsePlayerLimit && !ClassLimiterModel.Instance.CharactersAvailable[initialHeroSelection.TroopCharacter];
                    if (initialChoiceUnavailable)
                    {
                        ALHeroClassVM = Classes.FirstOrDefault()?.SubClasses.FirstOrDefault();
                    }
                }
                else if (IsGoldEnabled && initialHeroSelection.TroopCost > Gold)
                {
                    ALHeroClassVM = Classes.FirstOrDefault()?.SubClasses.FirstOrDefault();
                }
                else
                {
                    foreach (ALHeroClassGroupVM @class in Classes)
                    {
                        foreach (ALHeroClassVM subClass in @class.SubClasses)
                        {
                            if (subClass.HeroClass == initialHeroSelection)
                            {
                                ALHeroClassVM = subClass;
                                break;
                            }
                        }

                        if (ALHeroClassVM != null)
                        {
                            break;
                        }
                    }

                    if (ALHeroClassVM == null)
                    {
                        ALHeroClassVM = Classes.FirstOrDefault()?.SubClasses.FirstOrDefault();
                    }
                }
            }
            else
            {
                ALHeroClassVM = Classes.FirstOrDefault()?.SubClasses.FirstOrDefault();
            }

            _isInitializing = false;
            RefreshCharacter(ALHeroClassVM);
            _teammateDictionary = new Dictionary<MissionPeer, MPPlayerVM>();
            Teammates = new MBBindingList<MPPlayerVM>();
            Enemies = new MBBindingList<MPPlayerVM>();
            MissionPeer.OnEquipmentIndexRefreshed += RefreshPeerDivision;
            MissionPeer.OnPerkSelectionUpdated += RefreshPeerPerkSelection;
            NetworkCommunicator.OnPeerComponentAdded += OnPeerComponentAdded;
            BasicCultureObject culture = GameNetwork.MyPeer.GetComponent<MissionPeer>().Culture;
            CultureId = culture.StringId;
            CultureColor1 = Color.FromUint(UseSecondary ? culture.Color2 : culture.Color);
            CultureColor2 = Color.FromUint(UseSecondary ? culture.Color : culture.Color2);
            if (Mission.Current.HasMissionBehavior<MissionMultiplayerSiegeClient>())
            {
                ShowAttackerOrDefenderIcons = true;
                IsAttacker = team.Side == BattleSideEnum.Attacker;
            }

            // Setup CharacterToVM dictionary
            foreach (ALHeroClassGroupVM groupVM in Classes)
            {
                foreach (ALHeroClassVM classVM in groupVM.SubClasses)
                {
                    CharacterToVM[classVM.Character] = classVM;
                }
            }


            RefreshValues();

            // Refresh enemies and teammates values only in certain gamemodes
            _isTeammateAndEnemiesRelevant =
                Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>().IsGameModeTactical
                && !Mission.Current.HasMissionBehavior<MissionMultiplayerSiegeClient>()
                && Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>().GameType != MultiplayerGameType.Battle;
            if (_isTeammateAndEnemiesRelevant)
            {
                OnRefreshTeamMembers();
                OnRefreshEnemyMembers();
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            UpdateSpawnAndTimerLabels();
            string strValue = MultiplayerOptions.OptionType.GameType.GetStrValue();
            TextObject textObject = new TextObject("{=XJTX8w8M}Warmup Phase - {GAME_MODE}\nWaiting for players to join");
            textObject.SetTextVariable("GAME_MODE", GameTexts.FindText("str_multiplayer_official_game_type_name", strValue));
            WarmupInfoText = textObject.ToString();
            BasicCultureObject culture = GameNetwork.MyPeer.GetComponent<MissionPeer>().Culture;
            Culture = culture.Name.ToString();
            Classes.ApplyActionOnAllItems(delegate (ALHeroClassGroupVM x)
            {
                x.RefreshValues();
            });
            CurrentSelectedClass.RefreshValues();
            HeroInformation.RefreshValues();
        }

        private void UpdateSpawnAndTimerLabels()
        {
            string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
            GameTexts.SetVariable("USE_KEY", keyHyperlinkText);
            SpawnLabelText = GameTexts.FindText("str_skirmish_battle_press_action_to_spawn").ToString();
            if (_missionMultiplayerGameMode.RoundComponent != null)
            {
                if (_missionMultiplayerGameMode.IsInWarmup || _missionMultiplayerGameMode.IsRoundInProgress)
                {
                    IsSpawnTimerVisible = false;
                    IsSpawnLabelVisible = true;
                    if (_missionMultiplayerGameMode.IsRoundInProgress && _missionMultiplayerGameMode is MissionMultiplayerGameModeFlagDominationClient && _missionMultiplayerGameMode.GameType == MultiplayerGameType.Skirmish && GameNetwork.MyPeer.GetComponent<MissionPeer>() != null)
                    {
                        IsSpawnForfeitLabelVisible = true;
                        string keyHyperlinkText2 = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", "ForfeitSpawn"));
                        GameTexts.SetVariable("ALT_WEAP_KEY", keyHyperlinkText2);
                        SpawnForfeitLabelText = GameTexts.FindText("str_skirmish_battle_press_alternative_to_forfeit_spawning").ToString();
                    }
                }
                else
                {
                    IsSpawnTimerVisible = true;
                }
            }
            // Handle scenario case (no round component)
            else if (_missionMultiplayerGameMode is ScenarioClientBehavior)
            {
                IsSpawnTimerVisible = !_missionMultiplayerGameMode.IsRoundInProgress;
                IsSpawnLabelVisible = false;
            }
            else
            {
                IsSpawnTimerVisible = false;
                IsSpawnLabelVisible = true;
            }
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            MissionPeer.OnEquipmentIndexRefreshed -= RefreshPeerDivision;
            MissionPeer.OnPerkSelectionUpdated -= RefreshPeerPerkSelection;
            NetworkCommunicator.OnPeerComponentAdded -= OnPeerComponentAdded;
            if (_multiplayerRoundComponent != null)
            {
                _multiplayerRoundComponent.OnRoundStarted -= RefreshSelection;
            }
        }

        public void RefreshSelection()
        {
            if (CurrentSelectedClass != null)
            {
                RefreshCharacter(CurrentSelectedClass);
            }
        }

        private void RefreshCharacter(ALHeroClassVM heroClass)
        {
            if (_isInitializing)
            {
                return;
            }

            foreach (ALHeroClassGroupVM @class in Classes)
            {
                foreach (ALHeroClassVM subClass in @class.SubClasses)
                {
                    subClass.IsSelected = false;
                }
            }

            heroClass.IsSelected = true;
            CurrentSelectedClass = heroClass;

            if (Config.Instance.UsePlayerLimit)
            {
                ClassLimiterModel.Instance.RequestUsage(heroClass.Character);
            }

            if (GameNetwork.IsMyPeerReady)
            {
                MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                int nextSelectedTroopIndex = MultiplayerClassDivisions.GetMPHeroClasses(heroClass.HeroClass.Culture).ToList().IndexOf(heroClass.HeroClass);
                component.NextSelectedTroopIndex = nextSelectedTroopIndex;
            }

            HeroInformation.RefreshWith(heroClass.HeroClass, heroClass.SelectedPerks);
            _missionLobbyEquipmentNetworkComponent.EquipmentUpdated();
            if (_missionMultiplayerGameMode.IsGameModeUsingGold)
            {
                Gold = _missionMultiplayerGameMode.GetGoldAmount();
            }

            List<IReadOnlyPerkObject> perks = heroClass.Perks.Select((HeroPerkVM x) => x.SelectedPerk).ToList();
            HeroInformation.RefreshWith(HeroInformation.HeroClass, perks);
            List<Tuple<HeroPerkVM, MPPerkVM>> list = new List<Tuple<HeroPerkVM, MPPerkVM>>();
            foreach (HeroPerkVM perk in heroClass.Perks)
            {
                list.Add(new Tuple<HeroPerkVM, MPPerkVM>(perk, perk.SelectedPerkItem));
            }

            list.ForEach(delegate (Tuple<HeroPerkVM, MPPerkVM> p)
            {
                OnSelectPerk(p.Item1, p.Item2);
            });
            _onRefreshSelection?.Invoke(heroClass.HeroClass);
        }

        private void OnSelectPerk(HeroPerkVM heroPerk, MPPerkVM candidate)
        {
            if (GameNetwork.IsMyPeerReady && HeroInformation.HeroClass != null && CurrentSelectedClass != null)
            {
                MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                if (!GameNetwork.IsServer || component.SelectPerk(heroPerk.PerkIndex, candidate.PerkIndex))
                {
                    _missionLobbyEquipmentNetworkComponent.PerkUpdated(heroPerk.PerkIndex, candidate.PerkIndex);
                }

                List<IReadOnlyPerkObject> list = CurrentSelectedClass.Perks.Select((HeroPerkVM x) => x.SelectedPerk).ToList();
                if (list.Count > 0)
                {
                    HeroInformation.RefreshWith(HeroInformation.HeroClass, list);
                }
            }
        }

        public void RefreshPeerDivision(MissionPeer peer, int divisionType)
        {
            Teammates.FirstOrDefault((MPPlayerVM t) => t.Peer == peer)?.RefreshDivision();
        }

        private void RefreshPeerPerkSelection(MissionPeer peer)
        {
            Teammates.FirstOrDefault((MPPlayerVM t) => t.Peer == peer)?.RefreshActivePerks();
        }

        public void Tick(float dt)
        {
            if (_missionMultiplayerGameMode != null)
            {
                IsInWarmup = _missionMultiplayerGameMode.IsInWarmup;
                IsGoldEnabled = !IsInWarmup && _missionMultiplayerGameMode.IsGameModeUsingGold;
                if (IsGoldEnabled)
                {
                    Gold = _missionMultiplayerGameMode.GetGoldAmount();
                }

                foreach (ALHeroClassGroupVM @class in Classes)
                {
                    foreach (ALHeroClassVM subClass in @class.SubClasses)
                    {
                        subClass.IsGoldEnabled = IsGoldEnabled;
                    }
                }
            }

            RefreshRemainingTime();
            _updateTimeElapsed += dt;
            if (!(_updateTimeElapsed < 1f))
            {
                _updateTimeElapsed = 0f;
                if (_isTeammateAndEnemiesRelevant)
                {
                    OnRefreshTeamMembers();
                    OnRefreshEnemyMembers();
                }
            }
        }

        private void OnPeerComponentAdded(PeerComponent component)
        {
            if (component.IsMine && component is MissionRepresentativeBase)
            {
                // Refresh enemies and teammates values only in certain gamemodes
                // No need to recalculate _isTeammateAndEnemiesRelevant value here ?
                //_isTeammateAndEnemiesRelevant = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>().IsGameModeTactical && !Mission.Current.HasMissionBehavior<MissionMultiplayerSiegeClient>();
                if (_isTeammateAndEnemiesRelevant)
                {
                    OnRefreshTeamMembers();
                    OnRefreshEnemyMembers();
                }
            }
        }

        private void OnRefreshTeamMembers()
        {
            List<MPPlayerVM> list = Teammates.ToList();
            foreach (MissionPeer item in VirtualPlayer.Peers<MissionPeer>())
            {
                if (item.GetNetworkPeer().GetComponent<MissionPeer>() != null && _playerTeam != null && item.Team == _playerTeam)
                {
                    if (!_teammateDictionary.ContainsKey(item))
                    {
                        MPPlayerVM mPPlayerVM = new MPPlayerVM(item);
                        Teammates.Add(mPPlayerVM);
                        _teammateDictionary.Add(item, mPPlayerVM);
                    }
                    else
                    {
                        list.Remove(_teammateDictionary[item]);
                    }
                }
            }

            foreach (MPPlayerVM item2 in list)
            {
                Teammates.Remove(item2);
                _teammateDictionary.Remove(item2.Peer);
            }

            foreach (MPPlayerVM teammate in Teammates)
            {
                if (teammate.CompassElement == null)
                {
                    teammate.RefreshDivision();
                }
            }
        }

        private void OnRefreshEnemyMembers()
        {
            List<MPPlayerVM> list = Enemies.ToList();
            foreach (MissionPeer item in VirtualPlayer.Peers<MissionPeer>())
            {
                if (item.GetNetworkPeer().GetComponent<MissionPeer>() != null && _playerTeam != null && item.Team != null && item.Team != _playerTeam && item.Team != Mission.Current.SpectatorTeam)
                {
                    if (!_enemyDictionary.ContainsKey(item))
                    {
                        MPPlayerVM mPPlayerVM = new MPPlayerVM(item);
                        Enemies.Add(mPPlayerVM);
                        _enemyDictionary.Add(item, mPPlayerVM);
                    }
                    else
                    {
                        list.Remove(_enemyDictionary[item]);
                    }
                }
            }

            foreach (MPPlayerVM item2 in list)
            {
                Enemies.Remove(item2);
                _enemyDictionary.Remove(item2.Peer);
            }

            foreach (MPPlayerVM enemy in Enemies)
            {
                enemy.RefreshDivision();
                enemy.UpdateDisabled();
            }
        }

        public void OnPeerEquipmentRefreshed(MissionPeer peer)
        {
            if (_teammateDictionary.ContainsKey(peer))
            {
                _teammateDictionary[peer].RefreshActivePerks();
            }
            else if (_enemyDictionary.ContainsKey(peer))
            {
                _enemyDictionary[peer].RefreshActivePerks();
            }
        }

        public void OnGoldUpdated()
        {
            foreach (ALHeroClassGroupVM @class in Classes)
            {
                @class.SubClasses.ApplyActionOnAllItems(delegate (ALHeroClassVM sc)
                {
                    sc.UpdateEnabled();
                });
            }
        }

        public void RefreshRemainingTime()
        {
            int num = MathF.Ceiling(_missionMultiplayerGameMode.RemainingTime);
            RemainingTimeText = TimeSpan.FromSeconds(num).ToString("mm':'ss");
            WarnRemainingTime = (float)num < 5f;
        }
    }
}
