using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    /// <summary>
    /// ViewModel handling user's bindings of its animation sequences.
    /// </summary>
    internal class ShortcutBinderVM : ViewModel
    {
        private AnimationSet _animSet;

        private AnimationVM _animationVM;
        private SelectorVM<AnimSeqSelectorItemVM> _animSeqSelector;
        private string _shortcut;
        private int _shortcutIndex;
        private CharacterViewModel _animationPreview;

        /// <summary>
        /// List of custom animations that can be binded.
        /// </summary>
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
                    OnPropertyChangedWithValue(value, "CustomAnimationsList");
                }
            }
        }

        /// <summary>
        /// Shortcut name.
        /// </summary>
        [DataSourceProperty]
        public string Shortcut
        {
            get
            {
                return _shortcut;
            }
            set
            {
                if (value != _shortcut)
                {
                    _shortcut = value;
                    OnPropertyChangedWithValue(value, "Shortcut");
                }
            }
        }

        /// <summary>
        /// Shortcut index (from 0 to 8).
        /// </summary>
        [DataSourceProperty]
        public int ShortcutIndex
        {
            get
            {
                return _shortcutIndex;
            }
            set
            {
                if (value != _shortcutIndex)
                {
                    _shortcutIndex = value;
                    OnPropertyChangedWithValue(value, "ShortcutIndex");
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

        public ShortcutBinderVM(AnimationVM animationVM, AnimationSet animSet, int shortcutIndex, string shortcut)
        {
            _animationVM = animationVM;
            _animSet = animSet;
            ShortcutIndex = shortcutIndex;
            Shortcut = shortcut;

            // Initialize Animation preview
            AnimationPreview = new CharacterViewModel();
            BasicCharacterObject bco = Mission.Current.MainAgent != null ? Mission.Current.MainAgent.Character : MBObjectManager.Instance.GetFirstObject<BasicCharacterObject>();
            AnimationPreview.FillFrom(bco);
            if (bco.HasMount()) AnimationPreview.SetEquipment(EquipmentIndex.Horse, EquipmentElement.Invalid);

            AnimationSequenceSelector = new SelectorVM<AnimSeqSelectorItemVM>(0, null);

            if (_animSet != null)
            {
                // Fill animations sequences dropdown list
                foreach (AnimationSequence animSequence in AnimationUserStore.Instance.AnimationSequences)
                {
                    AnimSeqSelectorItemVM selectorItemVM = new AnimSeqSelectorItemVM(animSequence);
                    AnimationSequenceSelector.AddItem(selectorItemVM);
                    // Select the animation sequence if it is binded to this shortcut
                    if (animSequence.Index == _animSet.BindedAnimSequence[shortcutIndex]?.AnimSequenceIndex)
                    {
                        AnimationSequenceSelector.SelectedIndex = AnimationSequenceSelector.ItemList.IndexOf(selectorItemVM);
                    }
                }

                // Set action on bind selection
                AnimationSequenceSelector.SetOnChangeAction(new Action<SelectorVM<AnimSeqSelectorItemVM>>(OnAnimationSequenceBindSelected));
            }
        }

        /// <summary>
        /// Method called by the SelectorVM when an item from the AnimationSequenceSelector is selected.
        /// </summary>
        private void OnAnimationSequenceBindSelected(SelectorVM<AnimSeqSelectorItemVM> selector)
        {
            if (selector.SelectedItem?.AnimationSequence == null) return;
            BindAnimationSequence(AnimationUserStore.Instance.AnimationSequences.Find(animation => animation == selector.SelectedItem.AnimationSequence));
        }

        /// <summary>
        /// Bind an animation sequence to the shortcut.
        /// </summary>
        private void BindAnimationSequence(AnimationSequence animSeq)
        {
            if (animSeq == null) return;
            AnimationUserStore.Instance.BindAnimation(_animSet, ShortcutIndex, Shortcut, animSeq.Index);
        }

        /// <summary>
        /// Refresh shortcut bindable animations list with given list.
        /// Keep previously selected animation if possible.
        /// </summary>
        public void Refresh(AnimationSet animSet, List<AnimSeqSelectorItemVM> newList)
        {
            int selectedIndex = -1;
            if (animSet != _animSet)
            {
                _animSet = animSet;
                selectedIndex = newList.FindIndex(x => x.AnimationSequence.Index == _animSet.BindedAnimSequence[ShortcutIndex]?.AnimSequenceIndex);

            }
            else
            {
                selectedIndex = AnimationUserStore.Instance.AnimationSequences.IndexOf(AnimationSequenceSelector.GetCurrentItem()?.AnimationSequence);

            }
            AnimationSequenceSelector.Refresh(newList, selectedIndex, null);
            AnimationSequenceSelector.SetOnChangeAction(OnAnimationSequenceBindSelected);
            PlayAnimationPreview();
        }

        /// <summary>
        /// Play animation in the CharacterViewModel
        /// </summary>
        public void PlayAnimationPreview()
        {
            AnimationPreview.IdleAction = AnimationSequenceSelector.GetCurrentItem()?.AnimationSequence?.Animations[0].Name;
        }

        /// <summary>
        /// Play animation for real and exit menu.
        /// </summary>
        public void PlayAnimation()
        {
            AnimationRequestEmitter.Instance.RequestAnimationSequenceForTarget(AnimationSequenceSelector.GetCurrentItem()?.AnimationSequence, Agent.Main);
            _animationVM.CloseMenu();
        }
    }
}