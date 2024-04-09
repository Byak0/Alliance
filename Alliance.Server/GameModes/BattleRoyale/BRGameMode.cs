using Alliance.Common.Extensions.ShrinkingZone.Behaviors;
using Alliance.Common.Extensions.VOIP.Behaviors;
using Alliance.Common.GameModes.BattleRoyale.Behaviors;
using Alliance.Server.GameModes.BattleRoyale.Behaviors;
using Alliance.Server.Patch.Behaviors;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.MountAndBlade.Source.Missions;

namespace Alliance.Server.GameModes.BattleRoyale
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
                    new AllianceLobbyComponent(),
                    new BRBehavior(),
                    new BRCommonBehavior(),
                    new ShrinkingZoneBehavior(),

                    new MultiplayerTimerComponent(),
                    new SpawnComponent(new BRSpawnFrameBehavior(), new BRSpawningBehavior()),
                    new AgentHumanAILogic(),
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