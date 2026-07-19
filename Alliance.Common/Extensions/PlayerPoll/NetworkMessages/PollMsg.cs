using Alliance.Common.Extensions.PlayerPoll.Models;
using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.FromClient;
using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.FromServer;
using System;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages
{
	public static class PollMsg
	{
		public static readonly CompressionInfo.Integer PollTypeCompressionInfo = new CompressionInfo.Integer(0, Enum.GetValues(typeof(PollType)).Length, true);
		public static readonly CompressionInfo.Integer PollAudienceTypeCompressionInfo = new CompressionInfo.Integer(0, Enum.GetValues(typeof(PollAudienceType)).Length, true);

		/// <summary>
		/// From server - Broadcast new vote count for a poll option.
		/// </summary>
		public static void UpdatePollVote(int pollId, int optionId, int newCount)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new UpdatePollVote(pollId, optionId, newCount));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// From server - Broadcast a new poll.
		/// </summary>
		public static void CreatePoll(Poll poll)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CreatePoll(poll));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// From server - Send poll to a player.
		/// </summary>
		public static void CreatePoll(NetworkCommunicator peer, Poll poll)
		{
			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new CreatePoll(poll));
			GameNetwork.EndModuleEventAsServer();
		}

		/// <summary>
		/// From client - Vote for a poll option.
		/// </summary>
		public static void RequestVoteForPoll(int pollId, int optionId, bool add = true)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new VoteForPoll(pollId, optionId, add));
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
