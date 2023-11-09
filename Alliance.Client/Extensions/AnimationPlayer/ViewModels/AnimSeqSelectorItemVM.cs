using Alliance.Common.Extensions.AnimationPlayer.Models;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// View model for each animation item composing the SelectorList
    /// Just a basic SelectorItemVM with an additional AnimationSequence attribute
    /// </summary>
    internal class AnimSeqSelectorItemVM : SelectorItemVM
    {
        private AnimationSequence _animation;

        [DataSourceProperty]
        public AnimationSequence AnimationSequence
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

        public AnimSeqSelectorItemVM(AnimationSequence animationSequence) : base(animationSequence.Name)
        {
            AnimationSequence = animationSequence;
        }
    }
}