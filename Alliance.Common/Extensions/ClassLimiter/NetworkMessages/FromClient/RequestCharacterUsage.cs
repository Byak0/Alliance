using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient
{
    /// <summary>
    /// Request to use a character.
    /// </summary>
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
    public sealed class RequestCharacterUsage : GameNetworkMessage
    {
        public BasicCharacterObject Character { get; private set; }

        public RequestCharacterUsage() { }

        public RequestCharacterUsage(BasicCharacterObject character)
        {
            Character = character;
        }

        protected override void OnWrite()
        {
            WriteObjectReferenceToPacket(Character, CompressionBasic.GUIDCompressionInfo);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Character = (BasicCharacterObject)ReadObjectReferenceFromPacket(MBObjectManager.Instance, CompressionBasic.GUIDCompressionInfo, ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Equipment;
        }

        protected override string OnGetLogFormat()
        {
            return "Request usage of a character.";
        }
    }
}
