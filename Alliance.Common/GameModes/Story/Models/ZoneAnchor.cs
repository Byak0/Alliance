using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Xml.Serialization;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Models
{
	/// <summary>
	/// Reference frame of a <see cref="Zone"/>: what its <c>Position</c> is relative to.
	/// Replaces the former <c>UseLocalSpace</c> boolean with an explicit, extensible choice.
	/// The editor lists concrete subclasses by their [PhrasePreview] label so the choice is obvious.
	/// </summary>
	[Serializable]
	[XmlInclude(typeof(WorldAnchor))]
	[XmlInclude(typeof(HostEntityAnchor))]
	[XmlInclude(typeof(EntityAnchor))]
	[XmlInclude(typeof(AgentAnchor))]
	public abstract class ZoneAnchor
	{
		/// <summary>Resolves the zone's world-space center from its local position.</summary>
		public abstract Vec3 Resolve(Vec3 localPos, WeakGameEntity host, VariableStore context);

		/// <summary>Inverse of <see cref="Resolve"/>: world point → local offset (for click placement in editor).</summary>
		public virtual Vec3 WorldToLocal(Vec3 worldPos, WeakGameEntity host) => worldPos;

		/// <summary>Called once when the zone is registered; entity anchors resolve/cache their target here.</summary>
		public virtual void OnRegister(WeakGameEntity host) { }
	}

	/// <summary>Absolute position in the scene.</summary>
	[Serializable]
	[PhrasePreview("")]
	public class WorldAnchor : ZoneAnchor
	{
		public override Vec3 Resolve(Vec3 localPos, WeakGameEntity host, VariableStore context) => localPos;
	}

	/// <summary>
	/// Relative to the entity hosting the enclosing ScriptedEvent (the map entity of an AL_TriggerAction).
	/// This is the direct, clean replacement of the former <c>UseLocalSpace = true</c>.
	/// For scenario ScriptedEvents the host is Invalid → behaves like <see cref="WorldAnchor"/>.
	/// </summary>
	[Serializable]
	[PhrasePreview("relative to host entity")]
	public class HostEntityAnchor : ZoneAnchor
	{
		public override Vec3 Resolve(Vec3 localPos, WeakGameEntity host, VariableStore context)
			=> host.IsValid ? host.GlobalPosition + localPos : localPos;

		public override Vec3 WorldToLocal(Vec3 worldPos, WeakGameEntity host)
			=> host.IsValid ? worldPos - host.GlobalPosition : worldPos;
	}

	/// <summary>Relative to a scene entity identified by its Name.</summary>
	[Serializable]
	[PhrasePreview("relative to entity '{EntityName}'")]
	public class EntityAnchor : ZoneAnchor
	{
		[ConfigProperty(label: "Entity name", tooltip: "Name of the scene entity to follow (as set in the modding kit).")]
		public string EntityName = "";

		[XmlIgnore]
		private WeakGameEntity _cached;

		public override void OnRegister(WeakGameEntity host) => _cached = ZoneEntityLookup.ByName(EntityName);

		public override Vec3 Resolve(Vec3 localPos, WeakGameEntity host, VariableStore context)
		{
			if (!_cached.IsValid) _cached = ZoneEntityLookup.ByName(EntityName);
			return _cached.IsValid ? _cached.GlobalPosition + localPos : localPos;
		}

		public override Vec3 WorldToLocal(Vec3 worldPos, WeakGameEntity host)
		{
			WeakGameEntity e = _cached.IsValid ? _cached : ZoneEntityLookup.ByName(EntityName);
			return e.IsValid ? worldPos - e.GlobalPosition : worldPos;
		}
	}

	/// <summary>Relative to an agent expression. The agent is resolved lazily at each call.</summary>
	[Serializable]
	[PhrasePreview("relative to agent {Agent}")]
	public class AgentAnchor : ZoneAnchor
	{
		[ConfigProperty(label: "Agent", tooltip: "Agent to follow: a variable or a function such as nearest agent.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public override Vec3 Resolve(Vec3 localPos, WeakGameEntity host, VariableStore context)
		{
			Agent agent = Agent?.Resolve(context);
			return agent != null ? agent.Position + localPos : localPos;
		}
	}
}
