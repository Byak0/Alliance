using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads;

namespace Alliance.Common.Extensions.PlayerPoll.Models
{
	public struct PollOption
	{
		public int Id { get; set; }
		public int Votes { get; set; }
		public PollPayloadBase Payload { get; }

		public PollOption(PollPayloadBase payload = null)
		{
			Id = -1;
			Votes = 0;
			Payload = payload;
		}

		public PollOption(int optionId, int votes, PollPayloadBase optionPayload) : this()
		{
			Id = optionId;
			Votes = votes;
			Payload = optionPayload;
		}
	}
}
