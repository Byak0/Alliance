using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncParticleObject : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public int EmitterIndex { get; private set; }
        public int ParticleIndex { get; private set; }

        public SyncParticleObject(MissionObject missionObject, int emitterIndex, int particleIndex)
        {
            MissionObject = missionObject;
            EmitterIndex = emitterIndex;
            ParticleIndex = particleIndex;
        }

        public SyncParticleObject()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            EmitterIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref bufferReadValid);
            ParticleIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, 4, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
            WriteIntToPacket(EmitterIndex, new CompressionInfo.Integer(0, 100, true));
            WriteIntToPacket(ParticleIndex, new CompressionInfo.Integer(0, 4, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize particle ", ParticleIndex, " on emitter ", EmitterIndex, " with Id: ", MissionObject.Id, " and name: ", MissionObject.GameEntity.Name);
        }
    }
}