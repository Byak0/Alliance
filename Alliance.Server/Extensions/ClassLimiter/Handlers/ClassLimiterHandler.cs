using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.ClassLimiter.Handlers
{
    public class ClassLimiterHandler : IHandlerRegister
    {
        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestCharacterUsage>(ClassLimiterModel.Instance.HandleRequestUsage);
        }
    }
}
