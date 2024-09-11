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

		public SyncCapturableZone(MissionObjectId missionObjectId, Vec3 position, BattleSideEnum owner)
		{
			MissionObjectId = missionObjectId;
			Position = position;
			Owner = owner;
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
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteMissionObjectIdToPacket(MissionObjectId);
			WriteVec3ToPacket(Position, CompressionBasic.PositionCompressionInfo);
			WriteIntToPacket((int)Owner, CompressionMission.TeamSideCompressionInfo);
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
