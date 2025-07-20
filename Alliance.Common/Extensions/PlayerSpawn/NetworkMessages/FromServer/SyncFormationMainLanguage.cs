using Alliance.Common.Core.Configuration.Utilities;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.PlayerSpawnMenuMsg;

namespace Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer
{
	/// <summary>
	/// From server : Update main language for formation.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncFormationMainLanguage : GameNetworkMessage
	{
		public int TeamIndex { get; private set; } = -1;
		public int FormationIndex { get; private set; } = -1;
		public int MainLanguageIndex { get; private set; } = -1;

		public SyncFormationMainLanguage(PlayerTeam playerTeam, PlayerFormation playerFormation, int mainLanguageIndex)
		{
			TeamIndex = playerTeam.Index;
			FormationIndex = playerFormation.Index;
			MainLanguageIndex = mainLanguageIndex;
		}

		public SyncFormationMainLanguage()
		{
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(TeamIndex, TeamIndexCompressionInfo);
			WriteIntToPacket(FormationIndex, FormationIndexCompressionInfo);
			WriteIntToPacket(MainLanguageIndex, CompressionHelper.LanguageCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			TeamIndex = ReadIntFromPacket(TeamIndexCompressionInfo, ref bufferReadValid);
			FormationIndex = ReadIntFromPacket(FormationIndexCompressionInfo, ref bufferReadValid);
			MainLanguageIndex = ReadIntFromPacket(CompressionHelper.LanguageCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Alliance - PlayerSpawnMenu - " + TeamIndex + " - " + FormationIndex + " main language updated to index " + MainLanguageIndex;
		}
	}
}