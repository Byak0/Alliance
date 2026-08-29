using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Server → client: seek the named (already playing) cinematic to an absolute time in seconds.
	/// Used for resync (pauses, debug) and future admin/late-join tooling. Ignored by clients not
	/// playing that cinematic.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SetCinematicTimeMessage : GameNetworkMessage
	{
		private string _cinematicId;
		private float _timeInSeconds;

		public string CinematicId
		{
			get => _cinematicId;
			private set => _cinematicId = value;
		}

		public float TimeInSeconds
		{
			get => _timeInSeconds;
			private set => _timeInSeconds = value;
		}

		public SetCinematicTimeMessage() { }

		public SetCinematicTimeMessage(string cinematicId, float timeInSeconds)
		{
			_cinematicId = cinematicId;
			_timeInSeconds = timeInSeconds;
		}

		protected override void OnWrite()
		{
			WriteStringToPacket(CinematicId);
			WriteFloatToPacket(TimeInSeconds, StoryMessages.CinematicTimeCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			CinematicId = ReadStringFromPacket(ref bufferReadValid);
			if (!bufferReadValid) return false;
			TimeInSeconds = ReadFloatFromPacket(StoryMessages.CinematicTimeCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => $"Set cinematic '{CinematicId}' time to {TimeInSeconds:F2}s";
	}
}
