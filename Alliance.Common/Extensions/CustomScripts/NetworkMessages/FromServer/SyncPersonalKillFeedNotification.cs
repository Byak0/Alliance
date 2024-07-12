using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{

    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncPersonalKillFeedNotification : GameNetworkMessage
    {
        public Agent CasterAgent { get; set; }
        public Agent TargetAgent { get; set; }
        public bool IsFriendlyFireDamage { get; set; }
        public bool IsFriendlyFireKill { get; set; }
        public bool IsTargetMount { get; set; }
        public bool IsFatal { get; set; }
        public bool IsHeal { get; set; }
        public bool IsHeadshot { get; set; }
        public int HealthDifference { get; set; }

        public SyncPersonalKillFeedNotification(Agent casterAgent, Agent targetAgent, bool isFriendlyFireDamage, bool isFriendlyFireKill, bool isTargetMount, bool isFatal, bool isHeal, bool isHeadshot, int healthDifference)
        {
            CasterAgent = casterAgent;
            TargetAgent = targetAgent;
            IsFriendlyFireDamage = isFriendlyFireDamage;
            IsFriendlyFireKill = isFriendlyFireKill;
            IsTargetMount = isTargetMount;
            IsFatal = isFatal;
            IsHeal = isHeal;
            IsHeadshot = isHeadshot;
            HealthDifference = healthDifference;
        }

        public SyncPersonalKillFeedNotification()
        {

        }
        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            int agentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
            int agentIndex2 = ReadAgentIndexFromPacket(ref bufferReadValid);
            CasterAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(agentIndex, true);
            TargetAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(agentIndex2, true);
            IsFriendlyFireDamage = ReadBoolFromPacket(ref bufferReadValid);
            IsFriendlyFireKill = ReadBoolFromPacket(ref bufferReadValid);
            IsTargetMount = ReadBoolFromPacket(ref bufferReadValid);
            IsFatal = ReadBoolFromPacket(ref bufferReadValid);
            IsHeal = ReadBoolFromPacket(ref bufferReadValid);
            IsHeadshot = ReadBoolFromPacket(ref bufferReadValid);
            HealthDifference = ReadIntFromPacket(new CompressionInfo.Integer(0, 10000, true), ref bufferReadValid);

            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteAgentIndexToPacket(CasterAgent.Index);
            WriteAgentIndexToPacket(TargetAgent.Index);
            WriteBoolToPacket(IsFriendlyFireDamage);
            WriteBoolToPacket(IsFriendlyFireKill);
            WriteBoolToPacket(IsTargetMount);
            WriteBoolToPacket(IsFatal);
            WriteBoolToPacket(IsHeal);
            WriteBoolToPacket(IsHeadshot);
            WriteIntToPacket(HealthDifference, new CompressionInfo.Integer(0, 10000, true));
        }

        protected override string OnGetLogFormat()
        {
            return "Sending PersonalKillFeedNotification. Caster : " + CasterAgent.Name + ". Target : " + TargetAgent.Name + ".";
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }

    }
}

