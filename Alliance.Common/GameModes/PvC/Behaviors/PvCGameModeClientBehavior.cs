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
using static Alliance.Common.Utilities.Logger;

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


        private void OnTeamChanged(NetworkCommunicator peer, Team oldTeam, Team newTeam)
        {
            bool isCommanderSide = newTeam.Side == (BattleSideEnum)Config.Instance.CommanderSide;
            Log($"{peer.UserName} joined the {(isCommanderSide ? "commander" : "player")}s' side.", LogLevel.Debug);
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
    }
}