using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromServer
{
    /// <summary>
    /// NetworkMessage to synchronize ClassLimiter model between server and clients.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class CharacterAvailableMessage : GameNetworkMessage
    {
        public BasicCharacterObject Character { get; private set; }
        public bool Enabled { get; private set; }

        public CharacterAvailableMessage() { }

        public CharacterAvailableMessage(BasicCharacterObject character, bool enabled = true)
        {
            Character = character;
            Enabled = enabled;
        }

        protected override void OnWrite()
        {
            WriteObjectReferenceToPacket(Character, CompressionBasic.GUIDCompressionInfo);
            WriteBoolToPacket(Enabled);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Character = (BasicCharacterObject)ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
            Enabled = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Equipment;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync available charactesr info";
        }
    }
}
