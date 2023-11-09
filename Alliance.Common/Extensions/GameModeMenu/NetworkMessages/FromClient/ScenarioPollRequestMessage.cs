using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.GameModeMenu.NetworkMessages.FromClient
{
    /// <summary>
    /// NetworkMessage to request a new Scenario poll to the server.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class ScenarioPollRequestMessage : GameNetworkMessage
    {
        public string Scenario { get; private set; }
        public int Act { get; private set; }
        public bool SkipPoll { get; private set; }

        public ScenarioPollRequestMessage() { }

        public ScenarioPollRequestMessage(string scenario, int act, bool skipPoll)
        {
            Scenario = scenario;
            Act = act;
            SkipPoll = skipPoll;
        }

        protected override void OnWrite()
        {
            WriteStringToPacket(Scenario);
            WriteIntToPacket(Act, new CompressionInfo.Integer(0, 100, true));
            WriteBoolToPacket(SkipPoll);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Scenario = ReadStringFromPacket(ref bufferReadValid);
            Act = ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref bufferReadValid);
            SkipPoll = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Request a new scenario poll to the server";
        }
    }
}