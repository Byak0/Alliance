using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.TroopSpawner.Interfaces;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromClient;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using Alliance.Server.Extensions.TroopSpawner.Interfaces;
using Alliance.Server.GameModes.CaptainX.Behaviors;
using NetworkMessages.FromServer;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MPPerkObject;

namespace Alliance.Server.Extensions.TroopSpawner.Handlers
{
	public class SpawnTroopHandler : IHandlerRegister
	{
		static ALMissionMultiplayerFlagDomination GameMode => Mission.Current.GetMissionBehavior<MissionMultiplayerFlagDomination>() as ALMissionMultiplayerFlagDomination;
		static IBotControllerBehavior GameModeClient => Mission.Current.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>() as IBotControllerBehavior;
		static ISpawnBehavior SpawnBehavior => Mission.Current.GetMissionBehavior<SpawnComponent>().SpawningBehavior as ISpawnBehavior;
		static ISpawnFrameBehavior SpawnFrame => Mission.Current.GetMissionBehavior<SpawnComponent>().SpawnFrameBehavior as ISpawnFrameBehavior;

		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestSpawnTroop>(HandleRequestSpawnTroop);
			reg.Register<RequestSpawnThing>(HandleRequestSpawnThing);
			reg.Register<FormationRequestControlMessage>(HandleFormationRequestControl);
		}

		public bool HandleFormationRequestControl(NetworkCommunicator peer, FormationRequestControlMessage model)
		{
			MissionPeer commander = model.Peer.GetComponent<MissionPeer>();
			if (commander == null) return false;

			FormationControlModel.Instance.AssignControlToPlayer(commander, model.Formation, true);
			return true;
		}

		public bool HandleRequestSpawnThing(NetworkCommunicator peer, RequestSpawnThing model)
		{
			if (!peer.IsDev()) return false;

			MatrixFrame spawnPos = model.SpawnPosition;

			// Spawn the thing
			//Mission.Current.CreateMissionObjectFromPrefab("frazer_supersport", spawnPos);

			//GameEntity entity = GameEntity.Instantiate(Mission.Current.Scene, "frazer_supersport", spawnPos);            
			//CS_Car script = entity.GetFirstScriptOfType<CS_Car>();
			//List<MissionObjectId> list = new List<MissionObjectId>();
			//Utility.Log($"I'm spawning the thing. ID={script.Id} | {spawnPos.origin} | {list}", color: ConsoleColor.Green, logToAll: true);
			//GameNetwork.BeginBroadcastModuleEvent();
			//GameNetwork.WriteMessage(new CreateMissionObject(script.Id, "frazer_supersport", spawnPos, list));
			//GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
			//List<GameEntity> children = new List<GameEntity>();
			//entity.GetChildrenRecursive(ref children);
			//foreach (GameEntity child in children)
			//{
			//    MissionObject mo = child.GetFirstScriptOfType<MissionObject>();
			//    if (mo != null) list.Add(mo.Id);
			//}
			//Utility.Log($"ID={script.Id} | {entity.GetChildren().Count()} | {list}", color: ConsoleColor.Green, logToAll: true);
			//Mission.Current.AddDynamicallySpawnedMissionObjectInfo(new Mission.DynamicallyCreatedEntity("frazer_supersport", script.Id, spawnPos, ref list));
			////GameNetwork.BeginBroadcastModuleEvent();
			////GameNetwork.WriteMessage(new SynchronizeMissionObject(script));
			////GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
			//Utility.Log($"I've spawned the thing. ID={script.Id} | {spawnPos.origin} | {list}", color: ConsoleColor.Green, logToAll: true);
			return true;
		}

