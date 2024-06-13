using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncObjectState : GameNetworkMessage
	{
		public MissionObjectId MissionObjectId { get; private set; }

		public int State { get; private set; }

		public SyncObjectState(MissionObjectId missionObjectId, int state)
		{
			MissionObjectId = missionObjectId;
			State = state;
		}

		public SyncObjectState()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
			State = ReadIntFromPacket(CompressionMission.UsableGameObjectDestructionStateCompressionInfo, ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteMissionObjectIdToPacket(MissionObjectId);
			WriteIntToPacket(State, CompressionMission.UsableGameObjectDestructionStateCompressionInfo);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjectsDetailed | MultiplayerMessageFilter.SiegeWeaponsDetailed;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Synchronize state: ", State, " of MissionObject with Id: ", MissionObjectId.Id);
		}
	}
}