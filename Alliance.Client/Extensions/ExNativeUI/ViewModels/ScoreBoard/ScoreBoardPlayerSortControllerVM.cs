using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.Multiplayer.Scoreboard;

namespace Alliance.Client.Extensions.ExNativeUI.ViewModels.ScoreBoard
{
    public class ScoreBoardPlayerSortControllerVM : ViewModel
    {
        private enum SortState
        {
            Default,
            Ascending,
            Descending
        }

        public abstract class ItemComparerBase : IComparer<MissionScoreboardPlayerVM>
        {
            protected bool _isAscending;

            public void SetSortMode(bool isAscending)
            {
                _isAscending = isAscending;
            }

            public abstract int Compare(MissionScoreboardPlayerVM x, MissionScoreboardPlayerVM y);
        }

        public class ItemNameComparer : ItemComparerBase
        {
            public override int Compare(MissionScoreboardPlayerVM x, MissionScoreboardPlayerVM y)
            {
                return y.Name.CompareTo(x.Name) * (!_isAscending ? 1 : -1);
            }
        }

        public class ItemScoreComparer : ItemComparerBase
        {
            public override int Compare(MissionScoreboardPlayerVM x, MissionScoreboardPlayerVM y)
            {
                return y.Score.CompareTo(x.Score) * (!_isAscending ? 1 : -1);
            }
        }

        public class ItemKillComparer : ItemComparerBase
        {
            public override int Compare(MissionScoreboardPlayerVM x, MissionScoreboardPlayerVM y)
            {
                MissionScoreboardStatItemVM missionScoreboardStatItemVM = x.Stats.FirstOrDefault((s) => s.HeaderID == "kill");
                MissionScoreboardStatItemVM missionScoreboardStatItemVM2 = y.Stats.FirstOrDefault((s) => s.HeaderID == "kill");
                if (missionScoreboardStatItemVM != null && missionScoreboardStatItemVM2 != null)
                {
                    return int.Parse(missionScoreboardStatItemVM2.Item).CompareTo(int.Parse(missionScoreboardStatItemVM.Item)) * (!_isAscending ? 1 : -1);
                }

                return 0;
            }
        }

        public class ItemAssistComparer : ItemComparerBase
        {
            public override int Compare(MissionScoreboardPlayerVM x, MissionScoreboardPlayerVM y)
            {
                MissionScoreboardStatItemVM missionScoreboardStatItemVM = x.Stats.FirstOrDefault((s) => s.HeaderID == "assist");
                MissionScoreboardStatItemVM missionScoreboardStatItemVM2 = y.Stats.FirstOrDefault((s) => s.HeaderID == "assist");
                if (missionScoreboardStatItemVM != null && missionScoreboardStatItemVM2 != null)
                {
                    return int.Parse(missionScoreboardStatItemVM2.Item).CompareTo(int.Parse(missionScoreboardStatItemVM.Item)) * (!_isAscending ? 1 : -1);
                }

                return 0;
            }
        }

        private const string _nameHeaderID = "name";

        private const string _scoreHeaderID = "score";

        private const string _killHeaderID = "kill";

        private const string _assistHeaderID = "assist";

        private readonly MBBindingList<MissionScoreboardPlayerVM> _listToControl;

        private readonly ItemNameComparer _nameComparer;

        private readonly ItemScoreComparer _scoreComparer;

        private readonly ItemKillComparer _killComparer;

        private readonly ItemAssistComparer _assistComparer;

        private string _nameText;

        private string _scoreText;

        private string _killText;

        private string _assistText;

        private int _nameState = 1;

        private int _scoreState = 1;

        private int _killState = 1;

        private int _assistState = 1;

        private bool _isNameSelected;

        private bool _isScoreSelected;

        private bool _isKillSelected;

        private bool _isAssistSelected;

        [DataSourceProperty]
        public string NameText
        {
            get
            {
                return _nameText;
            }
            set
            {
                if (value != _nameText)
                {
                    _nameText = value;
                    OnPropertyChangedWithValue(value, "NameText");
                }
            }
        }

        [DataSourceProperty]
        public string ScoreText
        {
            get
            {
                return _scoreText;
            }
            set
            {
                if (value != _scoreText)
                {
                    _scoreText = value;
                    OnPropertyChangedWithValue(value, "ScoreText");
                }
            }
        }

        [DataSourceProperty]
        public string KillText
        {
            get
            {
                return _killText;
            }
            set
            {
                if (value != _killText)
                {
                    _killText = value;
                    OnPropertyChangedWithValue(value, "KillText");
                }
            }
        }

        [DataSourceProperty]
        public string AssistText
        {
            get
            {
                return _assistText;
            }
            set
            {
                if (value != _assistText)
                {
                    _assistText = value;
                    OnPropertyChangedWithValue(value, "AssistText");
                }
            }
        }

        [DataSourceProperty]
        public int NameState
        {
            get
            {
                return _nameState;
            }
            set
            {
                if (value != _nameState)
                {
                    _nameState = value;
                    OnPropertyChangedWithValue(value, "NameState");
                }
            }
        }

