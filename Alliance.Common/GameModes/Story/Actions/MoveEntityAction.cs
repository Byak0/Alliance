using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Move a <see cref="WeakGameEntity"/> to a destination <see cref="MatrixFrame"/>. The move is synced
	/// to clients by <see cref="BuildBehavior.BroadcastMove"/>.
	/// </summary>
	[Serializable]
	[PhrasePreview("Move {Entity} to {Destination}")]
	[PhraseTemplate("Move {Entity} to {Destination}")]
	public class MoveEntityAction : ActionBase
	{
		[ConfigProperty(label: "Entity", tooltip: "Entity to move.")]
		public ValueSource<WeakGameEntity> Entity = new VariableValue<WeakGameEntity>();

		[ConfigProperty(label: "Destination", tooltip: "New position, rotation and scale of entity.")]
		public ValueSource<MatrixFrame> Destination = new FrameLiteralValue();

		public MoveEntityAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			WeakGameEntity e = Entity?.Resolve(context) ?? WeakGameEntity.Invalid;
			if (!e.IsValid) return ActionTask.CompletedTask;

			MatrixFrame? dest = Destination?.Resolve(context);
			if (dest == null) return ActionTask.CompletedTask;

			e.SetGlobalFrame(dest.Value);
			e.SetFrameChanged();

			Mission.Current?.GetMissionBehavior<BuildBehavior>()?.BroadcastMove(e, dest.Value);
			return ActionTask.CompletedTask;
		}
	}
}
