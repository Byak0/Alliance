using System.Threading;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.NativeIntermissionVote.Behaviors
{
	/// <summary>
	/// Triggers the native intermission vote after mission close, when clients are back in LobbyGameStateCustomGameClient.
	/// </summary>
	internal class NativeIntermissionVoteMissionListener : IMissionListener
	{
		public void OnEndMission()
		{
			Mission.Current?.RemoveListener(this);
			new Thread(NativeIntermissionVoteService.RunVoteAndRestartMission).Start();
		}

		public void OnInitialDeploymentPlanMade(BattleSideEnum battleSide, bool isFirstPlan) { }
		public void OnMissionModeChange(MissionMode oldMissionMode, bool atStart) { }
		public void OnResetMission() { }
		public void OnEquipItemsFromSpawnEquipmentBegin(Agent agent, Agent.CreationType creationType) { }
		public void OnEquipItemsFromSpawnEquipment(Agent agent, Agent.CreationType creationType) { }
		public void OnConversationCharacterChanged() { }
		public void OnDeploymentPlanMade(Team team, bool isFirstPlan) { }
	}
}

