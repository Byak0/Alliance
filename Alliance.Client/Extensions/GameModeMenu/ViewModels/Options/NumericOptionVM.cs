using TaleWorlds.Library;
using TaleWorlds.Localization;
using System;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels.Options
{
    public class NumericOptionVM : OptionVM
    {
        private readonly Func<float> _getValue;
        private readonly Action<float> _setValue;
        private float _min;
        private float _max;
        private bool _isDiscrete;
        private bool _updateContinuously;

        [DataSourceProperty]
        public float Min
        {
            get
            {
                return _min;
            }
            set
            {
                _min = value;
                OnPropertyChangedWithValue(value, nameof(Min));
            }
        }

        [DataSourceProperty]
        public float Max
        {
            get
            {
                return _max;
            }
            set
            {
                _max = value;
                OnPropertyChangedWithValue(value, nameof(Max));
            }
        }

        [DataSourceProperty]
        public float OptionValue
        {
            get
            {
                return _getValue();
            }
            set
            {
                _setValue(value);
                OnPropertyChangedWithValue(value, nameof(OptionValue));
                OnPropertyChanged(nameof(OptionValueAsString));
            }
        }

        [DataSourceProperty]
        public string OptionValueAsString
        {
            get
            {
                return IsDiscrete ? ((int)OptionValue).ToString() : OptionValue.ToString("F");
            }
        }

        [DataSourceProperty]
        public bool IsDiscrete
        {
            get => _isDiscrete;
            set
            {
                if (value == _isDiscrete)
                    return;
                _isDiscrete = value;
                OnPropertyChangedWithValue(value, nameof(IsDiscrete));
            }
        }

        [DataSourceProperty]
        public bool UpdateContinuously
        {
            get => _updateContinuously;
            set
            {
                if (value == _updateContinuously)
                    return;
                _updateContinuously = value;
                OnPropertyChangedWithValue(value, nameof(UpdateContinuously));
            }
        }

        public NumericOptionVM(TextObject name, TextObject description, Func<float> getValue,
            Action<float> setValue, float min, float max, bool isDiscrete, bool updateContinuously)
            : base(name, description, OptionsType.NumericOption)
        {
            _getValue = getValue;
            _setValue = setValue;
            Min = min;
            Max = max;
            IsDiscrete = isDiscrete;
            UpdateContinuously = updateContinuously;
        }

        public NumericOptionVM(MultiplayerOption option) : base(
            new TextObject(option.OptionType.ToString()),
            new TextObject(option.OptionType.GetOptionProperty().Description),
            OptionsType.NumericOption)
        {
            MultiplayerOptionsProperty optionProperty = option.OptionType.GetOptionProperty();
            _getValue = () => GetIntValue(option);
            _setValue = newValue => option.UpdateValue((int)newValue);
            Min = optionProperty.BoundsMin;
            Max = optionProperty.BoundsMax;
            IsDiscrete = true;
            UpdateContinuously = true;
        }

        private int GetIntValue(MultiplayerOption option)
        {
            option.GetValue(out int value);
            return value;
        }
    }
}
