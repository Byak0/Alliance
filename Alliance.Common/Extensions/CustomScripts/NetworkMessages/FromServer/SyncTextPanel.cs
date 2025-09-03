using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncTextPanel : GameNetworkMessage
	{
		public MissionObjectId MissionObjectId { get; private set; }
		public string Text { get; private set; }

		public SyncTextPanel()
		{
		}

		public SyncTextPanel(MissionObjectId missionObjectId, string text)
		{
			MissionObjectId = missionObjectId;
			Text = text;
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			MissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
			Text = ReadStringFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteMissionObjectIdToPacket(MissionObjectId);
			WriteStringToPacket(Text);
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.MissionObjects;
		}

		protected override string OnGetLogFormat()
		{
			return string.Concat("Synchronize Text : ", Text, " for object: ", MissionObjectId.Id);
		}
	}
}