using Alliance.Common.Extensions.NativeIntermissionVote.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.NativeIntermissionVote.Handlers
{
	/// <summary>
	/// Handles client-side synchronization required by Alliance custom native intermission vote flows.
	/// Registered globally because native intermission voting runs after mission behaviors are removed.
	/// </summary>
	internal static class NativeIntermissionVoteHandler
	{
		private static bool _registered;

		public static void Register()
		{
			if (_registered)
			{
				return;
			}

			GameNetwork.NetworkMessageHandlerRegisterer reg = new GameNetwork.NetworkMessageHandlerRegisterer(GameNetwork.NetworkMessageHandlerRegisterer.RegisterMode.Add);
			reg.Register<ClearNativeIntermissionVoteItems>(HandleClearNativeIntermissionVoteItems);
			_registered = true;
		}

		private static void HandleClearNativeIntermissionVoteItems(ClearNativeIntermissionVoteItems message)
		{
			MultiplayerIntermissionVotingManager votingManager = MultiplayerIntermissionVotingManager.Instance;
			if (votingManager == null)
			{
				Log("Failed to clear native intermission vote items: MultiplayerIntermissionVotingManager is missing.", LogLevel.Warning);
				return;
			}

			votingManager.ClearVotes();
			votingManager.ClearItems();
			Log("Native intermission vote items cleared on client.", LogLevel.Debug);
		}
	}
}


