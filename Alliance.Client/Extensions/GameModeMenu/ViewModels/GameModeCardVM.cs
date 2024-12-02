using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.GameModeMenu.Models;
using Alliance.Common.GameModes;
using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels
{
    public class GameModeCardVM : ViewModel
    {
        public GameModeSettings GameModeSettings;

        public GameModeCardVM(Action<GameModeCardVM> onSelect, GameModeSettings gameModeSettings)
        {
            GameModeSettings = gameModeSettings;
            _onSelect = onSelect;
            Name = gameModeSettings.GameModeName;
            Hint = new HintViewModel(new TextObject(gameModeSettings.GameModeDescription) ?? TextObject.Empty, null);
            IsDisabled = !GameNetwork.MyPeer.IsAdmin() && !GameModeMenuConstants.AVAILABLE_GAME_MODES.Contains(gameModeSettings.GameMode);
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
        public HintViewModel Hint
        {
            get
            {
                return _hint;
            }
            set
            {
                if (value != _hint)
                {
                    _hint = value;
                    OnPropertyChangedWithValue(value, "Hint");
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
        public bool IsDisabled
        {
            get
            {
                return _isDisabled;
            }
            set
            {
                if (value != _isDisabled)
                {
                    _isDisabled = value;
                    OnPropertyChangedWithValue(value, "IsDisabled");
                }
            }
        }

        protected Action<GameModeCardVM> _onSelect;

        private string _name;
        private HintViewModel _hint;
        private bool _isSelected;
        private bool _isDisabled;
    }
}