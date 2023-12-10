using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestSpawnTroop : GameNetworkMessage
    {
        public MatrixFrame SpawnPosition { get; private set; }
        public bool SpawnAtExactPosition { get; private set; }
        public BasicCharacterObject CharacterToSpawn { get; private set; }
        public int Formation { get; private set; }
        public int TroopCount { get; private set; }
        public float Difficulty { get; private set; }

        public RequestSpawnTroop() { }

        public RequestSpawnTroop(MatrixFrame spawnPosition, bool spawnAtExactPosition, BasicCharacterObject characterToSpawn, int formation, int troopCount, float difficulty)
        {
            SpawnPosition = spawnPosition;
            SpawnAtExactPosition = spawnAtExactPosition;
            CharacterToSpawn = characterToSpawn;
            Formation = formation;
            TroopCount = troopCount;
            Difficulty = difficulty;
        }

        protected override void OnWrite()
        {
            WriteMatrixFrameToPacket(SpawnPosition);
            WriteBoolToPacket(SpawnAtExactPosition);
            WriteObjectReferenceToPacket(CharacterToSpawn, CompressionBasic.GUIDCompressionInfo);
            WriteIntToPacket(Formation, CompressionMission.FormationClassCompressionInfo);
            WriteIntToPacket(TroopCount, new CompressionInfo.Integer(0, 9999, true));
            WriteFloatToPacket(Difficulty, new CompressionInfo.Float(0f, 5, 0.1f));
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            SpawnPosition = ReadMatrixFrameFromPacket(ref bufferReadValid);
            SpawnAtExactPosition = ReadBoolFromPacket(ref bufferReadValid);
            CharacterToSpawn = (BasicCharacterObject)ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
            Formation = ReadIntFromPacket(CompressionMission.FormationClassCompressionInfo, ref bufferReadValid);
            TroopCount = ReadIntFromPacket(new CompressionInfo.Integer(0, 9999, true), ref bufferReadValid);
            Difficulty = ReadFloatFromPacket(new CompressionInfo.Float(0f, 5, 0.1f), ref bufferReadValid);

            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Agents;
        }
        protected override string OnGetLogFormat()
        {
            return "Spawn a special bot";
        }
    }
}