using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;

namespace Alliance.Server.GameModes.PvC.Behaviors
{
    public class PvCSpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
    {
        public PvCSpawningBehavior()
        {
            _enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
        }

        public override void Initialize(SpawnComponent spawnComponent)
        {
            base.Initialize(spawnComponent);
            _flagDominationMissionController = Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
            _roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
            _roundController.OnRoundStarted += RequestStartSpawnSession;
            _roundController.OnRoundEnding += RequestStopSpawnSession;
            _roundController.OnRoundEnding += SetRemainingAgentsInvulnerable;
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) == 0)
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
                    foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
                    {
                        MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
                        if ((component?.Team) != null && component.Team.Side != BattleSideEnum.None)
                        {
                            SpawnComponent.SetEarlyAgentVisualsDespawning(component, true);
                        }
                    }
                    _roundInitialSpawnOver = true;
                    Mission.AllowAiTicking = true;
                }
                OnTickAux(dt);
                SpawnAgents();
                if (_roundInitialSpawnOver && _flagDominationMissionController.GameModeUsesSingleSpawning && _spawningTimer > MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions))
                {
                    IsSpawningEnabled = false;
                    _spawningTimer = 0f;
                    _spawningTimerTicking = false;
                    EnableMortalityAfterTimer(30000);
                }
            }
        }

        public void OnTickAux(float dt)
        {
            // Loop on players and spawn them
            foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
            {
                if (!networkPeer.IsSynchronized)
                {
                    continue;
                }

                MissionPeer component = networkPeer.GetComponent<MissionPeer>();
                if (component == null || component.ControlledAgent != null || !component.HasSpawnedAgentVisuals || CanUpdateSpawnEquipment(component))
                {
                    continue;
                }

                MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(component);
                MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(component);
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(networkPeer, component.Perks[component.SelectedTroopIndex]));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);

                BasicCharacterObject basicCharacterObject;
                // If player is officer, spawn hero instead of standard troop
                if (networkPeer.IsOfficer())
                {
                    basicCharacterObject = mPHeroClassForPeer.HeroCharacter;
                }
                else
                {
                    basicCharacterObject = mPHeroClassForPeer.TroopCharacter;
                }

                // Spawn player, make him invulnerable in the beginning to prevent TK
                SpawnHelper.SpawnPlayer(networkPeer, onSpawnPerkHandler, basicCharacterObject, mortalityState: MortalityState.Invulnerable);

                OnPeerSpawnedFromVisuals(component);
                OnAllAgentsFromPeerSpawnedFromVisuals(component);
                AgentVisualSpawnComponent.RemoveAgentVisuals(component, sync: true);
                MPPerkObject.GetPerkHandler(component)?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
            }

            if (IsSpawningEnabled || !IsRoundInProgress())
            {
                return;
            }

            SpawningDelayTimer += dt;
        }

        public override void RequestStartSpawnSession()
        {
            Log("Starting new round - PvCSpawningBehavior.RequestStartSpawnSession() | IsSpawningEnabled = " + IsSpawningEnabled, LogLevel.Debug);

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

        private async void EnableMortalityAfterTimer(int waitTime)
        {
            await Task.Delay(waitTime);
            EnableMortality();
        }

        private void EnableMortality()
        {
            SendNotificationToAll($"Rappel : le TK est actif, ne frappez pas vos alliés!");

            // Make everyone mortal
            foreach (Agent agent in Mission.Current?.AllAgents)
            {
                if (agent.CurrentMortalityState != MortalityState.Mortal)
                {
                    agent.SetMortalityState(MortalityState.Mortal);
                }
            }
        }

        protected override void SpawnAgents()
        {
            BasicCultureObject culture1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            BasicCultureObject culture2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
            Mission.Current.AllowAiTicking = false;

            // Spawn players on both side
            foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
            {
                MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();

                if (component != null)
                {
                    CreateEnforcedSpawnTimerForPeer(component, 15);
                    if (networkCommunicator.IsSynchronized && component.Team != null && !CheckIfEnforcedSpawnTimerExpiredForPeer(component))
                    {
                        BasicCultureObject culture = component.Team.Side == BattleSideEnum.Attacker ? culture1 : culture2;

                        try
                        {
                            SpawnHelper.SpawnPlayerPreview(networkCommunicator, culture);
                            Mission.Current.AllowAiTicking = true;
                        }
                        catch (Exception e)
                        {
                            Log("Alliance - Error spawning player " + component.Name, LogLevel.Error);
                            Log(e.ToString());
                        }
                    }
                }
            }

            // Spawn bots for testing purpose when alone to prevent round ending instantly
            if (!_haveBotsBeenSpawned)
            {
                int nbBotsToSpawnAtt = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
                for (int i = 0; i < nbBotsToSpawnAtt; i++)
                {
                    BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture1).ToList().GetRandomElement().TroopCharacter;
                    SpawnHelper.SpawnBot(Mission.AttackerTeam, culture1, troopCharacter);
                }
                Log("Spawned " + nbBotsToSpawnAtt + " bots for attacker side.", LogLevel.Debug);

                int nbBotsToSpawnDef = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
                for (int i = 0; i < nbBotsToSpawnDef; i++)
                {
                    BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture2).ToList().GetRandomElement().TroopCharacter;
                    SpawnHelper.SpawnBot(Mission.DefenderTeam, culture2, troopCharacter);
                }
                Log("Spawned " + nbBotsToSpawnDef + " bots for defender side.", LogLevel.Debug);

                _haveBotsBeenSpawned = true;
            }
        }

        public new void OnPeerSpawnedFromVisuals(MissionPeer peer)
        {
            // TODO : Try to give control to all formations. Maybe not here ?
            //if (((PvCRepresentative)peer.Representative).IsCommander)
            //{
            //	foreach (Formation formation in peer.Team.FormationsIncludingEmpty)
            //	{
            //		peer.Team?.AssignPlayerAsSergeantOfFormation(peer, formation.PrimaryClass);
            //	}
            //}

            //if (peer.ControlledFormation != null)
            //{
            //	peer.ControlledAgent.Team.AssignPlayerAsSergeantOfFormation(peer, peer.ControlledFormation.FormationIndex);
            //}
        }

        private new void OnAllAgentsFromPeerSpawnedFromVisuals(MissionPeer peer)
        {
            if (peer.ControlledFormation != null)
            {
                //peer.ControlledFormation.OnFormationDispersed();
                //peer.ControlledFormation.SetMovementOrder(MovementOrder.MovementOrderFollow(peer.ControlledAgent));
                NetworkCommunicator networkPeer = peer.GetNetworkPeer();
                // Check if >= 0 to prevent crash
                if (peer.BotsUnderControlAlive >= 0 && peer.BotsUnderControlTotal >= 0)
                {
                    GameNetwork.BeginBroadcastModuleEvent();
                    GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, peer.BotsUnderControlAlive, peer.BotsUnderControlTotal));
                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
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
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
            }
            /*if (_flagDominationMissionController.UseGold())
			{
				bool flag = peer.Team == Mission.AttackerTeam;
				MultiplayerClassDivisions.MPHeroClass mpheroClass = MultiplayerClassDivisions.GetMPHeroClasses(MBObjectManager.Instance.GetObject<BasicCultureObject>(flag ? MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) : MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions))).ElementAt(peer.SelectedTroopIndex);
				int num = ((_flagDominationMissionController.GetMissionType() == MissionLobbyComponent.MultiplayerGameType.Battle) ? mpheroClass.TroopBattleCost : mpheroClass.TroopCost);
				_flagDominationMissionController.ChangeCurrentGoldForPeer(peer, _flagDominationMissionController.GetCurrentGoldForPeer(peer) - num);
			}*/
        }

        public override bool AllowEarlyAgentVisualsDespawning(MissionPeer lobbyPeer)
        {
            if (MultiplayerOptions.OptionType.NumberOfBotsPerFormation.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions) != 0)
            {
                return false;
            }
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

        protected override bool IsRoundInProgress()
        {
            return _roundController.IsRoundInProgress;
        }

        public bool AllowExternalSpawn()
        {
            return IsRoundInProgress();
        }

        private void CreateEnforcedSpawnTimerForPeer(MissionPeer peer, int durationInSeconds)
        {
            try
            {
                if (peer == null || peer.Name == null)
                {
                    throw new ArgumentException("peer == null || peer.Name == null");
                }
                if (_enforcedSpawnTimers.Any((pair) => pair.Key == peer))
                {
                    return;
                }
                _enforcedSpawnTimers.Add(new KeyValuePair<MissionPeer, Timer>(peer, new Timer(Mission.CurrentTime, durationInSeconds, true)));
                Log("EST for " + peer.Name + " set to " + durationInSeconds + " seconds.", LogLevel.Debug);
            }
            catch (Exception e)
            {
                Log("ERROR in CreateEnforcedSpawnTimerForPeer", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
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
                Log("EST for " + peer.Name + " is no longer valid (spawned already).", LogLevel.Debug);
                return false;
            }
            Timer value = keyValuePair.Value;
            if (peer.HasSpawnedAgentVisuals && value.Check(Mission.Current.CurrentTime))
            {
                SpawnComponent.SetEarlyAgentVisualsDespawning(peer, true);
                _enforcedSpawnTimers.RemoveAll((p) => p.Key == peer);
                Log("EST for " + peer.Name + " has expired.", LogLevel.Debug);
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

        protected void SpawnBotVisualsInPlayerFormation(MissionPeer missionPeer, int visualsIndex, Team agentTeam, BasicCultureObject cultureLimit, string troopName, Formation formation, bool updateExistingAgentVisuals, int totalCount, IEnumerable<ValueTuple<EquipmentIndex, EquipmentElement>> alternativeEquipments)
        {
            BasicCharacterObject @object = MBObjectManager.Instance.GetObject<BasicCharacterObject>(troopName);
            AgentBuildData agentBuildData = new AgentBuildData(@object).Team(agentTeam).OwningMissionPeer(missionPeer).VisualsIndex(visualsIndex)
                .TroopOrigin(new BasicBattleAgentOrigin(@object))
                .EquipmentSeed(MissionLobbyComponent.GetRandomFaceSeedForCharacter(@object, visualsIndex))
                .Formation(formation)
                .IsFemale(@object.IsFemale)
                .ClothingColor1(agentTeam.Side == BattleSideEnum.Attacker ? cultureLimit.Color : cultureLimit.ClothAlternativeColor)
                .ClothingColor2(agentTeam.Side == BattleSideEnum.Attacker ? cultureLimit.Color2 : cultureLimit.ClothAlternativeColor2);
            Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, !(Game.Current.GameType is MultiplayerGame), false, MBRandom.RandomInt());
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
            if (GameMode.ShouldSpawnVisualsForServer(networkPeer))
            {
                AgentVisualSpawnComponent.SpawnAgentVisualsForPeer(missionPeer, agentBuildData, -1, true, totalCount);
            }
            GameMode.HandleAgentVisualSpawning(networkPeer, agentBuildData, totalCount, false);
        }

        private const int EnforcedSpawnTimeInSeconds = 15;

        private float _spawningTimer;

        private bool _spawningTimerTicking;

        private bool _haveBotsBeenSpawned;

        private bool _roundInitialSpawnOver;

        private MissionMultiplayerFlagDomination _flagDominationMissionController;

        private MultiplayerRoundController _roundController;

        private List<KeyValuePair<MissionPeer, Timer>> _enforcedSpawnTimers;
    }
}
