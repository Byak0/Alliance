using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.VOIP.NetworkMessages
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class SendBotVoiceToPlay : GameNetworkMessage
    {
        public Agent Agent { get; private set; }

        public byte[] Buffer { get; private set; }

        public int BufferLength { get; private set; }

        public SendBotVoiceToPlay()
        {
        }

        public SendBotVoiceToPlay(Agent agent, byte[] buffer, int bufferLength)
        {
            Agent = agent;
            Buffer = buffer;
            BufferLength = bufferLength;
        }

        protected override void OnWrite()
        {
            WriteAgentIndexToPacket(Agent.Index);
            WriteByteArrayToPacket(Buffer, 0, BufferLength);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            int agentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
            Agent = Mission.MissionNetworkHelper.GetAgentFromIndex(agentIndex, true);
            Buffer = new byte[1440];
            BufferLength = ReadByteArrayFromPacket(Buffer, 0, 1440, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.None;
        }

        protected override string OnGetLogFormat()
        {
            return string.Empty;
        }
    }
}
