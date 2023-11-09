using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ObjectivesProgressMessage : GameNetworkMessage
    {
        public int AttackersDead { get; private set; }
        public int DefendersDead { get; private set; }
        public float TimerStart { get; private set; }
        public int TimerDuration { get; private set; }

        public ObjectivesProgressMessage() { }

        public ObjectivesProgressMessage(int attackersDead, int defendersDead, long startTimeInTicks, int timerDuration)
        {
            AttackersDead = MathF.Min(attackersDead, 12000);
            DefendersDead = MathF.Min(defendersDead, 12000);
            TimerStart = startTimeInTicks / 10000000f;
            TimerDuration = timerDuration;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(AttackersDead, new CompressionInfo.Integer(0, 12000, true));
            WriteIntToPacket(DefendersDead, new CompressionInfo.Integer(0, 12000, true));
            WriteFloatToPacket(TimerStart, CompressionMatchmaker.MissionTimeCompressionInfo);
            WriteIntToPacket(TimerDuration, new CompressionInfo.Integer(0, 3600, true));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            AttackersDead = ReadIntFromPacket(new CompressionInfo.Integer(0, 12000, true), ref bufferReadValid);
            DefendersDead = ReadIntFromPacket(new CompressionInfo.Integer(0, 12000, true), ref bufferReadValid);
            TimerStart = ReadFloatFromPacket(CompressionMatchmaker.MissionTimeCompressionInfo, ref bufferReadValid);
            TimerDuration = ReadIntFromPacket(new CompressionInfo.Integer(0, 3600, true), ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync objectives progress";
        }
    }
}
