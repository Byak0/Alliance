using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Drives the client waiting screen shown while the scenario waits for players to load:
	/// a full-black screen with the ready/total player counter. Sent periodically while waiting,
	/// and once with Visible=false when the wait is over.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class WaitingScreenStateMessage : GameNetworkMessage
	{
		private bool _visible;
		private int _readyPlayers;
		private int _totalPlayers;

		public bool Visible
		{
			get => _visible;
			private set => _visible = value;
		}

		public int ReadyPlayers
		{
			get => _readyPlayers;
			private set => _readyPlayers = value;
		}

		public int TotalPlayers
		{
			get => _totalPlayers;
			private set => _totalPlayers = value;
		}

		public WaitingScreenStateMessage() { }

		public WaitingScreenStateMessage(bool visible, int readyPlayers, int totalPlayers)
		{
			_visible = visible;
			_readyPlayers = readyPlayers;
			_totalPlayers = totalPlayers;
		}

		protected override void OnWrite()
		{
			WriteBoolToPacket(Visible);
			WriteIntToPacket(ReadyPlayers, new CompressionInfo.Integer(0, 200, true));
			WriteIntToPacket(TotalPlayers, new CompressionInfo.Integer(0, 200, true));
		}

		protected override bool OnRead()
		{
			bool valid = true;
			Visible = ReadBoolFromPacket(ref valid);
			if (!valid) return false;
			ReadyPlayers = ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref valid);
			if (!valid) return false;
			TotalPlayers = ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref valid);
			return valid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => $"Waiting screen visible={Visible} ({ReadyPlayers}/{TotalPlayers} ready)";
	}
}
