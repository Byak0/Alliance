using TaleWorlds.Library;
using TaleWorlds.Localization;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Core;
using System.Linq;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels.Options
{
    public class SelectionOptionVM : OptionVM
    {
        private readonly SelectionOptionData _selectionOptionData;
        private readonly bool _commitOnlyWhenChange;
        private SelectorVM<SelectorItemVM> _selector;
        private bool _includeOrdinal;

        [DataSourceProperty]
        public SelectorVM<SelectorItemVM> Selector
        {
            get
            {
                return _selector;
            }
            set
            {
                if (value != _selector)
                {
                    _selector = value;
                    OnPropertyChangedWithValue(value, nameof(Selector));
                }
            }
        }

        public SelectionOptionVM(TextObject name, TextObject description, SelectionOptionData selectionOptionData, bool commitOnlyWhenChange, bool includeOrdinal = false)
            : base(name, description, OptionsType.MultipleSelectionOption)
        {
            _selectionOptionData = selectionOptionData;
            _commitOnlyWhenChange = commitOnlyWhenChange;
            _includeOrdinal = includeOrdinal;
            Selector = new SelectorVM<SelectorItemVM>(0, null);
            UpdateData(true);
            Selector.SelectedIndex = _selectionOptionData.GetDefaultValue();
        }

        public SelectionOptionVM(MultiplayerOption option, List<SelectionItem> choices) : base(
            new TextObject(option.OptionType.ToString()),
            new TextObject(option.OptionType.GetOptionProperty().Description),
            OptionsType.MultipleSelectionOption)
        {
            MultiplayerOptionsProperty optionProperty = option.OptionType.GetOptionProperty();

            _selectionOptionData = new SelectionOptionData(
                        () => GetChoiceIndex(option, choices),
                        newValue => option.UpdateValue(choices[newValue].Data),
                        2,
                        choices);

            _commitOnlyWhenChange = false;
            _includeOrdinal = false;
            Selector = new SelectorVM<SelectorItemVM>(0, null);
            UpdateData(true);
            Selector.SelectedIndex = _selectionOptionData.GetDefaultValue();
        }

        private int GetChoiceIndex(MultiplayerOption option, List<SelectionItem> choices)
        {
            option.GetValue(out string value);
            return choices.FindIndex(choice => choice.Data == value);
        }

        public void Commit()
        {
            _selectionOptionData.Commit();
        }

        public void Cancel()
        {
            Selector.SelectedIndex = _selectionOptionData.GetDefaultValue();
            UpdateValue(_selector);
        }

        public void UpdateData(bool initialUpdate)
        {
            IEnumerable<SelectionItem> selectableOptionNames = _selectionOptionData.GetSelectableOptionNames();
            Selector.SetOnChangeAction(null);
            Selector.SelectedIndex = -1;
            var selectionItems = selectableOptionNames as SelectionItem[] ?? selectableOptionNames.ToArray();
            if (selectionItems.Any() && selectionItems.All(n => n.IsLocalizationId))
            {
                List<TextObject> textObjectList = new List<TextObject>();
                foreach (var (selectionData, i) in selectionItems.Select((item, i) => (item, i)))
                {
                    TextObject text = GameTexts.FindText(selectionData.Data, selectionData.Variation);
                    textObjectList.Add(_includeOrdinal
                        ? new TextObject($"{i + 1}: " + "{Text}").SetTextVariable("Text", text)
                        : text);
                }
                Selector.Refresh(textObjectList, _selectionOptionData.GetValue(), UpdateValue);
            }
            else
            {
                List<string> stringList = new List<string>();
                foreach (SelectionItem selectionData in selectionItems)
                {
                    if (selectionData.IsLocalizationId)
                    {
                        TextObject text = GameTexts.FindText(selectionData.Data);
                        stringList.Add(text.ToString());
                    }
                    else
                        stringList.Add(selectionData.Data);
                }
                Selector.Refresh(stringList, _selectionOptionData.GetValue(), UpdateValue);
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            Selector?.RefreshValues();
        }

        private void UpdateValue(SelectorVM<SelectorItemVM> selector)
        {
            if (selector.SelectedIndex < 0)
                return;
            _selectionOptionData.SetValue(selector.SelectedIndex);
            if (!_commitOnlyWhenChange || _selectionOptionData.GetValue() != _selectionOptionData.GetDefaultValue())
                _selectionOptionData.Commit();
        }
    }
}
