using Alliance.Common.Core.Configuration.Models;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads
{
	public class GameModePollPayload : PollPayloadBase
	{
		public TWConfig NativeOptions { get; }
		public Config ModOptions { get; }

		public GameModePollPayload(TWConfig twConfig, Config config) : base((int)PollPayloadRegistry.PollPayloadType.GameMode)
		{
			NativeOptions = twConfig;
			ModOptions = config;
		}

		public GameModePollPayload() : base((int)PollPayloadRegistry.PollPayloadType.GameMode)
		{
		}

		public override void WriteToPacket()
		{
			// todo
			//WriteNativeOptions();
			//WriteModOptions();
		}
		public override void ReadFromPacket(ref bool bufferReadValid)
		{
			// todo
			//ReadNativeOptions(ref bufferReadValid);
			//ReadModOptions(ref bufferReadValid);
		}
	}
}
