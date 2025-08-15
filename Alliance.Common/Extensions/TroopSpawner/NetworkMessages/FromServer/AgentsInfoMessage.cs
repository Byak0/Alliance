using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.TroopSpawner.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer
{
	/// <summary>
	/// NetworkMessage to synchronize AgentsInfoModel between server and clients.
	/// Use DataType to specify which field is being send.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class AgentsInfoMessage : GameNetworkMessage
	{
		public AgentDataType DataType { get; private set; }
		public int AgentIndex { get; private set; }
		public float Difficulty { get; private set; }
		public int Lives { get; private set; }
		public int SpeakingRange { get; private set; }

		public AgentsInfoMessage() { }

		public AgentsInfoMessage(int agentIndex, AgentDataType dataType, AgentInfo agentInfo)
		{
			AgentIndex = agentIndex;
			DataType = dataType;
			Difficulty = agentInfo.Difficulty;
			Lives = agentInfo.Lives;
			SpeakingRange = agentInfo.SpeakingRange;
		}

		protected override void OnWrite()
		{
			WriteAgentIndexToPacket(AgentIndex);
			WriteIntToPacket((int)DataType, CompressionHelper.AgentDataTypeCompressionInfo);
			switch (DataType)
			{
				case AgentDataType.All:
					WriteFloatToPacket(Difficulty, CompressionHelper.DefaultFloatValueCompressionInfo);
					WriteIntToPacket(Lives, CompressionHelper.DefaultIntValueCompressionInfo);
					WriteIntToPacket(SpeakingRange, CompressionHelper.DefaultIntValueCompressionInfo);
					break;
				case AgentDataType.Difficulty:
					WriteFloatToPacket(Difficulty, CompressionHelper.DefaultFloatValueCompressionInfo);
					break;
				case AgentDataType.Lives:
					WriteIntToPacket(Lives, CompressionHelper.DefaultIntValueCompressionInfo);
					break;
				case AgentDataType.SpeakingRange:
					WriteIntToPacket(SpeakingRange, CompressionHelper.DefaultIntValueCompressionInfo);
					break;
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			AgentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
			DataType = (AgentDataType)ReadIntFromPacket(CompressionHelper.AgentDataTypeCompressionInfo, ref bufferReadValid);
			switch (DataType)
			{
				case AgentDataType.All:
					Difficulty = ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid);
					Lives = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					SpeakingRange = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					break;
				case AgentDataType.Difficulty:
					Difficulty = ReadFloatFromPacket(CompressionHelper.DefaultFloatValueCompressionInfo, ref bufferReadValid);
					break;
				case AgentDataType.Lives:
					Lives = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					break;
				case AgentDataType.SpeakingRange:
					SpeakingRange = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
					break;
			}
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Agents;
		}

		protected override string OnGetLogFormat()
		{
			return "Sync agent informations";
		}
	}
}
