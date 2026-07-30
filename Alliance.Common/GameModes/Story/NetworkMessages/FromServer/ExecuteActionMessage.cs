using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
    [DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
    public sealed class ExecuteActionMessage : GameNetworkMessage
    {
        public int ScopeId { get; private set; }
        public int ActionId { get; private set; }
        public VariableStore Data { get; private set; }

        public ExecuteActionMessage() { }

        public ExecuteActionMessage(int scopeId, int actionId, VariableStore data)
        {
            ScopeId = scopeId;
            ActionId = actionId;
            Data = data;
        }

        protected override void OnWrite()
        {
            WriteIntToPacket(ScopeId, StoryMessages.ScopeIdCompressionInfo);
            WriteIntToPacket(ActionId, StoryMessages.ActionIdCompressionInfo);

            var entries = new List<KeyValuePair<string, object>>(Data.GetAllEntries());
            WriteIntToPacket(entries.Count, StoryMessages.DataCountCompressionInfo);

            foreach (var kv in entries)
            {
                WriteStringToPacket(kv.Key);
                WriteValue(kv.Value);
            }
        }

        protected override bool OnRead()
        {
            bool valid = true;
            ScopeId = ReadIntFromPacket(StoryMessages.ScopeIdCompressionInfo, ref valid);
            if (!valid) return false;
            ActionId = ReadIntFromPacket(StoryMessages.ActionIdCompressionInfo, ref valid);
            if (!valid) return false;

            int count = ReadIntFromPacket(StoryMessages.DataCountCompressionInfo, ref valid);
            if (!valid) return false;

            Data = new VariableStore();
            for (int i = 0; i < count; i++)
            {
                string key = ReadStringFromPacket(ref valid);
                object value = ReadValue(ref valid);
                if (!valid) return false;
                Data.Set(key, value);
            }

            return valid;
        }

        private void WriteValue(object value)
        {
            if (value == null)
            {
                WriteIntToPacket(0, StoryMessages.ValueTagCompressionInfo);
                return;
            }

            Type type = value.GetType();

            if (type == typeof(int))
            {
                WriteIntToPacket(1, StoryMessages.ValueTagCompressionInfo);
                WriteIntToPacket((int)value, StoryMessages.FullRangeIntCompressionInfo);
            }
            else if (type == typeof(float))
            {
                WriteIntToPacket(2, StoryMessages.ValueTagCompressionInfo);
                WriteFloatToPacket((float)value, StoryMessages.FullRangeFloatCompressionInfo);
            }
            else if (type == typeof(bool))
            {
                WriteIntToPacket(3, StoryMessages.ValueTagCompressionInfo);
                WriteBoolToPacket((bool)value);
            }
            else if (type == typeof(string))
            {
                WriteIntToPacket(4, StoryMessages.ValueTagCompressionInfo);
                WriteStringToPacket((string)value);
            }
            else if (type.IsEnum)
            {
                WriteIntToPacket(5, StoryMessages.ValueTagCompressionInfo);
                WriteIntToPacket(Convert.ToInt32(value), StoryMessages.EnumValueCompressionInfo);
            }
            else if (value is Zone zone)
            {
                WriteIntToPacket(6, StoryMessages.ValueTagCompressionInfo);
                Vec3 center = zone.ResolveCenter(null);
                WriteFloatToPacket(center.X, StoryMessages.ZonePositionCompressionInfo);
                WriteFloatToPacket(center.Y, StoryMessages.ZonePositionCompressionInfo);
                WriteFloatToPacket(center.Z, StoryMessages.ZonePositionCompressionInfo);
                WriteFloatToPacket(zone.Radius, StoryMessages.ZoneRadiusCompressionInfo);
            }
            else if (value is Agent agent)
            {
                WriteIntToPacket(7, StoryMessages.ValueTagCompressionInfo);
                WriteAgentIndexToPacket(agent.Index);
            }
            else if (value is List<Agent> agents)
            {
                WriteIntToPacket(8, StoryMessages.ValueTagCompressionInfo);
                WriteIntToPacket(agents.Count, StoryMessages.AgentListCountCompressionInfo);
                for (int i = 0; i < agents.Count; i++)
                {
                    WriteAgentIndexToPacket(agents[i]?.Index ?? -1);
                }
            }
            else
            {
                WriteIntToPacket(4, StoryMessages.ValueTagCompressionInfo);
                WriteStringToPacket(value.ToString());
            }
        }

        private object ReadValue(ref bool valid)
        {
            int tag = ReadIntFromPacket(StoryMessages.ValueTagCompressionInfo, ref valid);
            if (!valid) return null;

            switch (tag)
            {
                case 0: return null;
                case 1: return ReadIntFromPacket(StoryMessages.FullRangeIntCompressionInfo, ref valid);
                case 2: return ReadFloatFromPacket(StoryMessages.FullRangeFloatCompressionInfo, ref valid);
                case 3: return ReadBoolFromPacket(ref valid);
                case 4: return ReadStringFromPacket(ref valid);
                case 5: return ReadIntFromPacket(StoryMessages.EnumValueCompressionInfo, ref valid);
                case 6:
                {
                    float cx = ReadFloatFromPacket(StoryMessages.ZonePositionCompressionInfo, ref valid);
                    float cy = ReadFloatFromPacket(StoryMessages.ZonePositionCompressionInfo, ref valid);
                    float cz = ReadFloatFromPacket(StoryMessages.ZonePositionCompressionInfo, ref valid);
                    float radius = ReadFloatFromPacket(StoryMessages.ZoneRadiusCompressionInfo, ref valid);
                    if (!valid) return null;
                    var zone = new Zone();
                    zone.Position = new Vec3(cx, cy, cz);
                    zone.Anchor = new WorldAnchor();
                    zone.Shape = new CircleShape { Radius = radius };
                    return zone;
                }
                case 7:
                {
                    int index = ReadAgentIndexFromPacket(ref valid);
                    if (!valid) return null;
                    return Mission.MissionNetworkHelper.GetAgentFromIndex(index, true);
                }
                case 8:
                {
                    int count = ReadIntFromPacket(StoryMessages.AgentListCountCompressionInfo, ref valid);
                    if (!valid) return null;
                    var agents = new List<Agent>(count);
                    for (int i = 0; i < count; i++)
                    {
                        int index = ReadAgentIndexFromPacket(ref valid);
                        if (!valid) return null;
                        agents.Add(Mission.MissionNetworkHelper.GetAgentFromIndex(index, true));
                    }
                    return agents;
                }
                default: valid = false; return null;
            }
        }

        protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

        protected override string OnGetLogFormat() => $"ExecuteActionMessage: ScopeId={ScopeId}, ActionId={ActionId}";
    }
}
