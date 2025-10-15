using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestUseEntity : GameNetworkMessage
	{
		public Guid ID { get; private set; }

		// This empty constructor is required so the engine recognize this class as a valid NetworkMessage
		public RequestUseEntity() { }

		public RequestUseEntity(Guid id)
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
			return $"Player request to use entity {ID}";
		}
	}
}
