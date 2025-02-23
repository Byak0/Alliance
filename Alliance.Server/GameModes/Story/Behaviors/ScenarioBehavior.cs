using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Models;
using Alliance.Common.GameModes.Story.NetworkMessages.FromServer;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Extensions.CustomScripts.Scripts.CS_RefillAmmo;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.GameModes.Story.Behaviors
{
	/// <summary>
	/// Server-side base behavior for scenario game mode.
	/// Responsible for updating scenario state.
	/// </summary>
	public class ScenarioBehavior : MissionMultiplayerGameModeBase, IMissionBehavior
	{
		public Scenario Scenario => ScenarioManagerServer.Instance.CurrentScenario;
		public Act Act => ScenarioManagerServer.Instance.CurrentAct;
		public ActState State => ScenarioManagerServer.Instance.ActState;

		public override bool IsGameModeHidingAllAgentVisuals => true;
		public override bool IsGameModeUsingOpposingTeams => true;

		public ScenarioSpawningBehavior SpawningBehavior { get; set; }
		public ObjectivesBehavior ObjectivesBehavior { get; set; }
		public ScenarioClientBehavior ClientBehavior { get; set; }

		private float _minimumStateDuration = 5f;
		private float _stateDuration;
		private float _checkStateDelay = 1f;

		public bool EnableStateChange { get; private set; }

		private float _checkStateDt;

		public ScenarioBehavior()
		{
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			BasicCultureObject attacker = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam1.GetStrValue());
			BasicCultureObject defender = MBObjectManager.Instance.GetObject<BasicCultureObject>(MultiplayerOptions.OptionType.CultureTeam2.GetStrValue());
			Banner banner = new Banner(attacker.BannerKey, attacker.BackgroundColor1, attacker.ForegroundColor1);
			Banner banner2 = new Banner(defender.BannerKey, defender.BackgroundColor1, defender.ForegroundColor1);
			Mission.Teams.Add(BattleSideEnum.Attacker, attacker.BackgroundColor1, attacker.ForegroundColor1, banner, isPlayerGeneral: false, isPlayerSergeant: true);
			Mission.Teams.Add(BattleSideEnum.Defender, defender.BackgroundColor1, defender.ForegroundColor1, banner2, isPlayerGeneral: false, isPlayerSergeant: true);
		}

		public override void AfterStart()
		{
			SpawningBehavior = (ScenarioSpawningBehavior)SpawnComponent.SpawningBehavior;
			ObjectivesBehavior = Mission.Current.GetMissionBehavior<ObjectivesBehavior>();
			ClientBehavior = Mission.Current.GetMissionBehavior<ScenarioClientBehavior>();

			ChangeState(ActState.AwaitingPlayerJoin);
			EnableStateChange = true;
		}

		protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			if (Scenario == null || Act == null) return;

			string scenarioId = Scenario.Id;
			int actId = Scenario.Acts.IndexOf(Act);

			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new InitScenarioMessage(scenarioId, actId));
			GameNetwork.EndModuleEventAsServer();

			float stateRemainingTime = TimerComponent.GetRemainingTime(false);
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new UpdateScenarioMessage(State, MissionTime.Now.NumberOfTicks, MathF.Ceiling(stateRemainingTime)));
			GameNetwork.EndModuleEventAsServer();

			long timerStart = 0;
			int timerDuration = 0;
			if (State == ActState.InProgress && ObjectivesBehavior.MissionTimer != null)
			{
				timerStart = ObjectivesBehavior.MissionTimer.GetStartTime().NumberOfTicks;
				timerDuration = (int)ObjectivesBehavior.MissionTimer.GetTimerDuration();
			}
			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new ObjectivesProgressMessage(
				ObjectivesBehavior.TotalAttackerDead,
				ObjectivesBehavior.TotalDefenderDead,
				timerStart,
				timerDuration));
			GameNetwork.EndModuleEventAsServer();
		}

		public override void OnMissionTick(float dt)
		{
			if (Scenario == null || Act == null) return;
			base.OnMissionTick(dt);
			if (EnableStateChange)
			{
				CheckScenarioState(dt);
			}
		}

		private void CheckScenarioState(float dt)
		{
			_checkStateDt += dt;
			_stateDuration += dt;
			if (_checkStateDt < _checkStateDelay) return;
			_checkStateDt = 0;
			if (_stateDuration < _minimumStateDuration) return;
			switch (State)
			{
				case ActState.Invalid:
					ChangeState(ActState.AwaitingPlayerJoin);
					break;
				case ActState.AwaitingPlayerJoin:
					if (EnoughPlayersJoined())
					{
						ChangeState(ActState.SpawningParticipants);
						StartSpawn();
					}
					break;
				case ActState.SpawningParticipants:
					if (InitialSpawnFinished())
					{
						ChangeState(ActState.InProgress);
						StartAct();
						SetStartingGold();
						SyncObjectives();
					}
					break;
				case ActState.InProgress:
					if (CheckObjectives())
					{
						ChangeState(ActState.DisplayingResults);
						DisplayResults();
					}
					break;
				case ActState.DisplayingResults:
					if (CanEndAct())
					{
						ChangeState(ActState.Completed);
						EndAct();
					}
					break;
				case ActState.Completed:
					EnableStateChange = false;
					break;
			}
		}

		private bool CanEndAct()
		{
			return _stateDuration > 15f;
		}

		private void ChangeState(ActState newState)
		{
			ScenarioManagerServer.Instance.SetActState(newState);
			_stateDuration = 0;
		}

		private void SyncObjectives()
		{
			long timerStart = 0;
			int timerDuration = 0;
			if (State == ActState.InProgress && ObjectivesBehavior.MissionTimer != null)
			{
				timerStart = ObjectivesBehavior.MissionTimer.GetStartTime().NumberOfTicks;
				timerDuration = (int)ObjectivesBehavior.MissionTimer.GetTimerDuration();
			}
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ObjectivesProgressMessage(
				ObjectivesBehavior.TotalAttackerDead,
				ObjectivesBehavior.TotalDefenderDead,
				timerStart,
				timerDuration));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		private bool EnoughPlayersJoined()
		{
			int minPlayersForStart = (int)MathF.Clamp((float)Math.Round(GameNetwork.NetworkPeers.Count / 1.1), 1, Math.Max(GameNetwork.NetworkPeers.Count - 1, 1));
			int playersReady = 0;
			foreach (ICommunicator peer in GameNetwork.NetworkPeers)
			{
				if (peer.IsSynchronized) playersReady++;
			}
			string log = "Waiting for players to load... (" + playersReady + "/" + minPlayersForStart + ")";
			Log(log);
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new ServerMessage(log));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			return playersReady >= minPlayersForStart;
		}

		private bool InitialSpawnFinished()
		{
			//return !SpawningBehavior.AreAgentsSpawning() || SpawningBehavior.SpawningStrategy.SpawningTimer > MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions);
			// TODO check if this fix the act starting before spawn ended
			return !SpawningBehavior.AreAgentsSpawning() || SpawningBehavior.SpawningStrategy.SpawningTimer > 10f + MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue();
		}

		private bool CheckObjectives()
		{
			return ObjectivesBehavior.ObjectiveIsOver;
		}

		private void StartSpawn()
		{
			Log("Alliance - Starting spawn...", LogLevel.Debug);
			TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundPreparationTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
		}

		private void StartAct()
		{
			Log("Alliance - Starting act...", LogLevel.Debug);
			TimerComponent.StartTimerAsServer(MultiplayerOptions.OptionType.RoundTimeLimit.GetIntValue(MultiplayerOptions.MultiplayerOptionsAccessMode.CurrentMapOptions));
		}

		private void DisplayResults()
		{
			Log("Alliance - Displaying results...", LogLevel.Debug);
		}

		private void EndAct()
		{
			Log("Alliance - Ended act.", LogLevel.Debug);
		}

		public void ResetState()
		{
			EnableStateChange = true;
			_checkStateDt = 0;
			ObjectivesBehavior.Reset();
			//SpawningBehavior.SetSpawningStrategy(new SpawningStrategyBase()); // Todo : define strategy in act ?
		}

		public override void OnRemoveBehavior()
		{
			ScenarioManagerServer.Instance.CurrentAct?.UnregisterObjectives();
			GameNetwork.RemoveNetworkHandler(this);
		}

		public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
		{
			ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), Config.Instance.StartingGold);
		}

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			agent.UpdateSyncHealthToAllClients(value: true);

			// Set ammo of players to 0 when respawning
			//if (SpawningBehavior.SpawningStrategy is SpawningStrategyBase baseSpawningStrategy && agent?.MissionPeer != null)
			//{
			//	// Check if player is respawning
			//	if (baseSpawningStrategy.PlayerUsedLives.ContainsKey(agent.MissionPeer) && baseSpawningStrategy.PlayerUsedLives[agent.MissionPeer] > 1)
			//	{
			//		SetAmmoOfAgent(agent, 0);
			//	}
			//}
		}

		public void SetAmmoOfAgent(Agent agent, short ammoCount)
		{
			if (agent == null || agent.Equipment == null) return;

			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
			{
				if (!agent.Equipment[equipmentIndex].IsEmpty)
				{
					if (Enum.IsDefined(typeof(AmmoClass), (int)agent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass) && agent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass != (WeaponClass)AmmoClass.All)
					{
						if (agent.Equipment[equipmentIndex].Amount > ammoCount)
						{
							agent.SetWeaponAmountInSlot(equipmentIndex, ammoCount, true);
						}
					}
				}
			}
		}

		// Mask parent method and remove gold limit
		public new void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
		{
			newAmount = MBMath.ClampInt(newAmount, 0, CompressionBasic.RoundGoldAmountCompressionInfo.GetMaximumValue());

			if (peer.Peer.Communicator.IsConnectionActive)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncGoldsForSkirmish(peer.Peer, newAmount));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}

			if (GameModeBaseClient != null)
			{
				GameModeBaseClient.OnGoldAmountChangedForRepresentative(peer.Representative, newAmount);
			}
		}

		private async void SetStartingGold()
		{
			await Task.Delay(5000);

			int goldToGive = Config.Instance.StartingGold;
			List<MissionPeer> commandersAttack = new List<MissionPeer>();
			List<MissionPeer> commandersDefend = new List<MissionPeer>();
			foreach (NetworkCommunicator networkCommunicator in GameNetwork.NetworkPeers)
			{
				MissionPeer peer = networkCommunicator?.GetComponent<MissionPeer>();
				if (peer?.Team == Mission.Current.AttackerTeam)
				{
					commandersAttack.Add(peer);
				}
				else if (peer?.Team == Mission.Current.DefenderTeam)
				{
					commandersDefend.Add(peer);
				}
			}
			SplitGoldBetweenPlayers(goldToGive + GetTeamArmyValue(BattleSideEnum.Defender), commandersAttack);
			SplitGoldBetweenPlayers(goldToGive + GetTeamArmyValue(BattleSideEnum.Attacker), commandersDefend);
		}

		private void SplitGoldBetweenPlayers(int goldToGive, List<MissionPeer> players)
		{
			if (players.Count < 1) return;

			int amountPerPlayer = goldToGive / players.Count;
			foreach (MissionPeer peer in players)
			{
				Log($"Giving {amountPerPlayer}g to {peer.Name}", LogLevel.Debug);
				ChangeCurrentGoldForPeer(peer, amountPerPlayer);
			}
		}

		public int GetTeamArmyValue(BattleSideEnum side)
		{
			int value = 0;
			int agentsCount;

			if (side == Mission.Current.AttackerTeam.Side)
			{
				agentsCount = Mission.Current.AttackerTeam.ActiveAgents.Count;
				foreach (Agent agent in Mission.Current.AttackerTeam.ActiveAgents)
				{
					value += SpawnHelper.GetTroopCost(agent.Character);
				}
			}
			else
			{
				agentsCount = Mission.Current.DefenderTeam.ActiveAgents.Count;
				foreach (Agent agent in Mission.Current.DefenderTeam.ActiveAgents)
				{
					value += SpawnHelper.GetTroopCost(agent.Character);
				}
			}

			Log($"Value of {side}'s army : {value} * {Config.Instance.GoldMultiplier} ({agentsCount} agents)", LogLevel.Debug);

			value = (int)(value * Config.Instance.GoldMultiplier);

			return value;
		}

		public override MultiplayerGameType GetMissionType()
		{
			return MultiplayerGameType.Captain;
		}
	}
}