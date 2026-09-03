using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Server -> client: seek the named (already playing) cinematic to an absolute time in seconds.
	/// Ignored by clients not playing that cinematic.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetCinematicTimeMessage : GameNetworkMessage
	{
		private string _cinematicName;
		private float _timeInSeconds;

		public string CinematicName
		{
			get => _cinematicName;
			private set => _cinematicName = value;
		}

		public float TimeInSeconds
		{
			get => _timeInSeconds;
			private set => _timeInSeconds = value;
		}

		public SetCinematicTimeMessage() { }

		public SetCinematicTimeMessage(string cinematicName, float timeInSeconds)
		{
			_cinematicName = cinematicName;
			_timeInSeconds = timeInSeconds;
		}

		protected override void OnWrite()
		{
			WriteStringToPacket(CinematicName);
			WriteFloatToPacket(TimeInSeconds, StoryMessages.CinematicTimeCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			CinematicName = ReadStringFromPacket(ref bufferReadValid);
			if (!bufferReadValid) return false;
			TimeInSeconds = ReadFloatFromPacket(StoryMessages.CinematicTimeCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => $"Set cinematic '{CinematicName}' time to {TimeInSeconds:F2}s";
	}
}
