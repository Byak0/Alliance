using Alliance.Common.Extensions.ClassLimiter.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.ClassLimiter.Behaviors
{
	public class ClassLimiterBehavior : MissionNetwork
	{
		private MultiplayerRoundController _roundController;

		protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			ClassLimiterModel.Instance.SendAvailableCharactersToClient(networkPeer);
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			_roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
		}

		public override void AfterStart()
		{
			if (_roundController != null)
			{
				_roundController.OnRoundStarted += OnRoundStart;
			}
			else
			{
				ClassLimiterModel.Instance.Init();
			}
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			if (_roundController != null)
			{
				_roundController.OnRoundStarted -= OnRoundStart;
			}
		}

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			// For test purpose, reserve slot whenever a bot spawn.
			if (agent.Character != null && agent.MissionPeer == null)
			{
				ClassLimiterModel.Instance.ReserveCharacterSlot(agent.Character);
			}
		}

		private void OnRoundStart()
		{
			// Refresh class limits on every round
			ClassLimiterModel.Instance.Init();
		}
	}
}
