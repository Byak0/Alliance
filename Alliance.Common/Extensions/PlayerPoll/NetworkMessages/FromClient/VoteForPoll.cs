using Alliance.Common.Core.Utils;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class VoteForPoll : GameNetworkMessage
	{
		public int PollId { get; private set; }
		public int OptionId { get; private set; }
		public bool Add { get; private set; } = true;

		public VoteForPoll(int pollId, int optionId, bool add = true)
		{
			PollId = pollId;
			OptionId = optionId;
			Add = add;
		}

		public VoteForPoll()
		{
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(PollId, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(OptionId, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteBoolToPacket(Add);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PollId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			OptionId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			Add = ReadBoolFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return $"Alliance - PlayerPoll - Requesting to {(Add ? "vote for" : "remove vote for")} option {OptionId} in poll {PollId}";
		}
	}
}