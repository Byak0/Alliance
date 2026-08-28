using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;

namespace Alliance.Common.GameModes.Story.NetworkMessages.FromServer
{
	/// <summary>
	/// Start playing a cinematic.
	/// Two addressing modes:
	/// - By Id : scenario-scoped cinematic, resolved from the loaded scenario
	/// - By action ref : inline cinematic of a <c>PlayCinematicAction</c> in a AL_TriggerAction
	/// </summary>
	[DefineGameNetworkMessageTypeForMod(GameNetworkMessageSendType.FromServer)]
	public sealed class PlayCinematicMessage : GameNetworkMessage
	{
		private string _cinematicId;
		private int _scopeId;
		private int _actionId;
		private bool _useActionRef;
		private float _startTimeInSeconds;
		private bool _isSkippable;
		private bool _useViewerOrigin;

		public string CinematicId
		{
			get => _cinematicId;
			private set => _cinematicId = value;
		}

		public int ScopeId
		{
			get => _scopeId;
			private set => _scopeId = value;
		}

		public int ActionId
		{
			get => _actionId;
			private set => _actionId = value;
		}

		/// <summary>True when the cinematic is the inline copy of the referenced action (entity-scoped).</summary>
		public bool UseActionRef
		{
			get => _useActionRef;
			private set => _useActionRef = value;
		}

		public float StartTimeInSeconds
		{
			get => _startTimeInSeconds;
			private set => _startTimeInSeconds = value;
		}

		public bool IsSkippable
		{
			get => _isSkippable;
			private set => _isSkippable = value;
		}

		/// <summary>When true, the client resolves the "Viewer" role to its own agent (per-viewer variant).</summary>
		public bool UseViewerOrigin
		{
			get => _useViewerOrigin;
			private set => _useViewerOrigin = value;
		}

		public PlayCinematicMessage() { }

		/// <summary>Scenario-scoped cinematic, resolved by Id on the receiving client.</summary>
		public PlayCinematicMessage(string cinematicId, long startTimeInTicks, bool isSkippable, bool useViewerOrigin)
		{
			_useActionRef = false;
			_cinematicId = cinematicId;
			_scopeId = 0;
			_actionId = 0;
			_startTimeInSeconds = startTimeInTicks / 10000000f;
			_isSkippable = isSkippable;
			_useViewerOrigin = useViewerOrigin;
		}

		/// <summary>Entity-scoped inline cinematic, resolved from the action registry on the receiving client.</summary>
		public PlayCinematicMessage(int scopeId, int actionId, long startTimeInTicks, bool isSkippable, bool useViewerOrigin)
		{
			_useActionRef = true;
			_cinematicId = null;
			_scopeId = scopeId;
			_actionId = actionId;
			_startTimeInSeconds = startTimeInTicks / 10000000f;
			_isSkippable = isSkippable;
			_useViewerOrigin = useViewerOrigin;
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
				WriteStringToPacket(CinematicId);
			}
			WriteFloatToPacket(StartTimeInSeconds, StoryMessages.CinematicTimeCompressionInfo);
			WriteBoolToPacket(IsSkippable);
			WriteBoolToPacket(UseViewerOrigin);
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
				CinematicId = ReadStringFromPacket(ref bufferReadValid);
				if (!bufferReadValid) return false;
			}

			StartTimeInSeconds = ReadFloatFromPacket(StoryMessages.CinematicTimeCompressionInfo, ref bufferReadValid);
			if (!bufferReadValid) return false;
			IsSkippable = ReadBoolFromPacket(ref bufferReadValid);
			if (!bufferReadValid) return false;
			UseViewerOrigin = ReadBoolFromPacket(ref bufferReadValid);
			return bufferReadValid;
		}

		protected override MultiplayerMessageFilter OnGetLogFilter() => MultiplayerMessageFilter.Mission;

		protected override string OnGetLogFormat() => UseActionRef
			? $"Play cinematic (action scope={ScopeId}, id={ActionId}) (skippable={IsSkippable}, viewerOrigin={UseViewerOrigin})"
			: $"Play cinematic '{CinematicId}' (skippable={IsSkippable}, viewerOrigin={UseViewerOrigin})";
	}
}