        [DataSourceProperty]
        public int ScoreState
        {
            get
            {
                return _scoreState;
            }
            set
            {
                if (value != _scoreState)
                {
                    _scoreState = value;
                    OnPropertyChangedWithValue(value, "ScoreState");
                }
            }
        }

        [DataSourceProperty]
        public int KillState
        {
            get
            {
                return _killState;
            }
            set
            {
                if (value != _killState)
                {
                    _killState = value;
                    OnPropertyChangedWithValue(value, "KillState");
                }
            }
        }

        [DataSourceProperty]
        public int AssistState
        {
            get
            {
                return _assistState;
            }
            set
            {
                if (value != _assistState)
                {
                    _assistState = value;
                    OnPropertyChangedWithValue(value, "AssistState");
                }
            }
        }

        [DataSourceProperty]
        public bool IsNameSelected
        {
            get
            {
                return _isNameSelected;
            }
            set
            {
                if (value != _isNameSelected)
                {
                    _isNameSelected = value;
                    OnPropertyChangedWithValue(value, "IsNameSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool IsScoreSelected
        {
            get
            {
                return _isScoreSelected;
            }
            set
            {
                if (value != _isScoreSelected)
                {
                    _isScoreSelected = value;
                    OnPropertyChangedWithValue(value, "IsScoreSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool IsKillSelected
        {
            get
            {
                return _isKillSelected;
            }
            set
            {
                if (value != _isKillSelected)
                {
                    _isKillSelected = value;
                    OnPropertyChangedWithValue(value, "IsKillSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool IsAssistSelected
        {
            get
            {
                return _isAssistSelected;
            }
            set
            {
                if (value != _isAssistSelected)
                {
                    _isAssistSelected = value;
                    OnPropertyChangedWithValue(value, "IsAssistSelected");
                }
            }
        }

        public ScoreBoardPlayerSortControllerVM(ref MBBindingList<MissionScoreboardPlayerVM> listToControl)
        {
            _listToControl = listToControl;
            _nameComparer = new ItemNameComparer();
            _scoreComparer = new ItemScoreComparer();
            _killComparer = new ItemKillComparer();
            _assistComparer = new ItemAssistComparer();
            ExecuteSortByScore();
            RefreshValues();
        }

        public override void RefreshValues()
        {
            NameText = GameTexts.FindText("str_scoreboard_header", "name").ToString();
            ScoreText = GameTexts.FindText("str_scoreboard_header", "score").ToString();
            KillText = GameTexts.FindText("str_scoreboard_header", "kill").ToString();
            AssistText = GameTexts.FindText("str_scoreboard_header", "assist").ToString();
        }

        public void SortByCurrentState()
        {
            if (IsNameSelected)
            {
                _listToControl.Sort(_nameComparer);
            }
            else if (IsScoreSelected)
            {
                _listToControl.Sort(_scoreComparer);
            }
            else if (IsKillSelected)
            {
                _listToControl.Sort(_killComparer);
            }
            else if (IsAssistSelected)
            {
                _listToControl.Sort(_assistComparer);
            }
        }

        public void ExecuteSortByName()
        {
            int nameState = NameState;
            SetAllStates(SortState.Default);
            NameState = (nameState + 1) % 3;
            if (NameState == 0)
            {
                NameState++;
            }

            _nameComparer.SetSortMode(NameState == 1);
            _listToControl.Sort(_nameComparer);
            IsNameSelected = true;
        }

        public void ExecuteSortByScore()
        {
            int scoreState = ScoreState;
            SetAllStates(SortState.Default);
            ScoreState = (scoreState + 1) % 3;
            if (ScoreState == 0)
            {
                ScoreState++;
            }

            _scoreComparer.SetSortMode(ScoreState == 1);
            _listToControl.Sort(_scoreComparer);
            IsScoreSelected = true;
        }

        public void ExecuteSortByKill()
        {
            int killState = KillState;
            SetAllStates(SortState.Default);
            KillState = (killState + 1) % 3;
            if (KillState == 0)
            {
                KillState++;
            }

            _killComparer.SetSortMode(KillState == 1);
            _listToControl.Sort(_killComparer);
            IsKillSelected = true;
        }

        public void ExecuteSortByAssist()
        {
            int assistState = AssistState;
            SetAllStates(SortState.Default);
            AssistState = (assistState + 1) % 3;
            if (AssistState == 0)
            {
                AssistState++;
            }

            _assistComparer.SetSortMode(AssistState == 1);
            _listToControl.Sort(_assistComparer);
            IsAssistSelected = true;
        }

        private void SetAllStates(SortState state)
        {
            NameState = (int)state;
            ScoreState = (int)state;
            KillState = (int)state;
            AssistState = (int)state;
            IsNameSelected = false;
            IsScoreSelected = false;
            IsKillSelected = false;
            IsAssistSelected = false;
        }
    }
}
