using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using TaleWorlds.PlayerServices;

namespace Alliance.Common.Core.Security.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class UpdateRole : GameNetworkMessage
    {
        public int Role { get; private set; }
        public PlayerId PlayerId { get; private set; }
        public bool Remove { get; private set; }

        public UpdateRole() { }

        public UpdateRole(int role, PlayerId playerId, bool remove = false)
        {
            Role = role;
            PlayerId = playerId;
            Remove = remove;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(Role, new CompressionInfo.Integer(0, RoleManager.Instance.RolesFields.Count, true));
            WriteUlongToPacket(PlayerId.Part1, CompressionBasic.DebugULongNonCompressionInfo);
            WriteUlongToPacket(PlayerId.Part2, CompressionBasic.DebugULongNonCompressionInfo);
            WriteUlongToPacket(PlayerId.Part3, CompressionBasic.DebugULongNonCompressionInfo);
            WriteUlongToPacket(PlayerId.Part4, CompressionBasic.DebugULongNonCompressionInfo);
            WriteBoolToPacket(Remove);
        }

        protected override bool OnRead()
        {
            bool bufferReadValid = true;
            Role = ReadIntFromPacket(new CompressionInfo.Integer(0, RoleManager.Instance.RolesFields.Count, true), ref bufferReadValid);
            ulong num = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
            ulong num2 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
            ulong num3 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
            ulong num4 = ReadUlongFromPacket(CompressionBasic.DebugULongNonCompressionInfo, ref bufferReadValid);
            PlayerId = new PlayerId(num, num2, num3, num4);
            Remove = ReadBoolFromPacket(ref bufferReadValid);
            return bufferReadValid;
        }

        protected override MultiplayerMessageFilter OnGetLogFilter()
        {
            return MultiplayerMessageFilter.Mission;
        }

        protected override string OnGetLogFormat()
        {
            return "Sync players roles from server.";
        }
    }
}
