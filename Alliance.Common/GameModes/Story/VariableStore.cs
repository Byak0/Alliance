using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story
{
	public class VariableStore
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

		public T Get<T>(string key)
		{
			if (_values.TryGetValue(key, out object v) && v is T typed) return typed;
			return default;
		}

		public void Set(string key, object value) => _values[key] = value;

		public bool Has(string key) => _values.ContainsKey(key);

		public void Reset() => _values.Clear();
	}
}
