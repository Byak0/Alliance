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
	/// rotation + scale), optionally capturing it into a local variable for the current
	/// <see cref="ScriptedEvent"/>. Spawning is server-authoritative and synced to clients through
	/// <see cref="BuildBehavior"/>. Promote the capture to a global with a follow-up
	/// <see cref="SetGlobalVariableAction"/> if it must outlive the action pipeline.
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
			if (!GameNetwork.IsServer) return ActionTask.CompletedTask;
			if (Mission.Current?.Scene == null) return ActionTask.CompletedTask;

			MatrixFrame frame = Frame?.Resolve(context) ?? MatrixFrame.Identity;

			BuildBehavior build = Mission.Current.GetMissionBehavior<BuildBehavior>();
			if (build == null) return ActionTask.CompletedTask;

			int idx = build.AllocateBuildIndex();
			GameEntity entity = build.BuildPrefab(idx, Prefab, frame);

			if (entity != null && !string.IsNullOrWhiteSpace(CaptureAs))
			{
				context?.Set(CaptureAs, entity.WeakEntity);
			}

			return ActionTask.CompletedTask;
		}
	}
}

