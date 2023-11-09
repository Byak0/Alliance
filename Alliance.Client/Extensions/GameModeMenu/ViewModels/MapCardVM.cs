using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels
{
    public class MapCardVM : ViewModel
    {
        private readonly Action<MapCardVM> _onSelect;

        private bool _isSelected;
        private bool _isFiltered;
        private string _mapID;
        private string _mapName;

        public MapCardVM(string mapID, Action<MapCardVM> onSelect)
        {
            MapID = mapID;
            _onSelect = onSelect;
            RefreshValues();
        }

        public override void RefreshValues()
        {
            if (GameTexts.TryGetText("str_multiplayer_scene_name", out TextObject text, MapID))
            {
                Name = text.ToString();
            }
            else
            {
                Name = MapID;
            }
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
        public string MapID
        {
            get
            {
                return _mapID;
            }
            set
            {
                if (value != _mapID)
                {
                    _mapID = value;
                    OnPropertyChangedWithValue(value, "MapID");
                }
            }
        }

        [DataSourceProperty]
        public virtual string Name
        {
            get
            {
                return _mapName;
            }
            set
            {
                if (value != _mapName)
                {
                    _mapName = value;
                    OnPropertyChangedWithValue(value, "Name");
                }
            }
        }
    }
}