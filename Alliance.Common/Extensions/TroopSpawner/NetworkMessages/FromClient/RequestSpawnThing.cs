using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestSpawnThing : GameNetworkMessage
    {
        private MatrixFrame spawnPosition;

        public RequestSpawnThing() { }

        public RequestSpawnThing(MatrixFrame spawnPosition)
        {
            this.spawnPosition = spawnPosition;
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

        protected override void OnWrite()
        {
            WriteMatrixFrameToPacket(SpawnPosition);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            SpawnPosition = ReadMatrixFrameFromPacket(ref bufferReadValid);
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
