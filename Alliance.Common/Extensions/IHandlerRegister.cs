using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Common.Extensions
{
    /// <summary>
    /// Implement this interface in your extension if you need to register network messages handlers.
    /// Your class will be instantiated automatically and the Register method will be called.
    /// /!\ Do not use directly on a MissionBehavior as it will cause unwanted instantiation.
    /// </summary>
    public interface IHandlerRegister
    {
        void Register(NetworkMessageHandlerRegisterer reg);
    }
}
