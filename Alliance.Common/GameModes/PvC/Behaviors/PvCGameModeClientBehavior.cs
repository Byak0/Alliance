using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.GameModes.Captain.Behaviors;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.GameModes.PvC.Behaviors
{
    public class PvCGameModeClientBehavior : ALMissionMultiplayerFlagDominationClient, IBotControllerBehavior
    {
        public PvCGameModeClientBehavior() : base()
        {
        }

        public override bool IsGameModeUsingGold => false;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            RoundComponent.OnPostRoundEnded += OnPostRoundEnd;
            MissionPeer.OnTeamChanged += OnTeamChanged;

            // Fix to allow more then 3 flags. Use Reflection to init private variable of parent class with correct amount of flags.
            FieldInfo capturePointOwners = typeof(MissionMultiplayerGameModeFlagDominationClient).GetField("_capturePointOwners", BindingFlags.Instance | BindingFlags.NonPublic);
            capturePointOwners.SetValue(this, new Team[AllCapturePoints.Count()]);
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();

            RoundComponent.OnPostRoundEnded -= OnPostRoundEnd;
            MissionPeer.OnTeamChanged -= OnTeamChanged;
        }

        public override SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer)
        {
            SpectatorCameraTypes result = SpectatorCameraTypes.Invalid;
            MissionPeer missionPeer = (GameNetwork.IsMyPeerReady ? GameNetwork.MyPeer.GetComponent<MissionPeer>() : null);
            if (!lockedToMainPlayer && missionPeer != null)
            {
                if (missionPeer.Team != Mission.SpectatorTeam)
                {
                    if (IsRoundInProgress)
                    {
                        Formation controlledFormation = missionPeer.ControlledFormation;
                        if (controlledFormation != null && controlledFormation.HasUnitsWithCondition((Agent agent) => !agent.IsPlayerControlled && agent.IsActive()))
                        {
                            result = SpectatorCameraTypes.LockToPlayerFormation;
                        }
                        else
                        {
                            result = SpectatorCameraTypes.LockToTeamMembers;
                        }
                    }
                }
                else
                {
                    result = SpectatorCameraTypes.Free;
                }
            }

            return result;
        }

        private void OnTeamChanged(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            bool isCommanderSide = newTeam.Side == (BattleSideEnum)Config.Instance.CommanderSide;
            Log($"{peer.UserName} joined the {(isCommanderSide ? "commander" : "player")}s' side.", LogLevel.Debug);
        }

        private void OnPostRoundEnd()
        {
            FormationControlModel.Instance.Clear();

            // Reset TroopLeft count after round
            MBReadOnlyList<ExtendedCharacter> extCharacterObjects = MBObjectManager.Instance.GetObjectTypeList<ExtendedCharacter>();
            foreach (ExtendedCharacter extCharacterObject in extCharacterObjects)
            {
                extCharacterObject.TroopLeft = extCharacterObject.TroopLimit;
            }
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
    }
}