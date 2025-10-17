namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads
{
	public class QuestionPollPayload : PollPayloadBase
	{
		public string Question { get; }

		public QuestionPollPayload(string question) : base((int)PollPayloadRegistry.PollPayloadType.Question)
		{
			Question = question;
		}

		public QuestionPollPayload() : base((int)PollPayloadRegistry.PollPayloadType.Question)
		{
		}

		public override void WriteToPacket()
		{
			// todo
		}
		public override void ReadFromPacket(ref bool bufferReadValid)
		{
			// todo
		}
	}
}
