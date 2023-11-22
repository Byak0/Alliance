using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Revive.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestRescueAgent : GameNetworkMessage
    {
        private MatrixFrame spawnPosition;
        private bool spawnAtExactPosition;
        private string characterToSpawn;
        private int formation;
        private float difficulty;

        public RequestRescueAgent() { }

        public RequestRescueAgent(MatrixFrame spawnPosition, bool spawnAtExactPosition, string characterToSpawn, int formation, float difficulty)
        {
            this.spawnPosition = spawnPosition;
            this.spawnAtExactPosition = spawnAtExactPosition;
            this.characterToSpawn = characterToSpawn;
            this.formation = formation;
            this.difficulty = difficulty;
        }

        public MatrixFrame SpawnPosition
        {
            get
            {
                return spawnPosition;
            }
            private set
            {
                spawnPosition = value;
            }
        }

        public bool SpawnAtExactPosition
        {
            get
            {
                return spawnAtExactPosition;
            }
            private set
            {
                spawnAtExactPosition = value;
            }
        }

        public string CharacterToSpawn
        {
            get
            {
                return characterToSpawn;
            }
            private set
            {
                characterToSpawn = value;
            }
        }

        public int Formation
        {
            get
            {
                return formation;
            }
            private set
            {
                formation = value;
            }
        }

        public float Difficulty
        {
            get
            {
                return difficulty;
            }
            private set
            {
                difficulty = value;
            }
        }

        protected override void OnWrite()
        {
            WriteMatrixFrameToPacket(SpawnPosition);
            WriteBoolToPacket(SpawnAtExactPosition);
            WriteStringToPacket(CharacterToSpawn);
            WriteIntToPacket(Formation, CompressionOrder.FormationClassCompressionInfo);
            WriteFloatToPacket(Difficulty, new CompressionInfo.Float(0f, 5, 0.1f));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            SpawnPosition = ReadMatrixFrameFromPacket(ref bufferReadValid);
            SpawnAtExactPosition = ReadBoolFromPacket(ref bufferReadValid);
            CharacterToSpawn = ReadStringFromPacket(ref bufferReadValid);
            Formation = ReadIntFromPacket(CompressionOrder.FormationClassCompressionInfo, ref bufferReadValid);
            Difficulty = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }
        protected override string OnGetLogFormat()
        {
            return "Rescue agent";
        }
    }
}
