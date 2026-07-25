using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Resolves the target-correct (Client or Server) implementation for a scenario action.
	/// <para>
	/// Each leaf project annotates its variant subclasses with <see cref="OverrideActionAttribute"/>.
	/// <see cref="Initialize"/> scans every loaded assembly and maps the Common base action type to the
	/// variant type available in the current process. Because each process only loads its own target
	/// assembly, the map holds the right variant per target.
	/// </para>
	/// Actions without a registered override are used as-is (the deserialized Common instance).
	/// </summary>
	public static class ActionOverrideRegistry
	{
		private static readonly Dictionary<Type, Type> _overrides = new Dictionary<Type, Type>();

		public static void Initialize()
		{
			_overrides.Clear();

			IEnumerable<Type> allTypes = AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(a =>
				{
					try
					{
						return a.GetTypes();
					}
					catch (ReflectionTypeLoadException ex)
					{
						return ex.Types.Where(t => t != null);
					}
					catch
					{
						return Enumerable.Empty<Type>();
					}
				});

			foreach (Type type in allTypes)
			{
				OverrideActionAttribute attr = type.GetCustomAttribute<OverrideActionAttribute>(false);
				if (attr?.BaseType != null)
				{
					_overrides[attr.BaseType] = type;
				}
			}
		}

		/// <summary>
		/// Returns the target-specific override type registered for the given action type, or null if none.
		/// </summary>
		public static Type GetOverrideType(Type actionType) =>
			actionType != null && _overrides.TryGetValue(actionType, out Type overrideType) ? overrideType : null;
	}
}
