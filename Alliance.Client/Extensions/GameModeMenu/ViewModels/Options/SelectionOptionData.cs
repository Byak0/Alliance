using System;
using System.Collections.Generic;

namespace Alliance.Client.Extensions.GameModeMenu.ViewModels.Options
{
    public class SelectionOptionData
    {
        private readonly Action<int> _setValue;
        private readonly Func<int> _getValue;
        private int _value;
        private readonly int _limit;
        private readonly IEnumerable<SelectionItem> _data;

        public SelectionOptionData(Func<int> getValue, Action<int> setValue, int limit, IEnumerable<SelectionItem> data)
        {
            _getValue = getValue;
            _setValue = setValue;
            _value = getValue();
            _limit = limit;
            _data = data;
        }

        public int GetDefaultValue()
        {
            return _getValue();
        }

        public void Commit()
        {
            _setValue(_value);
        }

        public int GetValue()
        {
            return _value;
        }

        public void SetValue(int value)
        {
            _value = value;
        }

        public int GetSelectableOptionsLimit()
        {
            return _limit;
        }

        public IEnumerable<SelectionItem> GetSelectableOptionNames()
        {
            return _data;
        }
    }

    public struct SelectionItem
    {
        public bool IsLocalizationId;
        public string Data;
        public string Variation;

        public SelectionItem(bool isLocalizationId, string data, string variation = null)
        {
            IsLocalizationId = isLocalizationId;
            Data = data;
            Variation = variation;
        }
    }
}
