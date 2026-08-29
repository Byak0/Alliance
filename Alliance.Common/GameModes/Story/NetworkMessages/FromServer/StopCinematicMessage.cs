using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Server → client: stop the named cinematic (if playing). Used when an act ends or the scenario
	/// aborts playback early. The cinematic <see cref="CinematicId"/> may be empty to stop any active one.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class StopCinematicMessage : GameNetworkMessage
	{
		private string _cinematicId;

		public string CinematicId
		{
			get => _cinematicId;
			private set => _cinematicId = value;
		}

		public StopCinematicMessage() { }

		public StopCinematicMessage(string cinematicId) => _cinematicId = cinematicId;

		protected override void OnWrite() => WriteStringToPacket(CinematicId);

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			CinematicId = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => $"Stop cinematic '{CinematicId}'";
	}
}
