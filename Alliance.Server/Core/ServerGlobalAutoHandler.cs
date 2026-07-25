using Alliance.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.GameNetwork;

namespace Alliance.Server.Core
{
    /// <summary>
    /// Discovers and registers all IGlobalHandlerRegister implementations once per game session.
    /// Unlike ServerAutoHandler, this is not tied to the mission lifecycle (not a MissionBehavior):
    /// handlers registered here remain active even after mission behaviors are removed.
    /// Call Initialize() once, typically from SubModule.OnGameInitializationFinished.
    /// </summary>
    public static class ServerGlobalAutoHandler
    {
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                NetworkMessageHandlerRegisterer reg = new NetworkMessageHandlerRegisterer(NetworkMessageHandlerRegisterer.RegisterMode.Add);
                int handlerCount = 0;
                IEnumerable<Type> handlerTypes = Assembly.GetExecutingAssembly()
                                           .GetTypes()
                                           .Where(t => t.GetInterfaces().Contains(typeof(IGlobalHandlerRegister)) && !t.IsAbstract);

                foreach (Type type in handlerTypes)
                {
                    if (Activator.CreateInstance(type) is IGlobalHandlerRegister handler)
                    {
                        handler.Register(reg);
                        handlerCount++;
                    }
                }

                _initialized = true;
                Log($"Alliance - Successfully registered {handlerCount} global server handlers", LogLevel.Debug);
            }
            catch (Exception ex)
            {
                Log($"Alliance - Error while registering global server handlers", LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }
    }
}

