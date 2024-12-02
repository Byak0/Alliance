using Alliance.Common.Extensions;
using Alliance.Common.Extensions.FlagsTracker.NetworkMessages.FromServer;
using Alliance.Common.Extensions.FlagsTracker.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.FlagsTracker.Handlers
{
	/// <summary>
	/// All request related to CapturableZone send by SERVER will be handled here
	/// </summary>
	public class CapturableZoneHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncCapturableZone>(HandleCapturableZoneUpdate);
		}

		/// <summary>
		/// Server request to update CapturableZone
		/// </summary>
		public void HandleCapturableZoneUpdate(SyncCapturableZone message)
		{
			if (message.MissionObjectId != null)
			{
				MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
				CS_CapturableZone capturableZone = missionObject.GameEntity.GetFirstScriptOfType<CS_CapturableZone>();
				capturableZone.Position = message.Position;
				if (capturableZone.Owner != message.Owner)
				{
					capturableZone.SetOwner(message.Owner);
				}
				capturableZone.SetBearer(message.Bearer);
			}
		}
	}
}
