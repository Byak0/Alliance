using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer
{
	/// <summary>
	/// Server → clients: delete an entity. Identified by BuildIndex (runtime-spawned, BuildBehavior) or
	/// AL_EntityMarker RefId (marked scene entity). Replaces SyncPrefabRemoval for all consumers.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncEntityDelete : GameNetworkMessage
	{
		private static readonly CompressionInfo.Integer IndexCompression = new CompressionInfo.Integer(0, 10000, true);

		public bool IsBuildTracked { get; private set; }
		public int BuildIndex { get; private set; }
		public string RefId { get; private set; }

		public SyncEntityDelete() { }

		public SyncEntityDelete(int buildIndex)
		{
			IsBuildTracked = true;
			BuildIndex = buildIndex;
		}

		public SyncEntityDelete(string refId)
		{
			IsBuildTracked = false;
			RefId = refId;
		}

		protected override void OnWrite()
		{
			WriteBoolToPacket(IsBuildTracked);
			if (IsBuildTracked) WriteIntToPacket(BuildIndex, IndexCompression);
			else WriteStringToPacket(RefId ?? "");
		}

		protected override bool OnRead()
		{
			bool valid = true;
			IsBuildTracked = ReadBoolFromPacket(ref valid);
			if (IsBuildTracked) BuildIndex = ReadIntFromPacket(IndexCompression, ref valid);
			else RefId = ReadStringFromPacket(ref valid);
			return valid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;

		protected override string OnGetLogFormat() => IsBuildTracked
			? "Sync entity delete build#" + BuildIndex
			: "Sync entity delete ref " + RefId;
	}
}
