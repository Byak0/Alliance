namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads
{
	/// <summary>
	/// Base class for different poll payload types.
	/// Register the payload types in PollPayloadRegistry.
	/// </summary>
	public abstract class PollPayloadBase
	{
		public int TypeId { get; private set; }

		public PollPayloadBase(int typeId)
		{
			TypeId = typeId;
		}

		public abstract void WriteToPacket();
		public abstract void ReadFromPacket(ref bool bufferReadValid);
	}
}
