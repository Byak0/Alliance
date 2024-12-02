using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Captain.Behaviors
{
    public class ALMissionMultiplayerFlagDominationClient : MissionMultiplayerGameModeFlagDominationClient, IBotControllerBehavior
    {
        private bool _informedAboutFlagRemoval;

        protected override int GetWarningTimer()
        {
            int num = 0;
            if (IsRoundInProgress)
            {
                float roundTimeForFlagRemoval = MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue() - Config.Instance.TimeBeforeFlagRemoval;
                float roundTimeForFlagRemovalWarning = roundTimeForFlagRemoval + 30f;
                if (RoundComponent.RemainingRoundTime <= roundTimeForFlagRemovalWarning && RoundComponent.RemainingRoundTime > roundTimeForFlagRemoval)
                {
                    num = MathF.Ceiling(30f - (roundTimeForFlagRemovalWarning - RoundComponent.RemainingRoundTime));
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