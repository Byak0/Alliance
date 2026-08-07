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
	/// Spawn a prefab entity (from the build catalog) at a precise <see cref="MatrixFrame"/> (position +
	/// rotation + scale), optionally capturing it into a local variable for the current <see cref="ScriptedEvent"/>.
	/// Spawning is server-authoritative and synced to clients through <see cref="BuildBehavior"/>.
	/// Can be followed with a <see cref="SetGlobalVariableAction"/> if entity needs to be registered globally.
	/// </summary>
	[Serializable]
	[PhrasePreview("Spawn {Prefab} {Frame} {?CaptureAs!=:({CaptureAs})}")]
	[PhraseTemplate("Spawn {Prefab} {Frame}{?CaptureAs!=: and capture as {CaptureAs}}")]
	public class SpawnEntityAction : ActionBase
	{
		[ConfigProperty(label: "Prefab", tooltip: "Prefab name to spawn (from the build catalog).", dataType: AllianceData.DataTypes.Prefab)]
		public string Prefab = "";

		[ConfigProperty(label: "Frame", tooltip: "Position, rotation and scale of the spawned entity.")]
		public ValueSource<MatrixFrame> Frame = new FrameLiteralValue();

		[ConfigProperty(label: "Capture as", tooltip: "Local variable name to store the spawned entity under for this ScriptedEvent. Leave empty to skip. Add a SetGlobalVariableAction afterwards to promote it to a global.")]
		[VariableOutput(typeof(WeakGameEntity))]
		public string CaptureAs = "";

		public SpawnEntityAction() { }

		public override ActionTask Execute(VariableStore context)
		{
			if (Mission.Current?.Scene == null) return ActionTask.CompletedTask;

			MatrixFrame frame = Frame?.Resolve(context) ?? MatrixFrame.Identity;

			BuildBehavior build = Mission.Current.GetMissionBehavior<BuildBehavior>();
			if (build == null) return ActionTask.CompletedTask;

			int idx = build.AllocateBuildIndex();
			if (idx == -1) return ActionTask.CompletedTask;

			GameEntity entity = build.BuildPrefab(idx, Prefab, frame);

			if (entity != null)
			{
				// Sync to clients
				build.BroadcastCreation(idx, Prefab, frame, entity);

				if (!string.IsNullOrWhiteSpace(CaptureAs))
				{
					context?.Set(CaptureAs, entity.WeakEntity);
				}
			}

			return ActionTask.CompletedTask;
		}
	}
}

