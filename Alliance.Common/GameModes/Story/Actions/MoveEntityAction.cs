using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace Alliance.Common.GameModes.Story.Actions
{
	/// <summary>
	/// Move a <see cref="WeakGameEntity"/> to a zone (its center), optionally applying a heading rotation.
	/// Moves the entity through its <see cref="WeakGameEntity"/> handle directly. Networking the move for
	/// MissionObject-backed entities is a follow-up (see plan §9.3).
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

			return ActionTask.CompletedTask;
		}
	}
}
