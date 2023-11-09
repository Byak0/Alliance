using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Common.Extensions
{
    /// <summary>
    /// Implement this interface in your extension if you need to register network messages handlers.
    /// </summary>
    public interface IHandlerRegister
    {
        void Register(NetworkMessageHandlerRegisterer reg);
    }
}
