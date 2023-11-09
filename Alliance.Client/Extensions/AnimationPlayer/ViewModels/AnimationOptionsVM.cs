using Alliance.Common.Extensions.AnimationPlayer.Models;
using System.Linq;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// View model showing details and options for the currently selected animation
    /// </summary>
    internal class AnimationOptionsVM : ViewModel
    {
        private AnimationVM _animationVM;

        private bool _isVisible;
        private Animation _animation;
        private string _name;
        private string _actionSet;
        private string _skeleton;
        private float _speed;
        private float _maxDuration;

        [DataSourceProperty]
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (_isVisible != value)
                {
                    _isVisible = value;
                    OnPropertyChangedWithValue(value, "IsVisible");
                }
            }
        }

        [DataSourceProperty]
        public Animation Animation
        {
            get
            {
                return _animation;
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
        public string ActionSet
        {
            get
            {
                return _actionSet;
            }
            set
            {
                if (_actionSet != value)
                {
                    _actionSet = value;
                    OnPropertyChangedWithValue(value, "ActionSet");
                }
            }
        }

        [DataSourceProperty]
        public string Skeleton
        {
            get
            {
                return _skeleton;
            }
            set
            {
                if (_skeleton != value)
                {
                    _skeleton = value;
                    OnPropertyChangedWithValue(value, "Skeleton");
                }
            }
        }

        [DataSourceProperty]
        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    OnPropertyChangedWithValue(value, "Speed");
                }
            }
        }

        [DataSourceProperty]
        public float MaxDuration
        {
            get
            {
                return _maxDuration;
            }
            set
            {
                if (_maxDuration != value)
                {
                    _maxDuration = value;
                    OnPropertyChangedWithValue(value, "MaxDuration");
                }
            }
        }

        public AnimationOptionsVM(AnimationVM animationVM, Animation animation)
        {
            _animationVM = animationVM;
            _animation = animation;
            Name = animation.Name;
            ActionSet = animation.ActionSets == null ? "" : string.Join(" | ", animation.ActionSets.Select(actSet => actSet.GetName()).Distinct());
            Skeleton = animation.ActionSets == null ? "" : string.Join(" | ", animation.ActionSets.Select(actSet => actSet.GetSkeletonName()).Distinct());
            Speed = animation.Speed;
            MaxDuration = animation.MaxDuration;
            IsVisible = true;
        }

        public AnimationOptionsVM() { }
    }
}