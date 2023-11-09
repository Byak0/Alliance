using Alliance.Common.Core;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Lobby.Behaviors
{
    public class LobbyClientBehavior : MissionMultiplayerGameModeBaseClient, IBotControllerBehavior
    {
        public override bool IsGameModeUsingGold
        {
            get
            {
                return true;
            }
        }

        public override bool IsGameModeTactical
        {
            get
            {
                return false;
            }
        }

        public override bool IsGameModeUsingRoundCountdown
        {
            get
            {
                return false;
            }
        }

        public override MissionLobbyComponent.MultiplayerGameType GameType
        {
            get
            {
                return MissionLobbyComponent.MultiplayerGameType.FreeForAll;
            }
        }

        public override int GetGoldAmount()
        {
            return 2000;
        }

        public override void OnGoldAmountChangedForRepresentative(MissionRepresentativeBase representative, int goldAmount)
        {
        }

        public override void AfterStart()
        {
            Mission.SetMissionMode(MissionMode.Battle, true);
        }

        public void OnBotsControlledChanged(MissionPeer component, int aliveCount, int totalCount)
        {
        }

        public LobbyClientBehavior()
        {
        }
    }
}