using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Utilities
{
	public class TriggerContext
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

		public void Set(string key, object value) => _values[key] = value;

		public T Get<T>(string key)
		{
			if (_values.TryGetValue(key, out object v) && v is T typed) return typed;
			return default;
		}

		public bool Has(string key) => _values.ContainsKey(key);
	}
}
