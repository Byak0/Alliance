using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.ClassLimiter.Handlers
{
    public class ClassLimiterHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<CharacterAvailableMessage>(HandleCharacterAvailableMessage);
        }

        public void HandleCharacterAvailableMessage(CharacterAvailableMessage message)
        {
            ClassLimiterModel.Instance.ChangeCharacterAvailability(message.Character, message.Enabled);
        }
    }
}
