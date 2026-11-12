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
		public bool Visible { get; private set; }
		public bool HoldingForCinematic { get; private set; }

		public int ReadyPlayers { get; private set; }
		public int TotalPlayers { get; private set; }
		public WaitingScreenStateMessage() { }

		public WaitingScreenStateMessage(bool visible, bool holdingForCinematic, int readyPlayers, int totalPlayers)
		{
			Visible = visible;
			HoldingForCinematic = holdingForCinematic;
			ReadyPlayers = readyPlayers;
			TotalPlayers = totalPlayers;
		}

		protected override void OnWrite()
		{
			WriteBoolToPacket(Visible);
			WriteBoolToPacket(HoldingForCinematic);
			WriteIntToPacket(ReadyPlayers, new CompressionInfo.Integer(0, 200, true));
			WriteIntToPacket(TotalPlayers, new CompressionInfo.Integer(0, 200, true));
		}

		protected override bool OnRead()
		{
			bool valid = true;
			Visible = ReadBoolFromPacket(ref valid);
			if (!valid) return false;
			HoldingForCinematic = ReadBoolFromPacket(ref valid);
			if (!valid) return false;
			ReadyPlayers = ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref valid);
			if (!valid) return false;
			TotalPlayers = ReadIntFromPacket(new CompressionInfo.Integer(0, 200, true), ref valid);
			return valid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => $"Waiting screen visible={Visible} holding={HoldingForCinematic} ({ReadyPlayers}/{TotalPlayers} ready)";
	}
}
