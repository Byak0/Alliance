using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using System;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.ObjectSystem;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModes.Story.Behaviors
{
    public class ScenarioClientBehavior : MissionMultiplayerGameModeBaseClient, IBotControllerBehavior
    {
        public event Action<NetworkCommunicator> OnBotsControlledChangedEvent;

        public override bool IsGameModeUsingGold => true;
        public override bool IsGameModeTactical => true;
        public override bool IsGameModeUsingRoundCountdown => false;
        public override MultiplayerGameType GameType => MultiplayerGameType.Captain;

        private MissionRepresentativeBase _myRepresentative;

        public ScenarioClientBehavior() : base()
        {
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            MissionNetworkComponent.OnMyClientSynchronized += OnMyClientSynchronized;
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
            MissionNetworkComponent.OnMyClientSynchronized -= OnMyClientSynchronized;

            // Reset troop spawn limit
            foreach (ExtendedCharacter pvcChar in MBObjectManager.Instance.GetObjectTypeList<ExtendedCharacter>())
            {
                pvcChar.TroopLeft = pvcChar.TroopLimit;
            }
        }

        private void OnMyClientSynchronized()
        {
            _myRepresentative = GameNetwork.MyPeer.GetComponent<FlagDominationMissionRepresentative>();
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<ScenarioRepresentative>();
        }

        public new bool CheckTimer(out int remainingTime, out int remainingWarningTime, bool forceUpdate = false)
        {
            bool flag = false;
            float f = 0f;
            if (WarmupComponent != null && MissionLobbyComponent.CurrentMultiplayerState == MissionLobbyComponent.MultiplayerGameState.WaitingFirstPlayers)
            {
                flag = !WarmupComponent.IsInWarmup;
            }
            else if (RoundComponent != null)
            {
                flag = !RoundComponent.CurrentRoundState.StateHasVisualTimer();
                f = RoundComponent.LastRoundEndRemainingTime;
            }

            if (forceUpdate || !flag)
            {
                if (flag)
                {
                    remainingTime = MathF.Ceiling(f);
                }
                else
                {
                    remainingTime = MathF.Ceiling(RemainingTime);
                }

                remainingWarningTime = GetWarningTimer();
                return true;
            }

            remainingTime = 0;
            remainingWarningTime = 0;
            return false;
        }

        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
            if (representative != null)
            {
                representative.UpdateGold(goldAmount);
            }
        }

        public override int GetGoldAmount()
        {
            if (_myRepresentative != null)
            {
                return _myRepresentative.Gold;
            }

            return 0;
        }

        public void OnBotsControlledChanged(MissionPeer missionPeer, int botAliveCount, int botTotalCount)
        {
            missionPeer.BotsUnderControlAlive = botAliveCount;
            // Reflection cause internal setter -_-
            missionPeer.GetType().GetProperty(nameof(missionPeer.BotsUnderControlTotal)).SetValue(missionPeer, botTotalCount);
            OnBotsControlledChangedEvent?.Invoke(missionPeer.GetNetworkPeer());
        }
    }
}