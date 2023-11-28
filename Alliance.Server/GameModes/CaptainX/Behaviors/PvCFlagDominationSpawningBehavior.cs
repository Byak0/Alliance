using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
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
            _flagDominationMissionController = new MissionMultiplayerFlagDomination(MultiplayerGameType.Captain);
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
            BasicCultureObject @object = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            BasicCultureObject object2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            int intValue = MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                if (networkCommunicator.IsSynchronized && component.Team != null && component.Team.Side != BattleSideEnum.None && (intValue != 0 || !CheckIfEnforcedSpawnTimerExpiredForPeer(component)))
                {
                    Team team = component.Team;
                    bool flag = team == Mission.AttackerTeam;
                    Team defenderTeam = Mission.DefenderTeam;
                    BasicCultureObject basicCultureObject = (flag ? @object : object2);
                    MultiplayerClassDivisions.MPHeroClass mpheroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component, false);
                    int num = ((_flagDominationMissionController.GetMissionType() == MultiplayerGameType.Battle) ? mpheroClassForPeer.TroopBattleCost : mpheroClassForPeer.TroopCost);
                    if (component.ControlledAgent == null && !component.HasSpawnedAgentVisuals && component.Team != null && component.Team != Mission.SpectatorTeam && component.TeamInitialPerkInfoReady && component.SpawnTimer.Check(Mission.CurrentTime))
                    {
                        int currentGoldForPeer = _flagDominationMissionController.GetCurrentGoldForPeer(component);
                        if (mpheroClassForPeer == null || (_flagDominationMissionController.UseGold() && num > currentGoldForPeer))
                        {
                            if (currentGoldForPeer >= MultiplayerClassDivisions.GetMinimumTroopCost(basicCultureObject) && component.SelectedTroopIndex != 0)
                            {
                                component.SelectedTroopIndex = 0;
                                GameNetwork.BeginBroadcastModuleEvent();
                                GameNetwork.WriteMessage(new UpdateSelectedTroopIndex(networkCommunicator, 0));
                                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkCommunicator);
                            }
                        }
                        else
                        {
                            if (intValue == 0)
                            {
                                CreateEnforcedSpawnTimerForPeer(component, 15);
                            }
                            Formation formation = component.ControlledFormation;
                            if (intValue > 0 && formation == null)
                            {
                                FormationClass formationIndex = Enumerable.First<Formation>(component.Team.FormationsIncludingEmpty, (Formation x) => x.PlayerOwner == null && !x.ContainsAgentVisuals && x.CountOfUnits == 0).FormationIndex;
                                formation = team.GetFormation(formationIndex);
                                formation.ContainsAgentVisuals = true;
                                if (string.IsNullOrEmpty(formation.BannerCode))
                                {
                                    formation.BannerCode = component.Peer.BannerCode;
                                }
                            }
                            BasicCharacterObject heroCharacter = mpheroClassForPeer.HeroCharacter;
                            AgentBuildData agentBuildData = new AgentBuildData(heroCharacter).MissionPeer(component).Team(component.Team).VisualsIndex(0)
                                .Formation(formation)
                                .MakeUnitStandOutOfFormationDistance(7f)
                                .IsFemale(component.Peer.IsFemale)
                                .BodyProperties(GetBodyProperties(component, (component.Team == Mission.AttackerTeam) ? @object : object2))
                                .ClothingColor1((team == Mission.AttackerTeam) ? basicCultureObject.Color : basicCultureObject.ClothAlternativeColor)
                                .ClothingColor2((team == Mission.AttackerTeam) ? basicCultureObject.Color2 : basicCultureObject.ClothAlternativeColor2);
                            MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
                            Equipment equipment = heroCharacter.Equipment.Clone(false);
                            IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable = ((onSpawnPerkHandler != null) ? onSpawnPerkHandler.GetAlternativeEquipments(true) : null);
                            if (enumerable != null)
                            {
                                foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in enumerable)
                                {
                                    equipment[valueTuple.Item1] = valueTuple.Item2;
                                }
                            }
                            bool flag2 = false;
                            agentBuildData.Equipment(equipment);
                            if (intValue == 0 && !flag2)
                            {
                                MatrixFrame spawnFrame = SpawnComponent.GetSpawnFrame(component.Team, equipment[EquipmentIndex.ArmorItemEndSlot].Item != null, true);
                                agentBuildData.InitialPosition(spawnFrame.origin);
                                AgentBuildData agentBuildData2 = agentBuildData;
                                Vec2 vec = spawnFrame.rotation.f.AsVec2;
                                vec = vec.Normalized();
                                agentBuildData2.InitialDirection(vec);
                            }
                            if (GameMode.ShouldSpawnVisualsForServer(networkCommunicator) && agentBuildData.AgentVisualsIndex == 0)
                            {
                                component.HasSpawnedAgentVisuals = true;
                                component.EquipmentUpdatingExpired = false;
                            }
                            GameMode.HandleAgentVisualSpawning(networkCommunicator, agentBuildData, 0, true);
                            component.ControlledFormation = formation;
                            if (intValue > 0)
                            {
                                int troopCount = MPPerkObject.GetTroopCount(mpheroClassForPeer, intValue, onSpawnPerkHandler);
                                IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> enumerable2 = ((onSpawnPerkHandler != null) ? onSpawnPerkHandler.GetAlternativeEquipments(false) : null);
                                for (int i = 0; i < troopCount; i++)
                                {
                                    SpawnBotVisualsInPlayerFormation(component, i + 1, team, basicCultureObject, mpheroClassForPeer.TroopCharacter.StringId, formation, flag2, troopCount, enumerable2);
                                }
                            }
                        }
                    }
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

        protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool updateExistingAgentVisuals, int totalCount, IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> alternativeEquipments)
        {
            BasicCharacterObject @object = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
            AgentBuildData agentBuildData = new AgentBuildData(@object).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
                .TroopOrigin(new BasicBattleAgentOrigin(@object))
                .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(@object, visualsIndex))
                .Formation(formation)
                .IsFemale(@object.IsFemale)
                .ClothingColor1((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2((agentTeam.Side == BattleSideEnum.Attacker) ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, !GameNetwork.IsMultiplayer, false, MBRandom.RandomInt());
            if (alternativeEquipments != null)
            {
                foreach (ValueTuple<EquipmentIndex, EquipmentElement> valueTuple in alternativeEquipments)
                {
                    randomEquipmentElements[valueTuple.Item1] = valueTuple.Item2;
                }
            }
            agentBuildData.Equipment(randomEquipmentElements);
            agentBuildData.BodyProperties(BodyProperties.GetRandomBodyProperties(agentBuildData.AgentRace, agentBuildData.AgentIsFemale, @object.GetBodyPropertiesMin(false), @object.GetBodyPropertiesMax(), (int)agentBuildData.AgentOverridenSpawnEquipment.HairCoverType, agentBuildData.AgentEquipmentSeed, @object.HairTags, @object.BeardTags, @object.TattooTags));
            NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
            if (GameMode.ShouldSpawnVisualsForServer(networkPeer) && agentBuildData.AgentVisualsIndex == 0)
            {
                missionPeer.HasSpawnedAgentVisuals = true;
                missionPeer.EquipmentUpdatingExpired = false;
            }
            GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, false);
        }

        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }
    }
}
