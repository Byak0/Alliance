using Alliance.Common.Core.Security.Extension;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.GameModes.CaptainX.Behaviors
{
    public class PvCWarmupSpawningBehavior : WarmupSpawningBehavior, ISpawnBehavior
    {
        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                {
                    continue;
                }

                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component == null || component.ControlledAgent != null || component.HasSpawnedAgentVisuals || component.Team == null || component.Team == base.Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(base.Mission.CurrentTime))
                {
                    continue;
                }

                IAgentVisual agentVisual = null;
                BasicCultureObject basicCultureObject = ((component.Team.Side == BattleSideEnum.Attacker) ? @object : object2);
                int selectedTroopIndex = component.SelectedTroopIndex;
                IEnumerable<MultiplayerClassDivisions.MPHeroClass> mPHeroClasses = MultiplayerClassDivisions.GetMPHeroClasses(basicCultureObject);
                MultiplayerClassDivisions.MPHeroClass mPHeroClass = ((selectedTroopIndex < 0) ? null : mPHeroClasses.ElementAt(selectedTroopIndex));
                if (mPHeroClass == null && selectedTroopIndex < 0)
                {
                    mPHeroClass = mPHeroClasses.First();
                    selectedTroopIndex = 0;
                }

                // Spawn hero for officers, troop for others
                BasicCharacterObject heroCharacter = networkPeer.IsOfficer() ? mPHeroClass.HeroCharacter : mPHeroClass.TroopCharacter;

                Equipment equipment = heroCharacter.Equipment.Clone();
                IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = MPPerkObject.GetOnSpawnPerkHandler(component)?.GetAlternativeEquipments(isPlayer: true);
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        equipment[item.Item1] = item.Item2;
                    }
                }

                MatrixFrame matrixFrame;
                if (agentVisual == null)
                {
                    matrixFrame = SpawnComponent.GetSpawnFrame(component.Team, heroCharacter.Equipment.Horse.Item != null);
                }
                else
                {
                    matrixFrame = agentVisual.GetFrame();
                    matrixFrame.rotation.MakeUnit();
                }

                AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Equipment(equipment).Team(component.Team)
                    .TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
                    .InitialPosition(in matrixFrame.origin);
                Vec2 direction = matrixFrame.rotation.f.AsVec2.Normalized();
                AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(in direction).IsFemale(component.Peer.IsFemale).BodyProperties(GetBodyProperties(component, basicCultureObject))
                    .VisualsIndex(0)
                    .ClothingColor1((component.Team == base.Mission.AttackerTeam) ? basicCultureObject.Color : basicCultureObject.ClothAlternativeColor)
                    .ClothingColor2((component.Team == base.Mission.AttackerTeam) ? basicCultureObject.Color2 : basicCultureObject.ClothAlternativeColor2);
                if (GameMode.ShouldSpawnVisualsForServer(networkPeer) && agentBuildData2.AgentVisualsIndex == 0)
                {
                    component.HasSpawnedAgentVisuals = true;
                    component.EquipmentUpdatingExpired = false;
                }

                GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData2);
            }
        }
    }
}
