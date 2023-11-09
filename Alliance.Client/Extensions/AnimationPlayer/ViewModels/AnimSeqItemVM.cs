using Alliance.Common.Extensions.AnimationPlayer.Models;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// View model for each animation item composing an animation sequence
    /// Handle select, remove, move up, move down
    /// </summary>
    internal class AnimSeqItemVM : AnimationItemVM
    {
        private UserAnimSeqListVM _userAnimSeqListVM;

        private int _position;
        private bool _isFirst;
        private bool _isLast;

        [DataSourceProperty]
        public int Position
        {
            get
            {
                return _position;
            }
            set
            {
                if (value != _position)
                {
                    _position = value;
                    OnPropertyChangedWithValue(value, "Position");
                }
            }
        }

        [DataSourceProperty]
        public bool IsFirst
        {
            get
            {
                return _isFirst;
            }
            set
            {
                if (value != _isFirst)
                {
                    _isFirst = value;
                    OnPropertyChangedWithValue(value, "IsFirst");
                }
            }
        }

        [DataSourceProperty]
        public bool IsLast
        {
            get
            {
                return _isLast;
            }
            set
            {
                if (value != _isLast)
                {
                    _isLast = value;
                    OnPropertyChangedWithValue(value, "IsLast");
                }
            }
        }

        public void MoveUp()
        {
            _userAnimSeqListVM.MoveAnimationUp(this);
        }

        public void MoveDown()
        {
            _userAnimSeqListVM.MoveAnimationDown(this);
        }

        public void Remove()
        {
            _userAnimSeqListVM.RemoveAnimation(this);
        }

        public AnimSeqItemVM(AnimationVM animationVM, UserAnimSeqListVM userAnimSeqListVM, Animation animation) :
            base(animationVM, animation)
        {
            _userAnimSeqListVM = userAnimSeqListVM;
        }
    }
}