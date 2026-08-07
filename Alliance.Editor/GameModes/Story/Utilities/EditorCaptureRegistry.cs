using Alliance.Common.GameModes.Story.Actions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Alliance.Editor.GameModes.Story.Utilities
{
	/// <summary>
	/// Editor-side resolution of captured entity variables to the prefab that produces them. Built once
	/// from the enclosing scope (ScriptedEvent/Act) when a frame-hosting action is edited, so a
	/// <see cref="MoveEntityAction"/> referencing a captured variable can preview the entity that a
	/// <see cref="SpawnEntityAction"/> will produce.
	/// </summary>
	public static class EditorCaptureRegistry
	{
		private static readonly Dictionary<string, string> _captureToPrefab = new(StringComparer.Ordinal);

		/// <summary>Scans scope (a ScriptedEvent/Act) for every SpawnEntityAction and
		/// records its <c>CaptureAs</c> → <c>Prefab</c>. Replaces any previous contents.</summary>
		public static void Build(object scope)
		{
			_captureToPrefab.Clear();
			if (scope != null) Collect(scope, new HashSet<object>());
		}

		/// <summary>Returns the prefab name that produces the given capture, or null when unknown.</summary>
		public static string ResolvePrefab(string captureName)
			=> (captureName != null && _captureToPrefab.TryGetValue(captureName, out string p)) ? p : null;

		public static void Clear() => _captureToPrefab.Clear();

		private static void Collect(object obj, HashSet<object> visited)
		{
			if (obj == null || !visited.Add(obj)) return;
			if (obj is SpawnEntityAction sea && !string.IsNullOrWhiteSpace(sea.CaptureAs))
			{
				_captureToPrefab[sea.CaptureAs] = sea.Prefab;
			}

			Type t = obj.GetType();
			if (t.IsPrimitive || t == typeof(string) || t.IsEnum || t.IsValueType) return;

			if (obj is IList list)
			{
				foreach (var item in list) Collect(item, visited);
				return;
			}

			foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public))
			{
				if (f.FieldType.IsPrimitive || f.FieldType == typeof(string) || f.FieldType.IsEnum || f.FieldType.IsValueType) continue;
				Collect(f.GetValue(obj), visited);
			}
		}
	}
}
