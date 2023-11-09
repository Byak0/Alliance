using Alliance.Common.Extensions.AnimationPlayer.Models;
using System;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AnimationPlayer.ViewModels
{
    public class AnimationSetCardVM : ViewModel
    {
        public AnimationSet AnimSet;
        protected Action<AnimationSetCardVM> _onSelect;
        private string _name;
        private bool _isSelected;

        public AnimationSetCardVM(Action<AnimationSetCardVM> onSelect, AnimationSet animSet)
        {
            AnimSet = animSet;
            _onSelect = onSelect;
            Name = animSet.Name;
        }

        public void Select()
        {
            _onSelect(this);
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
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChangedWithValue(value, "Name");
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
    }
}