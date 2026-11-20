using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetHealthLimitMessage : GameNetworkMessage
	{
		public int AgentIndex { get; private set; }
		public int HealthLimit { get; private set; }

		public SetHealthLimitMessage() { }

		public SetHealthLimitMessage(int agentIndex, int healthLimit)
		{
			AgentIndex = agentIndex;
			HealthLimit = healthLimit;
		}

		protected override void OnWrite()
		{
			WriteAgentIndexToPacket(AgentIndex);
			WriteIntToPacket(HealthLimit, CompressionMission.AgentHealthCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			AgentIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
			HealthLimit = ReadIntFromPacket(CompressionMission.AgentHealthCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}


		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Agents;
		}

		protected override string OnGetLogFormat()
		{
			return $"Sync agent {AgentIndex} health limit: {HealthLimit}";
		}
	}
}
