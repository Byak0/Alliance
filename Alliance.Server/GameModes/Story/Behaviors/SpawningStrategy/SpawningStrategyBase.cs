﻿using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Server.Extensions.FlagsTracker.Behaviors;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using static TaleWorlds.MountAndBlade.MultiplayerOptions;

namespace Alliance.Server.GameModes.Story.Behaviors.SpawningStrategy
{
	/// <summary>
	/// Base stragegy implementation for the spawning mechanic.
	/// Can be inherited to implement more specific behavior.
	/// </summary>
	public class SpawningStrategyBase : ISpawningStrategy
	{
		public float SpawningTimer => _spawningTimer;

		public Dictionary<MissionPeer, int> PlayerUsedLives { get; set; }
		public Dictionary<MissionPeer, int> PlayerRemainingLives { get; set; }

		protected SpawnComponent SpawnComponent { get; set; }
		protected ScenarioSpawningBehavior SpawnBehavior { get; set; }
		protected SpawnFrameBehaviorBase DefaultSpawnFrameBehavior { get; set; }
		protected FlagTrackerBehavior FlagTrackerBehavior { get; set; }
		protected bool SpawnEnabled { get; set; }
		protected Dictionary<MissionPeer, SpawnLocation> SelectedLocations { get; set; }
		protected Dictionary<Team, int> TeamRemainingLives { get; set; }
		protected bool ShowRespawnPreview { get; set; }

		protected BasicCultureObject[] _cultures;
		protected float _spawningTimer;
		protected int _spawnPreparationTimeLimit;
		protected bool[] _haveBotsBeenSpawned;
		private float _tickDelay;

		public Act CurrentAct => ScenarioManagerServer.Instance.CurrentAct;
		public SpawnLogic SpawnLogic => ScenarioManagerServer.Instance.CurrentAct.SpawnLogic;

		public SpawningStrategyBase()
		{
			SelectedLocations = new Dictionary<MissionPeer, SpawnLocation>();
			PlayerUsedLives = new Dictionary<MissionPeer, int>();
			PlayerRemainingLives = new Dictionary<MissionPeer, int>();
			TeamRemainingLives = new Dictionary<Team, int>();
			ShowRespawnPreview = true;
		}

		public virtual void Initialize(SpawnComponent spawnComponent, ScenarioSpawningBehavior spawnBehavior, SpawnFrameBehaviorBase defaultSpawnFrameBehavior)
		{
			SpawnComponent = spawnComponent;
			SpawnBehavior = spawnBehavior;
			DefaultSpawnFrameBehavior = defaultSpawnFrameBehavior;

			FlagTrackerBehavior = Mission.Current.GetMissionBehavior<FlagTrackerBehavior>();

			Log(GetType().Name + " initialized", LogLevel.Debug);
		}

		public virtual void OnTick(float dt)
		{
			if (!SpawnEnabled) return;

			_spawningTimer += dt;
			_tickDelay += dt;

			if (_tickDelay < 0.25f) return;
			_tickDelay = 0f;

			foreach (NetworkCommunicator player in GameNetwork.NetworkPeers)
			{
				if (player.ControlledAgent == null)
				{
					MissionPeer peer = player.GetComponent<MissionPeer>();
					if (peer?.Team != null && CanPlayerSpawn(player, peer))
					{
						bool ignoreVisual = !ShowRespawnPreview && PlayerUsedLives.ContainsKey(peer) && PlayerUsedLives[peer] > 1;
						if (SpawnBehavior.CheckIfEnforcedSpawnTimerExpiredForPeer(player.GetComponent<MissionPeer>(), ignoreVisual))
						{
							SpawnPlayer(player, peer);
						}
						else
						{
							SpawnPlayerPreview(player, peer, ignoreVisual);
						}
					}
				}
			}

			if (CanBotSpawn(Mission.Current.AttackerTeam))
			{
				SpawnBots(Mission.Current.AttackerTeam);
			}

			if (CanBotSpawn(Mission.Current.DefenderTeam))
			{
				SpawnBots(Mission.Current.DefenderTeam);
			}

			CheckForPlayersSpawningAsBots();
		}

