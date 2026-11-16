using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Cinematics.Models
{
	/// <summary>What a target resolves to at playback time. Viewer* targets resolve per receiver;
	/// Specific* targets resolve to the same object for everyone.</summary>
	public enum CinematicTargetType
	{
		None,
		Position,
		ViewerAgent,
		ViewerCamera,
		SpecificAgent,
		SpecificEntity
	}

	/// <summary>
	/// Unified runtime target used by camera keyframes and look-at overrides. 
	/// A target is either a specific agent/entity (same for everyone),
	/// or the receiving player's own agent/camera (different for each player).
	/// </summary>
	[Serializable]
	public class CinematicTarget
	{
		[ConfigProperty(label: "Target", tooltip: "What this resolves to at playback time. Viewer targets resolve to each receiver's own agent/camera; Specific targets are the same for everyone.")]
		public CinematicTargetType Type = CinematicTargetType.None;

		[ConfigProperty(label: "Position", tooltip: "World position used when Type = Position.", category: "Target", dependency: "?Type=Position")]
		public FrameValue Position = new FrameValue();

		[ConfigProperty(label: "Agent variable", tooltip: "Variable holding the Agent to target. Resolved by the server when the cinematic starts.", category: "Target", dependency: "?Type=SpecificAgent")]
		[SyncToClient]
		public ValueSource<Agent> AgentVariable = new VariableValue<Agent>();

		[ConfigProperty(label: "Entity", tooltip: "Entity to target: a scene entity (literal, picked visually) or a variable holding one.", category: "Target", dependency: "?Type=SpecificEntity")]
		[SyncToClient]
		public ValueSource<WeakGameEntity> Entity = new SceneEntityLiteralValue();

		public CinematicTarget() { }

		public CinematicTarget(CinematicTargetType type) { Type = type; }

		/// <summary>Resolves this target to a world frame for authoritative/editor-side execution:
		/// Position, SpecificEntity and SpecificAgent resolve from data; Viewer targets return null
		/// (per-receiver concepts cannot be executed server-side).</summary>
		public MatrixFrame? ResolveWorldFrame(VariableStore context)
		{
			switch (Type)
			{
				case CinematicTargetType.Position:
					return Position.ToFrame();
				case CinematicTargetType.SpecificEntity:
				{
					WeakGameEntity entity = Entity?.Resolve(context) ?? WeakGameEntity.Invalid;
					return entity.IsValid ? entity.GetGlobalFrame() : (MatrixFrame?)null;
				}
				case CinematicTargetType.SpecificAgent:
				{
					Agent agent = AgentVariable?.Resolve(context);
					return agent != null ? new MatrixFrame(agent.Frame.rotation, agent.Position) : (MatrixFrame?)null;
				}
				default:
					return null;
			}
		}
	}
}
