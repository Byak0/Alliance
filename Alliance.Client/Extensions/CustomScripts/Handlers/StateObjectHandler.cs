using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.CustomScripts.Handlers
{
	public class StateObjectHandler : IHandlerRegister
	{
		public StateObjectHandler()
		{
		}

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncObjectState>(CS_HandleSyncObjectState);
		}

		public void CS_HandleSyncObjectState(SyncObjectState message)
		{
			if (message.MissionObjectId != null)
			{
				MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
				missionObject?.GameEntity?.GetFirstScriptOfType<CS_StateObject>()?.SetState(message.State);
			}
		}

		//public void CS_HandleSyncAbilityOfNavmesh(SyncAbilityOfNavmesh message)
		//{
		//	MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
		//	if (missionObject == null)
		//	{
		//		return;
		//	}
		//	missionObject.GameEntity.GetFirstScriptOfType<CS_StateObject>().SetAbilityOfNavmesh(message.Navmesh1, message.Navmesh2);
		//}
	}
}