		public static bool CheckForPlayersSpawningAsBots()
		{
			foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
			{
				if (networkCommunicator.IsSynchronized)
				{
					MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();

					if (component != null && component.ControlledAgent == null && component.Team != null && component.SpawnCountThisRound > 0)
					{
						if (!component.HasSpawnTimerExpired && component.SpawnTimer.Check(Mission.Current.CurrentTime))
						{
							component.HasSpawnTimerExpired = true;
						}
						if (component.HasSpawnTimerExpired && component.WantsToSpawnAsBot)
						{
							Agent followedAgent = component.FollowedAgent;

							// Check if the followed agent is in a formation controlled by the player
							if (followedAgent != null && followedAgent.IsActive() && followedAgent.IsAIControlled && followedAgent.Formation != null && followedAgent.Health > 0
								&& FormationControlModel.Instance.GetControlledFormations(component).Contains(followedAgent.Formation.FormationIndex))
							{
								// Update player controlled formation to target
								component.ControlledFormation = followedAgent.Formation;
								ReplaceBotWithPlayer(followedAgent, component);
								component.WantsToSpawnAsBot = false;
								component.HasSpawnTimerExpired = false;
							}
						}
					}
				}
			}

			return false;
		}

		public static Agent ReplaceBotWithPlayer(Agent botAgent, MissionPeer missionPeer)
		{
			if (!GameNetwork.IsClientOrReplay && botAgent != null)
			{
				if (GameNetwork.IsServer)
				{
					NetworkCommunicator networkPeer = missionPeer.GetNetworkPeer();
					if (!networkPeer.IsServerPeer)
					{
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new ReplaceBotWithPlayer(networkPeer, botAgent.Index, botAgent.Health, botAgent.MountAgent?.Health ?? (-1f)));
						GameNetwork.EndModuleEventAsServer();
					}
				}

				if (botAgent.Formation != null)
				{
					botAgent.Formation.PlayerOwner = botAgent;
				}

				botAgent.OwningAgentMissionPeer = null;
				botAgent.MissionPeer = missionPeer;
				botAgent.Formation = missionPeer.ControlledFormation;
				AgentFlag agentFlags = botAgent.GetAgentFlags();
				if (!agentFlags.HasAnyFlag(AgentFlag.CanRide))
				{
					botAgent.SetAgentFlags(agentFlags | AgentFlag.CanRide);
				}

				// Prevent BotsUnderControlAlive from going under 0 and causing crash
				missionPeer.BotsUnderControlAlive = Math.Max(0, missionPeer.BotsUnderControlAlive - 1);
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new BotsControlledChange(missionPeer.GetNetworkPeer(), missionPeer.BotsUnderControlAlive, missionPeer.BotsUnderControlTotal));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				if (botAgent.Formation != null)
				{
					missionPeer.Team.AssignPlayerAsSergeantOfFormation(missionPeer, missionPeer.ControlledFormation.FormationIndex);
				}

				return botAgent;
			}

