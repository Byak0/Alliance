using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Start playing a cinematic. The cinematic itself is never sent: receivers use their local copy,
	/// addressed by name (scenario cinematics) or by (ScopeId, ActionId) (inline action cinematics).
	/// StartTimeInSeconds is the shared anchor everyone (including late joiners) catches up to;
	/// DynamicValues carries the resolved values of the cinematic's dynamic slots, in walk order.
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class PlayCinematicMessage : GameNetworkMessage
	{
		/// <summary>Name of the scenario cinematic to play (when not using the action ref).</summary>
		public string CinematicName { get; private set; }

		public int ScopeId { get; private set; }

		public int ActionId { get; private set; }

		/// <summary>True when addressing the inline cinematic of the referenced action (entity-scoped).</summary>
		public bool UseActionRef { get; private set; }

		public float StartTimeInSeconds { get; private set; }

		/// <summary>Resolved values for the cinematic's dynamic slots, in walk order. May be null.</summary>
		public List<object> DynamicValues { get; private set; }

		public PlayCinematicMessage() { }

		/// <summary>Scenario-scoped cinematic, resolved by name on the receiving client.</summary>
		public PlayCinematicMessage(string cinematicName, long startTimeInTicks, List<object> dynamicValues)
		{
			UseActionRef = false;
			CinematicName = cinematicName;
			ScopeId = 0;
			ActionId = 0;
			StartTimeInSeconds = startTimeInTicks / 10000000f;
			DynamicValues = dynamicValues;
		}

		/// <summary>Entity-scoped inline cinematic, resolved from the action registry on the receiving client.</summary>
		public PlayCinematicMessage(int scopeId, int actionId, long startTimeInTicks, List<object> dynamicValues)
		{
			UseActionRef = true;
			CinematicName = null;
			ScopeId = scopeId;
			ActionId = actionId;
			StartTimeInSeconds = startTimeInTicks / 10000000f;
			DynamicValues = dynamicValues;
		}

		protected override void OnWrite()
		{
			WriteBoolToPacket(UseActionRef);
			if (UseActionRef)
			{
				WriteIntToPacket(ScopeId, StoryMessages.ScopeIdCompressionInfo);
				WriteIntToPacket(ActionId, StoryMessages.ActionIdCompressionInfo);
			}
			else
			{
				WriteStringToPacket(CinematicName);
			}
			WriteFloatToPacket(StartTimeInSeconds, StoryMessages.FullRangeFloatCompressionInfo);
			WriteIntToPacket(DynamicValues?.Count ?? 0, StoryMessages.DynamicSlotCountCompressionInfo);
			foreach (object value in DynamicValues ?? new List<object>())
			{
				StoryMessages.WriteSyncValue(value);
			}
		}

		protected override bool OnRead()
		{
			bool bufferReadValid = true;
			UseActionRef = ReadBoolFromPacket(ref bufferReadValid);
			if (!bufferReadValid) return false;

			if (UseActionRef)
			{
				ScopeId = ReadIntFromPacket(StoryMessages.ScopeIdCompressionInfo, ref bufferReadValid);
				if (!bufferReadValid) return false;
				ActionId = ReadIntFromPacket(StoryMessages.ActionIdCompressionInfo, ref bufferReadValid);
				if (!bufferReadValid) return false;
			}
			else
			{
				CinematicName = ReadStringFromPacket(ref bufferReadValid);
				if (!bufferReadValid) return false;
			}

			StartTimeInSeconds = ReadFloatFromPacket(StoryMessages.FullRangeFloatCompressionInfo, ref bufferReadValid);
			if (!bufferReadValid) return false;
			int count = ReadIntFromPacket(StoryMessages.DynamicSlotCountCompressionInfo, ref bufferReadValid);
			if (!bufferReadValid) return false;
			DynamicValues = new List<object>(count);
			for (int i = 0; i < count; i++)
			{
				object value = StoryMessages.ReadSyncValue(ref bufferReadValid);
				if (!bufferReadValid) return false;
				DynamicValues.Add(value);
			}
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => UseActionRef
			? $"Play cinematic (action scope={ScopeId}, id={ActionId})"
			: $"Play cinematic '{CinematicName}'";
	}
}
