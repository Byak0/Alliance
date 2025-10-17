using Alliance.Common.Core.Utils;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class UpdatePollVote : GameNetworkMessage
	{
		public int PollId;
		public int OptionId;
		public int NewVoteCount;

		public UpdatePollVote(int pollId, int optionId, int newVoteCount)
		{
			PollId = pollId;
			OptionId = optionId;
			NewVoteCount = newVoteCount;
		}

		public UpdatePollVote()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PollId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			OptionId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			NewVoteCount = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(PollId, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(OptionId, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(NewVoteCount, CompressionHelper.DefaultIntValueCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Peers;
		}

		protected override string OnGetLogFormat()
		{
			return $"Alliance - PlayerPoll - Updating poll {PollId} - option {OptionId} with new vote count : {NewVoteCount}";
		}
	}
}