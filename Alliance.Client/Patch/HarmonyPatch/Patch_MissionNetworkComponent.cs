using Alliance.Common.Core.Configuration.Models;
using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Patch.HarmonyPatch
{
    class Patch_MissionNetworkComponent
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_MissionNetworkComponent));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(MissionNetworkComponent).GetMethod("HandleServerEventCreateAgent",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionNetworkComponent).GetMethod(
                        nameof(Prefix_HandleServerEventCreateAgent), BindingFlags.Static | BindingFlags.Public)));
                Harmony.Patch(
                    typeof(MissionNetworkComponent).GetMethod("HandleServerEventCreateAgentVisuals",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_MissionNetworkComponent).GetMethod(
                        nameof(Prefix_HandleServerEventCreateAgentVisuals), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_MissionNetworkComponent)}", LogLevel.Error);
                Log(e.Message, LogLevel.Error);
                return false;
            }

            return true;
        }

        // Use player custom bodyproperties only if allowed
        public static bool Prefix_HandleServerEventCreateAgent(Mission __instance, CreateAgent message)
        {
            BasicCharacterObject character = message.Character;
            NetworkCommunicator peer = message.Peer;
            MissionPeer missionPeer = peer != null ? peer.GetComponent<MissionPeer>() : null;
            Team teamFromTeamIndex = Mission.MissionNetworkHelper.GetTeamFromTeamIndex(message.TeamIndex);
            AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(message.IsPlayerAgent ? missionPeer : null).Monster(message.Monster).TroopOrigin(new BasicBattleAgentOrigin(character))
            .Equipment(message.SpawnEquipment)
                .EquipmentSeed(message.BodyPropertiesSeed);
            Vec3 position = message.Position;
            AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(position);
            Vec2 vec = message.Direction;
            vec = vec.Normalized();
            AgentBuildData agentBuildData3 = agentBuildData2.InitialDirection(vec).MissionEquipment(message.MissionEquipment).Team(teamFromTeamIndex)
            .Index(message.AgentIndex)
            .MountIndex(message.MountAgentIndex)
            .IsFemale(message.IsFemale)
                .ClothingColor1(message.ClothingColor1)
                .ClothingColor2(message.ClothingColor2);
            Formation formation = null;
            if (teamFromTeamIndex != null && message.FormationIndex >= 0 && !GameNetwork.IsReplay)
            {
                formation = teamFromTeamIndex.GetFormation((FormationClass)message.FormationIndex);
                agentBuildData3.Formation(formation);
            }
            // Use peer.BodyProperties only if allowed
            if (message.IsPlayerAgent && Config.Instance.AllowCustomBody)
            {
                agentBuildData3.BodyProperties(missionPeer.Peer.BodyProperties);
                agentBuildData3.Age((int)agentBuildData3.AgentBodyProperties.Age);
            }
            else
            {
                agentBuildData3.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData3.AgentRace, agentBuildData3.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData3.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData3.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
            }

            Banner banner = teamFromTeamIndex.Banner;

            agentBuildData3.Banner(banner);
            Agent mountAgent = Mission.Current.SpawnAgent(agentBuildData3, false).MountAgent;

            return false;
        }

        // Use player custom bodyproperties only if allowed
        public static bool Prefix_HandleServerEventCreateAgentVisuals(Mission __instance, CreateAgentVisuals message)
        {
            MissionPeer component = message.Peer.GetComponent<MissionPeer>();
            BattleSideEnum side = component.Team.Side;
            BasicCharacterObject character = message.Character;
            AgentBuildData agentBuildData = new AgentBuildData(character).VisualsIndex(message.VisualsIndex).Equipment(message.Equipment).EquipmentSeed(message.BodyPropertiesSeed)
                .IsFemale(message.IsFemale)
                .ClothingColor1(character.Culture.Color)
                .ClothingColor2(character.Culture.Color2);
            // Use peer.BodyProperties only if allowed
            if (message.VisualsIndex == 0 && Config.Instance.AllowCustomBody)
            {
                agentBuildData.BodyProperties(component.Peer.BodyProperties);
            }
            else
            {
                agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, message.BodyPropertiesSeed, character.HairTags, character.BeardTags, character.TattooTags));
            }
            Mission.Current.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().SpawnAgentVisualsForPeer(component, agentBuildData, message.SelectedEquipmentSetIndex, false, message.TroopCountInFormation);

            return false;
        }

        /* Original method
        private void HandleServerEventCreateAgent(CreateAgent message)
        {
            BasicCharacterObject character = message.Character;
            NetworkCommunicator peer = message.Peer;
            MissionPeer missionPeer = ((peer != null) ? peer.GetComponent<MissionPeer>() : null);
            AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(message.IsPlayerAgent ? missionPeer : null).Monster(message.Monster).TroopOrigin(new BasicBattleAgentOrigin(character))
                .Equipment(message.SpawnEquipment)
                .EquipmentSeed(message.BodyPropertiesSeed);
            Vec3 position = message.Position;
            AgentBuildData agentBuildData2 = agentBuildData.InitialPosition(position);
            Vec2 vec = message.Direction;
            vec = vec.Normalized();
            AgentBuildData agentBuildData3 = agentBuildData2.InitialDirection(vec).MissionEquipment(message.SpawnMissionEquipment).Team(message.Team)
                .Index(message.AgentIndex)
                .MountIndex(message.MountAgentIndex)
                .IsFemale(message.IsFemale)
                .ClothingColor1(message.ClothingColor1)
                .ClothingColor2(message.ClothingColor2);
            Formation formation = null;
            if (message.Team != null && message.FormationIndex >= 0 && !GameNetwork.IsReplay)
            {
                formation = message.Team.GetFormation((FormationClass)message.FormationIndex);
                agentBuildData3.Formation(formation);
            }
            if (message.IsPlayerAgent)
            {
                agentBuildData3.BodyProperties(missionPeer.Peer.BodyProperties);
                agentBuildData3.Age((int)agentBuildData3.AgentBodyProperties.Age);
            }
            else
            {
                agentBuildData3.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData3.AgentRace, agentBuildData3.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData3.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData3.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
            }
            Banner banner = null;
            if (formation != null)
            {
                if (!string.IsNullOrEmpty(formation.BannerCode))
                {
                    if (formation.Banner == null)
                    {
                        banner = new Banner(formation.BannerCode, message.Team.Color, message.Team.Color2);
                        formation.Banner = banner;
                    }
                    else
                    {
                        banner = formation.Banner;
                    }
                }
            }
            else if (missionPeer != null)
            {
                banner = new Banner(missionPeer.Peer.BannerCode, message.Team.Color, message.Team.Color2);
            }
            agentBuildData3.Banner(banner);
            Agent mountAgent = base.Mission.SpawnAgent(agentBuildData3, false, 0).MountAgent;
        }
        
        private void HandleServerEventCreateAgentVisuals(CreateAgentVisuals message)
		{
			MissionPeer component = message.Peer.GetComponent<MissionPeer>();
			BattleSideEnum side = component.Team.Side;
			BasicCharacterObject character = message.Character;
			AgentBuildData agentBuildData = new AgentBuildData(character).VisualsIndex(message.VisualsIndex).Equipment(message.Equipment).EquipmentSeed(message.BodyPropertiesSeed)
				.IsFemale(message.IsFemale)
				.ClothingColor1(character.Culture.Color)
				.ClothingColor2(character.Culture.Color2);
			if (message.VisualsIndex == 0)
			{
				agentBuildData.BodyProperties(component.Peer.BodyProperties);
			}
			else
			{
				agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, message.BodyPropertiesSeed, character.HairTags, character.BeardTags, character.TattooTags));
			}
			base.Mission.GetMissionBehavior<MultiplayerMissionAgentVisualSpawnComponent>().SpawnAgentVisualsForPeer(component, agentBuildData, message.SelectedEquipmentSetIndex, false, message.TroopCountInFormation);
		}
        */
    }
}
