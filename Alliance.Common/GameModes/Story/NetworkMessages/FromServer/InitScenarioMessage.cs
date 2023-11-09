using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class InitScenarioMessage : GameNetworkMessage
    {
        private string _scenarioId;
        private int _act;

        public string ScenarioId
        {
            get
            {
                return _scenarioId;
            }
            private set
            {
                _scenarioId = value;
            }
        }

        public int Act
        {
            get
            {
                return _act;
            }
            private set
            {
                _act = value;
            }
        }

        public InitScenarioMessage() { }

        public InitScenarioMessage(string scenarioId, int act)
        {
            _scenarioId = scenarioId;
            _act = act;
        }

        protected override void OnWrite()
        {
            WriteStringToPacket(ScenarioId);
            WriteIntToPacket(Act, new CompressionInfo.Integer(0, 10, true));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            ScenarioId = ReadStringFromPacket(ref bufferReadValid);
            Act = ReadIntFromPacket(new CompressionInfo.Integer(0, 10, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Init scenario";
        }
    }
}
