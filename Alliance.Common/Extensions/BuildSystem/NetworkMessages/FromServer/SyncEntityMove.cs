using Alliance.Common.Extensions.BuildSystem.Behaviors;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer
{
	/// <summary>
	/// Server → clients: move an entity to a new frame. The entity is identified either by its
	/// BuildIndex (runtime-spawned, tracked by BuildBehavior) or by its AL_EntityMarker RefId
	/// (a marked scene entity, resolved via EntityMarkerIndex).
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncEntityMove : GameNetworkMessage
	{
		private static readonly CompressionInfo.Integer IndexCompression = new CompressionInfo.Integer(0, BuildBehavior.MAX_BUILD_ENTRIES, true);

		public bool IsBuildTracked { get; private set; }
		public int BuildIndex { get; private set; }
		public string RefId { get; private set; }
		public MatrixFrame Frame { get; private set; }

		public SyncEntityMove() { }

		public SyncEntityMove(int buildIndex, MatrixFrame frame)
		{
			IsBuildTracked = true;
			BuildIndex = buildIndex;
			Frame = frame;
		}

		public SyncEntityMove(string refId, MatrixFrame frame)
		{
			IsBuildTracked = false;
			RefId = refId;
			Frame = frame;
		}

		protected override void OnWrite()
		{
			WriteBoolToPacket(IsBuildTracked);
			if (IsBuildTracked) WriteIntToPacket(BuildIndex, IndexCompression);
			else WriteStringToPacket(RefId ?? "");
			WriteMatrixFrameToPacket(Frame);
		}

		protected override bool OnRead()
		{
			bool valid = true;
			IsBuildTracked = ReadBoolFromPacket(ref valid);
			if (IsBuildTracked) BuildIndex = ReadIntFromPacket(IndexCompression, ref valid);
			else RefId = ReadStringFromPacket(ref valid);
			Frame = ReadMatrixFrameFromPacket(ref valid);
			return valid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.General;

		protected override string OnGetLogFormat() => IsBuildTracked
			? "Sync entity move build#" + BuildIndex
			: "Sync entity move ref " + RefId;
	}
}
