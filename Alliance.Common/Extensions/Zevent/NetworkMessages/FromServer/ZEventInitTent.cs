using Alliance.Common.Core.Utils;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.Zevent.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class ZEventInitTent : GameNetworkMessage
	{
		public int TentId { get; private set; }
		public int Tier { get; private set; }
		public int Variant { get; private set; }
		public int TotalDonations { get; private set; }
		public string Name { get; private set; }
		public string Message { get; private set; }

		public ZEventInitTent() { }

		public ZEventInitTent(int tentId, int tier, int variant, int totalDonations, string name, string message)
		{
			TentId = tentId;
			Tier = tier;
			Variant = variant;
			TotalDonations = totalDonations;

			// Crop name and message if needed
			if (name == null)
			{
				Name = string.Empty;
			}
			else
			{
				Name = name.Length > 13 ? name.Substring(0, 13) : name;
			}
			if (message == null)
			{
				Message = string.Empty;
			}
			else
			{
				Message = message.Length > 17 ? message.Substring(0, 17) : message;
			}

		}

		protected override void OnWrite()
		{
			WriteIntToPacket(TentId, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(Tier, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(Variant, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(TotalDonations, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteStringToPacket(Name);
			WriteStringToPacket(Message);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;

			TentId = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			Tier = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			Variant = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			TotalDonations = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			Name = ReadStringFromPacket(ref bufferReadValid);
			Message = ReadStringFromPacket(ref bufferReadValid);

			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Tell client to display tent";
		}
	}
}
