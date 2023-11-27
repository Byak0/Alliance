using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class BurstAllHeavyHitParticles : GameNetworkMessage
    {
        public MissionObjectId MissionObjectId { get; private set; }

        public BurstAllHeavyHitParticles(MissionObjectId missionObjectId)
        {
            MissionObjectId = missionObjectId;
        }

        public BurstAllHeavyHitParticles()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectIdToPacket(MissionObjectId);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Bursting all heavy-hit particles for the DestructableComponent of MissionObject with Id: ", MissionObjectId);
        }
    }
}