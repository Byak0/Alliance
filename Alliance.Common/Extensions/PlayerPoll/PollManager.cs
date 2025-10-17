using Alliance.Common.Extensions.PlayerPoll.Models;
using Alliance.Common.Extensions.PlayerPoll.NetworkMessages;
using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerPoll
{
	public class PollManager
	{
		private static PollManager _instance;

		public static PollManager Instance
		{
			get
			{
				_instance ??= new PollManager();
				return _instance;
			}
		}

		private int _nextPollId = 0;
		public readonly Dictionary<int, Poll> ActivePolls = new();
		public readonly Dictionary<int, Poll> EndedPolls = new();

		private PollManager() { }

		public void RemoveVote(NetworkCommunicator player, int pollId, int optionId)
		{
			UpdateVote(player, pollId, optionId, false);
		}

		public void AddVote(NetworkCommunicator player, int pollId, int optionId)
		{
			UpdateVote(player, pollId, optionId, true);
		}

		private void UpdateVote(NetworkCommunicator player, int pollId, int optionId, bool add = true)
		{
			if (!ActivePolls.TryGetValue(pollId, out var poll)) return;

			bool changed = add ? poll.AddVote(player, optionId) : poll.RemoveVote(player, optionId);
			if (changed && GameNetwork.IsServer) PollMsg.UpdatePollVote(pollId, optionId, poll.OptionsById[optionId].Votes);
		}

		public void Tick(float dt)
		{
			List<int> pollsToRemove = new();
			foreach (Poll poll in ActivePolls.Values)
			{
				if (poll.State == PollState.Active && (DateTime.UtcNow - poll.StartTime).TotalSeconds >= poll.Header.DurationSeconds)
				{
					poll.EndPoll();
					EndedPolls.Add(poll.Header.Id, poll);
					pollsToRemove.Add(poll.Header.Id);
				}
			}
			foreach (var pollId in pollsToRemove)
			{
				ActivePolls.Remove(pollId);
			}
		}

		public Poll Start(PollAudience audience, PollType type, PollPayloadBase payload, byte durationSeconds, NetworkCommunicator creator, List<PollOption> options = null)
		{
			PollHeader header = new PollHeader(_nextPollId++, type, audience, durationSeconds, creator);
			Poll poll = new Poll(header, payload);
			foreach (PollOption option in options)
			{
				poll.AddOption(option);
			}
			ActivePolls.Add(poll.Header.Id, poll);
			poll.Start();
			return poll;
		}

		public Poll CreateYesNoPoll()
		{
			Poll poll1 = Start(
				new PollAudience(PollAudienceType.All),
				PollType.YesNo,
				new QuestionPollPayload("Is this a dummy poll?"),
				30,
				GameNetwork.MyPeer
			);

			Poll poll2 = Start(
				new PollAudience(PollAudienceType.FormationOnly, 0, 2),
				PollType.SingleChoice,
				new QuestionPollPayload("Choose your next officer :"),
				30,
				GameNetwork.MyPeer,
				new List<PollOption>
				{
					new PollOption(new OfficerPollPayload(GameNetwork.MyPeer)),
					new PollOption(new OfficerPollPayload(GameNetwork.MyPeer)),
					new PollOption(new OfficerPollPayload(GameNetwork.MyPeer)),
				}
			);

			return poll1;
		}
	}
}
