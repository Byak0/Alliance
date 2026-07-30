using Alliance.Common.GameModes.Story.Utilities;
using Alliance.Common.Patch;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.NetworkMessages
{
	public static class StoryMessages
	{
		// ── Shared compression infos ───────

		public static readonly CompressionInfo.Integer ScopeIdCompressionInfo = new(0, DirtyCommonPatcher.MAX_MISSION_OBJECTS * 3 + 2, true);
		public static readonly CompressionInfo.Integer ActionIdCompressionInfo = new(1, 100000, true);
		public static readonly CompressionInfo.Integer DataCountCompressionInfo = new(0, 20, true);
		public static readonly CompressionInfo.Integer ValueTagCompressionInfo = new(0, 10, true);
		public static readonly CompressionInfo.Integer EnumValueCompressionInfo = new(0, 100, true);
		public static readonly CompressionInfo.Integer AgentListCountCompressionInfo = new(0, 100, true);
		public static readonly CompressionInfo.Integer FullRangeIntCompressionInfo = new(int.MinValue, int.MaxValue, true);
		public static readonly CompressionInfo.Float FullRangeFloatCompressionInfo = new(float.MinValue, float.MaxValue, 4);
		public static readonly CompressionInfo.Float ZonePositionCompressionInfo = new(float.MinValue, float.MaxValue, 2);
		public static readonly CompressionInfo.Float ZoneRadiusCompressionInfo = new(0f, 1000f, 2);

		// ── Message sending helpers ───────────────────────────────────────

		public static void SendExecuteAction(int scopeId, int actionId, VariableStore data)
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new FromServer.ExecuteActionMessage(scopeId, actionId, data));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
	}
}
