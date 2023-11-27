using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Lobby.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.GameModes.Lobby.Behaviors
{
    public class LobbyBehavior : MissionMultiplayerGameModeBase
    {
        public override bool IsGameModeHidingAllAgentVisuals
        {
            get
            {
                return true;
            }
        }

        public override bool IsGameModeUsingOpposingTeams
        {
            get
            {
                return false;
            }
        }

        public override MultiplayerGameType GetMissionType()
        {
            return MultiplayerGameType.FreeForAll;
        }

        public override void AfterStart()
        {
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            Banner banner = new Banner(@object.BannerKey, @object.BackgroundColor1, @object.ForegroundColor1);
            Team team = Mission.Teams.Add(BattleSideEnum.Attacker, @object.BackgroundColor1, @object.ForegroundColor1, banner, isPlayerGeneral: false, isPlayerSergeant: true, true);
            team.SetIsEnemyOf(team, true);
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            networkPeer.AddComponent<LobbyRepresentative>();
        }

        protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
        {
            MissionPeer component = networkPeer.GetComponent<MissionPeer>();
            component.Team = Mission.AttackerTeam;
            component.Culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            // Free spawn slot of victim
            SpawnHelper.RemoveBot(affectedAgent);
        }

        public LobbyBehavior()
        {
        }
    }
}
