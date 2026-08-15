using Alliance.Common.Extensions.NativeIntermissionVote.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.NativeIntermissionVote.NetworkMessages
{
	/// <summary>
	/// Server-side message sender for the native intermission vote UI.
	/// Uses TaleWorlds native intermission messages, plus one Alliance reset message to rebuild client vote lists safely.
	/// </summary>
	internal static class NativeIntermissionVoteMsg
	{
		public static void SyncVotingManagerValuesToAllPeers()
		{
			SendToAllClients(new UpdateIntermissionVotingManagerValues());
		}

		public static void SendVoteItemsToAllPeers(MultiplayerIntermissionVotingManager votingManager)
		{
			Log($"Vote broadcast - {votingManager.MapVoteItems.Count} maps, {votingManager.CultureVoteItems.Count} cultures.", LogLevel.Debug);
			ResetVoteItemsOnAllPeers();

			foreach (IntermissionVoteItem item in votingManager.MapVoteItems)
			{
				SendMapItemAddedToAllPeers(item.Id);
				SendMapVoteCountChangedToAllPeers(item.Index, item.VoteCount);
			}

			foreach (IntermissionVoteItem item in votingManager.CultureVoteItems)
			{
				SendCultureItemAddedToAllPeers(item.Id);
				SendCultureVoteCountChangedToAllPeers(item.Index, item.VoteCount);
			}
		}

		public static void SendIntermissionUpdateToAllPeers(MultiplayerIntermissionState state, float timer)
		{
			SendToAllClients(new MultiplayerIntermissionUpdate(state, timer));
		}

		private static void ResetVoteItemsOnAllPeers()
		{
			// Native item messages are append-only and vote counts are later updated by index.
			// CountingForMission makes MPIntermissionVM clear its displayed lists, while the custom
			// message clears MultiplayerIntermissionVotingManager items on the client.
			SendIntermissionUpdateToAllPeers(MultiplayerIntermissionState.CountingForMission, 0f);
			ClearVoteItemsOnAllPeers();
		}

		private static void SendMapItemAddedToAllPeers(string mapId)
		{
			Log($"Native vote broadcast - map item added: {mapId}", LogLevel.Debug);
			SendToAllClients(new MultiplayerIntermissionMapItemAdded(mapId));
		}

		private static void ClearVoteItemsOnAllPeers()
		{
			Log("Native vote broadcast - clearing vote items on clients.", LogLevel.Debug);
			SendToAllClients(new ClearNativeIntermissionVoteItems());
		}

		private static void SendCultureItemAddedToAllPeers(string cultureId)
		{
			Log($"Native vote broadcast - culture item added: {cultureId}", LogLevel.Debug);
			SendToAllClients(new MultiplayerIntermissionCultureItemAdded(cultureId));
		}

		private static void SendMapVoteCountChangedToAllPeers(int mapItemIndex, int voteCount)
		{
			Log($"Native vote broadcast - map vote count changed: index={mapItemIndex} votes={voteCount}", LogLevel.Debug);
			SendToAllClients(new MultiplayerIntermissionMapItemVoteCountChanged(mapItemIndex, voteCount));
		}

		private static void SendCultureVoteCountChangedToAllPeers(int cultureItemIndex, int voteCount)
		{
			Log($"Native vote broadcast - culture vote count changed: index={cultureItemIndex} votes={voteCount}", LogLevel.Debug);
			SendToAllClients(new MultiplayerIntermissionCultureItemVoteCountChanged(cultureItemIndex, voteCount));
		}

		private static void SendToAllClients(GameNetworkMessage message)
		{
			foreach (NetworkCommunicator peer in GameNetwork.NetworkPeers)
			{
				if (peer.IsServerPeer)
				{
					continue;
				}

				GameNetwork.BeginModuleEventAsServer(peer);
				GameNetwork.WriteMessage(message);
				GameNetwork.EndModuleEventAsServer();
			}
		}
	}
}

