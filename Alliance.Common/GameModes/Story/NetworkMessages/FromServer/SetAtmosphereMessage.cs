using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>Sent to late joiners: replays the atmosphere a SetAtmosphereAction already applied
	/// to the scene before they loaded in.</summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetAtmosphereMessage : GameNetworkMessage
	{
		public string AtmosphereName { get; private set; }

		public SetAtmosphereMessage() { }

		public SetAtmosphereMessage(string atmosphereName)
		{
			AtmosphereName = atmosphereName;
		}

		protected override void OnWrite()
		{
			WriteStringToPacket(AtmosphereName);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			AtmosphereName = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => "Set atmosphere";
	}
}
