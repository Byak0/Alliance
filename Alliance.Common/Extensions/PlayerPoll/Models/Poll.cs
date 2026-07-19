using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.PlayerPoll.Models
{
	public enum PollType
	{
		SingleChoice,
		MultipleChoices,
		YesNo
	}

	public enum PollState
	{
		Pending,
		Active,
		Ended
	}

	public enum PollAudienceType
	{
		All,
		TeamOnly,
		FormationOnly,
		OfficersOnly,
		AdminsOnly
	}

	public readonly struct PollAudience
	{
		public PollAudienceType AudienceType { get; }
		public int? TeamIndex { get; }
		public int? FormationIndex { get; }

		public PollAudience(PollAudienceType audienceType, int? teamIndex = null, int? formationIndex = null)
		{
			AudienceType = audienceType;
			TeamIndex = teamIndex;
			FormationIndex = formationIndex;
		}
	}

	public struct PollHeader
	{
		public int Id { get; }
		public PollType Type { get; }
		public PollAudience Audience { get; }
		public byte DurationSeconds { get; }
		public NetworkCommunicator Creator { get; }

		public PollHeader(int id, PollType type, PollAudience audience, byte durationSeconds, NetworkCommunicator creator)
		{
			Id = id;
			Type = type;
			Audience = audience;
			DurationSeconds = durationSeconds;
			Creator = creator;
		}
	}

	public class Poll
	{
		public readonly PollHeader Header;
		public readonly PollPayloadBase Payload;
		public readonly List<PollOption> Options = new();
		public readonly Dictionary<int, PollOption> OptionsById = new();
		private readonly Dictionary<int, HashSet<int>> _playerSelections = new();

		public DateTime StartTime { get; private set; }
		public PollState State { get; private set; }

		public event Action<Poll, PollOption> OnPollEnded;

		public Poll(PollHeader header, PollPayloadBase payload)
		{
			Header = header;
			Payload = payload;
			if (header.Type == PollType.YesNo)
			{
				AddOption(new PollOption());
				AddOption(new PollOption());
			}
			State = PollState.Pending;
		}

		public void Start()
		{
			StartTime = DateTime.UtcNow;
			State = PollState.Active;
		}

		public void EndPoll()
		{
			State = PollState.Ended;
			OnPollEnded?.Invoke(this, GetWinningOption());
		}

		public void AddOption(PollOption option)
		{
			if (option.Id == -1) option.Id = Options.Count + 1;
			Options.Add(option);
			OptionsById[option.Id] = option;
		}

		public bool RemoveVote(NetworkCommunicator player, int optionId)
		{
			//todo player id maybe not pertinent
			int playerId = player.Index;

			if (!_playerSelections.ContainsKey(playerId)
				|| !_playerSelections[playerId].Contains(optionId))
			{
				return false;
			}

			if (!OptionsById.TryGetValue(optionId, out PollOption option))
			{
				Log($"Poll \"{Header.Id}\" -> Option {optionId} not found", LogLevel.Error);
				return false; // Invalid option
			}

			_playerSelections[playerId].Remove(optionId);
			option.Votes--;
			return true;
		}

		public bool AddVote(NetworkCommunicator player, int optionId)
		{
			//todo player id maybe not pertinent
			int playerId = player.Index;

			// Initialize player votes if not present
			if (!_playerSelections.ContainsKey(playerId)) _playerSelections[playerId] = new HashSet<int>();
			else if (_playerSelections[playerId].Contains(optionId)) return false;

			if (!OptionsById.TryGetValue(optionId, out PollOption option))
			{
				Log($"Poll \"{Header.Id}\" -> Option {optionId} not found", LogLevel.Error);
				return false; // Invalid option
			}

			// Clear previous votes if single choice
			if (Header.Type == PollType.SingleChoice || Header.Type == PollType.YesNo)
			{
				foreach (int selectedOptionId in _playerSelections[playerId])
				{
					if (OptionsById.TryGetValue(selectedOptionId, out PollOption previousOption))
					{
						previousOption.Votes--;
					}
				}
				_playerSelections[playerId].Clear();
			}

			_playerSelections[playerId].Add(option.Id);
			option.Votes++;

			return true;
		}

		public PollOption GetWinningOption() => Options.OrderByDescending(o => o.Votes).FirstOrDefault();
	}
}
