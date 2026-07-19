using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads;
using System;
using System.Collections.Generic;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages
{
	public static class PollPayloadRegistry
	{
		public enum PollPayloadType
		{
			GameMode = 1,
			Officer = 2,
			Question = 3,
		}

		private static readonly Dictionary<PollPayloadType, Func<PollPayloadBase>> _create = new()
		{
			{ PollPayloadType.GameMode, () => new GameModePollPayload() },
			{ PollPayloadType.Officer,  () => new OfficerPollPayload() },
			{ PollPayloadType.Question, () => new QuestionPollPayload() },
		};

		public static PollPayloadBase Create(int type) => _create[(PollPayloadType)type]();
	}
}
