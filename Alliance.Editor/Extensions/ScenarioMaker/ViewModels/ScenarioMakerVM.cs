using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Alliance.Editor.Extensions.ScenarioMaker.ViewModels
{
    /// <summary>
    /// ViewModel for the scenario maker menu.
    /// </summary>
    internal class ScenarioMakerVM : ViewModel
    {
        private bool _isVisible;
        private bool _showPlayerMenu;
        private bool _showAdminMenu;
        private MBBindingList<GameEntityVM> _gameEntities;
        private GameEntityVM _selectedGameEntity;

        [DataSourceProperty]
        public MBBindingList<GameEntityVM> GameEntities
        {
            get
            {
                return _gameEntities;
            }
            set
            {
                if (value != _gameEntities)
                {
                    _gameEntities = value;
                    OnPropertyChangedWithValue(value, "GameEntities");
                }
            }
        }

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

        public event EventHandler OnCloseMenu;

        public ScenarioMakerVM(Scene currentScene)
        {
            List<GameEntity> gameEntities = new List<GameEntity>();
            currentScene?.GetEntities(ref gameEntities);

            if (gameEntities.Count > 0)
            {
                GameEntities = new MBBindingList<GameEntityVM>();
                foreach (GameEntity entity in gameEntities)
                {
                    GameEntities.Add(new GameEntityVM(entity, new Action<GameEntityVM>(OnGameEntitySelected)));
                }

                OnGameEntitySelected(GameEntities[0]);
            }
        }

        private void OnGameEntitySelected(GameEntityVM gameEntityVM)
        {
            if (_selectedGameEntity != null) _selectedGameEntity.IsSelected = false;
            _selectedGameEntity = gameEntityVM;
            _selectedGameEntity.IsSelected = true;
        }

        public void CloseMenu()
        {
            OnCloseMenu?.Invoke(this, EventArgs.Empty);
        }
    }
}