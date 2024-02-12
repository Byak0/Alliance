using Alliance.Common.Extensions.AnimationPlayer;
using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// Main ViewModel for Animation menu.
    /// Responsible for storing all necessary informations for the interface and coordinating the underlying ViewModels.
    /// </summary>
    internal class AnimationVM : ViewModel
    {
        private bool _isVisible;
        private bool _showPlayerMenu;
        private bool _showAdminMenu;
        private string _filterText;
        private MBBindingList<AnimationItemVM> _animationsList;
        private MBBindingList<ShortcutBinderVM> _shortcutBinders;
        private AnimationItemVM _selectedAnimationItem;
        private CharacterViewModel _animationPreview;
        private UserAnimSeqListVM _userAnimSeqListVM;
        private AnimationOptionsVM _animationOptionsVM;
        private MBBindingList<AnimationSetCardVM> _animationSetsVM;
        private AnimationSetCardVM _selectedAnimationSet;

        public AnimationSet SelectedAnimSet => _selectedAnimationSet?.AnimSet;

        [DataSourceProperty]
        public MBBindingList<AnimationSetCardVM> AnimationSetsVM
        {
            get
            {
                return _animationSetsVM;
            }
            set
            {
                if (value != _animationSetsVM)
                {
                    _animationSetsVM = value;
                    OnPropertyChangedWithValue(value, "AnimationSetsVM");
                }
            }
        }

        //[DataSourceProperty]
        //public int SelectedSet
        //{
        //    get
        //    {
        //        return _selectedSet;
        //    }
        //    set
        //    {
        //        if (value != _selectedSet)
        //        {
        //            _selectedSet = value;
        //            OnPropertyChangedWithValue(value, "SelectedSet");
        //        }
        //    }
        //}

        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (value != _isVisible)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowPlayerMenu
        {
            get
            {
                return _showPlayerMenu;
            }
            set
            {
                if (value != _showPlayerMenu)
                {
                    _showPlayerMenu = value;
                    OnPropertyChangedWithValue(value, "ShowPlayerMenu");
                }
            }
        }

        [DataSourceProperty]
        public bool ShowAdminMenu
        {
            get
            {
                return _showAdminMenu;
            }
            set
            {
                if (value != _showAdminMenu)
                {
                    _showAdminMenu = value;
                    OnPropertyChangedWithValue(value, "ShowAdminMenu");
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
                    FilterAnimations(_filterText);
                    OnPropertyChangedWithValue(value, "FilterText");
                }
            }
        }

        [DataSourceProperty]
        public AnimationItemVM SelectedAnimationItem
        {
            get
            {
                return _selectedAnimationItem;
            }
            set
            {
                if (value != _selectedAnimationItem)
                {
                    _selectedAnimationItem = value;
                    OnPropertyChangedWithValue(value, "SelectedAnimationItem");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<AnimationItemVM> AnimationsList
        {
            get
            {
                return _animationsList;
            }
            set
            {
                if (value != _animationsList)
                {
                    _animationsList = value;
                    OnPropertyChangedWithValue(value, "AnimationsList");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<ShortcutBinderVM> ShortcutBinders
        {
            get
            {
                return _shortcutBinders;
            }
            set
            {
                if (value != _shortcutBinders)
                {
                    _shortcutBinders = value;
                    OnPropertyChangedWithValue(value, "ShortcutBinders");
                }
            }
        }

        [DataSourceProperty]
        public CharacterViewModel AnimationPreview
        {
            get
            {
                return _animationPreview;
            }
            set
            {
                if (value != _animationPreview)
                {
                    _animationPreview = value;
                    OnPropertyChangedWithValue(value, "AnimationPreview");
                }
            }
        }

        [DataSourceProperty]
        public UserAnimSeqListVM UserAnimSeqListVM
        {
            get
            {
                return _userAnimSeqListVM;
            }
            set
            {
                if (value != _userAnimSeqListVM)
                {
                    _userAnimSeqListVM = value;
                    OnPropertyChangedWithValue(value, "CustomUserListVM");
                }
            }
        }

        [DataSourceProperty]
        public AnimationOptionsVM AnimationOptionsVM
        {
            get
            {
                return _animationOptionsVM;
            }
            set
            {
                if (value != _animationOptionsVM)
                {
                    _animationOptionsVM = value;
                    OnPropertyChangedWithValue(value, "AnimationOptionsVM");
                }
            }
        }

        public event EventHandler OnCloseMenu;

        public AnimationVM()
        {
            // Fill Animations Sets
            AnimationSetsVM = new MBBindingList<AnimationSetCardVM>();
            foreach (AnimationSet set in AnimationUserStore.Instance.AnimationSets)
            {
                AnimationSetsVM.Add(new AnimationSetCardVM(OnAnimationSetSelected, set));
            }

            // Fill Animations list
            MBBindingList<AnimationItemVM> animationsList = new MBBindingList<AnimationItemVM>();
            foreach (Animation animation in AnimationSystem.Instance.DefaultAnimations)
            {
                bool isFavorite = AnimationUserStore.Instance.FavoriteAnimations.Contains(animation.Index);
                animationsList.Add(new AnimationItemVM(this, animation, favorite: isFavorite));
            }
            animationsList.Sort(AnimationItemVMSortByFavorite.Instance);
            AnimationsList = animationsList;

            // Initialize Animation preview
            AnimationPreview = new CharacterViewModel();
            AnimationPreview.ShouldLoopCustomAnimation = true;
            BasicCharacterObject bco = Mission.Current.MainAgent != null ? Mission.Current.MainAgent.Character : MBObjectManager.Instance.GetFirstObject<BasicCharacterObject>();
            AnimationPreview.FillFrom(bco);

            // Initialize user custom animations list
            UserAnimSeqListVM = new UserAnimSeqListVM(this);

            // Initialize shortcut binders;
            ShortcutBinders = new MBBindingList<ShortcutBinderVM>();
            for (int i = 0; i <= 8; i++)
            {
                ShortcutBinders.Add(new ShortcutBinderVM(this, _selectedAnimationSet?.AnimSet, i, (i + 1).ToString()));
            }

            // Initialize animation options
            AnimationOptionsVM = new AnimationOptionsVM();

            _selectedAnimationSet = AnimationSetsVM.ElementAtOrDefault(0);
            OnAnimationSetSelected(_selectedAnimationSet);
        }

        public void CloseMenu()
        {
            OnCloseMenu?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Refresh keybinders VM with the updated list of custom animations
        /// </summary>
        public void ReloadKeyBinders()
        {
            List<AnimSeqSelectorItemVM> newList = new List<AnimSeqSelectorItemVM>();
            foreach (AnimationSequence animSequence in AnimationUserStore.Instance.AnimationSequences)
            {
                newList.Add(new AnimSeqSelectorItemVM(animSequence));
            }

            foreach (ShortcutBinderVM shortcutBinder in ShortcutBinders)
            {
                shortcutBinder.Refresh(SelectedAnimSet, newList);
            }
        }

        /// <summary>
        /// Filter list of animations with given text filter
        /// </summary>
        public void FilterAnimations(string filterText)
        {
            foreach (AnimationItemVM animationItemVM in _animationsList)
            {
                if (animationItemVM != null)
                {
                    animationItemVM.IsFiltered = !animationItemVM.Name.ToLower().Contains(filterText);
                }
            }
        }

        /// <summary>
        /// Sort animations list with given comparer
        /// </summary>
        public void SortAnimations(IComparer<AnimationItemVM> comparer)
        {
            _animationsList.Sort(comparer);
        }

        public void SelectAnimation(AnimationItemVM animationItemVM)
        {
            if (SelectedAnimationItem != null) SelectedAnimationItem.IsSelected = false;
            SelectedAnimationItem = animationItemVM;
            SelectedAnimationItem.IsSelected = true;
            PlayAnimationPreview(SelectedAnimationItem.Animation.Name);

            AnimationOptionsVM = new AnimationOptionsVM(this, animationItemVM.Animation);
        }

        /// <summary>
        /// Play given animation in the CharacterViewModel
        /// </summary>
        public void PlayAnimationPreview(string animationName)
        {
            // TODO : Check if it causes issues
            // Using ExecuteStartCustomAnimation instead of IdleAction as it allow to loop more animations.
            // However it may be less stable
            //AnimationPreview.IdleAction = animationName;
            AnimationPreview.ExecuteStartCustomAnimation(animationName, true, 0);
        }

        private void OnAnimationSetSelected(AnimationSetCardVM animSetCardVM)
        {
            if (_selectedAnimationSet != null) _selectedAnimationSet.IsSelected = false;
            _selectedAnimationSet = animSetCardVM;
            _selectedAnimationSet.IsSelected = true;
            ReloadKeyBinders();
        }
    }

    /// <summary>
    /// Comparer of AnimationItemVM, first by favorite status and then by Index
    /// </summary>
    class AnimationItemVMSortByFavorite : IComparer<AnimationItemVM>
    {
        public static AnimationItemVMSortByFavorite Instance = new AnimationItemVMSortByFavorite();

        public int Compare(AnimationItemVM x, AnimationItemVM y)
        {
            if (x.IsFavorite && !y.IsFavorite)
                return -1;
            if (!x.IsFavorite && y.IsFavorite)
                return 1;
            return x.Animation.Index < y.Animation.Index ? -1 : 1;
        }
    }
}