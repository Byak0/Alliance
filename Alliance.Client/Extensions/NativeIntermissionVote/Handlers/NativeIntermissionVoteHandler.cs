using Alliance.Common.Extensions;
using Alliance.Common.Extensions.NativeIntermissionVote.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.NativeIntermissionVote.Handlers
{
	/// <summary>
	/// Handles client-side synchronization required by Alliance custom native intermission vote flows.
	/// Implements IGlobalHandlerRegister (not IHandlerRegister) and is discovered/registered by
	/// ClientGlobalAutoHandler, because native intermission voting runs after mission behaviors
	/// (and thus ClientAutoHandler) have been removed.
	/// </summary>
	internal class NativeIntermissionVoteHandler : IGlobalHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<ClearNativeIntermissionVoteItems>(HandleClearNativeIntermissionVoteItems);
		}

		private void HandleClearNativeIntermissionVoteItems(ClearNativeIntermissionVoteItems message)
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


