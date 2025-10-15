using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.UsableEntity.Handlers;
using Alliance.Common.Extensions.UsableEntity.Interfaces;
using Alliance.Common.Extensions.UsableEntity.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.UsableEntity.Behaviors
{
	/// <summary>
	/// MissionBehavior used to handle usable entities.
	/// </summary>
	public class UsableEntityBehavior : MissionNetwork, IMissionBehavior
	{
		public readonly struct InteractionTarget
		{
			public readonly Guid ID;
			public readonly GameEntity Entity;
			public readonly IInteractionHandler Handler;
			public InteractionTarget(Guid id, GameEntity e, IInteractionHandler h) { ID = id; Entity = e; Handler = h; }
			public bool IsValid => Entity != null && Handler != null;
		}

		public sealed class InteractionRegistry
		{
			private readonly List<IInteractionHandler> _handlers = new();
			public void Register(IInteractionHandler handler) => _handlers.Add(handler);
			public IEnumerable<IInteractionHandler> All => _handlers;
			public IInteractionHandler FindMatch(GameEntity e) => _handlers.FirstOrDefault(h => h.CanHandle(e));
		}

		private InteractionRegistry _registry;
		private List<InteractionTarget> _usableEntities = new List<InteractionTarget>();
		private Dictionary<Guid, InteractionTarget> _usableEntitiesById = new Dictionary<Guid, InteractionTarget>();
		private List<InteractionTarget> _hiddenEntities = new List<InteractionTarget>();
		private readonly MultiplayerRoundController _roundController;

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();

			_registry = new InteractionRegistry();

			_registry.Register(new PickUpItemHandler());
			_registry.Register(new EditableTextHandler());

			List<GameEntity> entitiesInScene = new();
			Mission.Current.Scene.GetEntities(ref entitiesInScene);
			foreach (GameEntity entity in entitiesInScene)
			{
				IInteractionHandler handler = _registry.FindMatch(entity);
				if (handler != null)
				{
					// Generate a unique ID for the entity
					Guid id = entity.GetDeterministicID();
					InteractionTarget target = new InteractionTarget(id, entity, handler);
					_usableEntities.Add(target);
					_usableEntitiesById[id] = target;
				}
			}

			MultiplayerRoundController roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();

			if (roundController != null)
			{
				roundController.OnRoundStarted += ResetItemsWithTagRespawnEachRound;
			}
		}

		protected override void HandleLateNewClientAfterSynchronized(NetworkCommunicator networkPeer)
		{
			// When a new player connects, send him the hidden entities
			foreach (InteractionTarget entity in _hiddenEntities)
			{
				UsableEntityMsg.SyncHideEntity(entity, networkPeer);
			}
		}

		public override void OnRemoveBehavior()
		{
			if (_roundController != null)
			{
				_roundController.OnRoundStarted -= ResetItemsWithTagRespawnEachRound;
			}
			base.OnRemoveBehavior();
		}

		public void InteractWithEntity(Guid entityId, Agent agent)
		{
			if (_usableEntitiesById.TryGetValue(entityId, out InteractionTarget target) && target.IsValid)
			{
				target.Handler.Interact(agent, target);
			}
		}

		public void InteractWithTextPanel(NetworkCommunicator peer, Guid id, string text)
		{
			if (_usableEntitiesById.TryGetValue(id, out InteractionTarget target) && target.IsValid)
			{
				CS_TextPanel textPanel = target.Entity.GetFirstScriptOfType<CS_TextPanel>();
				if (textPanel == null || !textPanel.IsEditable) return;
				textPanel.UpdateText(text);
				if (GameNetwork.IsServer)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SyncTextPanel(textPanel.Id, text));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
			}
		}

		public void HideEntity(Guid entityID)
		{
			if (!_usableEntitiesById.TryGetValue(entityID, out InteractionTarget target)) return;

			if (GameNetwork.IsServer)
			{
				UsableEntityMsg.SyncHideEntity(target);
			}

			target.Entity.SetVisibilityExcludeParents(false);
			_hiddenEntities.Add(target);
		}

		public void ResetItemsWithTagRespawnEachRound()
		{
			List<GameEntity> itemsToRespawnList = Mission.Current.Scene.FindEntitiesWithTag(AllianceTags.ENTITY_TO_RESPAWN_ON_EACH_ROUND_TAG).ToList();

			if (GameNetwork.IsServer)
			{
				// Ask client to make them visible again (Code above in the foreach)
				UsableEntityMsg.SyncResetEntityVisibility();
			}

			itemsToRespawnList.ForEach(gameEntity =>
			{
				if (!gameEntity.IsVisibleIncludeParents())
				{
					gameEntity.SetVisibilityExcludeParents(true);
				}
			});

			// Remove them from the hidden entities list
			_hiddenEntities.RemoveAll(hiddenEntity => itemsToRespawnList.Contains(hiddenEntity.Entity));
		}

		public InteractionTarget? FindEntityUsableByAgent(Agent agent)
		{
			Vec3 eyePosition = agent.GetEyeGlobalPosition();
			Vec3 lookDirection = agent.LookDirection;
			float lookLenSq = lookDirection.LengthSquared;
			lookDirection /= (float)Math.Sqrt(lookLenSq);

			InteractionTarget? closestTarget = null;
			float closestDistanceSquared = 2f;

			foreach (InteractionTarget target in _usableEntities)
			{
				// Check if the entity is in the direction the agent is looking
				if (target.Handler.CanInteract(agent, target.Entity)
					&& IsEntityInLookDirection(target.Entity, eyePosition, lookDirection, out float distanceSquared)
					&& target.Entity.IsVisibleIncludeParents())
				{
					if (distanceSquared < closestDistanceSquared)
					{
						closestDistanceSquared = distanceSquared;
						closestTarget = target;
					}
				}
			}
			return closestTarget;
		}

		private bool IsEntityInLookDirection(GameEntity entity, Vec3 eyePosition, Vec3 lookDirection, out float distanceSquared)
		{
			MatrixFrame globalFrame = entity.GetGlobalFrame();
			Vec3 entityPosition = globalFrame.origin;

			// Calculate the squared distance from the eye position to the entity
			distanceSquared = (entityPosition - eyePosition).LengthSquared;

			// Check if the entity is in the direction the agent is looking
			return Vec3.DotProduct(entityPosition - eyePosition, lookDirection) > 0;
		}
	}
}