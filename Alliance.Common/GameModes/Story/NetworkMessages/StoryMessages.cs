using Alliance.Common.Patch;
using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages
{
	public static class StoryMessages
	{
		// ── Shared compression infos ───────

		public static readonly CompressionInfo.Integer ScopeIdCompressionInfo = new(0, DirtyCommonPatcher.MAX_MISSION_OBJECTS * 3 + 2, true);
		public static readonly CompressionInfo.Integer ActionIdCompressionInfo = new(1, 100000, true);
		public static readonly CompressionInfo.Integer DynamicSlotCountCompressionInfo = new(0, 512, true);
		public static readonly CompressionInfo.Integer ValueTagCompressionInfo = new(0, 10, true);
		public static readonly CompressionInfo.Integer EnumValueCompressionInfo = new(0, 100, true);
		public static readonly CompressionInfo.Integer AgentListCountCompressionInfo = new(0, 100, true);
		public static readonly CompressionInfo.Integer FullRangeIntCompressionInfo = new(int.MinValue, int.MaxValue, true);
		public static readonly CompressionInfo.Float FullRangeFloatCompressionInfo = new(float.MinValue, float.MaxValue, 4);
		public static readonly CompressionInfo.Float ZonePositionCompressionInfo = new(float.MinValue, float.MaxValue, 2);
		public static readonly CompressionInfo.Float ZoneRadiusCompressionInfo = new(0f, 1000f, 2);
		public static readonly CompressionInfo.Float CinematicTimeCompressionInfo = new(0f, 86400f, 32);

		// ── Shared dynamic-data codec (positional values over the network) ─────────
		//
		// Used by ExecuteActionMessage and PlayCinematicMessage. Values are tagged; Agents travel as
		// indices and are mapped back to the receiver's live agents.

		/// <summary>Writes an ordered list of resolved dynamic values (count + one tagged value each).</summary>
		public static void WriteSyncValues(List<object> values)
		{
			GameNetworkMessage.WriteIntToPacket(values?.Count ?? 0, DynamicSlotCountCompressionInfo);
			foreach (object value in values ?? EmptyValues)
			{
				WriteSyncValue(value);
			}
		}

		private static readonly List<object> EmptyValues = new List<object>();

		/// <summary>Reads back a list written by WriteSyncValues. Returns null on invalid data.</summary>
		public static List<object> ReadSyncValues(ref bool valid)
		{
			int count = GameNetworkMessage.ReadIntFromPacket(DynamicSlotCountCompressionInfo, ref valid);
			if (!valid) return null;
			List<object> values = new List<object>(count);
			for (int i = 0; i < count; i++)
			{
				object value = ReadSyncValue(ref valid);
				if (!valid) return null;
				values.Add(value);
			}
			return values;
		}

		public static void WriteSyncValue(object value)
		{
			if (value == null)
			{
				GameNetworkMessage.WriteIntToPacket(0, ValueTagCompressionInfo);
				return;
			}

			Type type = value.GetType();
			if (type == typeof(int))
			{
				GameNetworkMessage.WriteIntToPacket(1, ValueTagCompressionInfo);
				GameNetworkMessage.WriteIntToPacket((int)value, FullRangeIntCompressionInfo);
			}
			else if (type == typeof(float))
			{
				GameNetworkMessage.WriteIntToPacket(2, ValueTagCompressionInfo);
				GameNetworkMessage.WriteFloatToPacket((float)value, FullRangeFloatCompressionInfo);
			}
			else if (type == typeof(bool))
			{
				GameNetworkMessage.WriteIntToPacket(3, ValueTagCompressionInfo);
				GameNetworkMessage.WriteBoolToPacket((bool)value);
			}
			else if (type == typeof(string))
			{
				GameNetworkMessage.WriteIntToPacket(4, ValueTagCompressionInfo);
				GameNetworkMessage.WriteStringToPacket((string)value);
			}
			else if (type.IsEnum)
			{
				GameNetworkMessage.WriteIntToPacket(5, ValueTagCompressionInfo);
				GameNetworkMessage.WriteIntToPacket(Convert.ToInt32(value), EnumValueCompressionInfo);
			}
			else if (value is Models.Zone zone)
			{
				GameNetworkMessage.WriteIntToPacket(6, ValueTagCompressionInfo);
				TaleWorlds.Library.Vec3 center = zone.ResolveCenter(null);
				GameNetworkMessage.WriteFloatToPacket(center.X, ZonePositionCompressionInfo);
				GameNetworkMessage.WriteFloatToPacket(center.Y, ZonePositionCompressionInfo);
				GameNetworkMessage.WriteFloatToPacket(center.Z, ZonePositionCompressionInfo);
				GameNetworkMessage.WriteFloatToPacket(zone.Radius, ZoneRadiusCompressionInfo);
			}
			else if (value is Agent agent)
			{
				GameNetworkMessage.WriteIntToPacket(7, ValueTagCompressionInfo);
				GameNetworkMessage.WriteAgentIndexToPacket((int)agent.Index);
			}
			else if (value is List<Agent> agents)
			{
				GameNetworkMessage.WriteIntToPacket(8, ValueTagCompressionInfo);
				GameNetworkMessage.WriteIntToPacket(agents.Count, AgentListCountCompressionInfo);
				for (int i = 0; i < agents.Count; i++)
				{
					GameNetworkMessage.WriteAgentIndexToPacket(agents[i] != null ? (int)agents[i].Index : -1);
				}
			}
			else
			{
				GameNetworkMessage.WriteIntToPacket(4, ValueTagCompressionInfo);
				GameNetworkMessage.WriteStringToPacket(value.ToString());
			}
		}

		public static object ReadSyncValue(ref bool valid)
		{
			int tag = GameNetworkMessage.ReadIntFromPacket(ValueTagCompressionInfo, ref valid);
			if (!valid) return null;

			switch (tag)
			{
				case 0: return null;
				case 1: return GameNetworkMessage.ReadIntFromPacket(FullRangeIntCompressionInfo, ref valid);
				case 2: return GameNetworkMessage.ReadFloatFromPacket(FullRangeFloatCompressionInfo, ref valid);
				case 3: return GameNetworkMessage.ReadBoolFromPacket(ref valid);
				case 4: return GameNetworkMessage.ReadStringFromPacket(ref valid);
				case 5: return GameNetworkMessage.ReadIntFromPacket(EnumValueCompressionInfo, ref valid);
				case 6:
					{
						float cx = GameNetworkMessage.ReadFloatFromPacket(ZonePositionCompressionInfo, ref valid);
						float cy = GameNetworkMessage.ReadFloatFromPacket(ZonePositionCompressionInfo, ref valid);
						float cz = GameNetworkMessage.ReadFloatFromPacket(ZonePositionCompressionInfo, ref valid);
						float radius = GameNetworkMessage.ReadFloatFromPacket(ZoneRadiusCompressionInfo, ref valid);
						if (!valid) return null;
						var zone = new Models.Zone();
						zone.Position = new TaleWorlds.Library.Vec3(cx, cy, cz);
						zone.Anchor = new Models.WorldAnchor();
						zone.Shape = new Models.CircleShape { Radius = radius };
						return zone;
					}
				case 7:
					{
						int index = GameNetworkMessage.ReadAgentIndexFromPacket(ref valid);
						if (!valid) return null;
						return Mission.MissionNetworkHelper.GetAgentFromIndex(index, true);
					}
				case 8:
					{
						int count = GameNetworkMessage.ReadIntFromPacket(AgentListCountCompressionInfo, ref valid);
						if (!valid) return null;
						var agents = new List<Agent>(count);
						for (int i = 0; i < count; i++)
						{
							int index = GameNetworkMessage.ReadAgentIndexFromPacket(ref valid);
							if (!valid) return null;
							agents.Add(Mission.MissionNetworkHelper.GetAgentFromIndex(index, true));
						}
						return agents;
					}
				default: valid = false; return null;
			}
		}


		// ── Message sending helpers ───────────────────────────────────────

		public static void SendExecuteAction(int scopeId, int actionId, List<object> dynamicValues)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new FromServer.ExecuteActionMessage(scopeId, actionId, dynamicValues));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
