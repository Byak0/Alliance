using Alliance.Common.Core.Utils;
using Alliance.Common.GameModes.Story.Models;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncScenarioLivesMessage : GameNetworkMessage
	{
		public static readonly CompressionInfo.Integer RespawnStrategyCompressionInfo = new CompressionInfo.Integer(0, Enum.GetValues(typeof(RespawnStrategy)).Length, true);

		private int _respawnStrategy;

		public RespawnStrategy RespawnStrategy => (RespawnStrategy)_respawnStrategy;
		public int TeamRemainingLives { get; private set; }
		public int PlayerRemainingLives { get; private set; }

		public SyncScenarioLivesMessage()
		{
		}

		public SyncScenarioLivesMessage(RespawnStrategy respawnStrategy, int teamRemainingLives, int playerRemainingLives)
		{
			_respawnStrategy = (int)respawnStrategy;
			TeamRemainingLives = teamRemainingLives;
			PlayerRemainingLives = playerRemainingLives;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(_respawnStrategy, RespawnStrategyCompressionInfo);
			WriteIntToPacket(TeamRemainingLives, CompressionHelper.DefaultIntValueCompressionInfo);
			WriteIntToPacket(PlayerRemainingLives, CompressionHelper.DefaultIntValueCompressionInfo);
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			_respawnStrategy = ReadIntFromPacket(RespawnStrategyCompressionInfo, ref bufferReadValid);
			TeamRemainingLives = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			PlayerRemainingLives = ReadIntFromPacket(CompressionHelper.DefaultIntValueCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return $"Sync scenario lives: {RespawnStrategy} / Team={TeamRemainingLives} / Player={PlayerRemainingLives}";
		}
	}
}
