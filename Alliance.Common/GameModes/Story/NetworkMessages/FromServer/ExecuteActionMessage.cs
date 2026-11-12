using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ExecuteActionMessage : GameNetworkMessage
    {
        public int ScopeId { get; private set; }
        public int ActionId { get; private set; }
        public List<object> DynamicValues { get; private set; }

        public ExecuteActionMessage() { }

        public ExecuteActionMessage(int scopeId, int actionId, List<object> dynamicValues)
        {
            ScopeId = scopeId;
            ActionId = actionId;
            DynamicValues = dynamicValues;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(ScopeId, StoryMessages.ScopeIdCompressionInfo);
            WriteIntToPacket(ActionId, StoryMessages.ActionIdCompressionInfo);
            StoryMessages.WriteSyncValues(DynamicValues);
        }

        protected override bool OnRead()
        {
            bool valid = true;
            ScopeId = ReadIntFromPacket(StoryMessages.ScopeIdCompressionInfo, ref valid);
            if (!valid) return false;
            ActionId = ReadIntFromPacket(StoryMessages.ActionIdCompressionInfo, ref valid);
            if (!valid) return false;
            DynamicValues = StoryMessages.ReadSyncValues(ref valid);
            return valid;
        }
        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

        protected override string OnGetLogFormat() => $"ExecuteActionMessage: ScopeId={ScopeId}, ActionId={ActionId}";
    }
}


