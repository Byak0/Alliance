using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.Story.Utilities
{
	/// <summary>
	/// Resolves scene entities by name for <see cref="Models.EntityAnchor"/>.
	/// Tries the current mission scene at runtime; returns <see cref="WeakGameEntity.Invalid"/> when
	/// unavailable (e.g. in the modding kit editor without a live mission, or unknown name).
	/// </summary>
	public static class ZoneEntityLookup
	{
		public static WeakGameEntity ByName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return WeakGameEntity.Invalid;

			Scene scene = Mission.Current?.Scene;
			GameEntity entity = scene?.GetFirstEntityWithName(name);
			if (entity == null)
			{
				Log($"[Zone] Entity '{name}' not found in the current scene.", LogLevel.Warning);
				return WeakGameEntity.Invalid;
			}
			return entity.WeakEntity;
		}
	}
}
