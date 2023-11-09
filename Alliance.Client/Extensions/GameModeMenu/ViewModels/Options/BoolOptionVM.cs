using TaleWorlds.Library;
using TaleWorlds.Localization;
using System;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels.Options
{
    public class BoolOptionVM : OptionVM
    {
        private readonly Func<bool> _getValue;
        private readonly Action<bool> _setValue;

        [DataSourceProperty]
        public bool OptionValueAsBoolean
        {
            get
            {
                return _getValue();
            }
            set
            {
                if (value != _getValue() && _setValue != null)
                {
                    _setValue(value);
                    OnPropertyChanged(nameof(OptionValueAsBoolean));
                }
            }
        }

        public BoolOptionVM(TextObject name, TextObject description, Func<bool> getValue, Action<bool> setValue)
            : base(name, description, OptionsType.BooleanOption)
        {
            _getValue = getValue;
            _setValue = setValue;
        }

        public BoolOptionVM(MultiplayerOption option) : base(
            new TextObject(option.OptionType.ToString()),
            new TextObject(option.OptionType.GetOptionProperty().Description),
            OptionsType.BooleanOption)
        {
            _getValue = () => GetBoolValue(option);
            _setValue = newValue => option.UpdateValue(newValue);
        }

        private bool GetBoolValue(MultiplayerOption option)
        {
            option.GetValue(out bool value);
            return value;
        }
    }
}
