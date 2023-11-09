using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Alliance.Editor.Extensions.ScenarioMaker.ViewModels
{
    public class GameEntityVM : ViewModel
    {
        private readonly Action<GameEntityVM> _onSelect;

        private bool _isSelected;
        private bool _isFiltered;
        private string _name;
        private GameEntity _gameEntity;

        public GameEntityVM(GameEntity gameEntity, Action<GameEntityVM> onSelect)
        {
            _gameEntity = gameEntity;
            _onSelect = onSelect;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            Name = _gameEntity.Name;
        }

        public void Select()
        {
            _onSelect(this);
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
        public virtual string Name
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
    }
}