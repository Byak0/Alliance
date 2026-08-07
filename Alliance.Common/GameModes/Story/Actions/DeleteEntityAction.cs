using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Delete a <see cref="WeakGameEntity"/>. Prefers the synced path through <see cref="BuildBehavior"/>
	/// for build-tracked entities (broadcasts <c>SyncPrefabRemoval</c>); otherwise removes the entity
	/// directly. Operates through the weak handle for both paths.
	/// </summary>
	[Serializable]
	[PhrasePreview("Delete {Entity}")]
	[PhraseTemplate("Delete {Entity}")]
	public class DeleteEntityAction : ActionBase
	{
		[ConfigProperty(label: "Entity", tooltip: "Entity to delete.")]
		public ValueSource<WeakGameEntity> Entity = new VariableValue<WeakGameEntity>();

		public DeleteEntityAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			WeakGameEntity e = Entity?.Resolve(context) ?? WeakGameEntity.Invalid;
			if (!e.IsValid) return ActionTask.CompletedTask;

			BuildBehavior build = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			int? idx = FindBuildIndex(build, e);
			if (idx.HasValue)
			{
				build.RemovePrefab(idx.Value);
				return ActionTask.CompletedTask;
			}

			// Not build-tracked: remove directly (same sequence as BuildBehavior.RemovePrefab).
			e.SetVisibilityExcludeParents(false);
			e.RemoveAllChildren();
			e.Remove(0);

			return ActionTask.CompletedTask;
		}

		private static int? FindBuildIndex(BuildBehavior build, WeakGameEntity target)
		{
			if (build == null || !target.IsValid) return null;
			foreach (var kvp in build.BuiltEntities)
			{
				if (kvp.Value.Entity != null && kvp.Value.Entity.WeakEntity == target)
				{
					return kvp.Key;
				}
			}
			return null;
		}
	}
}
