using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages;
using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Patch;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Base class for Actions that can be performed during a scenario.
	/// <para>Actions are always executed on the server first, where the context is resolved.
	/// A server action can then call ExecuteActionOnClient to execute a corresponding client action, passing along any relevant field marked with [SyncToClient].</para>
	/// Target-specific overrides (Client_/Server_) are resolved at load time through the ActionOverrideRegistry.
	/// </summary>
	[Serializable]
	public abstract class ActionBase
	{
		// ── Scope identity ────────────────────────────────────────────────
		//
		// Each action source (an Act or an AL_TriggerAction entity) occupies its
		// own ID space.  The runtime lookup key is (ScopeId, ActionId):
		//
		//   Act scope:    actIndex
		//   Entity scope: MakeEntityScopeId(MissionObject.Id)
		//
		// Both ScopeId and ActionId are assigned at registration time — no XML
		// persistence needed.  Determinism comes from the scope being derived
		// from deterministic data (act index or MissionObject.Id) and ActionId
		// being assigned by a deterministic graph walk within that scope.

		public const int EntityScopeBase = DirtyCommonPatcher.MAX_MISSION_OBJECTS + 1;
		private const int SceneEntityRange = DirtyCommonPatcher.MAX_MISSION_OBJECTS + 1;

		public static int MakeEntityScopeId(MissionObjectId id) =>
			EntityScopeBase + (id.CreatedAtRuntime ? SceneEntityRange : 0) + id.Id;

		[XmlIgnore]
		public int ScopeId { get; private set; }
		[XmlIgnore]
		public int ActionId { get; private set; }

		private static readonly Dictionary<(int scopeId, int actionId), ActionBase> _allActions = new();

		public static ActionBase FindById(int scopeId, int actionId) =>
			_allActions.TryGetValue((scopeId, actionId), out var action) ? action : null;

		public static void ClearRegistry() => _allActions.Clear();

		/// <summary>
		/// Removes all entries belonging to the given scope. Called automatically
		/// by <see cref="AssignActionIds"/> to prevent stale entries when a scope
		/// is re-registered with fewer actions.
		/// </summary>
		public static void ClearScope(int scopeId)
		{
			var toRemove = _allActions.Keys.Where(k => k.scopeId == scopeId).ToList();
			foreach (var key in toRemove)
				_allActions.Remove(key);
		}

		/// <summary>
		/// Walks the graph, assigns sequential ActionIds (1, 2, 3…) within the
		/// given scope, and registers each action in the lookup table.
		/// Clears any previous entries for this scope first.
		/// Field order is deterministic via metadata-token ordering, so server
		/// and client produce matching (scopeId, actionId) pairs.
		/// </summary>
		public static void AssignActionIds(object root, int scopeId)
		{
			if (root == null) return;
			ClearScope(scopeId);
			int nextId = 1;
			var visited = new HashSet<object>();
			AssignActionIdsRecursive(root, scopeId, visited, ref nextId);
		}

		private static void AssignActionIdsRecursive(object obj, int scopeId, HashSet<object> visited, ref int nextId)
		{
			if (obj == null || !visited.Add(obj)) return;

			if (obj is ActionBase action)
			{
				action.ScopeId = scopeId;
				action.ActionId = nextId++;
				_allActions[(scopeId, action.ActionId)] = action;
			}

			foreach (var child in GetChildren(obj))
				AssignActionIdsRecursive(child, scopeId, visited, ref nextId);
		}

		private static IEnumerable<object> GetChildren(object obj)
		{
			if (obj is IEnumerable enumerable && obj is not string)
			{
				foreach (var item in enumerable)
					yield return item;
			}

			FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach (var field in fields.OrderBy(f => f.MetadataToken))
			{
				yield return field.GetValue(obj);
			}
		}

		public virtual ActionTask Execute(VariableStore context) => ActionTask.CompletedTask;

		public virtual void ExecuteClient() { }

		public virtual void Register(WeakGameEntity entity)
		{
			RegisterZones(entity);
		}

		public virtual void UnassignActionId()
		{
			_allActions.Remove((ScopeId, ActionId));
		}

		protected void RegisterZones(WeakGameEntity entity)
		{
			ZoneRegistrar.RegisterAll(this, entity);
		}

		// ── Sync infrastructure ──────────────────────────────────────────

		[Serializable]
		public struct SyncFieldInfo
		{
			public string Name;
			public FieldInfo Field;
		}

		private static readonly Dictionary<Type, SyncFieldInfo[]> _syncFieldsCache = new();

		public SyncFieldInfo[] GetSyncFields()
		{
			Type type = GetType();
			if (_syncFieldsCache.TryGetValue(type, out var fields)) return fields;

			fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
				.Where(f => f.GetCustomAttribute<SyncToClientAttribute>() != null
						&& typeof(ValueSource).IsAssignableFrom(f.FieldType))
				.Select(f => new SyncFieldInfo { Name = f.Name, Field = f })
				.ToArray();

			_syncFieldsCache[type] = fields;
			return fields;
		}

		public bool HasSyncToClientFields => GetSyncFields().Length > 0;

		/// <summary>
		/// Packs resolved [SyncToClient] field values into a VariableStore and sends
		/// them to all clients. Call explicitly from <see cref="Execute"/> to opt in.
		/// </summary>
		protected void ExecuteOnClient(VariableStore context = null)
		{
			if (!GameNetwork.IsServer) return;

			var data = new VariableStore();
			VariableStore globals = ScenarioManager.Instance?.Globals;

			foreach (var sf in GetSyncFields())
			{
				var vs = (ValueSource)sf.Field.GetValue(this);
				if (vs == null) continue;
				data.Set(sf.Name, vs.ResolveObject(context, globals));
			}

			StoryMessages.SendExecuteAction(ScopeId, ActionId, data);
		}

		/// <summary>
		/// Called on the client when an <see cref="ExecuteActionMessage"/> arrives.
		/// Replaces each synced field with a <see cref="LiteralValue{T}"/> holding the
		/// received value. Fields not present in <paramref name="data"/> are left untouched.
		/// </summary>
		public void InjectSyncData(VariableStore data)
		{
			foreach (var sf in GetSyncFields())
			{
				if (!data.Has(sf.Name)) continue;

				Type fieldType = sf.Field.FieldType;
				if (!fieldType.IsGenericType) continue;

				Type valueType = fieldType.GetGenericArguments()[0];
				Type literalType = typeof(LiteralValue<>).MakeGenericType(valueType);

				object literal = sf.Field.GetValue(this);
				if (literal == null || literal.GetType() != literalType)
				{
					literal = Activator.CreateInstance(literalType);
					sf.Field.SetValue(this, literal);
				}

				object value = data.Get(sf.Name);
				literalType.GetField("Value").SetValue(literal, value);
			}
		}
	}
}
