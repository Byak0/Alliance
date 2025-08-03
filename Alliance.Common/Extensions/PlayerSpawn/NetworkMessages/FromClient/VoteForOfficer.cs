using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromClient
{
	/// <summary>
	/// From client : Vote for an officer candidate (or remove the vote).
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class VoteForOfficer : GameNetworkMessage
	{
		public NetworkCommunicator Target { get; private set; }
		public bool Add { get; private set; } = true;

		public VoteForOfficer(NetworkCommunicator target, bool add = true)
		{
			Target = target;
			Add = add;
		}

		public VoteForOfficer()
		{
		}

		protected override void OnWrite()
		{
			WriteNetworkPeerReferenceToPacket(Target);
			WriteBoolToPacket(Add);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			Target = ReadNetworkPeerReferenceFromPacket(ref bufferReadValid);
			Add = ReadBoolFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - Requesting to " + (Add ? "vote for " : "remove vote for ") + Target?.VirtualPlayer?.UserName;
		}
	}
}