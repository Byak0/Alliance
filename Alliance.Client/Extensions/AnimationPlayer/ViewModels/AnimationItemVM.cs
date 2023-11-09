using Alliance.Common.Extensions.AnimationPlayer.Models;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// View model for each animation item composing the list
    /// Handle selected, favorite and filtered status
    /// </summary>
    internal class AnimationItemVM : ViewModel
    {
        private AnimationVM _animationVM;

        private Animation _animation;
        private string _name;
        private bool _isFavorite;
        private bool _isSelected;
        private bool _isFiltered;

        [DataSourceProperty]
        public Animation Animation
        {
            get
            {
                return _animation;
            }
            set
            {
                if (_animation != value)
                {
                    _animation = value;
                    OnPropertyChangedWithValue(value, "Animation");
                }
            }
        }

        [DataSourceProperty]
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFavorite
        {
            get
            {
                return _isFavorite;
            }
            set
            {
                if (value != _isFavorite)
                {
                    _isFavorite = value;
                    OnPropertyChangedWithValue(value, "IsFavorite");
                }
            }
        }

        [DataSourceProperty]
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChangedWithValue(value, "IsSelected");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFiltered
        {
            get
            {
                return _isFiltered;
            }
            set
            {
                if (value != _isFiltered)
                {
                    _isFiltered = value;
                    OnPropertyChangedWithValue(value, "IsFiltered");
                }
            }
        }

        public void SelectAnimation()
        {
            _animationVM.SelectAnimation(this);
        }

        public void ToggleFavorite()
        {
            IsFavorite = !IsFavorite;
            AnimationUserStore.Instance.ToggleFavoriteAnimation(Animation.Index, IsFavorite);
            _animationVM.SortAnimations(AnimationItemVMSortByFavorite.Instance);
        }

        public AnimationItemVM(AnimationVM animationVM, Animation animation, string name = "", bool favorite = false)
        {
            _animationVM = animationVM;
            Animation = animation;
            Name = name != "" ? name : animation.Name;
            IsFavorite = favorite;
        }
    }
}