using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Configuration.Utilities;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Core.Configuration.NetworkMessages.FromClient
{
	/// <summary>
	/// Send user preferences to the server.
	/// TODO: Extend this message to include more user preferences if necessary. Maybe allow sending one field at a time like in SyncConfigField ?
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromClient)]
	public sealed class UpdateUserConfig : GameNetworkMessage
	{
		public int PreferredLanguageIndex { get; private set; }

		public UpdateUserConfig() { }

		public UpdateUserConfig(UserConfig userConfig)
		{
			PreferredLanguageIndex = userConfig.PreferredLanguageIndex;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(PreferredLanguageIndex, CompressionHelper.LanguageCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			PreferredLanguageIndex = ReadIntFromPacket(CompressionHelper.LanguageCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Peers;
		}

		protected override string OnGetLogFormat()
		{
			return "Send user preferences to server.";
		}
	}
}
