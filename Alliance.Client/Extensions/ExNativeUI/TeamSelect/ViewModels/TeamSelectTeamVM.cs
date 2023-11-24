using Alliance.Common.Core.Configuration.Models;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer;

namespace Alliance.Client.Extensions.ExNativeUI.TeamSelect.ViewModels
{
    public class TeamSelectTeamVM : ViewModel
    {
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

            CultureId = culture == null ? "" : culture.StringId;
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
            RefreshValues();
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            if (_culture == null)
            {
                DisplayedPrimary = new TextObject("{=pSheKLB4}Spectator").ToString();
            }
            else if ((int)Team.Side == Config.Instance.CommanderSide)
            {
                DisplayedPrimary = new TextObject("Commanders").ToString();
            }
            else
            {
                DisplayedPrimary = new TextObject("Players").ToString();
            }
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