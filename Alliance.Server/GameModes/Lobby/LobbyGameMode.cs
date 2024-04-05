using Alliance.Common.Extensions.VOIP.Behaviors;
using Alliance.Common.GameModes.Lobby.Behaviors;
using Alliance.Server.Extensions.DieUnderWater.Behaviors;
using Alliance.Server.Extensions.GameModeMenu.Behaviors;
using Alliance.Server.GameModes.Lobby.Behaviors;
using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.Lobby
{
    public class LobbyGameMode : MissionBasedMultiplayerGameMode
    {
        public LobbyGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("Lobby", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    new AllianceLobbyComponent(),
                    new LobbyBehavior(),
                    new LobbyClientBehavior(),
                    new PollBehavior(),
                    new VoipHandler(),
                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new LobbySpawnFrameBehavior(), new LobbySpawningBehavior()),
                    new MultiplayerAdminComponent(),
                    new AgentHumanAILogic(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new TDMScoreboardData()),
                    new DieUnderWaterBehavior()
                };
            }, true, true);
        }
    }
}
