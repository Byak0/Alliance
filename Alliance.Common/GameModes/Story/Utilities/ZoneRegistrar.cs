using Alliance.Common.GameModes.Story.Models;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using TaleWorlds.Engine;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Walks an action/condition object graph and registers every embedded <see cref="Zone"/> (inside a
	/// <c>LiteralValue&lt;Zone&gt;</c>, nested in a <c>Function</c>, or held directly) with its host entity.
	/// This replaces the old typed <c>ZoneSource</c>/<c>Zone</c> field scanning now that zone fields are
	/// <c>ValueSource&lt;Zone&gt;</c> expression trees.
	/// </summary>
	public static class ZoneRegistrar
	{
		/// <summary>Recursively registers every <see cref="Zone"/> reachable from <paramref name="root"/>.</summary>
		public static void RegisterAll(object root, WeakGameEntity host)
		{
			Walk(root, host, new HashSet<object>());
		}

		private static void Walk(object obj, WeakGameEntity host, HashSet<object> visited)
		{
			if (obj == null) return;
			System.Type t = obj.GetType();
			// Skip these as they can't hold a Zone
			if (t.IsPrimitive || t.IsEnum || t == typeof(string) || t.IsValueType) return;			
			// Skip already visited objects
			if (!visited.Add(obj)) return;
			if (obj is IEnumerable enumerable)
			{
				// Walk each item in lists/arrays/collections
				foreach (object item in enumerable) Walk(item, host, visited);
				return;
			}
			// If this is a Zone, register it with the host entity
			if (obj is Zone zone)
			{
				zone.Register(host);
				return;
			}
			// Walk each public field of the object
			foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				if (field.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;
				object value = field.GetValue(obj);
				if (value == null) continue;
				Walk(value, host, visited);
			}
		}
	}
}
