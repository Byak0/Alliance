using System;
using TaleWorlds.Library;

namespace Alliance.Client.Extensions.BrushPicker.ViewModels
{
    internal class BrushPickerVM : ViewModel
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

        public BrushPickerVM()
        {
        }

        public void CloseMenu()
        {
            OnCloseMenu?.Invoke(this, EventArgs.Empty);
        }
    }
}