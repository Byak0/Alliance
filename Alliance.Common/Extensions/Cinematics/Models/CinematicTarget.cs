using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using System;
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

		[ConfigProperty(label: "Entity", tooltip: "Entity to target, resolved locally through its AL_EntityMarker.", category: "Target", dependency: "?Type=SpecificEntity")]
		public GameEntityRef Entity = new GameEntityRef();

		public CinematicTarget() { }

		public CinematicTarget(CinematicTargetType type) { Type = type; }
	}
}
