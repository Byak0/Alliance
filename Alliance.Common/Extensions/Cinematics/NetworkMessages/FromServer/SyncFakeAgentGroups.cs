using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using Alliance.Common.Extensions.Cinematics.Models;

namespace Alliance.Common.Extensions.Cinematics.NetworkMessages.FromServer
{
	/// <summary>One persistent staged-extras group: a formation definition plus the frame where the
	/// group stands (the last ordered destination when its cinematic ended).</summary>
	public class FakeAgentGroupData
	{
		public string Key { get; set; }
		public string CharacterId { get; set; }
		public string CultureId { get; set; }
		public int Count { get; set; }
		public CinematicFormationLayout Layout { get; set; }
		public int Rows { get; set; }
		public float Spacing { get; set; }
		public bool WeaponsDrawn { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public float Z { get; set; }
		public float Yaw { get; set; }
	}

	/// <summary>
	/// Syncs the persistent staged-extras groups to late joiners: the server tracks groups flagged
	/// "persist after cinematic" and sends this snapshot to peers as they synchronize. Clients that
	/// already staged the same key locally (they played the cinematic themselves) skip it.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class SyncFakeAgentGroups : GameNetworkMessage
	{
		private static readonly CompressionInfo.Integer GroupCountCompressionInfo = new CompressionInfo.Integer(0, 32, true);
		private static readonly CompressionInfo.Integer FormationCountCompressionInfo = new CompressionInfo.Integer(1, 64, true);
		private static readonly CompressionInfo.Integer LayoutCompressionInfo = new CompressionInfo.Integer(0, 8, true);
		private static readonly CompressionInfo.Integer RowsCompressionInfo = new CompressionInfo.Integer(1, 16, true);
		private static readonly CompressionInfo.Float SpacingCompressionInfo = new CompressionInfo.Float(0f, 10, 0.01f);
		private static readonly CompressionInfo.Float PositionCompressionInfo = new CompressionInfo.Float(float.MinValue, float.MaxValue, 2);
		private static readonly CompressionInfo.Float YawCompressionInfo = new CompressionInfo.Float(-180, 180, 0.1f);

		public List<FakeAgentGroupData> Groups { get; private set; } = new List<FakeAgentGroupData>();

		public SyncFakeAgentGroups(List<FakeAgentGroupData> groups)
		{
			Groups = groups ?? new List<FakeAgentGroupData>();
		}

		public SyncFakeAgentGroups()
		{
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			int groupCount = ReadIntFromPacket(GroupCountCompressionInfo, ref bufferReadValid);
			for (int i = 0; i < groupCount && bufferReadValid; i++)
			{
				FakeAgentGroupData group = new FakeAgentGroupData
				{
					Key = ReadStringFromPacket(ref bufferReadValid),
					CharacterId = ReadStringFromPacket(ref bufferReadValid),
					CultureId = ReadStringFromPacket(ref bufferReadValid),
					Count = ReadIntFromPacket(FormationCountCompressionInfo, ref bufferReadValid),
					Layout = (CinematicFormationLayout)ReadIntFromPacket(LayoutCompressionInfo, ref bufferReadValid),
					Rows = ReadIntFromPacket(RowsCompressionInfo, ref bufferReadValid),
					Spacing = ReadFloatFromPacket(SpacingCompressionInfo, ref bufferReadValid),
					WeaponsDrawn = ReadBoolFromPacket(ref bufferReadValid),
					X = ReadFloatFromPacket(PositionCompressionInfo, ref bufferReadValid),
					Y = ReadFloatFromPacket(PositionCompressionInfo, ref bufferReadValid),
					Z = ReadFloatFromPacket(PositionCompressionInfo, ref bufferReadValid),
					Yaw = ReadFloatFromPacket(YawCompressionInfo, ref bufferReadValid)
				};
				if (bufferReadValid) Groups.Add(group);
			}
			return bufferReadValid;
		}

		protected override void OnWrite()
		{
			WriteIntToPacket(Groups.Count, GroupCountCompressionInfo);
			foreach (FakeAgentGroupData group in Groups)
			{
				WriteStringToPacket(group.Key ?? "");
				WriteStringToPacket(group.CharacterId ?? "");
				WriteStringToPacket(group.CultureId ?? "");
				WriteIntToPacket(group.Count, FormationCountCompressionInfo);
				WriteIntToPacket((int)group.Layout, LayoutCompressionInfo);
				WriteIntToPacket(group.Rows, RowsCompressionInfo);
				WriteFloatToPacket(group.Spacing, SpacingCompressionInfo);
				WriteBoolToPacket(group.WeaponsDrawn);
				WriteFloatToPacket(group.X, PositionCompressionInfo);
				WriteFloatToPacket(group.Y, PositionCompressionInfo);
				WriteFloatToPacket(group.Z, PositionCompressionInfo);
				WriteFloatToPacket(group.Yaw, YawCompressionInfo);
			}
		}

		protected override MultiplayerMessageFilter OnGetLogFilter()
		{
			return MultiplayerMessageFilter.Mission;
		}

		protected override string OnGetLogFormat()
		{
			return "Synchronize persistent fake agent groups : " + Groups.Count;
		}
	}
}
