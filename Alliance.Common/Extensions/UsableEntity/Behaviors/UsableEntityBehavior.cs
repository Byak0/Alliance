using Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer;
using Alliance.Common.Extensions.UsableEntity.Utilities;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.UsableEntity.Behaviors
{
    /// <summary>
    /// MissionBehavior used to handle usable entities.
    /// </summary>
    public class UsableEntityBehavior : MissionNetwork, IMissionBehavior
    {
        private List<GameEntity> _usableEntities = new List<GameEntity>();

        readonly MultiplayerRoundController roundController;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            _usableEntities = Mission.Current.Scene.FindEntitiesWithTag(AllianceTags.InteractiveTag).ToList();
            MultiplayerRoundController roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();

            if (roundController != null)
            {
                roundController.OnRoundStarted += ResetItemsWithTagRespawnEachRound;
            }
        }

        public void UseEntity(GameEntity entity, Agent agent)
        {
            string itemName = entity.GetTagValue(AllianceTags.InteractiveItemTag);
            ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemName);
            MissionWeapon missionWeapon = new MissionWeapon(itemObject, null, agent.Team.Banner);
            EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot;
            while (!agent.Equipment[slot].IsEmpty && slot < EquipmentIndex.Weapon3)
            {
                slot++;
            }
            agent.EquipWeaponWithNewEntity(slot, ref missionWeapon);
            agent.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.WithAnimation, true);

            HideEntity(entity);

            Log($"Agent {agent.Name} ({agent.MissionPeer?.Name}) used entity {entity.Name} and equipped {itemName}", LogLevel.Debug);
        }

        public override void OnRemoveBehavior()
        {
            if (roundController != null)
            {
                roundController.OnRoundStarted -= ResetItemsWithTagRespawnEachRound;
            }
            base.OnRemoveBehavior();
        }

        public void HideEntity(GameEntity entity)
        {
            if (entity == null) return;

            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new RemoveEntity(entity.GlobalPosition));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            entity.SetVisibilityExcludeParents(false);
        }

        public void ResetItemsWithTagRespawnEachRound()
        {
            List<GameEntity> itemsToRespawnList = Mission.Current.Scene.FindEntitiesWithTag(AllianceTags.ENTITY_TO_RESPAWN_ON_EACH_ROUND_TAG).ToList();

            if (GameNetwork.IsServer)
            {
                // As client to make them visible again (Code above in the foreach)
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new Reset());
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            itemsToRespawnList.ForEach(gameEntity =>
            {
                if (!gameEntity.IsVisibleIncludeParents())
                {
                    gameEntity.SetVisibilityExcludeParents(true);
                }
            });
        }

        public GameEntity FindClosestUsableEntity(Vec3 position, float range)
        {
            foreach (GameEntity gameEntity in _usableEntities)
            {
                if (gameEntity.GlobalPosition.NearlyEquals(position, range))
                {
                    return gameEntity;
                }
            }
            return null;
        }

        public GameEntity FindEntityUsableByAgent(Agent agent)
        {
            Vec3 eyePosition = agent.GetEyeGlobalPosition();
            Vec3 lookDirection = agent.LookDirection;

            GameEntity closestEntity = null;
            float closestDistanceSquared = 2f;

            foreach (GameEntity entity in _usableEntities)
            {
                // Check if the entity is in the direction the agent is looking
                if (IsEntityInLookDirection(entity, eyePosition, lookDirection, out float distanceSquared) && entity.IsVisibleIncludeParents())
                {
                    if (distanceSquared < closestDistanceSquared)
                    {
                        closestDistanceSquared = distanceSquared;
                        closestEntity = entity;
                    }
                }
            }
            return closestEntity;
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