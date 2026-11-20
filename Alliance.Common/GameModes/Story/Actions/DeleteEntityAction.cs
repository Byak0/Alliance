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
	/// Delete a <see cref="WeakGameEntity"/>. Sync is handled by <see cref="BuildBehavior.BroadcastDelete"/>
	/// (BuildIndex for spawned entities, AL_EntityMarker RefId for marked scene entities).
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

			// Sync while the entity still exists
			build?.BroadcastDelete(e);

			if (build != null && build.TryGetBuildIndex(e, out int buildIndex))
			{
				build.RemovePrefab(buildIndex);
			}
			else
			{
				e.SetVisibilityExcludeParents(false);
				e.RemoveAllChildren();
				e.Remove(0);
			}

			return ActionTask.CompletedTask;
		}
	}
}
