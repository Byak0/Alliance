using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;


namespace Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncTk : GameNetworkMessage
	{

		public Dictionary<NetworkCommunicator, (int TkCount, int TkDamage, int TkKill)> AgentTkData { get; set; } = new();
		static readonly CompressionInfo.Integer IntTkCountIndexCompressionInfo = new CompressionInfo.Integer(0, 2000, true);
		static readonly CompressionInfo.Integer IntTkDamageIndexCompressionInfo = new CompressionInfo.Integer(0, 260000, true);


		public SyncTk() { }

		public SyncTk(Dictionary<NetworkCommunicator, (int TkCount, int TkDamage, int TkKill)> dict)
		{
			AgentTkData = dict;
		}

		protected override void OnWrite()
		{
			// Write number of entry
			WriteIntToPacket(AgentTkData.Count, IntTkCountIndexCompressionInfo);

			// Write each entry
			foreach (var kvp in AgentTkData)
			{
				//int agentIndex = kvp.Key;
				int tkCount = Math.Min(kvp.Value.TkCount, IntTkCountIndexCompressionInfo.GetMaximumValue());
				int tkDamage = Math.Min(kvp.Value.TkDamage, IntTkDamageIndexCompressionInfo.GetMaximumValue());
				int tkKill = Math.Min(kvp.Value.TkKill, IntTkCountIndexCompressionInfo.GetMaximumValue());


				//WriteAgentIndexToPacket(agentIndex);
				WriteNetworkPeerReferenceToPacket(kvp.Key);
				WriteIntToPacket(tkCount, IntTkCountIndexCompressionInfo);
				WriteIntToPacket(tkDamage, IntTkDamageIndexCompressionInfo);
				WriteIntToPacket(tkKill, IntTkCountIndexCompressionInfo);
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			AgentTkData.Clear();

			int count = ReadIntFromPacket(IntTkCountIndexCompressionInfo, ref bufferReadValid);
			for (int i = 0; i < count; i++)
			{
				NetworkCommunicator networkCommunicator = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
				int tkCount = ReadIntFromPacket(IntTkCountIndexCompressionInfo, ref bufferReadValid);
				int tkDamage = ReadIntFromPacket(IntTkDamageIndexCompressionInfo, ref bufferReadValid);
				int tkKill = ReadIntFromPacket(IntTkCountIndexCompressionInfo, ref bufferReadValid);

				if (bufferReadValid)
					AgentTkData[networkCommunicator] = (tkCount, tkDamage, tkKill);
			}

			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "SyncTk - Send agents with TK : network communicator, tkCount, tkDamage, tkKill";
		}
	}
}
