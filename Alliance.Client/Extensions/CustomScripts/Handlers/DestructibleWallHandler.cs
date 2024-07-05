using Alliance.Common.Extensions;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using Alliance.Common.Extensions.CustomScripts.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.CustomScripts.Handlers
{
	public class DestructibleWallHandler : IHandlerRegister
	{
		public DestructibleWallHandler()
		{
		}

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncObjectHitpoints>(CS_HandleSyncObjectHitpoints);
			reg.Register<SyncObjectDestructionLevel>(CS_HandleSyncObjectDestructionLevel);
			reg.Register<BurstAllHeavyHitParticles>(CS_HandleServerEventHitBurstAllHeavyHitParticles);
			reg.Register<SyncAbilityOfNavmesh>(CS_HandleSyncAbilityOfNavmesh);
			reg.Register<SyncSoundDestructible>(CS_HandleSyncSoundDestructible);
		}

		public void CS_HandleSyncObjectHitpoints(SyncObjectHitpoints message)
		{
			if (message.MissionObjectId != null)
			{
				MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
				missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().HitPoint = message.Hitpoints;
			}
		}

		public void CS_HandleSyncObjectDestructionLevel(SyncObjectDestructionLevel message)
		{
			MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
			if (missionObject == null)
			{
				return;
			}
			missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().SetDestructionLevel(message.DestructionLevel, message.ForcedIndex, message.BlowMagnitude, message.BlowPosition, message.BlowDirection, false);
		}

		public void CS_HandleServerEventHitBurstAllHeavyHitParticles(BurstAllHeavyHitParticles message)
		{
			MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
			if (missionObject == null)
			{
				return;
			}
			missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>().BurstHeavyHitParticles();
		}

		public void CS_HandleSyncAbilityOfNavmesh(SyncAbilityOfNavmesh message)
		{
			MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
			if (missionObject == null)
			{
				return;
			}
			CS_DestructibleWall cS_DestructibleWall = missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>();
			if (cS_DestructibleWall != null)
			{
				cS_DestructibleWall.SetAbilityOfNavmesh(message.Navmesh1, message.Navmesh2);
			}
			else
			{
				CS_StateObject cS_StateObject = missionObject.GameEntity.GetFirstScriptOfType<CS_StateObject>();
				cS_StateObject?.SetAbilityOfNavmesh(message.Navmesh1, message.Navmesh2);
			}
		}

		public void CS_HandleSyncSoundDestructible(SyncSoundDestructible message)
		{
			MissionObject missionObject = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
			if (missionObject == null)
			{
				return;
			}
			CS_DestructibleWall cS_DestructibleWall = missionObject.GameEntity.GetFirstScriptOfType<CS_DestructibleWall>();
			if (cS_DestructibleWall != null)
			{
				cS_DestructibleWall.SyncSound();
			}
			else
			{
				CS_StateObject cS_StateObject = missionObject.GameEntity.GetFirstScriptOfType<CS_StateObject>();
				cS_StateObject?.SyncSound();
			}
		}
	}
}
