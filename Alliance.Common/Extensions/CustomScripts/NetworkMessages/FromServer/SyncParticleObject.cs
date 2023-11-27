using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncParticleObject : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public int EmitterIndex { get; private set; }
        public int ParticleIndex { get; private set; }

        public SyncParticleObject(MissionObjectId missionObjectId, int emitterIndex, int particleIndex)
        {
            MissionObjectId = missionObjectId;
            EmitterIndex = emitterIndex;
            ParticleIndex = particleIndex;
        }

        public SyncParticleObject()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            EmitterIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref bufferReadValid);
            ParticleIndex = ReadIntFromPacket(new CompressionInfo.Integer(0, 4, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
            WriteIntToPacket(EmitterIndex, new CompressionInfo.Integer(0, 100, true));
            WriteIntToPacket(ParticleIndex, new CompressionInfo.Integer(0, 4, true));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize particle ", ParticleIndex, " on emitter ", EmitterIndex, " with Id: ", MissionObjectId.Id);
        }
    }
}