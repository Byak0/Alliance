using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.CustomScripts.Handlers
{
    public class UsableObjectHandler : IHandlerRegister
    {
        public UsableObjectHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<SyncSetActionChannel>(CS_HandleSyncSetActionChannel);
            reg.Register<SyncNumberOfUse>(CS_HandleSyncNumberOfUse);
            reg.Register<SyncSoundObject>(CS_HandleSyncSound);
            reg.Register<SyncParticleObject>(CS_HandleSyncParticle);
        }

        public void CS_HandleSyncSetActionChannel(SyncSetActionChannel message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_UsableObject>().SyncSetActionChannel(message.UserAgent, message.Action);
        }

        public void CS_HandleSyncNumberOfUse(SyncNumberOfUse message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_UsableObject>().NumberOfUse = message.NumberOfUse;
        }

        public void CS_HandleSyncSound(SyncSoundObject message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_UsableObject>().SyncSound(message.UserAgent);
        }

        public void CS_HandleSyncParticle(SyncParticleObject message)
        {
            MissionObject missionObject = message.MissionObject;
            if (missionObject == null)
            {
                return;
            }
            missionObject.GameEntity.GetFirstScriptOfType<CS_PartyHard>().SyncParticle(message.EmitterIndex, message.ParticleIndex);
        }
    }
}