		/// <summary>
		/// Handle player request to spawn troops.
		/// </summary>
		public bool HandleRequestSpawnTroop(NetworkCommunicator peer, RequestSpawnTroop model)
		{
			// Player info
			MissionPeer missionPeer = peer.GetComponent<MissionPeer>();
			// Troop info
			BasicCharacterObject troopToSpawn = model.CharacterToSpawn;
			ExtendedCharacter extendedTroopToSpawn = troopToSpawn.GetExtendedCharacterObject();
			MPOnSpawnPerkHandler perkHandler = GetOnSpawnPerkHandler(SpawnHelper.GetPerks(troopToSpawn, model.SelectedPerks));

			float difficulty = SpawnHelper.DifficultyMultiplierFromLevel(model.DifficultyLevel);

			// If bot difficulty is forced and player is not admin, use difficulty from config
			if (Config.Instance.BotDifficulty != nameof(SpawnHelper.Difficulty.PlayerChoice) && !peer.IsAdmin())
			{
				difficulty = SpawnHelper.DifficultyMultiplierFromLevel(Config.Instance.BotDifficulty);
			}

			int goldRemaining = missionPeer.Representative.Gold - SpawnHelper.GetTotalTroopCost(troopToSpawn, model.TroopCount, difficulty);

			string refuseReason = "";
			if (!CanPlayerSpawn(peer, extendedTroopToSpawn, goldRemaining, model, ref refuseReason))
			{
				SendMessageToPeer(refuseReason, peer);
				return false;
			}

			string debugAvailableSlots = $"Remaining slots = {AgentsInfoModel.Instance.GetAvailableSlotCount()}";
			Log(debugAvailableSlots, LogLevel.Debug);

			string reportToPlayer = "";
			MatrixFrame spawnPos = model.SpawnPosition;
			int nbTroopToSpawn = model.TroopCount;

			bool playerSpawned = false;
			// Spawn player if dead
			if (missionPeer.ControlledAgent == null)
			{
				if (!model.SpawnAtExactPosition) spawnPos = SpawnFrame.GetClosestSpawnFrame(missionPeer.Team, troopToSpawn.HasMount(), false, spawnPos);

				SpawnHelper.SpawnPlayer(peer, perkHandler, troopToSpawn, spawnPos, model.Formation);

				reportToPlayer += "You respawned as " + troopToSpawn.Name + (Config.Instance.UseTroopCost ? " for " + SpawnHelper.GetTotalTroopCost(troopToSpawn, 1) + " golds.\n" : ".\n");
				nbTroopToSpawn--;
				playerSpawned = true;
			}


			// Assign player as sergeant BEFORE spawning to prevent crash OnBotsControlledChange ?
			if (missionPeer.ControlledAgent != null)
			{
				MissionPeer previousSergeant = FormationControlModel.Instance.GetControllerOfFormation((FormationClass)model.Formation, missionPeer.Team);

				if (previousSergeant != missionPeer)
				{
					// Unassign previous sergeant from this formation to prevent crash 
					if (previousSergeant != null)
					{
						FormationControlModel.Instance.RemoveControlFromPlayer(previousSergeant, (FormationClass)model.Formation, true);
						previousSergeant.ControlledFormation = null;
					}
					FormationControlModel.Instance.AssignControlToPlayer(missionPeer, (FormationClass)model.Formation, true);
				}
			}

			int troopSpawned = 0;
			string lackingReason = "";
			// Spawn the required number of bots
			for (int i = 0; i < nbTroopToSpawn; i++)
			{
				if (!model.SpawnAtExactPosition) spawnPos = SpawnFrame.GetClosestSpawnFrame(missionPeer.Team, troopToSpawn.HasMount(), false, spawnPos);
				if (Config.Instance.UseTroopLimit && extendedTroopToSpawn.TroopLeft <= 0 && !peer.IsCommander())
				{
					lackingReason = " There are no troops left to recruit.";
					break;
				}
				if (!SpawnHelper.SpawnBot(missionPeer.Team, missionPeer.Culture, troopToSpawn, spawnPos, perkHandler, model.Formation, difficulty))
				{
					Log($"Alliance : Can't spawn bot n.{SpawnHelper.TotalBots} (no slot available)", LogLevel.Error);
					lackingReason = " (engine is lacking slots for additional spawn)";
					break;
				};
				troopSpawned++;
				if (Config.Instance.UseTroopLimit) extendedTroopToSpawn.TroopLeft--;
			}

			int finalTroopCost = SpawnHelper.GetTotalTroopCost(troopToSpawn, troopSpawned + (playerSpawned ? 1 : 0), difficulty);

			if (Config.Instance.UseTroopCost && goldRemaining >= 0)
			{
				if (GameMode != null)
				{
					GameMode?.ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold - finalTroopCost);
				}
				else
				{
					ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold - finalTroopCost);
				}
			}

			if (missionPeer.ControlledFormation != null)
			{
				// Need to properly calculate bots number to prevent crash
				// Bots who spawned before the player took control are not counted natively
				int botsUnderControlAlive = missionPeer.BotsUnderControlAlive = Math.Max(missionPeer.BotsUnderControlAlive, missionPeer.ControlledFormation.CountOfUnits);
				int botsUnderControlTotal = Math.Max(missionPeer.BotsUnderControlTotal, missionPeer.BotsUnderControlAlive);

				if (botsUnderControlAlive <= 0 || botsUnderControlTotal <= 0)
				{
					string error = $"OBCC - {missionPeer.Name} - {(FormationClass)model.Formation} - alive: {botsUnderControlAlive} - total: {botsUnderControlTotal}";
					Log(error, LogLevel.Error);
					SendMessageToAll(error);
				}
				else if (GameModeClient != null)
				{
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new BotsControlledChange(peer, botsUnderControlAlive, botsUnderControlTotal));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

					GameModeClient.OnBotsControlledChanged(missionPeer, botsUnderControlAlive, botsUnderControlTotal);

					Log($"OBCC - {missionPeer.Name} - {(FormationClass)model.Formation} - alive: {botsUnderControlAlive} - total: {botsUnderControlTotal}", LogLevel.Debug);
				}
			}

			reportToPlayer += $"You recruited {troopSpawned} {SpawnHelper.DifficultyFromMultiplier(difficulty)} {troopToSpawn.Name} {(Config.Instance.UseTroopCost ? " for " + finalTroopCost + " golds." : ".")}";
			if (troopSpawned < nbTroopToSpawn)
			{
				reportToPlayer += lackingReason;
			}

			if (troopSpawned >= 1)
			{
				// Inform players of what just spawned
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SpawnInfoMessage(troopToSpawn, troopSpawned, extendedTroopToSpawn.TroopLeft));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);

				// Send report to player who made the spawn request
				SendMessageToPeer(reportToPlayer, peer);
			}

			return true;
		}

		public void ChangeCurrentGoldForPeer(MissionPeer peer, int newAmount)
		{
			if (newAmount >= 0)
			{
				newAmount = MBMath.ClampInt(newAmount, 0, CompressionBasic.RoundGoldAmountCompressionInfo.GetMaximumValue());
			}

			peer.Representative.UpdateGold(newAmount);

			if (peer.Peer.Communicator.IsConnectionActive)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncGoldsForSkirmish(peer.Peer, newAmount));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}
		}

		/// <summary>
		/// Check if a player is allowed to spawn.
		/// </summary>
		/// <param name="refuseReason">Explanation in case the player is not allowed to spawn.</param>
		/// <returns>True if allowed, false otherwise</returns>
		private bool CanPlayerSpawn(NetworkCommunicator peer, ExtendedCharacter troopToSpawn, int goldRemaining, RequestSpawnTroop model, ref string refuseReason)
		{
			// If game stage is inappropriate
			if (SpawnBehavior != null && !SpawnBehavior.AllowExternalSpawn())
			{
				refuseReason = "You can't recruit yet.";
				return false;
			}

			bool isAdmin = peer.IsAdmin();
			bool isCommander = peer.IsCommander();

			// If player is neither admin / commander nor authorized
			if (!isAdmin && !isCommander)
			{
				refuseReason = "You're not authorized to recruit.";
				return false;
			}
			// If player wants to spawn as an admin
			if (!isAdmin && model.SpawnAtExactPosition)
			{
				refuseReason = "You're not authorized to use this command. Use the Recruit button.";
				return false;
			}
			// If player lacks gold
			if (!model.SpawnAtExactPosition && isCommander && Config.Instance.UseTroopCost && goldRemaining < 0)
			{
				refuseReason = "You need more gold.";
				return false;
			}
			// If troop limit is reached
			if (!model.SpawnAtExactPosition && isCommander && Config.Instance.UseTroopLimit && troopToSpawn.TroopLeft <= 0)
			{
				refuseReason = "There are no more troops available.";
				return false;
			}
			// If troop limit is reached
			if (model.TroopCount > SpawnHelper.MaxBotsPerSpawn)
			{
				refuseReason = "You can't recruit this many troops at once.";
				return false;
			}
			return true;
		}
	}
}
