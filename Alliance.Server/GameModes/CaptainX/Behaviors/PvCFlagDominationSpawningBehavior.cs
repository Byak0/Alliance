using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.GameModes.CaptainX.Behaviors
{
    internal class PvCFlagDominationSpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
    {
        public PvCFlagDominationSpawningBehavior()
        {
            Debug.Print("PvC - New PvCFlagDominationSpawningBehavior...", 0, Debug.DebugColor.Green);
            _enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
            _roundController = new MultiplayerRoundController();
            _flagDominationMissionController = new MissionMultiplayerFlagDomination(MissionLobbyComponent.MultiplayerGameType.Captain);
        }

        private const int EnforcedSpawnTimeInSeconds = 15;

        private float _spawningTimer;

        private bool _spawningTimerTicking;

        private bool _haveBotsBeenSpawned;

        private bool _roundInitialSpawnOver;

        private MissionMultiplayerFlagDomination _flagDominationMissionController;

        private MultiplayerRoundController _roundController;

        private readonly List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers;

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            _flagDominationMissionController = Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
            _roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
            _roundController.OnRoundStarted += RequestStartSpawnSession;
            _roundController.OnRoundEnding += RequestStopSpawnSession;
            _roundController.OnRoundEnding += SetRemainingAgentsInvulnerable;
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
            {
                _roundController.EnableEquipmentUpdate();
            }
            base.OnAllAgentsFromPeerSpawnedFromVisuals += OnAllAgentsFromPeerSpawnedFromVisuals;
            base.OnPeerSpawnedFromVisuals += OnPeerSpawnedFromVisuals;
        }

        public override void Clear()
        {
            base.Clear();
            _roundController.OnRoundStarted -= RequestStartSpawnSession;
            _roundController.OnRoundEnding -= SetRemainingAgentsInvulnerable;
            _roundController.OnRoundEnding -= RequestStopSpawnSession;
            base.OnAllAgentsFromPeerSpawnedFromVisuals -= OnAllAgentsFromPeerSpawnedFromVisuals;
            base.OnPeerSpawnedFromVisuals -= OnPeerSpawnedFromVisuals;
        }

        public override void OnTick(float dt)
        {
            if (_spawningTimerTicking)
            {
                _spawningTimer += dt;
            }
            if (IsSpawningEnabled)
            {
                if (!_roundInitialSpawnOver && IsRoundInProgress())
                {
                    foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                        if (component?.Team != null && component.Team.Side != BattleSideEnum.None)
                        {
                            SpawnComponent.SetEarlyAgentVisualsDespawning(component);
                        }
                    }
                    _roundInitialSpawnOver = true;
                    Mission.AllowAiTicking = true;
                }
                SpawnAgents();
                if (_roundInitialSpawnOver && _flagDominationMissionController.GameModeUsesSingleSpawning && _spawningTimer > MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue())
                {
                    IsSpawningEnabled = false;
                    _spawningTimer = 0f;
                    _spawningTimerTicking = false;
                }
            }
            base.OnTick(dt);
        }

        public override void RequestStartSpawnSession()
        {
            if (!IsSpawningEnabled)
            {
                Mission.Current.SetBattleAgentCount(-1);
                IsSpawningEnabled = true;
                _haveBotsBeenSpawned = false;
                _spawningTimerTicking = true;
                ResetSpawnCounts();
                ResetSpawnTimers();
            }
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject cultureTeam1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
            BasicCultureObject cultureTeam2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
            int numberOfBotsTeam1 = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
            int numberOfBotsTeam2 = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();
            int numberOfBotsPerFormation = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue();
            if (!_haveBotsBeenSpawned && (numberOfBotsTeam1 > 0 || numberOfBotsTeam2 > 0))
            {
                Mission.Current.AllowAiTicking = false;
                List<string> list = new List<string>
                { "11.8.1.4345.4345.770.774.1.0.0.133.7.5.512.512.784.769.1.0.0",
                  "11.8.1.4345.4345.770.774.1.0.0.156.7.5.512.512.784.769.1.0.0",
                  "11.8.1.4345.4345.770.774.1.0.0.155.7.5.512.512.784.769.1.0.0",
                  "11.8.1.4345.4345.770.774.1.0.0.158.7.5.512.512.784.769.1.0.0",
                  "11.8.1.4345.4345.770.774.1.0.0.118.7.5.512.512.784.769.1.0.0",
                  "11.8.1.4345.4345.770.774.1.0.0.149.7.5.512.512.784.769.1.0.0" };
                foreach (Team team in Mission.Teams)
                {
                    //if (team.Side != BattleSideEnum.Attacker && team.Side != BattleSideEnum.Defender)
                    if (team != Mission.AttackerTeam && team != Mission.DefenderTeam)
                    {
                        continue;
                    }
                    BasicCultureObject teamCulture = team == Mission.AttackerTeam ? cultureTeam1 : cultureTeam2;
                    int teamBotsNum = team == Mission.AttackerTeam ? numberOfBotsTeam1 : numberOfBotsTeam2;
                    int num = 0;
                    for (int i = 0; i < teamBotsNum; i++)
                    {
                        Formation formation = null;
                        if (numberOfBotsPerFormation > 0)
                        {
                            while (formation == null || formation.PlayerOwner != null)
                            {
                                // Fix for captain > 6
                                FormationClass formationClass = (FormationClass)(num % team.FormationsIncludingEmpty.Count);
                                formation = team.GetFormation(formationClass);
                                num++;
                            }
                        }
                        if (formation != null)
                        {
                            formation.BannerCode = list[(num - 1) % list.Count]; // Fix for captain > 6
                        }
                        MultiplayerClassDivisions.MPHeroClass mPBotsHero = MultiplayerClassDivisions.GetMPHeroClasses().GetRandomElementWithPredicate((x) => x.Culture == teamCulture);
                        BasicCharacterObject heroCharacter = mPBotsHero.HeroCharacter;
                        AgentBuildData agentBuildData = new AgentBuildData(heroCharacter)
                            .Team(team)
                            .VisualsIndex(0)
                            .Formation(formation)
                            .IsFemale(heroCharacter.IsFemale)
                            .Equipment(mPBotsHero.HeroCharacter.Equipment)
                            .TroopOrigin(new BasicBattleAgentOrigin(heroCharacter))
                            .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(heroCharacter))
                            .ClothingColor1(team.Side == BattleSideEnum.Attacker ? teamCulture.Color : teamCulture.ClothAlternativeColor)
                            .ClothingColor2(team.Side == BattleSideEnum.Attacker ? teamCulture.Color2 : teamCulture.ClothAlternativeColor2);
                        if (numberOfBotsPerFormation == 0)
                        {
                            MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(team, mPBotsHero.HeroCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, isInitialSpawn: true);
                            agentBuildData.InitialPosition(in spawnFrame.origin);
                            Vec2 direction = spawnFrame.rotation.f.AsVec2.Normalized();
                            agentBuildData.InitialDirection(in direction);
                        }
                        agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, heroCharacter.GetBodyPropertiesMin(), heroCharacter.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, heroCharacter.HairTags, heroCharacter.BeardTags, heroCharacter.TattooTags));
                        Agent agent = Mission.SpawnAgent(agentBuildData);
                        agent.SetWatchState(Agent.WatchState.Alarmed);
                        agent.WieldInitialWeapons();
                        if (numberOfBotsPerFormation > 0)
                        {
                            int finalNumOfBotsFormation = MathF.Ceiling(numberOfBotsPerFormation * mPBotsHero.TroopMultiplier);
                            for (int j = 0; j < finalNumOfBotsFormation; j++)
                            {
                                SpawnBotInBotFormation(j + 1, team, teamCulture, mPBotsHero.TroopCharacter, formation);
                            }
                            BotFormationSpawned(team);
                            formation.SetControlledByAI(isControlledByAI: true);
                            formation.SetMovementOrder(MovementOrder.MovementOrderCharge);
                        }
                    }
                    if (teamBotsNum > 0 && team.FormationsIncludingEmpty.AnyQ((f) => f.CountOfUnits > 0))
                    {
                        TeamAIGeneral teamAIGeneral = new TeamAIGeneral(Mission.Current, team);
                        teamAIGeneral.AddTacticOption(new TacticSergeantMPBotTactic(team));
                        //team2.AddTeamAI(teamAIGeneral); // Disable TeamAI causing crash
                    }
                }
                AllBotFormationsSpawned();
                _haveBotsBeenSpawned = true;
            }
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                {
                    continue;
                }
                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                Team peerTeam = component.Team;
                if (peerTeam == null || peerTeam.Side == BattleSideEnum.None || numberOfBotsPerFormation == 0 && CheckIfEnforcedSpawnTimerExpiredForPeer(component))
                {
                    continue;
                }
                if (component.ControlledAgent != null || component.HasSpawnedAgentVisuals || peerTeam == null || peerTeam == Mission.SpectatorTeam || !component.TeamInitialPerkInfoReady || !component.SpawnTimer.Check(Mission.CurrentTime))
                {
                    continue;
                }
                BasicCultureObject peerCulture = peerTeam == Mission.AttackerTeam ? cultureTeam1 : cultureTeam2;
                MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
                Formation formation2 = component.ControlledFormation;
                if (numberOfBotsPerFormation == 0)
                {
                    CreateEnforcedSpawnTimerForPeer(component, EnforcedSpawnTimeInSeconds);
                }
                else if (formation2 == null)
                {
                    FormationClass formationIndex = peerTeam.FormationsIncludingSpecialAndEmpty
                        .First((x) => x.PlayerOwner == null && !x.ContainsAgentVisuals && x.CountOfUnits == 0).FormationIndex;
                    formation2 = peerTeam.GetFormation(formationIndex);
                    formation2.ContainsAgentVisuals = true;
                    if (string.IsNullOrEmpty(formation2.BannerCode))
                    {
                        formation2.BannerCode = component.Peer.BannerCode;
                    }
                }
                BasicCharacterObject heroCharacter2 = mPHeroClassForPeer.HeroCharacter;
                AgentBuildData agentBuildData2 = new AgentBuildData(heroCharacter2).MissionPeer(component).Team(peerTeam)
                    .VisualsIndex(0)
                    .Formation(formation2)
                    .IsFemale(component.Peer.IsFemale)
                    .MakeUnitStandOutOfFormationDistance(5f)
                    .BodyProperties(GetBodyProperties(component, peerTeam == Mission.AttackerTeam ? cultureTeam1 : cultureTeam2))
                    .ClothingColor1(peerTeam == Mission.AttackerTeam ? peerCulture.Color : peerCulture.ClothAlternativeColor)
                    .ClothingColor2(peerTeam == Mission.AttackerTeam ? peerCulture.Color2 : peerCulture.ClothAlternativeColor2);
                MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
                Equipment equipment = heroCharacter2.Equipment.Clone();
                IEnumerable<(EquipmentIndex, EquipmentElement)> enumerable = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: true);
                if (enumerable != null)
                {
                    foreach (var item in enumerable)
                    {
                        equipment[item.Item1] = item.Item2;
                    }
                }
                int amountOfAgentVisualsForPeer = component.GetAmountOfAgentVisualsForPeer();
                bool flag = amountOfAgentVisualsForPeer > 0;
                agentBuildData2.Equipment(equipment);
                if (numberOfBotsPerFormation == 0)
                {
                    if (!flag)
                    {
                        MatrixFrame spawnFrame2 = SpawnComponent.GetSpawnFrame(peerTeam, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, isInitialSpawn: true);
                        agentBuildData2.InitialPosition(in spawnFrame2.origin);
                        Vec2 direction = spawnFrame2.rotation.f.AsVec2.Normalized();
                        agentBuildData2.InitialDirection(in direction);
                    }
                    else
                    {
                        MatrixFrame frame = component.GetAgentVisualForPeer(0).GetFrame();
                        agentBuildData2.InitialPosition(in frame.origin);
                        Vec2 direction = frame.rotation.f.AsVec2.Normalized();
                        agentBuildData2.InitialDirection(in direction);
                    }
                }
                if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
                {
                    AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(component, agentBuildData2, component.SelectedTroopIndex);
                }
                GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData2);
                component.ControlledFormation = formation2;
                if (numberOfBotsPerFormation == 0)
                {
                    continue;
                }
                int troopCount = MPPerkObject.GetTroopCount(mPHeroClassForPeer, onSpawnPerkHandler);
                IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments = onSpawnPerkHandler?.GetAlternativeEquipments(isPlayer: false);
                for (int k = 0; k < troopCount; k++)
                {
                    if (k + 1 >= amountOfAgentVisualsForPeer)
                    {
                        flag = false;
                    }
                    SpawnBotVisualsInPlayerFormation(component, k + 1, peerTeam, peerCulture, mPHeroClassForPeer.TroopCharacter.StringId, formation2, flag, troopCount, alternativeEquipments);
                }
            }
        }

        private new void OnPeerSpawnedFromVisuals(MissionPeer peer)
        {
            if (peer.ControlledFormation != null)
            {
                peer.ControlledAgent.Team.AssignPlayerAsSergeantOfFormation(peer, peer.ControlledFormation.FormationIndex);
            }
        }

        private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
        {
            if (peer.ControlledFormation != null)
            {
                peer.ControlledFormation.OnFormationDispersed();
                peer.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(peer.ControlledAgent));
                NetworkCommunicator networkPeer = peer.GetNetworkPeer();
                if (peer.BotsUnderControlAlive != 0 || peer.BotsUnderControlTotal != 0)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
                    Mission.GetMissionBehavior<MissionMultiplayerGameModeFlagDominationClient>().OnBotsControlledChanged(peer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal);
                }
                if (peer.Team == Mission.AttackerTeam)
                {
                    Mission.NumOfFormationsSpawnedTeamOne++;
                }
                else
                {
                    Mission.NumOfFormationsSpawnedTeamTwo++;
                }
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SetSpawnedFormationCount(Mission.NumOfFormationsSpawnedTeamOne, Mission.NumOfFormationsSpawnedTeamTwo));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        private void BotFormationSpawned(Team team)
        {
            if (team == Mission.AttackerTeam)
            {
                Mission.NumOfFormationsSpawnedTeamOne++;
            }
            else if (team == Mission.DefenderTeam)
            {
                Mission.NumOfFormationsSpawnedTeamTwo++;
            }
        }

        private void AllBotFormationsSpawned()
        {
            if (Mission.NumOfFormationsSpawnedTeamOne != 0 || Mission.NumOfFormationsSpawnedTeamTwo != 0)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SetSpawnedFormationCount(Mission.NumOfFormationsSpawnedTeamOne, Mission.NumOfFormationsSpawnedTeamTwo));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
        {
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue() == 0)
            {
                if (!_roundController.IsRoundInProgress)
                {
                    return false;
                }
                if (!lobbyPeer.HasSpawnTimerExpired && lobbyPeer.SpawnTimer.Check(Mission.Current.CurrentTime))
                {
                    lobbyPeer.HasSpawnTimerExpired = true;
                }
                return lobbyPeer.HasSpawnTimerExpired;
            }
            return false;
        }

        protected override bool IsRoundInProgress()
        {
            return _roundController.IsRoundInProgress;
        }

        private void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
        {
            if (!_enforcedSpawnTimers.Any((pair) => pair.Key == peer))
            {
                _enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(Mission.CurrentTime, durationInSeconds)));
                Debug.Print("EST for " + peer.Name + " set to " + durationInSeconds + " seconds.", 0, Debug.DebugColor.Yellow, 64uL);
            }
        }

        private bool CheckIfEnforcedSpawnTimerExpiredForPeer(MissionPeer peer)
        {
            KeyValuePair<MissionPeer, Timer> keyValuePair = _enforcedSpawnTimers.FirstOrDefault((pr) => pr.Key == peer);
            if (keyValuePair.Key == null)
            {
                return false;
            }
            if (peer.ControlledAgent != null)
            {
                _enforcedSpawnTimers.RemoveAll((p) => p.Key == peer);
                Debug.Print("EST for " + peer.Name + " is no longer valid (spawned already).", 0, Debug.DebugColor.Yellow, 64uL);
                return false;
            }
            Timer value = keyValuePair.Value;
            if (peer.HasSpawnedAgentVisuals && value.Check(Mission.Current.CurrentTime))
            {
                SpawnComponent.SetEarlyAgentVisualsDespawning(peer);
                _enforcedSpawnTimers.RemoveAll((p) => p.Key == peer);
                Debug.Print("EST for " + peer.Name + " has expired.", 0, Debug.DebugColor.Yellow, 64uL);
                return true;
            }
            return false;
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
            _enforcedSpawnTimers.Clear();
            _roundInitialSpawnOver = false;
        }

        protected void SpawnBotInBotFormation(int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, BasicCharacterObject character, Formation formation)
        {
            AgentBuildData agentBuildData = new AgentBuildData(character).Team(agentTeam).TroopOrigin(new BasicBattleAgentOrigin(character)).VisualsIndex(visualsIndex)
                .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(character, visualsIndex))
                .Formation(formation)
                .IsFemale(character.IsFemale)
                .ClothingColor1(agentTeam.Side == BattleSideEnum.Attacker ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2(agentTeam.Side == BattleSideEnum.Attacker ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            agentBuildData.Equipment(Equipment.GetRandomEquipmentElements(character, !(Game.Current.GameType is MultiplayerGame), isCivilianEquipment: false, agentBuildData.AgentEquipmentSeed));
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, character.GetBodyPropertiesMin(), character.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, character.HairTags, character.BeardTags, character.TattooTags));
            Agent agent = Mission.SpawnAgent(agentBuildData);
            MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
            agent.AIStateFlags |= Agent.AIStateFlag.Alarmed;
            agent.AgentDrivenProperties.ArmorHead = mPHeroClassForCharacter.ArmorValue;
            agent.AgentDrivenProperties.ArmorTorso = mPHeroClassForCharacter.ArmorValue;
            agent.AgentDrivenProperties.ArmorArms = mPHeroClassForCharacter.ArmorValue;
            agent.AgentDrivenProperties.ArmorLegs = mPHeroClassForCharacter.ArmorValue;
        }

        protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool _, int totalCount, IEnumerable<(EquipmentIndex, EquipmentElement)> alternativeEquipments)
        {
            BasicCharacterObject @object = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
            AgentBuildData agentBuildData = new AgentBuildData(@object).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
                .TroopOrigin(new BasicBattleAgentOrigin(@object))
                .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(@object, visualsIndex))
                .Formation(formation)
                .IsFemale(@object.IsFemale)
                .ClothingColor1(agentTeam.Side == BattleSideEnum.Attacker ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2(agentTeam.Side == BattleSideEnum.Attacker ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, !(Game.Current.GameType is MultiplayerGame), isCivilianEquipment: false, MBRandom.RandomInt());
            if (alternativeEquipments != null)
            {
                foreach (var alternativeEquipment in alternativeEquipments)
                {
                    randomEquipmentElements[alternativeEquipment.Item1] = alternativeEquipment.Item2;
                }
            }
            agentBuildData.Equipment(randomEquipmentElements);
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, @object.GetBodyPropertiesMin(), @object.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, @object.HairTags, @object.BeardTags, @object.TattooTags));
            NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
            if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
            {
                AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(missionPeer, agentBuildData, -1, isBot: true, totalCount);
            }
            GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, useCosmetics: false);
        }

        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }
    }
}