			return null;
		}

		public virtual void SpawnPlayerPreview(NetworkCommunicator player, MissionPeer peer, bool ignoreVisual)
		{
			if (player.IsSynchronized && peer != null && peer.Team != null)
			{
				SpawnBehavior.CreateEnforcedSpawnTimerForPeer(peer, _spawnPreparationTimeLimit);

				if (ignoreVisual)
				{
					peer.HasSpawnedAgentVisuals = true;
					return;
				}

				SpawnHelper.SpawnPlayerPreview(player, _cultures[(int)peer.Team.Side]);
			}
		}

		/// <summary>
		/// If battle side was not found, return 0;
		/// </summary>
		/// <param name="battleSide"></param>
		/// <returns>Number of tickets of selected side. <b>/!\ Should only be use for ATTACKER and DEFENDER sides /!\</b></returns>
		public int GetRemainingLives(BattleSideEnum battleSide)
		{
			return TeamRemainingLives.Where(e => e.Key.Side == battleSide)
				.ToList()
				.FirstOrDefault(new KeyValuePair<Team, int>(null, 0))
				.Value;
		}

		public virtual void SpawnPlayer(NetworkCommunicator player, MissionPeer peer)
		{
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForPeer = MultiplayerClassDivisions.GetMPHeroClassForPeer(peer);
			MPOnSpawnPerkHandler onSpawnPerkHandler = GetOnSpawnPerkHandler(peer);

			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SyncPerksForCurrentlySelectedTroop(player, peer.Perks[peer.SelectedTroopIndex]));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.ExcludeOtherTeamPlayers, player);

			BasicCharacterObject basicCharacterObject;

			MatrixFrame? spawnLocation = null;

			// If player is officer, spawn hero instead of standard troop
			if (player.IsOfficer())
			{
				basicCharacterObject = mPHeroClassForPeer.HeroCharacter;
				spawnLocation = GetPlayerSpawnLocation(peer, basicCharacterObject.HasMount());
				// Give banner to officer if none are present for team
				if (FlagTrackerBehavior != null && !FlagTrackerBehavior.FlagTrackers.Exists(flag => flag.Team == peer.Team) && basicCharacterObject.Equipment[EquipmentIndex.ExtraWeaponSlot].IsEmpty)
				{
					EquipmentElement banner = GetBannerItem(_cultures[(int)peer.Team.Side]);
					List<(EquipmentIndex, EquipmentElement)> altEquipment = new List<(EquipmentIndex, EquipmentElement)>
					{
						(EquipmentIndex.ExtraWeaponSlot, banner)
					};
					SpawnHelper.SpawnPlayer(player, onSpawnPerkHandler, basicCharacterObject, spawnLocation, alternativeEquipment: altEquipment);
				}
				else
				{
					SpawnHelper.SpawnPlayer(player, onSpawnPerkHandler, basicCharacterObject, spawnLocation);
				}
			}
			else
			{
				basicCharacterObject = mPHeroClassForPeer.TroopCharacter;
				spawnLocation = GetPlayerSpawnLocation(peer, basicCharacterObject.HasMount());
				SpawnHelper.SpawnPlayer(player, onSpawnPerkHandler, basicCharacterObject, spawnLocation);
			}

			// TODO : rework the control state of formations
			if (peer.ControlledFormation != null)
			{
				UpdateBotControlState(peer, player);
			}

			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new RemoveAgentVisualsForPeer(player));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			peer.HasSpawnedAgentVisuals = false;

			GetPerkHandler(peer)?.OnEvent(MPPerkCondition.PerkEventFlags.SpawnEnd);
			UpdatePlayerLives(peer);
		}

		public static EquipmentElement GetBannerItem(BasicCultureObject culture)
		{
			ItemObject banner = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().FirstOrDefault(item => item.IsBannerItem && item.Culture == culture);
			// If no banner found for culture, get first banner found
			banner ??= MBObjectManager.Instance.GetObjectTypeList<ItemObject>().FirstOrDefault(item => item.IsBannerItem);
			return new EquipmentElement(banner, null, null, false);
		}

		public virtual void InitPlayerLives(MissionPeer peer)
		{
			if (!PlayerUsedLives.ContainsKey(peer))
			{
				PlayerUsedLives.Add(peer, 0);
			}

			if (!PlayerRemainingLives.ContainsKey(peer))
			{
				PlayerRemainingLives.Add(peer, SpawnLogic.MaxLives[(int)peer.Team.Side]);
			}
		}

		public virtual void UpdatePlayerLives(MissionPeer peer)
		{
			RespawnStrategy respawnStrategy = SpawnLogic.RespawnStrategies[(int)peer.Team.Side];

			PlayerUsedLives[peer]++;

			if (respawnStrategy == RespawnStrategy.MaxLivesPerTeam)
			{
				TeamRemainingLives[peer.Team]--;
				string log = $"{TeamRemainingLives[peer.Team]} lives remaining for {_cultures[(int)peer.Team.Side].Name}.";
				Log(log, LogLevel.Debug);
				// TODO send info to clients
			}
			else if (respawnStrategy == RespawnStrategy.MaxLivesPerPlayer)
			{
				PlayerRemainingLives[peer]--;
			}
		}

		public virtual void UpdateBotControlState(MissionPeer player, NetworkCommunicator networkPeer)
		{
			// Check if >= 0 to prevent crash
			if (player.BotsUnderControlAlive >= 0 && player.BotsUnderControlTotal >= 0)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new BotsControlledChange(networkPeer, player.BotsUnderControlAlive, player.BotsUnderControlTotal));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
				Mission.Current.GetMissionBehavior<ScenarioClientBehavior>().OnBotsControlledChanged(player, player.BotsUnderControlAlive, player.BotsUnderControlTotal);
			}

			// TODO : rework assignation of spawned formations to prevent going over 10
			if (PlayerUsedLives.ContainsKey(player) && PlayerUsedLives[player] > 0) return;

			if (player.Team == Mission.Current.AttackerTeam && Mission.Current.NumOfFormationsSpawnedTeamOne < 10)
			{
				Mission.Current.NumOfFormationsSpawnedTeamOne++;
			}
			else if (Mission.Current.NumOfFormationsSpawnedTeamTwo < 10)
			{
				Mission.Current.NumOfFormationsSpawnedTeamTwo++;
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new SetSpawnedFormationCount(Mission.Current.NumOfFormationsSpawnedTeamOne, Mission.Current.NumOfFormationsSpawnedTeamTwo));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
		}

		public virtual void SpawnBots(Team team)
		{
			RespawnStrategy respawnStrategy = SpawnLogic.RespawnStrategies[(int)team.Side];
			int currentBots = team.ActiveAgents.Count;
			int nbBotsToSpawn = team.Side == BattleSideEnum.Attacker ?
				OptionType.NumberOfBotsTeam1.GetIntValue(MultiplayerOptionsAccessMode.CurrentMapOptions) :
				OptionType.NumberOfBotsTeam2.GetIntValue(MultiplayerOptionsAccessMode.CurrentMapOptions);
			nbBotsToSpawn -= currentBots;
			if (respawnStrategy == RespawnStrategy.MaxLivesPerTeam)
			{
				nbBotsToSpawn = Math.Min(nbBotsToSpawn, TeamRemainingLives[team]);
			}
			if (nbBotsToSpawn > 0)
			{
				for (int i = 0; i < nbBotsToSpawn; i++)
				{
					BasicCharacterObject troopCharacter = MultiplayerClassDivisions.GetMPHeroClasses(_cultures[(int)team.Side]).ToList().GetRandomElement().TroopCharacter;
					MatrixFrame? spawnLocation = GetBotSpawnLocation(team, troopCharacter.HasMount());
					SpawnHelper.SpawnBot(team, _cultures[(int)team.Side], troopCharacter, spawnLocation);
				}
				if (respawnStrategy == RespawnStrategy.MaxLivesPerTeam)
				{
					TeamRemainingLives[team] -= nbBotsToSpawn;
					Log($"{TeamRemainingLives[team]} lives remaining for {_cultures[(int)team.Side].Name}.", LogLevel.Debug);
					// TODO : send info to clients

				}
				Log($"Spawned {nbBotsToSpawn} bots for {team.Side} side. {TeamRemainingLives[team]} lives remaining for team.", LogLevel.Debug);
			}

			_haveBotsBeenSpawned[(int)team.Side] = true;
		}

		public virtual bool CanBotSpawn(Team team)
		{
			if (GameNetwork.NetworkPeers.Count > 5) return false;

			switch (SpawnLogic.RespawnStrategies[(int)team.Side])
			{
				case RespawnStrategy.NoRespawn:
					if (_haveBotsBeenSpawned[(int)team.Side]) return false;
					break;
				case RespawnStrategy.MaxLivesPerTeam:
					if (TeamRemainingLives[team] <= 0) return false;
					break;
			}

			return _spawningTimer >= _spawnPreparationTimeLimit;
		}

		public virtual bool CanPlayerSpawn(NetworkCommunicator player, MissionPeer peer)
		{
			if (peer.Team.Side != BattleSideEnum.Defender && peer.Team.Side != BattleSideEnum.Attacker)
			{
				return false;
			}

			if (!PlayerUsedLives.ContainsKey(peer) || !PlayerRemainingLives.ContainsKey(peer))
			{
				InitPlayerLives(peer);
			}

			switch (SpawnLogic.RespawnStrategies[(int)peer.Team.Side])
			{
				case RespawnStrategy.NoRespawn:
					if (PlayerUsedLives[peer] > 0) return false;
					break;
				case RespawnStrategy.MaxLivesPerTeam:
					if (TeamRemainingLives[peer.Team] <= 0) return false;
					break;
				case RespawnStrategy.MaxLivesPerPlayer:
					if (PlayerRemainingLives[peer] <= 0) return false;
					break;
			}

			switch (SpawnLogic.LocationStrategies[(int)peer.Team.Side])
			{
				case LocationStrategy.OnlyFlags:
					if (!FlagUsableForTeam(peer.Team)) return false;
					break;
				case LocationStrategy.TagsThenFlags:
					if (PlayerUsedLives[peer] > 0 && !FlagUsableForTeam(peer.Team)) return false;
					break;
			}

			return true;
		}

		public bool FlagUsableForTeam(Team team)
		{
			FlagTracker flagTracker = FlagTrackerBehavior.FlagTrackers.Find(flag => flag.Team == team);
			if (flagTracker == null)
			{
				return false;
			}
			else if (team.Side != flagTracker.FlagZone.Owner)
			{
				return false;
			}
			return true;
		}

		public virtual bool CanPlayerSelectCharacter(NetworkCommunicator player, BasicCharacterObject character)
		{
			return true;
		}

		public virtual bool CanPlayerSelectLocation(NetworkCommunicator player, SpawnLocation location)
		{
			return true;
		}

		public virtual MatrixFrame? GetPlayerSpawnLocation(MissionPeer player, bool hasMount = false)
		{
			MatrixFrame? position = null;
			bool firstSpawn = PlayerUsedLives[player] == 0;

			switch (SpawnLogic.LocationStrategies[(int)player.Team.Side])
			{
				case LocationStrategy.OnlyTags:
					position = GetTagLocation(player.Team, hasMount, firstSpawn);
					break;
				case LocationStrategy.OnlyFlags:
					position = GetFlagLocation(player.Team);
					break;
				case LocationStrategy.TagsThenFlags:
					position = firstSpawn ? GetTagLocation(player.Team, hasMount) : GetFlagLocation(player.Team);
					break;
				case LocationStrategy.PlayerChoice:
					position = GetPlayerSelectedLocation(player);
					break;
			}

			return position;
		}

		public virtual MatrixFrame? GetBotSpawnLocation(Team team, bool hasMount = false)
		{
			MatrixFrame? position = null;
			bool firstSpawn = !_haveBotsBeenSpawned[(int)team.Side];

			switch (SpawnLogic.LocationStrategies[(int)team.Side])
			{
				case LocationStrategy.OnlyTags:
					position = GetTagLocation(team, hasMount, firstSpawn);
					break;
				case LocationStrategy.OnlyFlags:
					position = GetFlagLocation(team);
					break;
				case LocationStrategy.TagsThenFlags:
					position = firstSpawn ? GetTagLocation(team, hasMount) : GetFlagLocation(team);
					break;
				default:
					position = GetTagLocation(team, hasMount, firstSpawn);
					break;
			}

			return position;
		}

		public virtual MatrixFrame? GetTagLocation(Team team, bool mounted = false, bool initialSpawn = false)
		{
			return DefaultSpawnFrameBehavior.GetSpawnFrame(team, mounted, initialSpawn);
		}

		public virtual MatrixFrame? GetFlagLocation(Team team)
		{
			FlagTracker flag = FlagTrackerBehavior.FlagTrackers.Find(flag => flag.Team == team);
			if (flag == null)
			{
				return null;
			}
			return flag.GetPosition();
		}

		public virtual MatrixFrame? GetPlayerSelectedLocation(MissionPeer player)
		{
			return SelectedLocations[player]?.Position ?? GetFlagLocation(player.Team) ?? GetTagLocation(player.Team);
		}

		public virtual void SetPlayerSelectedLocation(MissionPeer player, SpawnLocation spawnLocation)
		{
			SelectedLocations[player] = spawnLocation;
		}

		public virtual bool AllowExternalSpawn()
		{
			return _spawningTimer >= _spawnPreparationTimeLimit;
		}

		public virtual void StartSpawnSession()
		{
			_spawnPreparationTimeLimit = OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptionsAccessMode.CurrentMapOptions);

			// Init available cultures based on current act
			string cultureAttacker = CurrentAct.ActSettings.TWOptions[OptionType.CultureTeam1].ToString();
			string cultureDefender = CurrentAct.ActSettings.TWOptions[OptionType.CultureTeam2].ToString();
			_cultures = new BasicCultureObject[2]
			{
				MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureDefender),
				MBObjectManager.Instance.GetObject<BasicCultureObject>(cultureAttacker)
			};

			// Reset bot spawn state
			_haveBotsBeenSpawned = new bool[2] { false, false };

			// Reset selected locations
			SelectedLocations = new Dictionary<MissionPeer, SpawnLocation>();

			// Reset player used lives
			PlayerUsedLives = new Dictionary<MissionPeer, int>();

			// Reset or add lives to Players/Teams
			if (SpawnLogic.KeepLivesFromPreviousAct)
			{
				List<MissionPeer> peers = PlayerRemainingLives.Keys.ToList();
				foreach (MissionPeer peer in peers)
				{
					PlayerRemainingLives[peer] += SpawnLogic.MaxLives[(int)peer.Team.Side];
				}
				if (TeamRemainingLives.ContainsKey(Mission.Current.DefenderTeam))
				{
					TeamRemainingLives[Mission.Current.DefenderTeam] += SpawnLogic.MaxLives[(int)BattleSideEnum.Defender];
				}
				else
				{
					TeamRemainingLives.Add(Mission.Current.DefenderTeam, SpawnLogic.MaxLives[(int)BattleSideEnum.Defender]);
				}
				if (TeamRemainingLives.ContainsKey(Mission.Current.AttackerTeam))
				{
					TeamRemainingLives[Mission.Current.AttackerTeam] += SpawnLogic.MaxLives[(int)BattleSideEnum.Attacker];
				}
				else
				{
					TeamRemainingLives.Add(Mission.Current.AttackerTeam, SpawnLogic.MaxLives[(int)BattleSideEnum.Attacker]);
				}
			}
			else
			{
				PlayerRemainingLives = new Dictionary<MissionPeer, int>();
				TeamRemainingLives = new Dictionary<Team, int> {
					{ Mission.Current.DefenderTeam, SpawnLogic.MaxLives[(int)BattleSideEnum.Defender] },
					{ Mission.Current.AttackerTeam, SpawnLogic.MaxLives[(int)BattleSideEnum.Attacker] }
				};
			}

			SpawnComponent.ToggleUpdatingSpawnEquipment(true);
			SpawnEnabled = true;
			Log(GetType().Name + " - StartSpawnSession", LogLevel.Debug);
		}

		public virtual void EndSpawnSession()
		{
			SpawnComponent.ToggleUpdatingSpawnEquipment(false);
			SpawnEnabled = false;
			Log(GetType().Name + " - EndSpawnSession", LogLevel.Debug);
		}

		public virtual void PauseSpawnSession()
		{
			SpawnComponent.ToggleUpdatingSpawnEquipment(false);
			SpawnEnabled = false;
			Log(GetType().Name + " - PauseSpawnSession", LogLevel.Debug);
		}

		public virtual void ResumeSpawnSession()
		{
			SpawnComponent.ToggleUpdatingSpawnEquipment(true);
			SpawnEnabled = true;
			Log(GetType().Name + " - ResumeSpawnSession", LogLevel.Debug);
		}

		public virtual void OnClearScene()
		{
			Log(GetType().Name + " - OnClearScene", LogLevel.Debug);
		}

		public virtual void OnLoadScene()
		{
			Log(GetType().Name + " - OnLoadScene", LogLevel.Debug);
		}

		public virtual void OnSpawn(Agent agent)
		{
			Log(GetType().Name + " - OnSpawn - " + agent.Name, LogLevel.Debug);
		}

		public virtual void OnDespawn(Agent agent)
		{
			Log(GetType().Name + " - OnDespawn - " + agent.Name, LogLevel.Debug);
		}
	}
}
