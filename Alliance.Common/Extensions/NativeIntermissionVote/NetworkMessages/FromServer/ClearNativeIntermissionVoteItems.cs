using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.NativeIntermissionVote.NetworkMessages.FromServer
{
	/// <summary>
	/// Clears native intermission vote items on clients before the server broadcasts its custom vote pool.
	/// TaleWorlds native item messages only append items and update vote counts by index.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class ClearNativeIntermissionVoteItems : GameNetworkMessage
	{
		protected override void OnWrite()
		{
		}

		protected override bool OnRead()
		{
			return true;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Administration;
		}

		protected override string OnGetLogFormat()
		{
			return "Clear native intermission vote items";
		}
	}
}

