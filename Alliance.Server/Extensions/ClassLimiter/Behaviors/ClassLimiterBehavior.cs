﻿using Alliance.Common.Extensions;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.ClassLimiter.Behaviors
{
    public class ClassLimiterBehavior : MissionNetwork, IHandlerRegister
    {
        private MultiplayerRoundController _roundController;

        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            ClassLimiterModel.Instance.SendAvailableCharactersToClient(networkPeer);
        }

        public override void OnBehaviorInitialize()
        {
            _roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
        }

        public override void AfterStart()
        {
            if (_roundController != null)
            {
                _roundController.OnRoundStarted += OnRoundStart;
            }
        }

        public override void OnRemoveBehavior()
        {
            if (_roundController != null)
            {
                _roundController.OnRoundStarted -= OnRoundStart;
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            // For test purpose, reserver slot whenever a bot spawn.
            if (agent.Character != null && agent.MissionPeer == null)
            {
                ClassLimiterModel.Instance.TryReserveCharacterSlot(agent.Character);
            }
        }

        private void OnRoundStart()
        {
            // Refresh class limits on every round
            ClassLimiterModel.Instance.Init();
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<RequestCharacterUsage>(HandleRequestUsage);
        }

        public bool HandleRequestUsage(NetworkCommunicator peer, RequestCharacterUsage message)
        {
            // DEBUG test
            //foreach (BasicCharacterObject character in MBObjectManager.Instance.GetObjectTypeList<BasicCharacterObject>())
            //{
            //    ClassLimiterModel.Instance.TryReserveCharacterSlot(character);
            //}
            return ClassLimiterModel.Instance.HandleRequestUsage(peer, message);
        }
    }
}
