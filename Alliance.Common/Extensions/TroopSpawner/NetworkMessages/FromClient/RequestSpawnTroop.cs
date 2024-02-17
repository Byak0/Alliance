using Alliance.Common.Core.Configuration.Utilities;
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
        public int DifficultyLevel { get; private set; }
        public List<int> SelectedPerks { get; private set; }
        public int NbPerks { get; private set; }


        public RequestSpawnTroop() { }

        public RequestSpawnTroop(MatrixFrame spawnPosition, bool spawnAtExactPosition, BasicCharacterObject characterToSpawn, int formation, int troopCount, int difficulty, List<int> selectedPerks)
        {
            SpawnPosition = spawnPosition;
            SpawnAtExactPosition = spawnAtExactPosition;
            CharacterToSpawn = characterToSpawn;
            Formation = formation;
            TroopCount = troopCount;
            DifficultyLevel = difficulty;
            SelectedPerks = selectedPerks;
            NbPerks = SelectedPerks.Count;
        }

        protected override void OnWrite()
        {
            WriteMatrixFrameToPacket(SpawnPosition);
            WriteBoolToPacket(SpawnAtExactPosition);
            WriteObjectReferenceToPacket(CharacterToSpawn, CompressionBasic.GUIDCompressionInfo);
            WriteIntToPacket(Formation, CompressionMission.FormationClassCompressionInfo);
            WriteIntToPacket(TroopCount, CompressionHelper.DefaultIntValueCompressionInfo);
            WriteIntToPacket(DifficultyLevel, CompressionHelper.DefaultIntValueCompressionInfo);
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
            TroopCount = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
            DifficultyLevel = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
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