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

    /// <summary>
    /// Implement this interface for handlers whose lifetime must exceed the mission's.
    /// Unlike <see cref="IHandlerRegister"/>, implementations are registered once (via
    /// ClientGlobalAutoHandler / ServerGlobalAutoHandler) and are never unregistered, since
    /// they may need to keep working after mission behaviors have been removed
    /// (e.g. native post-mission intermission voting).
    /// /!\ Do not use directly on a MissionBehavior as it will cause unwanted instantiation.
    /// </summary>
    public interface IGlobalHandlerRegister
    {
        void Register(NetworkMessageHandlerRegisterer reg);
    }
}
