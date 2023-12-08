using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.TroopSpawner.Utilities
{
    public static class SpawnHelper
    {
        public static int TotalBots = 0;

        public static void RemoveBot(Agent bot, int waitTime = 10000)
        {
            Log($"Freeing slot n.{bot.Index} in {waitTime / 1000}s", LogLevel.Debug);
            Task.Run(() => DelayedRemoveBot(bot.Index, waitTime));
        }

        // Free bot slot after a delay
        private static async void DelayedRemoveBot(int index, int waitTime)
        {
            await Task.Delay(waitTime);
            AgentsInfoModel.Instance.RemoveAgentInfo(index);
        }

        public static bool SpawnBot(Team team, BasicCultureObject culture, BasicCharacterObject character, MatrixFrame? origin = null, int selectedFormation = -1, float botDifficulty = 1f, Agent.MortalityState mortalityState = Agent.MortalityState.Mortal)
        {
            try
            {
                // Find a free slot to spawn the bot
                int forcedIndex = AgentsInfoModel.Instance.GetAvailableSlotIndex();
                if (forcedIndex == -1)
                {
                    Log("Alliance : Cannot spawn - no slot available.", LogLevel.Error);
                    return false;
                }

                Log($"Alliance : Trying to spawn bot n.{TotalBots} on slot {forcedIndex} \n Team : {team.Side} | character : {character.Name} | origin : {origin} | formation : {selectedFormation}", LogLevel.Debug);

                SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
                MissionLobbyComponent missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();

                Formation form;
                if (selectedFormation > -1)
                {
                    form = team.GetFormation((FormationClass)selectedFormation);
                    if (form == null) form = new Formation(team, selectedFormation);
                }
                else
                {
                    form = team.GetFormation(character.GetFormationClass());
                    if (form == null) form = new Formation(team, character.DefaultFormationGroup);
                }

                //string orders = " | OrderLocalAveragePosition=" + form.OrderLocalAveragePosition.ToString();
                //orders += " | OrderPosition=" + form.OrderPosition.ToString();
                //orders += " | OrderPositionIsValid=" + form.OrderPositionIsValid;
                //orders += " | ArrangementOrder=" + form.ArrangementOrder.OrderEnum;
                //orders += " | AttackEntityOrderDetachment=" + form.AttackEntityOrderDetachment?.ToString();
                //orders += " | FacingOrder=" + form.FacingOrder.OrderEnum;
                //orders += " | FiringOrder=" + form.FiringOrder.OrderEnum;
                //orders += " | FormOrder=" + form.FormOrder.OrderEnum;
                //orders += " | RidingOrder=" + form.RidingOrder.OrderEnum;
                //orders += " | WeaponUsageOrder=" + form.WeaponUsageOrder.OrderEnum;

                //Log("Formation " + form.Index + " current orders : \n" + orders, 0, Debug.DebugColor.Yellow);

                MatrixFrame spawnFrame = (MatrixFrame)(origin != null ? origin : spawnComponent.GetSpawnFrame(team, character.HasMount(), false));
                AgentBuildData agentBuildData = new AgentBuildData(character).Team(team).InitialPosition(spawnFrame.origin);
                Vec2 vec = spawnFrame.rotation.f.AsVec2;
                vec = vec.Normalized();
                int randomSeed = Config.Instance.RandomizeAppearance ? MBRandom.RandomInt() : 0;
                AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).TroopOrigin(new BasicBattleAgentOrigin(character))
                    .VisualsIndex(randomSeed)
                    .EquipmentSeed(missionLobbyComponent.GetRandomFaceSeedForCharacter(character, agentBuildData.AgentVisualsIndex))
                    .ClothingColor1(team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor)
                    .ClothingColor2(team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2)
                    .Banner(team == Mission.Current.AttackerTeam ? Mission.Current.AttackerTeam.Banner : Mission.Current.DefenderTeam.Banner)
                    .Formation(form)
                    //.Formation(team.FormationsIncludingSpecialAndEmpty.FirstOrDefault())
                    .IsFemale(character.IsFemale);
                agentBuildData2.Equipment(Equipment.GetRandomEquipmentElements(character, true, false, agentBuildData2.AgentEquipmentSeed));
                agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, character.GetBodyPropertiesMin(false), character.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
                agentBuildData2.Age((int)agentBuildData2.AgentBodyProperties.Age);

                //agentBuildData.EquipmentSeed(missionLobbyComponent.GetRandomFaceSeedForCharacter(basicCharacterObject, agentBuildData.AgentVisualsIndex));
                //agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, basicCharacterObject.GetBodyPropertiesMin(false), basicCharacterObject.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, basicCharacterObject.HairTags, basicCharacterObject.BeardTags, basicCharacterObject.TattooTags));

                // Force agent index to prevent duplicate
                agentBuildData2.Index(forcedIndex);
                Agent agent = Mission.Current.SpawnAgent(agentBuildData2, false);

                if (mortalityState != Agent.MortalityState.Mortal)
                {
                    agent.SetMortalityState(mortalityState);
                    agent.MountAgent?.SetMortalityState(mortalityState);
                }

                agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
                AgentsInfoModel.Instance.AddAgentInfo(agent, botDifficulty, synchronize: true);
                agent.UpdateAgentProperties();

                // Adjust bot difficulty
                //GameNetwork.BeginBroadcastModuleEvent();
                //GameNetwork.WriteMessage(new BotDifficultyMultiplierMessage(forcedIndex, botDifficulty));
                //GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

                Log("Alliance : Spawned bot n." + TotalBots++, LogLevel.Debug);
                return true;
            }
            catch (Exception ex)
            {
                Log("Alliance : ERROR spawning bot n." + TotalBots++, LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
                return false;
            }
        }

        public static void SpawnPlayer(NetworkCommunicator networkPeer, MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler, BasicCharacterObject character, MatrixFrame? origin = null, int selectedFormation = -1, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipment = null, Agent.MortalityState mortalityState = Agent.MortalityState.Mortal, BasicCultureObject customCulture = null)
        {
            try
            {
                //SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
                MissionLobbyComponent missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();
                MissionMultiplayerGameModeBase gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();

                Formation form;
                if (selectedFormation > -1)
                {
                    form = component.Team.GetFormation((FormationClass)selectedFormation);
                    if (form == null) form = new Formation(component.Team, selectedFormation);
                }
                else
                {
                    form = component.Team.GetFormation(character.GetFormationClass());
                    if (form == null) form = new Formation(component.Team, character.DefaultFormationGroup);
                }

                BasicCultureObject culture = customCulture != null ? customCulture : component.Culture;
                uint color = component.Team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor;
                uint color2 = component.Team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2;
                uint color3 = component.Team == Mission.Current.AttackerTeam ? culture.BackgroundColor1 : culture.BackgroundColor2;
                uint color4 = component.Team == Mission.Current.AttackerTeam ? culture.ForegroundColor1 : culture.ForegroundColor2;

                Banner banner = new Banner(component.Peer.BannerCode, color3, color4);
                int randomSeed = Config.Instance.RandomizeAppearance ? MBRandom.RandomInt() : 0;
                Log("Formation = " + form.PhysicalClass.GetName(), LogLevel.Debug);
                AgentBuildData agentBuildData = new AgentBuildData(character)
                    .VisualsIndex(randomSeed)
                    .Team(component.Team)
                    .TroopOrigin(new BasicBattleAgentOrigin(character))
                    .Formation(form)
                    //.Formation(component.ControlledFormation)
                    .ClothingColor1(color)
                    .ClothingColor2(color2)
                    .Banner(banner);
                agentBuildData.MissionPeer(component);
                bool randomEquipement = true;
                Equipment equipment = randomEquipement ? Equipment.GetRandomEquipmentElements(character, randomEquipmentModifier: false, isCivilianEquipment: false, MBRandom.RandomInt()) : character.Equipment.Clone();
                IEnumerable<(EquipmentIndex, EquipmentElement)> perkAlternativeEquipment = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
                if (perkAlternativeEquipment != null)
                {
                    foreach (var item in perkAlternativeEquipment)
                    {
                        equipment[item.Item1] = item.Item2;
                    }
                }

                if (alternativeEquipment != null)
                {
                    foreach (var item in alternativeEquipment)
                    {
                        if (item.Item1 == EquipmentIndex.Horse &&
                            !agentBuildData.AgentMonster.Flags.HasFlag(AgentFlag.CanRide)) continue;
                        equipment[item.Item1] = item.Item2;
                    }
                }
                agentBuildData.Equipment(equipment);

                // Use player custom bodyproperties only if allowed
                if (Config.Instance.AllowCustomBody)
                {
                    gameMode.AddCosmeticItemsToEquipment(equipment, gameMode.GetUsedCosmeticsFromPeer(component, character));
                    agentBuildData.BodyProperties(GetBodyProperties(component, component.Culture));
                    agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
                    agentBuildData.IsFemale(component.Peer.IsFemale);
                }
                else
                {
                    agentBuildData.EquipmentSeed(missionLobbyComponent.GetRandomFaceSeedForCharacter(character, agentBuildData.AgentVisualsIndex));
                    agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyProperties(agentBuildData.AgentOverridenSpawnEquipment), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
                    agentBuildData.Age((int)agentBuildData.AgentBodyProperties.Age);
                    agentBuildData.IsFemale(character.IsFemale);
                }

                if (component.ControlledFormation != null && component.ControlledFormation.Banner == null)
                {
                    component.ControlledFormation.Banner = banner;
                }

                MatrixFrame spawnFrame = (MatrixFrame)(origin != null ? origin : spawnComponent.GetSpawnFrame(component.Team, character.HasMount(), component.SpawnCountThisRound == 0));

                agentBuildData.InitialPosition(spawnFrame.origin);
                Vec2 vec = spawnFrame.rotation.f.AsVec2;
                vec = vec.Normalized();
                agentBuildData.InitialDirection(vec);
                Agent agent = Mission.Current.SpawnAgent(agentBuildData, spawnFromAgentVisuals: true);
                agent.AddComponent(new MPPerksAgentComponent(agent));
                agent.MountAgent?.UpdateAgentProperties();
                float num4 = onSpawnPerkHandler?.GetHitpoints(true) ?? 0f;
                // Additional health for officers
                if (networkPeer.IsOfficer())
                {
                    agent.HealthLimit *= Config.Instance.OfficerHPMultip;
                }
                agent.HealthLimit += num4;
                agent.Health = agent.HealthLimit;

                agent.WieldInitialWeapons();

                if (mortalityState != Agent.MortalityState.Mortal)
                {
                    agent.SetMortalityState(mortalityState);
                    agent.MountAgent?.SetMortalityState(mortalityState);
                }


                if (agent.Formation != null)
                {
                    MissionPeer temp = agent.MissionPeer;
                    agent.MissionPeer = null;
                    agent.Formation.OnUndetachableNonPlayerUnitAdded(agent);
                    agent.MissionPeer = temp;
                }

                component.SpawnCountThisRound++;

                Log("Alliance : Spawned player " + networkPeer.UserName, LogLevel.Information);
            }
            catch (Exception ex)
            {
                Log("Alliance : ERROR spawning player " + networkPeer.UserName, LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        public static void SpawnPlayerPreview(NetworkCommunicator player, BasicCultureObject culture)
        {
            try
            {
                SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
                MissionMultiplayerGameModeBase gameMode = Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBase>();
                MissionPeer peer = player.GetComponent<MissionPeer>();
                if (peer != null && peer.ControlledAgent == null && !peer.HasSpawnedAgentVisuals && peer.Team != null && peer.Team != Mission.Current.SpectatorTeam && peer.TeamInitialPerkInfoReady && peer.SpawnTimer.Check(Mission.Current.CurrentTime))
                {
                    int num = peer.SelectedTroopIndex;
                    IEnumerable<MultiplayerClassDivisions.MPHeroClass> mpheroClasses = MultiplayerClassDivisions.GetMPHeroClasses(culture);
                    MultiplayerClassDivisions.MPHeroClass mpheroClass = num < 0 ? null : mpheroClasses.ElementAt(num);
                    if (mpheroClass == null && num < 0)
                    {
                        mpheroClass = mpheroClasses.First();
                        num = 0;
                    }

                    BasicCharacterObject character = mpheroClass.TroopCharacter;

                    // If player is officer, assign a HeroCharacter instead
                    if (player.IsOfficer())
                    {
                        character = mpheroClass.HeroCharacter;
                        Log("Alliance - " + peer.DisplayedName + " is officer.", LogLevel.Debug);
                    }

                    Log($"Alliance - {peer.DisplayedName} will spawn as {character.StringId}", LogLevel.Debug);

                    Equipment equipment = character.Equipment.Clone(false);
                    MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(peer);
                    IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(true);
                    if (enumerable != null)
                    {
                        foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in enumerable)
                        {
                            equipment[valueTuple.Item1] = valueTuple.Item2;
                        }
                    }
                    MatrixFrame matrixFrame;
                    matrixFrame = spawnComponent.GetSpawnFrame(peer.Team, character.Equipment.Horse.Item != null, false);
                    AgentBuildData agentBuildData = new AgentBuildData(character).MissionPeer(peer).Equipment(equipment).Team(peer.Team)
                        .TroopOrigin(new BasicBattleAgentOrigin(character))
                        .InitialPosition(matrixFrame.origin);
                    Vec2 vec = matrixFrame.rotation.f.AsVec2;
                    vec = vec.Normalized();
                    AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(vec).IsFemale(peer.Peer.IsFemale);

                    // If custom body is allowed, retrieve player's choice
                    if (Config.Instance.AllowCustomBody) agentBuildData2.BodyProperties(GetBodyProperties(peer, culture));

                    agentBuildData2.VisualsIndex(0)
                        .ClothingColor1(peer.Team == Mission.Current.AttackerTeam ? culture.Color : culture.ClothAlternativeColor)
                        .ClothingColor2(peer.Team == Mission.Current.AttackerTeam ? culture.Color2 : culture.ClothAlternativeColor2);
                    gameMode.HandleAgentVisualSpawning(player, agentBuildData2, 0, Config.Instance.AllowCustomBody);

                    Log("Alliance : Spawned visuals for player " + peer.Name, LogLevel.Information);
                }
            }
            catch (Exception ex)
            {
                Log("Alliance : ERROR spawning visuals for player " + player.UserName, LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
            }
        }

        public static BodyProperties GetBodyProperties(MissionPeer missionPeer, BasicCultureObject cultureLimit)
        {
            NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
            MissionLobbyComponent missionLobbyComponent = Mission.Current.GetMissionBehavior<MissionLobbyComponent>();

            if (networkPeer != null)
            {
                return networkPeer.PlayerConnectionInfo.GetParameter<PlayerData>("PlayerData").BodyProperties;
            }

            SpawnComponent spawnComponent = Mission.Current.GetMissionBehavior<SpawnComponent>();
            Debug.FailedAssert("networkCommunicator != null", "C:\\Develop\\MB3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Missions\\Multiplayer\\SpawnBehaviors\\SpawningBehaviors\\SpawningBehaviorBase.cs", "GetBodyProperties", 366);
            Team team = missionPeer.Team;
            BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(cultureLimit).ToList().GetRandomElement()
                .TroopCharacter;
            MatrixFrame spawnFrame = spawnComponent.GetSpawnFrame(team, troopCharacter.HasMount(), isInitialSpawn: true);
            AgentBuildData agentBuildData = new AgentBuildData(troopCharacter).Team(team).InitialPosition(in spawnFrame.origin);
            Vec2 direction = spawnFrame.rotation.f.AsVec2.Normalized();
            AgentBuildData agentBuildData2 = agentBuildData.InitialDirection(in direction).TroopOrigin(new BasicBattleAgentOrigin(troopCharacter)).EquipmentSeed(missionLobbyComponent.GetRandomFaceSeedForCharacter(troopCharacter))
                .ClothingColor1(team.Side == BattleSideEnum.Attacker ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2(team.Side == BattleSideEnum.Attacker ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2)
                .IsFemale(troopCharacter.IsFemale);
            agentBuildData2.Equipment(Equipment.GetRandomEquipmentElements(troopCharacter, true, isCivilianEquipment: false, agentBuildData2.AgentEquipmentSeed));
            agentBuildData2.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData2.AgentRace, agentBuildData2.AgentIsFemale, troopCharacter.GetBodyPropertiesMin(), troopCharacter.GetBodyPropertiesMax(), (int)agentBuildData2.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData2.AgentEquipmentSeed, troopCharacter.HairTags, troopCharacter.BeardTags, troopCharacter.TattooTags));
            return agentBuildData2.AgentBodyProperties;
        }

        // Return a linear troop cost from minCost to MaxCost, depending on TroopMultiplier
        public static int GetTroopCost(BasicCharacterObject character, float difficulty = 1f)
        {
            float multiplier = 0.75f + (difficulty - 0.5f) * (1.5f - 0.75f) / (2.5f - 0.5f);

            if (MultiplayerClassDivisions.GetMPHeroClassForCharacter(character)?.TroopMultiplier is null)
            {
                Log("ERROR in GetTroopCost of " + character?.Name + " : Could not find associated TroopMultiplier.", LogLevel.Error);
                return 1;
            }

            return (int)(multiplier * (Config.Instance.MinTroopCost + (Config.Instance.MaxTroopCost - Config.Instance.MinTroopCost) * (1 - MultiplayerClassDivisions.GetMPHeroClassForCharacter(character).TroopMultiplier)));
        }
    }
}
