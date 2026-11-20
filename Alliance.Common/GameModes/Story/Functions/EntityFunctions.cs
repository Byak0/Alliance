using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.BuildSystem;
using Alliance.Common.Extensions.BuildSystem.Behaviors;
using Alliance.Common.GameModes.Story.Attributes;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Functions
{
	[Serializable]
	[PhrasePreview("marked entity {RefId}")]
	[PhraseTemplate("marked entity {RefId}")]
	public class GetMarkedEntityFunction : Function
	{
		public override Type ReturnType => typeof(WeakGameEntity);

		[ConfigProperty(label: "Ref Id", tooltip: "GUID of the entity's AL_EntityMarker script.")]
		public ValueSource<string> RefId = new LiteralValue<string>("");

		public override object Evaluate(VariableStore context)
			=> EntityMarkerIndex.Resolve(RefId?.Resolve(context));
	}

	[Serializable]
	[PhrasePreview("entity named {Name}")]
	[PhraseTemplate("entity named {Name}")]
	public class GetEntityByNameFunction : Function
	{
		public override Type ReturnType => typeof(WeakGameEntity);

		[ConfigProperty(label: "Name", tooltip: "Scene entity name (as set in the modding kit).")]
		public ValueSource<string> Name = new LiteralValue<string>("");

		public override object Evaluate(VariableStore context)
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null) return WeakGameEntity.Invalid;
			GameEntity entity = scene.GetFirstEntityWithName(Name?.Resolve(context) ?? "");
			return EntityQuery.IsIdentifiable(entity)
				? entity.WeakEntity : WeakGameEntity.Invalid;
		}
	}

	[Serializable]
	[PhrasePreview("entities tagged {Tag}")]
	[PhraseTemplate("entities tagged {Tag}")]
	public class GetEntitiesByTagFunction : Function
	{
		public override Type ReturnType => typeof(List<WeakGameEntity>);

		[ConfigProperty(label: "Tag", tooltip: "Entities carrying this tag are returned.")]
		public ValueSource<string> Tag = new LiteralValue<string>("");

		public override object Evaluate(VariableStore context)
		{
			List<WeakGameEntity> result = new List<WeakGameEntity>();
			Scene scene = Mission.Current?.Scene;
			if (scene == null) return result;
			string tag = Tag?.Resolve(context);
			if (string.IsNullOrEmpty(tag)) return result;

			List<GameEntity> entities = new List<GameEntity>();
			scene.GetEntities(ref entities);
			foreach (GameEntity entity in entities)
			{
				if (!EntityQuery.IsIdentifiable(entity)) continue;
				if (entity.HasTag(tag)) result.Add(entity.WeakEntity);
			}
			return result;
		}
	}

	[Serializable]
	[PhrasePreview("entities in {Zone}")]
	[PhraseTemplate("entities in {Zone}")]
	public class EntitiesInZoneFunction : Function
	{
		public override Type ReturnType => typeof(List<WeakGameEntity>);

		[ConfigProperty(label: "Zone", tooltip: "Entities whose position falls inside this zone are returned.")]
		public ValueSource<Zone> Zone = new LiteralValue<Zone>(new Zone());

		public override object Evaluate(VariableStore context)
		{
			List<WeakGameEntity> result = new List<WeakGameEntity>();
			Scene scene = Mission.Current?.Scene;
			if (scene == null) return result;
			Zone zone = Zone?.Resolve(context);
			if (zone == null) return result;

			List<GameEntity> entities = new List<GameEntity>();
			scene.GetEntities(ref entities);
			foreach (GameEntity entity in entities)
			{
				if (!EntityQuery.IsIdentifiable(entity)) continue;
				if (zone.Contains(entity.GetGlobalFrame().origin, context)) result.Add(entity.WeakEntity);
			}
			return result;
		}
	}

	[Serializable]
	[PhrasePreview("nearest entity to {Agent}")]
	[PhraseTemplate("nearest entity to {Agent}")]
	public class NearestEntityToAgentFunction : Function
	{
		public override Type ReturnType => typeof(WeakGameEntity);

		[ConfigProperty(label: "Agent", tooltip: "Distance is measured from this agent.")]
		public ValueSource<Agent> Agent = new VariableValue<Agent>();

		public override object Evaluate(VariableStore context)
		{
			Scene scene = Mission.Current?.Scene;
			if (scene == null) return WeakGameEntity.Invalid;
			Agent agent = Agent?.Resolve(context);
			if (agent == null) return WeakGameEntity.Invalid;

			List<GameEntity> entities = new List<GameEntity>();
			scene.GetEntities(ref entities);
			WeakGameEntity best = WeakGameEntity.Invalid;
			float bestDist = float.MaxValue;
			foreach (GameEntity entity in entities)
			{
				if (!EntityQuery.IsIdentifiable(entity)) continue;
				float d = entity.GetGlobalFrame().origin.DistanceSquared(agent.Position);
				if (d < bestDist)
				{
					bestDist = d;
					best = entity.WeakEntity;
				}
			}
			return best;
		}
	}

	[Serializable]
	[PhrasePreview("first of {Entities}")]
	[PhraseTemplate("first entity of {Entities}")]
	public class FirstEntityOfFunction : Function
	{
		public override Type ReturnType => typeof(WeakGameEntity);

		[ConfigProperty(label: "Entities", tooltip: "Source list of entities (e.g. result of an 'entities in zone' function).")]
		public ValueSource<List<WeakGameEntity>> Entities = new VariableValue<List<WeakGameEntity>>();

		public override object Evaluate(VariableStore context)
		{
			List<WeakGameEntity> entities = Entities?.Resolve(context);
			return (entities != null && entities.Count > 0) ? (object)entities[0] : WeakGameEntity.Invalid;
		}
	}

	/// <summary>
	/// Shared helpers for entity query functions. An entity is "identifiable" (queryable and reliably syncable across server/clients)
	/// if it carries an <see cref="AL_EntityMarker"/> or is tracked by <see cref="BuildBehavior"/> (a runtime-spawned prefab).
	/// </summary>
	internal static class EntityQuery
	{
		public static bool IsIdentifiable(GameEntity entity)
		{
			if (entity == null) return false;
			if (entity.HasScriptOfType<AL_EntityMarker>()) return true;
			BuildBehavior build = Mission.Current?.GetMissionBehavior<BuildBehavior>();
			return build != null && build.IsTracked(entity);
		}
	}
}
