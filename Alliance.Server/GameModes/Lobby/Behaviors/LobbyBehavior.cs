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
            BasicCultureObject cultureAttack = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            Banner bannerAttack = new Banner(cultureAttack.BannerKey, cultureAttack.BackgroundColor1, cultureAttack.ForegroundColor1);
            Team teamAttack = Mission.Teams.Add(BattleSideEnum.Attacker, cultureAttack.BackgroundColor1, cultureAttack.ForegroundColor1, bannerAttack, isPlayerGeneral: false, isPlayerSergeant: true, true);
            teamAttack.SetIsEnemyOf(teamAttack, true);

            BasicCultureObject cultureDef = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            Banner bannerDef = new Banner(cultureDef.BannerKey, cultureDef.BackgroundColor1, cultureDef.ForegroundColor1);
            Team teamDef = Mission.Teams.Add(BattleSideEnum.Defender, cultureDef.BackgroundColor1, cultureDef.ForegroundColor1, bannerDef, isPlayerGeneral: false, isPlayerSergeant: true, true);
            teamDef.SetIsEnemyOf(teamDef, true);
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
