using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.BuildSystem
{
	/// <summary>
	/// Builds and caches the <c>RefId → WeakGameEntity</c> map for the current scene, so that marked
	/// entities resolve in O(1) at runtime. Rebuilt lazily on first use and whenever the active scene
	/// changes (new mission / editor scene).
	/// </summary>
	public static class EntityMarkerIndex
	{
		private static Dictionary<string, WeakGameEntity> _byRefId;
		private static Scene _indexedScene;

		/// <summary>Resolves a marker RefId to its entity, or <see cref="WeakGameEntity.Invalid"/>.</summary>
		public static WeakGameEntity Resolve(string refId)
		{
			EnsureBuilt();
			if (string.IsNullOrEmpty(refId) || _byRefId == null) return WeakGameEntity.Invalid;
			return _byRefId.TryGetValue(refId, out WeakGameEntity weak) ? weak : WeakGameEntity.Invalid;
		}

		/// <summary>Rebuilds the index from the given scene.</summary>
		public static void Build(Scene scene)
		{
			_byRefId = new Dictionary<string, WeakGameEntity>();
			_indexedScene = scene;
			if (scene == null) return;

			List<GameEntity> entities = new List<GameEntity>();
			scene.GetEntities(ref entities);
			int count = 0;
			foreach (GameEntity e in entities)
			{
				foreach (AL_EntityMarker marker in e.GetScriptComponents<AL_EntityMarker>())
				{
					if (!string.IsNullOrEmpty(marker.RefId))
					{
						_byRefId[marker.RefId] = e.WeakEntity;
						count++;
					}
				}
			}
			Log($"[EntityMarkerIndex] Indexed {count} marked entities in scene '{scene.GetName()}'.", LogLevel.Debug);
		}

		public static void Clear()
		{
			_byRefId = null;
			_indexedScene = null;
		}

		private static void EnsureBuilt()
		{
			// Fall back to the editor scene in Modding Kit usage
			Scene current = Mission.Current?.Scene ?? MBEditor._editorScene;
			if (current == null) return;
			if (_indexedScene == current && _byRefId != null) return;
			Build(current);
		}
	}
}
