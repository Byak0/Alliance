using Alliance.Client.Core.KeyBinder.Models;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.InputSystem;

namespace Alliance.Client.Core.KeyBinder
{
    /// <summary>
    /// Game key context for a new key category
    /// </summary>
    public sealed class GameKeyBinderContext : GameKeyContext
    {
        private readonly IEnumerable<BindedKey> keys;

        public GameKeyBinderContext(string categoryId, IEnumerable<BindedKey> keys)
            : base(categoryId, 109 + keys.Count(), GameKeyContextType.Default)
        {
            this.keys = keys;
            RegisterHotKeys();
            RegisterGameKeys();
            RegisterGameAxisKeys();
        }

        private void RegisterHotKeys()
        {
            // TODO GameKeyBinderContext.RegisterHotKeys()
        }

        private void RegisterGameKeys()
        {
            // Be carefull with this 109
            int i = 109;
            foreach (var key in keys)
            {
                key.KeyId = i++;
                GameKey gameKey = new GameKey(key.KeyId, key.Id, GameKeyCategoryId, key.DefaultInputKey, GameKeyCategoryId);
                RegisterGameKey(gameKey);
            }

        }

        private void RegisterGameAxisKeys()
        {
            // TODO GameKeyBinderContext.RegisterGameAxisKeys()
        }
    }
}
