using Alliance.Common.Core.KeyBinder.Models;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.InputSystem;

namespace Alliance.Common.Core.KeyBinder
{
	/// <summary>
	/// Game key context for a new key category
	/// </summary>
	public sealed class GameKeyBinderContext : GameKeyContext
	{
		// Be careful with this value. GameKey KeyId must be unique and not already used in native.
		// We start at 300 to be sure to not conflict with native keys.
		private const int INITIAL_KEY_ID = 300;
		private readonly IEnumerable<BindedKey> keys;

		public GameKeyBinderContext(string categoryId, IEnumerable<BindedKey> keys)
			: base(categoryId, INITIAL_KEY_ID + keys.Count(), GameKeyContextType.Default)
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
			int i = INITIAL_KEY_ID;
			foreach (BindedKey key in keys)
			{
				key.KeyId = i++;
				GameKey gameKey = new GameKey(key.KeyId, key.Id, GameKeyCategoryId, key.DefaultInputKey, key.DefaultControllerKey, GameKeyCategoryId);
				RegisterGameKey(gameKey);
			}
		}

		private void RegisterGameAxisKeys()
		{
			// TODO GameKeyBinderContext.RegisterGameAxisKeys()
		}
	}
}
