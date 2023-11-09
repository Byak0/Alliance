using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class BurstAllHeavyHitParticles : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public BurstAllHeavyHitParticles(MissionObject missionObject)
        {
            MissionObject = missionObject;
        }

        public BurstAllHeavyHitParticles()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Bursting all heavy-hit particles for the DestructableComponent of MissionObject with Id: ", MissionObject.Id, " and name: ", MissionObject.GameEntity.Name);
        }
    }
}