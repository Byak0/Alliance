using System;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.TroopSpawner.ViewModels
{
    internal class SpawnTroopsVM : ViewModel
    {
        private bool _isVisible;

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

        public event EventHandler OnCloseMenu;

        public SpawnTroopsVM()
        {
        }

        public void CloseMenu()
        {
            OnCloseMenu(this, EventArgs.Empty);
        }
    }
}