using System.Collections.Generic;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// A simple key-value store for variables used in Scenario/ScriptedEvent.
	/// It allows storing and retrieving values of any type by string keys.
	/// </summary>
	public class VariableStore
	{
		private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

		public T Get<T>(string key)
		{
			if (_values.TryGetValue(key, out object v) && v is T typed) return typed;
			return default;
		}

		public object Get(string key)
		{
			_values.TryGetValue(key, out object v);
			return v;
		}

		public void Set(string key, object value) => _values[key] = value;

		public bool Has(string key) => _values.ContainsKey(key);

		public void Reset() => _values.Clear();

		public IEnumerable<KeyValuePair<string, object>> GetAllEntries() => _values;
	}
}
