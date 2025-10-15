using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class HideEntity : GameNetworkMessage
	{
		public Guid ID { get; private set; }

		// This empty constructor and the sealed class keyword is required so the engine recognize this class as a valid NetworkMessage
		public HideEntity() { }

		public HideEntity(Guid id)
		{
			ID = id;
		}

		protected override void OnWrite()
		{
			Byte[] bytes = ID.ToByteArray();
			WriteByteArrayToPacket(bytes, 0, 16);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Byte[] bytes = new Byte[16];
			ReadByteArrayFromPacket(bytes, 0, 16, ref bufferReadValid);
			ID = new Guid(bytes);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}
		protected override string OnGetLogFormat()
		{
			return $"Hide entity {ID}";
		}
	}
}
