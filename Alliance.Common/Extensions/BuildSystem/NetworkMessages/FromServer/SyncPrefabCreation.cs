using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.Extensions.BuildSystem.NetworkMessages.FromServer
{
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncPrefabCreation : GameNetworkMessage
	{
		static readonly CompressionInfo.Integer IndexCompressionInfo = new CompressionInfo.Integer(0, 10000, true);

		public int BuildIndex { get; private set; }
		public string PrefabName { get; private set; }
		public MatrixFrame PrefabFrame { get; private set; }

		/// <summary>
		/// Whether the prefab has MissionObject scripts. When true, the entity
		/// was already created on the client via native CreateMissionObject and
		/// <see cref="RootMissionObjectId"/> identifies it.
		/// </summary>
		public bool HasRootMissionObject { get; private set; }

		/// <summary>
		/// Full MissionObjectId (Id + CreatedAtRuntime) of the root MissionObject.
		/// Only valid when <see cref="HasRootMissionObject"/> is true.
		/// </summary>
		public MissionObjectId RootMissionObjectId { get; private set; }

		public SyncPrefabCreation(int buildIndex, string prefabName, MatrixFrame prefabFrame)
		{
			BuildIndex = buildIndex;
			PrefabName = prefabName;
			PrefabFrame = prefabFrame;
			HasRootMissionObject = false;
		}

		public SyncPrefabCreation(int buildIndex, string prefabName, MatrixFrame prefabFrame, MissionObjectId rootMissionObjectId)
		{
			BuildIndex = buildIndex;
			PrefabName = prefabName;
			PrefabFrame = prefabFrame;
			HasRootMissionObject = true;
			RootMissionObjectId = rootMissionObjectId;
		}

		public SyncPrefabCreation() { }

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			BuildIndex = ReadIntFromPacket(IndexCompressionInfo, ref bufferReadValid);
			PrefabName = ReadStringFromPacket(ref bufferReadValid);
			PrefabFrame = ReadMatrixFrameFromPacket(ref bufferReadValid);
			HasRootMissionObject = ReadBoolFromPacket(ref bufferReadValid);
			if (HasRootMissionObject)
			{
				RootMissionObjectId = ReadMissionObjectIdFromPacket(ref bufferReadValid);
			}
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(BuildIndex, IndexCompressionInfo);
			WriteStringToPacket(PrefabName);
			WriteMatrixFrameToPacket(PrefabFrame);
			WriteBoolToPacket(HasRootMissionObject);
			if (HasRootMissionObject)
			{
				WriteMissionObjectIdToPacket(RootMissionObjectId);
			}
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.General;
		}

		protected override string OnGetLogFormat()
		{
			return HasRootMissionObject
				? string.Concat("Sync building #", BuildIndex, " (", PrefabName, ") MO=", RootMissionObjectId.Id, " runtime=", RootMissionObjectId.CreatedAtRuntime)
				: string.Concat("Sync building #", BuildIndex, " (", PrefabName, ") no MO");
		}
	}
}