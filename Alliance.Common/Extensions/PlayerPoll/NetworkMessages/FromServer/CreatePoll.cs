using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.PlayerPoll.Models;
using Alliance.Common.Extensions.PlayerPoll.NetworkMessages.Payloads;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerPoll.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class CreatePoll : GameNetworkMessage
	{
		public Poll Poll { get; private set; }

		public CreatePoll(Poll poll)
		{
			Poll = poll;
		}

		public CreatePoll()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			int pollId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			PollType pollType = (PollType)ReadIntFromPacket(PollMsg.PollTypeCompressionInfo, ref bufferReadValid);
			PollAudienceType audienceType = (PollAudienceType)ReadIntFromPacket(PollMsg.PollAudienceTypeCompressionInfo, ref bufferReadValid);
			int? teamIndex = null;
			int? formationIndex = null;
			if (audienceType == PollAudienceType.TeamOnly)
			{
				teamIndex = ReadIntFromPacket(CompressionHelper.TeamIndexCompressionInfo, ref bufferReadValid);
			}
			else if (audienceType == PollAudienceType.FormationOnly)
			{
				teamIndex = ReadIntFromPacket(CompressionHelper.TeamIndexCompressionInfo, ref bufferReadValid);
				formationIndex = ReadIntFromPacket(CompressionHelper.FormationIndexCompressionInfo, ref bufferReadValid);
			}
			byte durationSeconds = (byte)ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			NetworkCommunicator creator = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
			PollHeader header = new(pollId, pollType, new PollAudience(audienceType, teamIndex, formationIndex), durationSeconds, creator);

			int payloadTypeId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			PollPayloadBase payload = null;
			if (payloadTypeId != -1)
			{
				payload = PollPayloadRegistry.Create(payloadTypeId);
				payload?.ReadFromPacket(ref bufferReadValid);
			}

			int optionsCount = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			var options = new List<PollOption>();
			for (int i = 0; i < optionsCount; i++)
			{
				int optionId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
				int votes = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
				int optionPayloadTypeId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
				PollPayloadBase optionPayload = null;
				if (optionPayloadTypeId != -1)
				{
					optionPayload = PollPayloadRegistry.Create(optionPayloadTypeId);
					optionPayload?.ReadFromPacket(ref bufferReadValid);
				}
				options.Add(new PollOption(optionId, votes, optionPayload));
			}
			Poll = new Poll(header, payload);
			foreach (var option in options)
			{
				Poll.Options.Add(option);
			}

			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(Poll.Header.Id, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket((int)Poll.Header.Type, PollMsg.PollTypeCompressionInfo);

			WriteIntToPacket((int)Poll.Header.Audience.AudienceType, PollMsg.PollAudienceTypeCompressionInfo);
			if (Poll.Header.Audience.AudienceType == PollAudienceType.TeamOnly
				&& Poll.Header.Audience.TeamIndex.HasValue)
			{
				WriteIntToPacket(Poll.Header.Audience.TeamIndex.Value, CompressionHelper.TeamIndexCompressionInfo);
			}
			else if (Poll.Header.Audience.AudienceType == PollAudienceType.FormationOnly
				&& Poll.Header.Audience.FormationIndex.HasValue
				&& Poll.Header.Audience.TeamIndex.HasValue)
			{
				WriteIntToPacket(Poll.Header.Audience.TeamIndex.Value, CompressionHelper.TeamIndexCompressionInfo);
				WriteIntToPacket(Poll.Header.Audience.FormationIndex.Value, CompressionHelper.FormationIndexCompressionInfo);
			}

			WriteIntToPacket(Poll.Header.DurationSeconds, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteNetworkPeerReferenceToPacket(Poll.Header.Creator);

			if (Poll.Payload != null)
			{
				WriteIntToPacket(Poll.Payload.TypeId, CompressionHelper.DefaultIntValueCompressionInfo);
				Poll.Payload.WriteToPacket();
			}
			else
			{
				WriteIntToPacket(-1, CompressionHelper.DefaultIntValueCompressionInfo);
			}

			WriteIntToPacket(Poll.Options.Count, CompressionHelper.DefaultIntValueCompressionInfo);
			foreach (PollOption option in Poll.Options)
			{
				WriteIntToPacket(option.Id, CompressionHelper.DefaultIntValueCompressionInfo);
				WriteIntToPacket(option.Votes, CompressionHelper.DefaultIntValueCompressionInfo);
				if (option.Payload != null)
				{
					WriteIntToPacket(option.Payload.TypeId, CompressionHelper.DefaultIntValueCompressionInfo);
					option.Payload.WriteToPacket();
				}
				else
				{
					WriteIntToPacket(-1, CompressionHelper.DefaultIntValueCompressionInfo);
				}
			}
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Peers;
		}

		protected override string OnGetLogFormat()
		{
			return $"Alliance - PlayerPoll - Syncing poll '{Poll.Header.Id}' with {Poll.Options.Count} options";
		}
	}
}