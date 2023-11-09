using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.CustomScripts.Handlers
{
    public class DestructibleWallHandler : IHandlerRegister
    {
        public DestructibleWallHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncObjectHitpoints>(CS_HandleSyncObjectHitpoints);
            reg.Register<SyncObjectDestructionLevel>(CS_HandleSyncObjectDestructionLevel);
            reg.Register<BurstAllHeavyHitParticles>(CS_HandleServerEventHitBurstAllHeavyHitParticles);
            reg.Register<SyncAbilityOfNavmesh>(CS_HandleSyncAbilityOfNavmesh);
            reg.Register<SyncSoundDestructible>(CS_HandleSyncSoundDestructible);
        }

        public void CS_HandleSyncObjectHitpoints(SyncObjectHitpoints message)
        {
            if (message.MissionObject != null)
            {
                message.MissionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().HitPoint = message.Hitpoints;
            }
        }

        public void CS_HandleSyncObjectDestructionLevel(SyncObjectDestructionLevel message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().SetDestructionLevel(message.DestructionLevel, message.ForcedIndex, message.BlowMagnitude, message.BlowPosition, message.BlowDirection, false);
        }

        public void CS_HandleServerEventHitBurstAllHeavyHitParticles(BurstAllHeavyHitParticles message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().BurstHeavyHitParticles();
        }

        public void CS_HandleSyncAbilityOfNavmesh(SyncAbilityOfNavmesh message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().SetAbilityOfNavmesh(message.Navmesh1, message.Navmesh2);
        }

        public void CS_HandleSyncSoundDestructible(SyncSoundDestructible message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().SyncSound();
        }
    }
}
