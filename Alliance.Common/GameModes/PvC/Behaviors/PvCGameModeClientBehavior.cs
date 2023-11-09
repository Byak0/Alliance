using Alliance.Common.Core;
using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedCharacter.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Common.GameModes.PvC.Behaviors
{
    public class PvCGameModeClientBehavior : MissionMultiplayerGameModeFlagDominationClient, IBotControllerBehavior
    {
        private bool _informedAboutFlagRemoval;

        public PvCGameModeClientBehavior() : base()
        {
        }

        public override bool IsGameModeUsingGold => true;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            // Fix to allow more then 3 flags. Use Reflection to init private variable of parent class with correct amount of flags.
            FieldInfo capturePointOwners = typeof(MissionMultiplayerGameModeFlagDominationClient).GetField("_capturePointOwners", BindingFlags.Instance | BindingFlags.NonPublic);
            capturePointOwners.SetValue(this, new Team[AllCapturePoints.Count()]);
        }

        public override void AfterStart()
        {
            base.AfterStart();
            RoundComponent.OnPostRoundEnded += OnPostRoundEnd;
        }

        private void OnPostRoundEnd()
        {
            FormationControlModel.Instance.Clear();

            // Reset TroopLeft count after round
            MBReadOnlyList<ExtendedCharacterObject> extCharacterObjects = MBObjectManager.Instance.GetObjectTypeList<ExtendedCharacterObject>();
            foreach (ExtendedCharacterObject extCharacterObject in extCharacterObjects)
            {
                extCharacterObject.TroopLeft = extCharacterObject.TroopLimit;
            }
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<PvCRepresentative>();
        }

        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
            if (representative != null)
            {
                representative.UpdateGold(goldAmount);
            }
        }

        protected override void AddRemoveMessageHandlers(GameNetwork.NetworkMessageHandlerRegistererContainer registerer)
        {
            // Moved to GameModeHandler
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            // Prevent native code from running. It's doing stupid.
        }

        protected override int GetWarningTimer()
        {
            int num = 0;
            if (IsRoundInProgress)
            {
                float num3 = MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) - Config.Instance.TimeBeforeFlagRemoval;
                float num4 = num3 + 30f;
                if (RoundComponent.RemainingRoundTime <= num4 && RoundComponent.RemainingRoundTime > num3)
                {
                    num = MathF.Ceiling(30f - (num4 - RoundComponent.RemainingRoundTime));
                    if (!_informedAboutFlagRemoval)
                    {
                        _informedAboutFlagRemoval = true;
                        NotificationsComponent.FlagsWillBeRemovedInXSeconds(30);
                    }
                }
            }
            return num;
        }

        public override SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
        {
            SpectatorCameraTypes spectatorCameraTypes = SpectatorCameraTypes.Invalid;
            MissionPeer missionPeer = GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>() : null;
            if (!lockedToMainPlayer && missionPeer != null)
            {
                if (missionPeer.Team != Mission.SpectatorTeam)
                {
                    if (GameType == MissionLobbyComponent.MultiplayerGameType.Captain && IsRoundInProgress)
                    {
                        spectatorCameraTypes = SpectatorCameraTypes.Free;
                        //Formation controlledFormation = missionPeer.ControlledFormation;
                        //if (controlledFormation != null && controlledFormation.HasUnitsWithCondition((Agent agent) => !agent.IsPlayerControlled && agent.IsActive()))
                        //{
                        //    spectatorCameraTypes = SpectatorCameraTypes.LockToTeamMembers;
                        //}
                    }
                }
                else
                {
                    spectatorCameraTypes = SpectatorCameraTypes.Free;
                }

                //            if (missionPeer.Team != Mission.SpectatorTeam)
                //{
                //                spectatorCameraTypes = SpectatorCameraTypes.Free;
                //                if (GameType == MissionLobbyComponent.MultiplayerGameType.Captain && IsRoundInProgress)
                //                {
                //                    if (GameNetwork.MyPeer.IsCommander())
                //                    {
                //                        spectatorCameraTypes = SpectatorCameraTypes.Free;
                //                    }
                //                    else
                //                    {
                //                        spectatorCameraTypes = SpectatorCameraTypes.LockToTeamMembers;
                //                    }
                //                }
                //                else
                //                {
                //                    spectatorCameraTypes = SpectatorCameraTypes.Free;
                //                }
                //            }
                //else
                //{
                //	spectatorCameraTypes = SpectatorCameraTypes.Free;
                //}
            }
            return spectatorCameraTypes;
        }
    }
}