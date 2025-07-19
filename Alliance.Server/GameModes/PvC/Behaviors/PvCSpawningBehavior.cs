using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.ClassLimiter.Models;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.ToggleEntities.NetworkMessages.FromServer;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.PlayerSpawn.Behaviors;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Extensions.UsableEntity.Utilities.AllianceTags;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.Agent;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace Alliance.Server.GameModes.PvC.Behaviors
{
	public class PvCSpawningBehavior : SpawningBehaviorBase, ISpawnBehavior
	{
		private PlayerSpawnBehavior _playerSpawnBehavior;

		public PvCSpawningBehavior()
		{
			_enforcedSpawnTimers = new List<KeyValuePair<MissionPeer, Timer>>();
		}

		public override void Initialize(SpawnComponent spawnComponent)
		{
			base.Initialize(spawnComponent);
			_flagDominationMissionController = Mission.GetMissionBehavior<MissionMultiplayerFlagDomination>();
			_roundController = Mission.GetMissionBehavior<MultiplayerRoundController>();
			_playerSpawnBehavior = Mission.GetMissionBehavior<PlayerSpawnBehavior>();
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

				if (_playerSpawnBehavior != null)
				{
					// Custom spawning behavior for PlayerSpawnMenu
					SpawnAgentsCustom(dt);
				}
				else
				{
					// "Native" spawning behavior
					TickSpawnAgents(dt);
					SpawnPlayerPreviews();
				}

				if (_roundInitialSpawnOver && _flagDominationMissionController.GameModeUsesSingleSpawning && _spawningTimer > MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions))
				{
					IsSpawningEnabled = false;
					_spawningTimer = 0f;
					_spawningTimerTicking = false;
					StartFightAfterTimer(5000);
					ReportClassRepartition();
				}
			}
		}

		private void SpawnAgentsCustom(float dt)
		{
			// Loop on players and spawn them
			foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
			{
				if (!networkPeer.IsSynchronized)
				{
					continue;
				}

				MissionPeer component = networkPeer.GetComponent<MissionPeer>();
				if (component == null || component.ControlledAgent != null)
				{
					continue;
				}

				PlayerAssignment playerAssignment = PlayerSpawnMenu.Instance.GetPlayerAssignment(networkPeer);
				if (playerAssignment?.Character == null || !playerAssignment.CanSpawn || playerAssignment.TimeBeforeSpawn > 0f)
				{
					continue;
				}

				BasicCultureObject culture = playerAssignment.Formation.MainCulture;
				BasicCharacterObject basicCharacterObject = playerAssignment.Character.Character;
				MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = playerAssignment.Character.Character.GetHeroClass();
				MPOnSpawnPerkHandler onSpawnPerkHandler = GetOnSpawnPerkHandler(SpawnHelper.GetPerks(mPHeroClassForPeer, playerAssignment.Perks));
				// Spawn player, make him invulnerable in the beginning to prevent TK
				SpawnHelper.SpawnPlayer(networkPeer, onSpawnPerkHandler, basicCharacterObject, mortalityState: MortalityState.Invulnerable, customCulture: culture);

				// not useful anymore since our perk list is not the native one ?
				//GameNetwork.BeginBroadcastModuleEvent();
				//GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(networkPeer, component.Perks[component.SelectedTroopIndex]));
				//GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, networkPeer);

				//OnPeerSpawnedFromVisuals(component);
				OnAllAgentsFromPeerSpawnedFromVisuals(component);

				// not useful anymore since we don't use agent visuals ?
				//GameNetwork.BeginBroadcastModuleEvent();
				//GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(networkPeer));
				//GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				//component.HasSpawnedAgentVisuals = false;

				MPPerkObject.GetPerkHandler(component)?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
			}

			// Spawn bots
			if (!_haveBotsBeenSpawned)
			{
				if (MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC")
				{
					SpawnBotsForCvC();
				}
				else
				{
					SpawnBots();
				}

			}

			if (IsSpawningEnabled || !IsRoundInProgress())
			{
				return;
			}

			SpawningDelayTimer += dt;
		}

		public void TickSpawnAgents(float dt)
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

				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(networkPeer));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				component.HasSpawnedAgentVisuals = false;

				MPPerkObject.GetPerkHandler(component)?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
			}

			// Spawn bots
			if (!_haveBotsBeenSpawned)
			{
				if (MultiplayerOptions.OptionType.GameType.GetStrValue() == "CvC")
				{
					SpawnBotsForCvC();
				}
				else
				{
					SpawnBots();
				}

			}

			if (IsSpawningEnabled || !IsRoundInProgress())
			{
				return;
			}

			SpawningDelayTimer += dt;
		}

		private void SpawnBots()
		{
			float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(Config.Instance.BotDifficulty);
			int nbBotsToSpawnAtt = MultiplayerOptions.OptionType.NumberOfBotsTeam1.GetIntValue();
			int nbBotsToSpawnDef = MultiplayerOptions.OptionType.NumberOfBotsTeam2.GetIntValue();

			// Spawn bots for attacker
			if (nbBotsToSpawnAtt > 0)
			{
				BasicCultureObject culture1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
				SpawnBotsForTeam(Mission.AttackerTeam, culture1, difficulty, nbBotsToSpawnAtt);
			}
			// Spawn bots for defender
			if (nbBotsToSpawnDef > 0)
			{
				BasicCultureObject culture2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
				SpawnBotsForTeam(Mission.DefenderTeam, culture2, difficulty, nbBotsToSpawnAtt);
			}
			_haveBotsBeenSpawned = true;
		}

		private void SpawnBotsForTeam(Team team, BasicCultureObject culture, float difficulty, int nbBotsToSpawn)
		{
			int i;
			for (i = 0; i < nbBotsToSpawn; i++)
			{
				BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture).ToList().GetRandomElement().TroopCharacter;
				if (Config.Instance.UsePlayerLimit)
				{
					if (!ClassLimiterModel.Instance.CharactersAvailability[troopCharacter].IsAvailable)
					{
						List<BasicCharacterObject> availableCharacters = ClassLimiterModel.Instance.CharactersAvailable.Where(p => p.Value == true).Select(p => p.Key).ToList();
						if (availableCharacters.Count > 0)
						{
							troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(culture).ToList().GetRandomElementWithPredicate(c => availableCharacters.Contains(c.TroopCharacter)).TroopCharacter;
						}
						else
						{
							Log("No more available characters to spawn bots.", LogLevel.Warning);
							break;
						}
					}
				}
				SpawnHelper.SpawnBot(team, culture, troopCharacter, botDifficulty: difficulty);
			}
			SendMessageToAll($"Spawned {i} bots in {team.Side}");
		}

		private void SpawnBotsForCvC()
		{
			float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(Config.Instance.BotDifficulty);

			// If no player were spawn in attacker team, spawn bots instead
			if (Mission.Current.AttackerTeam.ActiveAgents.Count == 0 && Mission.Current.AttackerTeam.HasAnyEnemyTeamsWithAgents(false))
			{
				BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
				int nbAgentsSpawn = SpawnHelper.SpawnArmy(Config.Instance.StartingGold, difficulty, Mission.Current.AttackerTeam, culture);
				SendMessageToAll("No player found in Attacker Team. Spawned " + nbAgentsSpawn + " bots instead.");
				_haveBotsBeenSpawned = true;
			}
			// If no player were spawn in defender team, spawn bots instead
			else if (Mission.Current.DefenderTeam.ActiveAgents.Count == 0 && Mission.Current.DefenderTeam.HasAnyEnemyTeamsWithAgents(false))
			{
				BasicCultureObject culture = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
				int nbAgentsSpawn = SpawnHelper.SpawnArmy(Config.Instance.StartingGold, difficulty, Mission.Current.DefenderTeam, culture);
				SendMessageToAll("No player found in Defender Team. Spawned " + nbAgentsSpawn + " bots instead.");
				_haveBotsBeenSpawned = true;
			}
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


		private async void StartFightAfterTimer(int waitTime)
		{
			if (waitTime <= 0)
			{
				EnableMortality();
			}
			else
			{
				ToggleBarriers(TEMPORARY_BARRIER_TAG, true);
				await Task.Delay(waitTime);
				EnableMortality();
				ToggleBarriers(TEMPORARY_BARRIER_TAG, false);
			}
		}

		private void ToggleBarriers(string tag, bool show)
		{
			foreach (GameEntity entity in Mission.Current.Scene.FindEntitiesWithTag(tag))
			{
				entity.SetVisibilityExcludeParents(show);
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncToggleEntities(tag, show));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
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

		private void ReportClassRepartition()
		{
			// Report class repartition at the beginning of the round
			BasicCultureObject cultureTeam1 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
			BasicCultureObject cultureTeam2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
			string reportTeam1 = $"{cultureTeam1.Name}: \n ";
			string reportTeam2 = $"{cultureTeam2.Name}: \n ";
			foreach (CharacterAvailability characterAvailability in ClassLimiterModel.Instance.CharactersAvailability.Values)
			{
				if (characterAvailability.Taken <= 0) continue;

				if (characterAvailability.Character.Culture == cultureTeam1)
				{
					reportTeam1 += $"{characterAvailability.Character.Name}: {characterAvailability.Taken}/{characterAvailability.Slots} \n ";
				}
				else if (characterAvailability.Character.Culture == cultureTeam2)
				{
					reportTeam2 += $"{characterAvailability.Character.Name}: {characterAvailability.Taken}/{characterAvailability.Slots} \n ";
				}
			}
			Log(reportTeam1, LogLevel.Information);
			Log(reportTeam2, LogLevel.Information);
			SendMessageToAll(reportTeam1);
			SendMessageToAll(reportTeam2);
		}

		// Spawn agents preview
		private void SpawnPlayerPreviews()
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
			Equipment randomEquipmentElements = Equipment.GetRandomEquipmentElements(@object, false, false, MBRandom.RandomInt());
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

		protected override void SpawnAgents()
		{
			// This method is not used in PvCSpawningBehavior, as spawning is handled in SpawnAgentsCustom() or TickSpawnAgents()/SpawnPlayerPreviews()
			// BTW its native implementation don't match its name, as it only spawns visuals and not agents.
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
