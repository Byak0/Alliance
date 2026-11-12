using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Server -> client: stop the named cinematic (if playing). Used when an act ends or the scenario
	/// aborts playback early. The cinematic <see cref="CinematicName"/> may be empty to stop any active one.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class StopCinematicMessage : GameNetworkMessage
	{
		private string _cinematicName;

		public string CinematicName
		{
			get => _cinematicName;
			private set => _cinematicName = value;
		}

		public StopCinematicMessage() { }

		public StopCinematicMessage(string cinematicName) => _cinematicName = cinematicName;

		protected override void OnWrite() => WriteStringToPacket(CinematicName);

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			CinematicName = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => $"Stop cinematic '{CinematicName}'";
	}
}
