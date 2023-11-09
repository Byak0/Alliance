using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestAnimation : GameNetworkMessage
    {
        public Agent UserAgent { get; private set; }

        public int ActionIndex { get; private set; }

        public float Speed { get; private set; }

        public RequestAnimation(Agent userAgent, int actionIndex, float speed)
        {
            UserAgent = userAgent;
            ActionIndex = actionIndex;
            Speed = speed;
        }

        public RequestAnimation()
        {
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            UserAgent = ReadAgentReferenceFromPacket(ref bufferReadValid);
            ActionIndex = ReadIntFromPacket(new CompressionInfo.Integer(-1, 5000, true), ref bufferReadValid);
            Speed = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override void OnWrite()
        {
            WriteAgentReferenceToPacket(UserAgent);
            WriteIntToPacket(ActionIndex, new CompressionInfo.Integer(-1, 5000, true));
            WriteFloatToPacket(Speed, new CompressionInfo.Float(0f, 5, 0.1f));
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.AgentAnimations;
        }

        protected override string OnGetLogFormat()
        {
            return string.Concat("Request animation : ", ActionIndex, " for UserAgent ", UserAgent.Name);
        }
    }
}