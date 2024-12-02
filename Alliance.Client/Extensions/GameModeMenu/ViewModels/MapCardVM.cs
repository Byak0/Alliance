using System;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static Alliance.Common.Utilities.SceneList;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels
{
    public class MapCardVM : ViewModel
    {
        private readonly Action<MapCardVM> _onSelect;

        private bool _isSelected;
        private bool _isFiltered;
        private SceneInfo _mapInfo;
        private string _mapId;
        private string _mapName;
        private HintViewModel _hint;

        public MapCardVM(SceneInfo mapInfo, Action<MapCardVM> onSelect)
        {
            MapInfo = mapInfo;
            _onSelect = onSelect;

            MapId = mapInfo.Name;

            if (GameTexts.TryGetText("str_multiplayer_scene_name", out TextObject text, MapInfo.Name))
            {
                Name = text.ToString();
            }
            else
            {
                Name = MapInfo.Name;
            }

            TextObject hint = new TextObject(
                $"Module : {MapInfo.Module}\n" +
                $"Name : {MapInfo.Name}\n" +
                $"Spawns : " +
                $"{(MapInfo.HasGenericSpawn ? "Generic" : "")} " +
                $"{(MapInfo.HasSpawnForAttacker ? ", Attacker" : "")}" +
                $"{(MapInfo.HasSpawnForDefender ? ", Defender" : "")}" +
                $"{(MapInfo.HasSpawnVisual ? ", Visuals" : "")}" +
                $"\nNavmesh : " +
                $"{(MapInfo.HasNavmesh ? "Yes" : "None")}" +
                $"{(MapInfo.HasSAEPos ? "\nContains SAE positions" : "")}"
            );
            Hint = new HintViewModel(hint);
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

        public SceneInfo MapInfo
        {
            get
            {
                return _mapInfo;
            }
            set
            {
                if (value.Name != _mapInfo.Name)
                {
                    _mapInfo = value;
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
        public virtual string MapId
        {
            get
            {
                return _mapId;
            }
            set
            {
                if (value != _mapId)
                {
                    _mapId = value;
                    OnPropertyChangedWithValue(value, "MapId");
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