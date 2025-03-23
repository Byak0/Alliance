using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncToggleEntitiesLocal : GameNetworkMessage
	{
		public string EntitiesTag { get; private set; }
		public bool Show { get; private set; }
		public MissionObjectId MissionObjectId { get; private set; }

		public SyncToggleEntitiesLocal(string entitiesTag, bool show, MissionObjectId missionObjectId)
		{
			EntitiesTag = entitiesTag;
			Show = show;
			MissionObjectId = missionObjectId;
		}

		public SyncToggleEntitiesLocal()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			EntitiesTag = ReadStringFromPacket(ref bufferReadValid);
			Show = ReadBoolFromPacket(ref bufferReadValid);
			MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteStringToPacket(EntitiesTag);
			WriteBoolToPacket(Show);
			WriteMissionObjectIdToPacket(MissionObjectId);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat(Show ? "Show" : "Hide", " entities with tag ", EntitiesTag, " on entity ", MissionObjectId);
		}
	}
}