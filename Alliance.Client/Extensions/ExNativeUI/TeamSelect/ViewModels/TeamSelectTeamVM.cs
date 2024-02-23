using Alliance.Common.Core.Configuration.Models;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.ObjectSystem;

namespace Alliance.Client.Extensions.ExNativeUI.TeamSelect.ViewModels
{
    public class TeamSelectTeamVM : ViewModel
    {
        private List<string> randomLeaderAnimationsLeft = new List<string>()
        {
            "act_taunt_31",
            "act_taunt_10",
            "act_taunt_13",
            "act_taunt_20"
        };

        private List<string> randomLeaderAnimationsRight = new List<string>()
        {
            "act_taunt_31_leftstance",
            "act_cheering_high_01",
            "act_arena_winner_1"
        };

        private List<string> randomTroopAnimationsLeft = new List<string>()
        {
            "act_taunt_cheer_3",
            "act_taunt_20",
            "act_cheering_high_01",
            "act_arena_winner_1",
            "act_arena_spectator"
        };

        private List<string> randomTroopAnimationsRight = new List<string>()
        {
            "act_taunt_24_leftstance",
            "act_cheering_low_01",
            "act_arena_spectator"
        };

        private const int MaxFriendAvatarCount = 6;

        public readonly Team Team;
        public readonly Action<Team> _onSelect;

        private readonly List<MPPlayerVM> _friends;
        private MissionScoreboardComponent _missionScoreboardComponent;
        private MissionScoreboardComponent.MissionScoreboardSide _missionScoreboardSide;
        private readonly BasicCultureObject _culture;
        private bool _isDisabled;
        private string _displayedPrimary;
        private string _displayedSecondary;
        private string _displayedSecondarySub;
        private string _lockText;
        private string _cultureId;
        private int _score;
        private ImageIdentifierVM _banner;
        private MBBindingList<MPPlayerVM> _friendAvatars;
        private bool _hasExtraFriends;
        private bool _useSecondary;
        private bool _isAttacker;
        private bool _isSiege;
        private string _friendsExtraText;
        private HintViewModel _friendsExtraHint;
        private Color _cultureColor1;
        private Color _cultureColor2;
        private CharacterViewModel _leaderPreview;
        private CharacterViewModel _troop1Preview;
        private CharacterViewModel _troop2Preview;

        [DataSourceProperty]
        public CharacterViewModel LeaderPreview
        {
            get
            {
                return _leaderPreview;
            }
            set
            {
                if (value != _leaderPreview)
                {
                    _leaderPreview = value;
                    OnPropertyChangedWithValue(value, "LeaderPreview");
                }
            }
        }

        [DataSourceProperty]
        public CharacterViewModel Troop1Preview
        {
            get
            {
                return _troop1Preview;
            }
            set
            {
                if (value != _troop1Preview)
                {
                    _troop1Preview = value;
                    OnPropertyChangedWithValue(value, "Troop1Preview");
                }
            }
        }

