using TaleWorlds.Library;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels.Options
{
    public enum OptionsType
    {
        None = -1,
        BooleanOption = 0,
        NumericOption = 1,
        MultipleSelectionOption = 3,
        InputOption = 4,
        ActionOption = 5
    }

    /// <summary>
    /// Generic VM for handling options. Highly inspired by RTS Camera mod menu from LiZhenhuan1019.
    /// </summary>
    public abstract class OptionVM : ViewModel
    {
        private int _optionTypeId = -1;
        private TextObjectVM _name;
        private HintViewModel _description;
        private bool _isEnabled;

        [DataSourceProperty]
        public TextObjectVM Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        [DataSourceProperty]
        public HintViewModel Description
        {
            get
            {
                return _description;
            }
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged(nameof(Description));
                }
            }
        }

        [DataSourceProperty]
        public int OptionTypeID
        {
            get
            {
                return _optionTypeId;
            }
            set
            {
                if (value != _optionTypeId)
                {
                    _optionTypeId = value;
                    OnPropertyChangedWithValue(value, nameof(OptionTypeID));
                }
            }
        }

        [DataSourceProperty]
        public bool IsEnabled
        {
            get
            {
                return _isEnabled;
            }
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    OnPropertyChangedWithValue(value, nameof(IsEnabled));
                }
            }
        }

        protected OptionVM(TextObject name, TextObject description, OptionsType typeID, bool isEnabled = true)
        {
            Name = new TextObjectVM(name);
            if (description != null)
            {
                Description = new HintViewModel(description);
            }
            OptionTypeID = (int)typeID;
            IsEnabled = isEnabled;
        }
    }
}
