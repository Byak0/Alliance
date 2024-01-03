using System.Collections.Generic;
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
        public List<int> SelectedPerks { get; private set; }
        public int NbPerks { get; private set; }


        public RequestSpawnTroop() { }

        public RequestSpawnTroop(MatrixFrame spawnPosition, bool spawnAtExactPosition, BasicCharacterObject characterToSpawn, int formation, int troopCount, float difficulty, List<int> selectedPerks)
        {
            SpawnPosition = spawnPosition;
            SpawnAtExactPosition = spawnAtExactPosition;
            CharacterToSpawn = characterToSpawn;
            Formation = formation;
            TroopCount = troopCount;
            Difficulty = difficulty;
            SelectedPerks = selectedPerks;
            NbPerks = SelectedPerks.Count;
        }

        protected override void OnWrite()
        {
            WriteMatrixFrameToPacket(SpawnPosition);
            WriteBoolToPacket(SpawnAtExactPosition);
            WriteObjectReferenceToPacket(CharacterToSpawn, CompressionBasic.GUIDCompressionInfo);
            WriteIntToPacket(Formation, CompressionMission.FormationClassCompressionInfo);
            WriteIntToPacket(TroopCount, new CompressionInfo.Integer(0, 9999, true));
            WriteFloatToPacket(Difficulty, new CompressionInfo.Float(0f, 5, 0.1f));
            WriteIntToPacket(NbPerks, CompressionMission.PerkIndexCompressionInfo);
            foreach (int selectedPerk in SelectedPerks)
            {
                WriteIntToPacket(selectedPerk, CompressionMission.PerkIndexCompressionInfo);
            }
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
            NbPerks = ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid);
            SelectedPerks = new List<int>();
            for (int i = 0; i < NbPerks; i++)
            {
                SelectedPerks.Add(ReadIntFromPacket(CompressionMission.PerkIndexCompressionInfo, ref bufferReadValid));
            }
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