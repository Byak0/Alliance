using TaleWorlds.Library;

namespace Alliance.Client.Extensions.AdminMenu.ViewModels
{
    public class ServerMessageVM : ViewModel
    {
        private string _message;
        private string _color;

        public ServerMessageVM(string message, ColorList colorList)
        {
            _message = message;
            _color = InitColorChoice(colorList);
        }

        public ServerMessageVM(string message, string color)
        {
            _message = message;
            _color = color;
        }

        [DataSourceProperty]
        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (value != _message)
                {
                    _message = value;
                    OnPropertyChangedWithValue(value, "Message");
                }
            }
        }

        [DataSourceProperty]
        public string Color
        {
            get
            {
                return _color;
            }
            set
            {
                if (value != _color)
                {
                    _color = value;
                    OnPropertyChangedWithValue(value, "Color");
                }
            }
        }


        private string InitColorChoice(ColorList listColor)
        {
            switch (listColor)
            {
                case ColorList.Success:
                    {
                        return "Vulcain.Text.Success";
                    }
                case ColorList.Info:
                    {
                        return "Vulcain.Text.Info";
                    }
                case ColorList.Warning:
                    {
                        return "Vulcain.Text.Warning";
                    }
                case ColorList.Danger:
                    {
                        return "Vulcain.Text.Danger";
                    }
                default:
                    {
                        return "Vulcain.Text.Danger";
                    }
            }
        }

        public enum ColorList
        {
            Success = 0,
            Info = 1,
            Warning = 2,
            Danger = 3,
        }
    }
}
