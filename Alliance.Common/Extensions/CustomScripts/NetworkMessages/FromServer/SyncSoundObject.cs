using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncSoundObject : GameNetworkMessage
    {
        public MissionObject MissionObject { get; private set; }

        public Agent UserAgent { get; private set; }

        public SyncSoundObject(MissionObject missionObject, Agent userAgent)
        {
            MissionObject = missionObject;
            UserAgent = userAgent;
        }

        public SyncSoundObject()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            MissionObject = ReadMissionObjectReferenceFromPacket(ref bufferReadValid);
            UserAgent = ReadAgentReferenceFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteMissionObjectReferenceToPacket(MissionObject);
            WriteAgentReferenceToPacket(UserAgent);
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize sound of UserAgent ", UserAgent.Name, " with Id: ", MissionObject.Id, " and name: ", MissionObject.GameEntity.Name);
        }
    }
}