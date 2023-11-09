using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer
{
    /// <summary>
    /// NetworkMessage to synchronize AgentsInfoModel between server and clients.
    /// Use DataType to specify which field is being send.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class AgentsInfoMessage : GameNetworkMessage
    {
        public DataType DataType { get; private set; }
        public Agent Agent { get; private set; }
        public float Difficulty { get; private set; }
        public int Experience { get; private set; }
        public int Lives { get; private set; }

        public AgentsInfoMessage() { }

        public AgentsInfoMessage(Agent agent, DataType dataType, float difficulty = 1f, int experience = 0, int lives = 0)
        {
            Agent = agent;
            DataType = dataType;
            Difficulty = difficulty;
            Experience = experience;
            Lives = lives;
        }

        protected override void OnWrite()
        {
            WriteAgentReferenceToPacket(Agent);
            WriteIntToPacket((int)DataType, new CompressionInfo.Integer(0, 4));
            switch (DataType)
            {
                case DataType.All:
                    WriteFloatToPacket(Difficulty, new CompressionInfo.Float(0f, 5, 0.1f));
                    WriteIntToPacket(Experience, new CompressionInfo.Integer(0, 1000, true));
                    WriteIntToPacket(Lives, new CompressionInfo.Integer(0, 100, true));
                    break;
                case DataType.Difficulty:
                    WriteFloatToPacket(Difficulty, new CompressionInfo.Float(0f, 5, 0.1f));
                    break;
                case DataType.Experience:
                    WriteIntToPacket(Experience, new CompressionInfo.Integer(0, 1000, true));
                    break;
                case DataType.Lives:
                    WriteIntToPacket(Lives, new CompressionInfo.Integer(0, 100, true));
                    break;
            }
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Agent = ReadAgentReferenceFromPacket(ref bufferReadValid);
            DataType = (DataType)ReadIntFromPacket(new CompressionInfo.Integer(0, 4), ref bufferReadValid);
            switch (DataType)
            {
                case DataType.All:
                    Difficulty = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);
                    Experience = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
                    Lives = ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref bufferReadValid);
                    break;
                case DataType.Difficulty:
                    Difficulty = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);
                    break;
                case DataType.Experience:
                    Experience = ReadIntFromPacket(new CompressionInfo.Integer(0, 1000, true), ref bufferReadValid);
                    break;
                case DataType.Lives:
                    Lives = ReadIntFromPacket(new CompressionInfo.Integer(0, 100, true), ref bufferReadValid);
                    break;
            }
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync agent informations";
        }
    }

    public enum DataType
    {
        All,
        Difficulty,
        Experience,
        Lives
    }
}
