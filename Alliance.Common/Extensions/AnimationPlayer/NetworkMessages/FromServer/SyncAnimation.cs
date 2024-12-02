using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SyncAnimation : GameNetworkMessage
    {
        public Agent UserAgent { get; private set; }

        public int Index { get; private set; }

        public float Speed { get; private set; }

        public SyncAnimation(Agent userAgent, int animationIndex, float speed)
        {
            UserAgent = userAgent;
            Index = animationIndex;
            Speed = speed;
        }

        public SyncAnimation()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            int agentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
            UserAgent = Mission.MissionNetworkHelper.GetAgentFromIndex(agentIndex, true);
            Index = ReadIntFromPacket(new CompressionInfo.Integer(-1, 5000, true), ref bufferReadValid);
            Speed = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteAgentIndexToPacket(UserAgent.Index);
            WriteIntToPacket(Index, new CompressionInfo.Integer(-1, 5000, true));
            WriteFloatToPacket(Speed, new CompressionInfo.Float(0f, 5, 0.1f));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.AgentAnimations;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Synchronize animation : ", Index, " of UserAgent ", UserAgent.Name);
        }
    }
}