        [DataSourceProperty]
        public CharacterViewModel Troop2Preview
        {
            get
            {
                return _troop2Preview;
            }
            set
            {
                if (value != _troop2Preview)
                {
                    _troop2Preview = value;
                    OnPropertyChangedWithValue(value, "Troop2Preview");
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
                if (_cultureId != value)
                {
                    _cultureId = value;
                    OnPropertyChangedWithValue(value, "CultureId");
                }
            }
        }

        [DataSourceProperty]
        public int Score
        {
            get
            {
                return _score;
            }
            set
            {
                if (value != _score)
                {
                    _score = value;
                    OnPropertyChangedWithValue(value, "Score");
                }
            }
        }

        [DataSourceProperty]
        public bool IsDisabled
        {
            get
            {
                return _isDisabled;
            }
            set
            {
                if (_isDisabled != value)
                {
                    _isDisabled = value;
                    OnPropertyChangedWithValue(value, "IsDisabled");
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
                if (_useSecondary != value)
                {
                    _useSecondary = value;
                    OnPropertyChangedWithValue(value, "UseSecondary");
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
                if (_isAttacker != value)
                {
                    _isAttacker = value;
                    OnPropertyChangedWithValue(value, "IsAttacker");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSiege
        {
            get
            {
                return _isSiege;
            }
            set
            {
                if (_isSiege != value)
                {
                    _isSiege = value;
                    OnPropertyChangedWithValue(value, "IsSiege");
                }
            }
        }

        [DataSourceProperty]
        public string DisplayedPrimary
        {
            get
            {
                return _displayedPrimary;
            }
            set
            {
                _displayedPrimary = value;
                OnPropertyChangedWithValue(value, "DisplayedPrimary");
            }
        }

        [DataSourceProperty]
        public string DisplayedSecondary
        {
            get
            {
                return _displayedSecondary;
            }
            set
            {
                _displayedSecondary = value;
                OnPropertyChangedWithValue(value, "DisplayedSecondary");
            }
        }

        [DataSourceProperty]
        public string DisplayedSecondarySub
        {
            get
            {
                return _displayedSecondarySub;
            }
            set
            {
                _displayedSecondarySub = value;
                OnPropertyChangedWithValue(value, "DisplayedSecondarySub");
            }
        }

        [DataSourceProperty]
        public string LockText
        {
            get
            {
                return _lockText;
            }
            set
            {
                _lockText = value;
                OnPropertyChangedWithValue(value, "LockText");
            }
        }

        [DataSourceProperty]
        public ImageIdentifierVM Banner
        {
            get
            {
                return _banner;
            }
            set
            {
                if (value != _banner && (value == null || _banner == null || _banner.Id != value.Id))
                {
                    _banner = value;
                    OnPropertyChangedWithValue(value, "Banner");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<MPPlayerVM> FriendAvatars
        {
            get
            {
                return _friendAvatars;
            }
            set
            {
                if (_friendAvatars != value)
                {
                    _friendAvatars = value;
                    OnPropertyChangedWithValue(value, "FriendAvatars");
                }
            }
        }

        [DataSourceProperty]
        public bool HasExtraFriends
        {
            get
            {
                return _hasExtraFriends;
            }
            set
            {
                if (_hasExtraFriends != value)
                {
                    _hasExtraFriends = value;
                    OnPropertyChangedWithValue(value, "HasExtraFriends");
                }
            }
        }

        [DataSourceProperty]
        public string FriendsExtraText
        {
            get
            {
                return _friendsExtraText;
            }
            set
            {
                if (_friendsExtraText != value)
                {
                    _friendsExtraText = value;
                    OnPropertyChangedWithValue(value, "FriendsExtraText");
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel FriendsExtraHint
        {
            get
            {
                return _friendsExtraHint;
            }
            set
            {
                if (_friendsExtraHint != value)
                {
                    _friendsExtraHint = value;
                    OnPropertyChangedWithValue(value, "FriendsExtraHint");
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

        public TeamSelectTeamVM(MissionScoreboardComponent missionScoreboardComponent, Team team, BasicCultureObject culture, BannerCode bannercode, Action<Team> onSelect, bool useSecondary)
        {
            Team = team;
            UseSecondary = useSecondary;
            _onSelect = onSelect;
            _culture = culture;
            IsSiege = Mission.Current?.HasMissionBehavior<MissionMultiplayerSiegeClient>() ?? false;

            if (Team != null && Team.Side != BattleSideEnum.None)
            {
                _missionScoreboardComponent = missionScoreboardComponent;
                _missionScoreboardComponent.OnRoundPropertiesChanged += UpdateTeamScores;
                _missionScoreboardSide = _missionScoreboardComponent.Sides.FirstOrDefault((s) => s != null && s.Side == Team.Side);
                IsAttacker = Team.Side == BattleSideEnum.Attacker;
                UpdateTeamScores();
            }

            if (culture != null)
            {
                uint color1 = useSecondary ? culture.Color2 : culture.Color;
                uint color2 = useSecondary ? culture.Color : culture.Color2;
                CultureColor1 = Color.FromUint(color1);
                CultureColor2 = Color.FromUint(color2);
                string bannerCodeStr = bannercode?.Code ?? "";

                BasicCharacterObject defaultChar = Mission.Current.MainAgent != null ? Mission.Current.MainAgent.Character : MBObjectManager.Instance.GetFirstObject<BasicCharacterObject>();
                List<BasicCharacterObject> cultureChars = MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>()
                    .Where(bco => bco.Culture == culture)
                    .GroupBy(bco => bco.Level) // Group by level to ensure unique levels
                    .OrderByDescending(group => group.Key) // Order by level descending
                    .SelectMany(group => group.Take(1)) // Take one character per level group
                    .Take(3) // Take the top 3 characters based on level
                    .ToList();
                BasicCharacterObject leaderChar = cultureChars.ElementAtOrDefault(0) ?? defaultChar;
                BasicCharacterObject troop1Char = cultureChars.ElementAtOrDefault(2) ?? defaultChar;
                BasicCharacterObject troop2Char = cultureChars.ElementAtOrDefault(1) ?? defaultChar;
                LeaderPreview = new CharacterViewModel();
                Troop1Preview = new CharacterViewModel();
                Troop2Preview = new CharacterViewModel();
                string leaderAnimation = IsAttacker ? randomLeaderAnimationsLeft.GetRandomElement() : randomLeaderAnimationsRight.GetRandomElement();
                string troopAnimation1 = "";
                string troopAnimation2 = "";
                List<string> troopAnimations = IsAttacker ? randomTroopAnimationsLeft : randomTroopAnimationsRight;
                if (troopAnimations.Count > 1)
                {
                    int index = MBRandom.RandomInt(troopAnimations.Count);
                    troopAnimation1 = troopAnimations[index];
                    troopAnimation2 = troopAnimations[(index + 1) % troopAnimations.Count];
                }
                FillCharacterViewModel(LeaderPreview, leaderChar, bannerCodeStr, color1, color2, leaderAnimation, true);
                FillCharacterViewModel(Troop1Preview, troop1Char, bannerCodeStr, color1, color2, troopAnimation1, false);
                FillCharacterViewModel(Troop2Preview, troop2Char, bannerCodeStr, color1, color2, troopAnimation2, false);
            }

            // Use the culture name instead of Id (we don't need culture id and name is capitalized properly)
            CultureId = culture == null ? "" : culture.Name.ToString();

            if (team == null)
            {
                IsDisabled = true;
            }

            if (bannercode == null)
            {
                Banner = new ImageIdentifierVM();
            }
            else
            {
                Banner = new ImageIdentifierVM(bannercode, nineGrid: true);
            }

            _friends = new List<MPPlayerVM>();
            FriendAvatars = new MBBindingList<MPPlayerVM>();

            if (_culture == null)
            {
                DisplayedPrimary = new TextObject("{=pSheKLB4}Spectator").ToString();
            }
            else if (MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC" ||
                (int)Team.Side == Config.Instance.CommanderSide && MultiplayerOptions.OptionType.GameType.GetStrValue() == "PvC")
            {
                DisplayedPrimary = new TextObject("{=al_commanders}Commanders").ToString();
            }
            else if (Team.Side == BattleSideEnum.Attacker)
            {
                DisplayedPrimary = new TextObject("{=al_attackers}Attackers").ToString();
            }
            else if (Team.Side == BattleSideEnum.Defender)
            {
                DisplayedPrimary = new TextObject("{=al_defenders}Defenders").ToString();
            }
        }

        private void FillCharacterViewModel(CharacterViewModel characterViewModel, BasicCharacterObject leaderChar, string bannerCode, uint color1, uint color2, string animation, bool equipWeapon)
        {
            Equipment equipment = leaderChar.Equipment.Clone();
            equipment[EquipmentIndex.Horse] = EquipmentElement.Invalid;
            if (equipWeapon)
            {
                int slot = 0;
                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumPrimaryWeaponSlots; i++)
                {
                    if (equipment?[i].Item?.WeaponComponent != null && (equipment[i].Item.IsCraftedWeapon || equipment[i].Item.IsBannerItem))
                    {
                        slot = (int)i;
                    }
                }
                characterViewModel.RightHandWieldedEquipmentIndex = slot;
            }
            characterViewModel.FillFrom(leaderChar);
            characterViewModel.SetEquipment(equipment);
            characterViewModel.ArmorColor1 = color1;
            characterViewModel.ArmorColor2 = color2;
            characterViewModel.BannerCodeText = bannerCode;
            characterViewModel.ExecuteStartCustomAnimation(animation, true, 10);
            characterViewModel.IdleAction = IsAttacker ? "act_walk_idle_1h" : "act_walk_idle_1h_left_stance";
        }

        public override void OnFinalize()
        {
            if (_missionScoreboardComponent != null)
            {
                _missionScoreboardComponent.OnRoundPropertiesChanged -= UpdateTeamScores;
            }

            _missionScoreboardComponent = null;
            _missionScoreboardSide = null;
            base.OnFinalize();
        }

        private void UpdateTeamScores()
        {
            if (_missionScoreboardSide != null)
            {
                Score = _missionScoreboardSide.SideScore;
            }
        }

        public void RefreshFriends(IEnumerable<MissionPeer> friends)
        {
            List<MissionPeer> list = friends.ToList();
            List<MPPlayerVM> list2 = new List<MPPlayerVM>();
            foreach (MPPlayerVM friend in _friends)
            {
                if (!list.Contains(friend.Peer))
                {
                    list2.Add(friend);
                }
            }

            foreach (MPPlayerVM item in list2)
            {
                _friends.Remove(item);
            }

            List<MissionPeer> list3 = _friends.Select((x) => x.Peer).ToList();
            foreach (MissionPeer item2 in list)
            {
                if (!list3.Contains(item2))
                {
                    _friends.Add(new MPPlayerVM(item2));
                }
            }

            FriendAvatars.Clear();
            MBStringBuilder mBStringBuilder = default;
            mBStringBuilder.Initialize(16, "RefreshFriends");
            for (int i = 0; i < _friends.Count; i++)
            {
                if (i < 6)
                {
                    FriendAvatars.Add(_friends[i]);
                }
                else
                {
                    mBStringBuilder.AppendLine(_friends[i].Peer.DisplayedName);
                }
            }

            int num = _friends.Count - 6;
            if (num > 0)
            {
                HasExtraFriends = true;
                TextObject textObject = new TextObject("{=hbwp3g3k}+{FRIEND_COUNT} {newline} {?PLURAL}friends{?}friend{\\?}");
                textObject.SetTextVariable("FRIEND_COUNT", num);
                textObject.SetTextVariable("PLURAL", num != 1 ? 1 : 0);
                FriendsExtraText = textObject.ToString();
                FriendsExtraHint = new HintViewModel(textObject);
            }
            else
            {
                mBStringBuilder.Release();
                HasExtraFriends = false;
                FriendsExtraText = "";
            }
        }

        public void SetIsDisabled(bool isCurrentTeam, bool disabledForBalance)
        {
            IsDisabled = isCurrentTeam || disabledForBalance;
            if (isCurrentTeam)
            {
                LockText = new TextObject("{=SoQcsslF}CURRENT TEAM").ToString();
            }
            else if (disabledForBalance)
            {
                LockText = new TextObject("{=qe46yXVJ}LOCKED FOR BALANCE").ToString();
            }
        }

        [UsedImplicitly]
        public void ExecuteSelectTeam()
        {
            if (_onSelect != null)
            {
                _onSelect(Team);
            }
        }
    }
}