using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads
{
	public class OfficerPollPayload : PollPayloadBase
	{
		public NetworkCommunicator Target { get; }

		public OfficerPollPayload(NetworkCommunicator target) : base((int)PollPayloadRegistry.PollPayloadType.Officer)
		{
			Target = target;
		}

		public OfficerPollPayload() : base((int)PollPayloadRegistry.PollPayloadType.Officer)
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
