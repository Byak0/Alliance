using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateScenarioMessage : GameNetworkMessage
    {
        private int _scenarioState;

        public ActState ScenarioState => (ActState)_scenarioState;
        public float StateStartTimeInSeconds { get; private set; }
        public int StateRemainingTime { get; private set; }

        public UpdateScenarioMessage() { }

        public UpdateScenarioMessage(ActState state, long stateStartTimeInTicks, int remainingTimeOnPreviousState)
        {
            _scenarioState = (int)state;
            StateStartTimeInSeconds = stateStartTimeInTicks / 10000000f;
            StateRemainingTime = remainingTimeOnPreviousState;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(_scenarioState, new CompressionInfo.Integer(0, Enum.GetNames(typeof(ActState)).Length, true));
            WriteFloatToPacket(StateStartTimeInSeconds, CompressionMatchmaker.MissionTimeCompressionInfo);
            WriteIntToPacket(StateRemainingTime, new CompressionInfo.Integer(0, 360000, true));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            _scenarioState = ReadIntFromPacket(new CompressionInfo.Integer(0, Enum.GetNames(typeof(ActState)).Length, true), ref bufferReadValid);
            StateStartTimeInSeconds = ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
            StateRemainingTime = ReadIntFromPacket(new CompressionInfo.Integer(0, 360000, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return $"Update scenario to state {ScenarioState} with StartTime {StateStartTimeInSeconds} and RemainingTime {StateRemainingTime}";
        }
    }
}
