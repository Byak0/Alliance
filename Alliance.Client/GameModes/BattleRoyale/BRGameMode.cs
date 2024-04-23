using Alliance.Common.Extensions.ShrinkingZone.Behaviors;
using Alliance.Common.GameModes.BattleRoyale.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Client.GameModes.BattleRoyale
{
    public class BRGameMode : MissionBasedMultiplayerGameMode
    {
        public BRGameMode(string name) : base(name) { }

        [MissionMethod]
        public override void StartMultiplayerGame(string scene)
        {
            MissionState.OpenNew("BattleRoyale", new MissionInitializerRecord(scene), delegate (Mission missionController)
            {
                return new MissionBehavior[]
                {
                    MissionLobbyComponent.CreateBehavior(),

                    // Custom behaviors
                    new BRCommonBehavior(),
                    new ShrinkingZoneBehavior(),

                    // Native behaviors
                    new MultiplayerTimerComponent(),
                    new MultiplayerMissionAgentVisualSpawnComponent(),
                    new MissionLobbyEquipmentNetworkComponent(),
                    new MissionHardBorderPlacer(),
                    new MissionBoundaryPlacer(),
                    new MissionBoundaryCrossingHandler(),
                    new MultiplayerPollComponent(),
                    new MultiplayerGameNotificationsComponent(),
                    new MissionOptionsComponent(),
                    new MissionScoreboardComponent(new TDMScoreboardData())
                };
            }, true, true);
        }
    }
}
