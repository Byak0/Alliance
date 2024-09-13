using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.FlagsTracker.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncCapturableZone : GameNetworkMessage
	{
		public MissionObjectId MissionObjectId { get; private set; }
		public Vec3 Position { get; private set; }
		public BattleSideEnum Owner { get; private set; }
		public Agent Bearer { get; private set; }

		public SyncCapturableZone(MissionObjectId missionObjectId, Vec3 position, BattleSideEnum owner, Agent bearer)
		{
			MissionObjectId = missionObjectId;
			Position = position;
			Owner = owner;
			Bearer = bearer;
		}

		public SyncCapturableZone()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
			Position = ReadVec3FromPacket(CompressionBasic.PositionCompressionInfo, ref bufferReadValid);
			Owner = (BattleSideEnum)ReadIntFromPacket(CompressionMission.TeamSideCompressionInfo, ref bufferReadValid);
			int bearerIndex = ReadAgentIndexFromPacket(ref bufferReadValid);
			Bearer = Mission.MissionNetworkHelper.GetAgentFromIndex(bearerIndex, true);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteMissionObjectIdToPacket(MissionObjectId);
			WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
			WriteIntToPacket((int)Owner, CompressionMission.TeamSideCompressionInfo);
			WriteAgentIndexToPacket(Bearer?.Index ?? -1);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjectsDetailed;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Synchronize CapturableZone MissionObject with Id: ", MissionObjectId.Id);
		}
	}
}
