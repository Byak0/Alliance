using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.UsableEntity.NetworkMessages.FromClient
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class RequestEditTextPanel : GameNetworkMessage
	{
		public Guid ID { get; private set; }
		public string Text { get; private set; }

		// This empty constructor is required so the engine recognize this class as a valid NetworkMessage
		public RequestEditTextPanel() { }

		public RequestEditTextPanel(Guid id, string text)
		{
			ID = id;
			Text = text;
		}

		protected override void OnWrite()
		{
			Byte[] bytes = ID.ToByteArray();
			WriteByteArrayToPacket(bytes, 0, 16);
			WriteStringToPacket(Text);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Byte[] bytes = new Byte[16];
			ReadByteArrayFromPacket(bytes, 0, 16, ref bufferReadValid);
			ID = new Guid(bytes);
			Text = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}

		protected override string OnGetLogFormat()
		{
			return $"Player request to edit text of entity {ID} : {Text}";
		}
	}
}
