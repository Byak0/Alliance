using Alliance.Client.Extensions.VOIP.Behaviors;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.VOIP.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.VOIP.Handlers
{
    public class VoipHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            PBVoiceChatHandlerClient voiceChatHandler = Mission.Current.GetMissionBehavior<PBVoiceChatHandlerClient>();
            if (voiceChatHandler == null) return;
            reg.RegisterBaseHandler<SendVoiceToPlay>(voiceChatHandler.HandleServerEventSendVoiceToPlay);
            reg.RegisterBaseHandler<SendBotVoiceToPlay>(voiceChatHandler.HandleServerEventSendBotVoiceToPlay);
        }
    }
}
