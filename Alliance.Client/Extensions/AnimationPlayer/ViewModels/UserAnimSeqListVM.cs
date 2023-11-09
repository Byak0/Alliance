using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// ViewModel handling user list of animations sequences.
    /// </summary>
    internal class UserAnimSeqListVM : ViewModel
    {
        private AnimationVM _animationVM;
        private SelectorVM<AnimSeqSelectorItemVM> _animSeqSelector;
        private MBBindingList<AnimSeqItemVM> _animationSequence;

        [DataSourceProperty]
        public SelectorVM<AnimSeqSelectorItemVM> AnimationSequenceSelector
        {
            get
            {
                return _animSeqSelector;
            }
            set
            {
                if (value != _animSeqSelector)
                {
                    _animSeqSelector = value;
                    OnPropertyChangedWithValue(value, "AnimationSequenceSelector");
                }
            }
        }

        [DataSourceProperty]
        public MBBindingList<AnimSeqItemVM> AnimationSequenceVM
        {
            get
            {
                return _animationSequence;
            }
            set
            {
                if (value != _animationSequence)
                {
                    _animationSequence = value;
                    OnPropertyChangedWithValue(value, "AnimationSequence");
                }
            }
        }

        public UserAnimSeqListVM(AnimationVM animationVM)
        {
            _animationVM = animationVM;

            // Initialize animations sequences list
            AnimationSequenceSelector = new SelectorVM<AnimSeqSelectorItemVM>(0, new Action<SelectorVM<AnimSeqSelectorItemVM>>(OnAnimationSequenceSelected));
            foreach (AnimationSequence animSeq in AnimationUserStore.Instance.AnimationSequences)
            {
                AnimSeqSelectorItemVM animSeqSelectorItemVM = new AnimSeqSelectorItemVM(animSeq);
                AnimationSequenceSelector.AddItem(animSeqSelectorItemVM);
            }

            // Initialize animation sequence
            AnimationSequenceVM = new MBBindingList<AnimSeqItemVM>();
        }

        /// <summary>
        /// Action automatically called by the AnimationSequenceSelector when an item is selected
        /// </summary>
        private void OnAnimationSequenceSelected(SelectorVM<AnimSeqSelectorItemVM> selector)
        {
            // Clear current animation sequence
            AnimationSequenceVM.Clear();
            // Check that selected item is not null (eg. when deleting last available animation sequence)
            if (selector.SelectedItem != null) LoadAnimationSequence(selector.SelectedItem.AnimationSequence);
        }

        /// <summary>
        /// Load a user animation sequence.
        /// </summary>
        private void LoadAnimationSequence(AnimationSequence animationSequence)
        {
            int pos = 0;
            foreach (Animation animation in animationSequence.Animations)
            {
                AnimSeqItemVM animSeqItemVM = new AnimSeqItemVM(_animationVM, this, animation);
                animSeqItemVM.Position = pos;
                if (pos == 0) animSeqItemVM.IsFirst = true;
                if (pos == animationSequence.Animations.Count - 1) animSeqItemVM.IsLast = true;
                AnimationSequenceVM.Add(animSeqItemVM);
                pos++;
            }
        }

        /// <summary>
        /// Rename selected animation sequence.
        /// </summary>
        public void RenameAnimationSequence()
        {
            if (AnimationSequenceSelector.SelectedItem?.AnimationSequence?.Name == null) return;
            // Prompt a text inquiry for user to enter a new name
            InformationManager.ShowTextInquiry(
                new TextInquiryData(AnimationSequenceSelector.SelectedItem.AnimationSequence.Name,
                "Choose a new name :", true, true,
                new TextObject("{=WiNRdfsm}Done", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(),
                new Action<string>(OnRenameDone), null, false, null, "", AnimationSequenceSelector.SelectedItem.AnimationSequence.Name),
                false);
        }

        /// <summary>
        /// Called when user validate the renaming inquiry.
        /// </summary>
        private void OnRenameDone(string newName)
        {
            AnimationUserStore.Instance.AnimationSequences.Find(anim => anim == AnimationSequenceSelector.SelectedItem.AnimationSequence).Name = newName;
            AnimationSequenceSelector.SelectedItem.StringItem = newName;
            _animationVM.ReloadKeyBinders();
        }

        /// <summary>
        /// Create a new animation sequence.
        /// </summary>
        public void NewAnimationSequence()
        {
            // Prompt a text inquiry for user to enter a name
            InformationManager.ShowTextInquiry(
                new TextInquiryData(new TextObject("{=7WdWK2Dt}New animation", null).ToString(),
                "Choose a name :", true, true,
                new TextObject("{=WiNRdfsm}Done", null).ToString(), new TextObject("{=3CpNUnVl}Cancel", null).ToString(),
                new Action<string>(OnNewAnimationSequenceNamed), null, false, null, "", "My new animation"),
                false);
        }

        /// <summary>
        /// Called when user validate the naming inquiry.
        /// </summary>
        private void OnNewAnimationSequenceNamed(string animSequenceName)
        {
            // Create a default animation based on user selection
            int animationIndex = _animationVM.SelectedAnimationItem?.Animation?.Index ?? 0;
            Animation animation = new Animation(animationIndex);

            // Create a new animation sequence and give it the default animation as a starter
            int id = 0;
            foreach (AnimationSequence seq in AnimationUserStore.Instance.AnimationSequences)
            {
                if (seq.Index > id) id = seq.Index;
            }
            AnimationSequence animSeq = new AnimationSequence(id + 1, animSequenceName);
            animSeq.Animations.Add(animation);

            // Add the sequence to the AnimationSequence selector and select it
            AnimSeqSelectorItemVM selectorItemVM = new AnimSeqSelectorItemVM(animSeq);
            AnimationSequenceSelector.AddItem(selectorItemVM);
            AnimationSequenceSelector.SelectedIndex = AnimationSequenceSelector.ItemList.IndexOf(selectorItemVM);

            // Save new sequence
            SaveAnimationSequence();

            // Reload KeyBinders to show new sequence
            _animationVM.ReloadKeyBinders();
        }

        /// <summary>
        /// Save animation sequence into user store.
        /// </summary>
        public void SaveAnimationSequence()
        {
            if (AnimationSequenceSelector.SelectedItem?.AnimationSequence == null) return;
            AnimationUserStore.Instance.AnimationSequences.Remove(AnimationSequenceSelector.SelectedItem.AnimationSequence);
            AnimationUserStore.Instance.AnimationSequences.Add(AnimationSequenceSelector.SelectedItem.AnimationSequence);
            AnimationUserStore.Instance.Serialize();
        }

        /// <summary>   
        /// Remove animation sequence from user store and VM.
        /// </summary>
        public void RemoveAnimationSequence()
        {
            if (AnimationSequenceSelector.SelectedItem?.AnimationSequence == null) return;
            AnimationUserStore.Instance.AnimationSequences.Remove(AnimationSequenceSelector.SelectedItem.AnimationSequence);
            AnimationUserStore.Instance.Serialize();
            AnimationSequenceSelector.ItemList.Remove(AnimationSequenceSelector.SelectedItem);
            AnimationSequenceSelector.SelectedIndex = -1;
            AnimationSequenceVM.Clear();
            _animationVM.ReloadKeyBinders();
        }

        /// <summary>
        /// Move selected AnimSeqItemVM one place up in the AnimationSequenceVM.
        /// </summary>
        public void MoveAnimationUp(AnimSeqItemVM animSeqItemVM)
        {
            AnimationSequenceSelector.SelectedItem.AnimationSequence.MoveAnimationUp(animSeqItemVM.Animation);

            // Reload the AnimationSequenceVM
            AnimationSequenceVM.Clear();
            LoadAnimationSequence(AnimationSequenceSelector.SelectedItem.AnimationSequence);
        }

        /// <summary>
        /// Move selected AnimSeqItemVM one place down in the AnimationSequenceVM.
        /// </summary>
        public void MoveAnimationDown(AnimSeqItemVM animSeqItemVM)
        {
            AnimationSequenceSelector.SelectedItem.AnimationSequence.MoveAnimationDown(animSeqItemVM.Animation);

            // Reload the AnimationSequenceVM
            AnimationSequenceVM.Clear();
            LoadAnimationSequence(AnimationSequenceSelector.SelectedItem.AnimationSequence);
        }

        /// <summary>
        /// Insert a new animation at the end of AnimationSequenceVM based on the selected animation.
        /// </summary>
        public void AddAnimation()
        {
            if (AnimationSequenceSelector.SelectedItem?.AnimationSequence == null) return;

            // Create an animation based on user selection
            int animationIndex = _animationVM.SelectedAnimationItem?.Animation?.Index ?? 0;
            Animation animation = new Animation(animationIndex);

            // Add the animation to the AnimationSequence
            AnimationSequenceSelector.SelectedItem.AnimationSequence.Animations.Add(animation);

            // Reload the AnimationSequenceVM
            AnimationSequenceVM.Clear();
            LoadAnimationSequence(AnimationSequenceSelector.SelectedItem.AnimationSequence);
        }

        /// <summary>
        /// Remove given AnimSeqItemVM from the AnimationSequenceVM.
        /// </summary>
        public void RemoveAnimation(AnimSeqItemVM animSeqItemVM)
        {
            // Remove given Animation from the selected AnimationSequence
            AnimationSequenceSelector.SelectedItem.AnimationSequence.Animations.Remove(animSeqItemVM.Animation);

            // Reload the AnimationSequenceVM
            AnimationSequenceVM.Clear();
            LoadAnimationSequence(AnimationSequenceSelector.SelectedItem.AnimationSequence);
        }
    }
}