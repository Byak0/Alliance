using Alliance.Common.Extensions.TroopSpawner.Models;
using HarmonyLib;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
	class Patch_MissionNetworkComponent
	{
		private static readonly Harmony Harmony = new Harmony(nameof(Patch_MissionNetworkComponent));

		private static bool _patched;
		public static bool Patch()
		{
			try
			{
				if (_patched)
					return false;
				_patched = true;
				Harmony.Patch(
					typeof(MissionNetworkComponent).GetMethod("HandleClientEventApplyOrderWithFormationAndNumber",
						BindingFlags.Instance | BindingFlags.NonPublic),
					prefix: new HarmonyMethod(typeof(Patch_MissionNetworkComponent).GetMethod(
						nameof(Prefix_HandleClientEventApplyOrderWithFormationAndNumber), BindingFlags.Static | BindingFlags.Public)));
				Harmony.Patch(
					typeof(MissionNetworkComponent).GetMethod("SendAgentsToPeer", BindingFlags.NonPublic | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(Patch_MissionNetworkComponent).GetMethod(
						nameof(Prefix_SendAgentsToPeer), BindingFlags.Static | BindingFlags.Public)));
			}
			catch (Exception e)
			{
				Log($"Alliance - ERROR in {nameof(Patch_MissionNetworkComponent)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Prefix for the SendAgentsToPeer method to separate animal agents and human agents during synchronization.
		/// </summary>
		public static bool Prefix_SendAgentsToPeer(MissionNetworkComponent __instance, NetworkCommunicator networkPeer)
		{
			try
			{
				Log("==> Starting sending agents to " + networkPeer?.UserName, LogLevel.Debug);
				int nbAgentsSent = 0;
				int nbAnimalsSent = 0;
				int nbAgentsIgnored = 0;

				// Iterate through all agents in the mission and process accordingly
				using (List<Agent>.Enumerator enumerator = __instance.Mission.AllAgents.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						Agent agent = enumerator.Current;

						bool isMount = agent.IsMount;
						bool isAnimal = agent.Monster.FamilyType > 0;

						// Complex conditions to determine if agent information should be sent
						AgentState state = agent.State;
						bool isAgentActive = state == AgentState.Active;
						bool isAgentKilledOrUnconscious = state == AgentState.Killed || state == AgentState.Unconscious;
						bool hasAttachedWeapons = agent.GetAttachedWeaponsCount() > 0;
						bool isHoldingWeapon = !isMount && !isAnimal &&
							(
								agent.GetWieldedItemIndex(Agent.HandIndex.MainHand) >= EquipmentIndex.WeaponItemBeginSlot ||
								agent.GetWieldedItemIndex(Agent.HandIndex.OffHand) >= EquipmentIndex.WeaponItemBeginSlot
							);
						bool isInProximityMap = Mission.Current.IsAgentInProximityMap(agent);
						bool hasMissilesInFlight = state != AgentState.Active && Mission.Current.Missiles.Any((Mission.Missile m) => m.ShooterAgent == agent);

						bool shouldSendInfo =
							isAgentActive ||
							(isAgentKilledOrUnconscious && (hasAttachedWeapons || isHoldingWeapon || isInProximityMap)) ||
							hasMissilesInFlight;

						// Skip if agent information should not be sent, otherwise might crash the client
						if (!shouldSendInfo)
						{
							Log($"Skipping agent {agent.Name} (state: {state}, active: {isAgentActive}, killed/unconscious: {isAgentKilledOrUnconscious}, attached weapons: {hasAttachedWeapons}, holding weapon: {isHoldingWeapon}, in proximity map: {isInProximityMap}, missiles in flight: {hasMissilesInFlight})", LogLevel.Debug);
							nbAgentsIgnored++;
							continue;
						}

						if (isAnimal && agent.RiderAgent == null)
						{
							// Handling mount agents (animals without riders)
							GameNetwork.BeginModuleEventAsServer(networkPeer);
							GameNetwork.WriteMessage(new CreateFreeMountAgent(agent, agent.Position, agent.GetMovementDirection()));
							GameNetwork.EndModuleEventAsServer();
							agent.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);

							// Sync attached weapons if any (e.g., saddle equipment)
							int attachedWeaponsCount = agent.GetAttachedWeaponsCount();
							for (int i = 0; i < attachedWeaponsCount; i++)
							{
								GameNetwork.BeginModuleEventAsServer(networkPeer);
								GameNetwork.WriteMessage(new AttachWeaponToAgent(agent.GetAttachedWeapon(i), agent.Index, agent.GetAttachedWeaponBoneIndex(i), agent.GetAttachedWeaponFrame(i)));
								GameNetwork.EndModuleEventAsServer();
							}

							if (!agent.IsActive())
							{
								// Access Violation Exception when accessing Action of killed agent ?
								ActionIndexValueCache actionIndex = agent.State != AgentState.Killed ? agent.GetCurrentActionValue(0) : ActionIndexValueCache.act_none;
								GameNetwork.BeginModuleEventAsServer(networkPeer);
								GameNetwork.WriteMessage(new MakeAgentDead(agent.Index, agent.State == AgentState.Killed, actionIndex));
								GameNetwork.EndModuleEventAsServer();
							}
							nbAnimalsSent++;
						}
						else if (!isMount && agent.IsHuman)
						{
							Agent agent2 = agent.MountAgent;
							if (agent2 != null && agent2.RiderAgent == null)
							{
								agent2 = null;
							}
							GameNetwork.BeginModuleEventAsServer(networkPeer);
							int index = agent.Index;
							BasicCharacterObject character = agent.Character;
							Monster monster = agent.Monster;
							Equipment spawnEquipment = agent.SpawnEquipment;
							MissionEquipment equipment = agent.Equipment;
							BodyProperties bodyPropertiesValue = agent.BodyPropertiesValue;
							int bodyPropertiesSeed = agent.BodyPropertiesSeed;
							bool isFemale = agent.IsFemale;
							Team team = agent.Team;
							int teamIndex = ((team != null) ? team.TeamIndex : (-1));
							Formation formation = agent.Formation;
							int formationIndex = ((formation != null) ? formation.Index : (-1));
							uint clothingColor = agent.ClothingColor1;
							uint clothingColor2 = agent.ClothingColor2;
							int agentIndex = ((agent2 != null) ? agent2.Index : (-1));
							Agent mountAgent = agent.MountAgent;
							Equipment equipment2 = ((mountAgent != null) ? mountAgent.SpawnEquipment : null);
							bool botControlled = agent.MissionPeer != null && agent.OwningAgentMissionPeer == null;
							Vec3 position = agent.Position;
							Vec2 movementDirection = agent.GetMovementDirection();
							MissionPeer missionPeer = agent.MissionPeer;
							NetworkCommunicator networkCommunicator;
							if ((networkCommunicator = ((missionPeer != null) ? missionPeer.GetNetworkPeer() : null)) == null)
							{
								MissionPeer owningAgentMissionPeer = agent.OwningAgentMissionPeer;
								networkCommunicator = ((owningAgentMissionPeer != null) ? owningAgentMissionPeer.GetNetworkPeer() : null);
							}
							GameNetwork.WriteMessage(new CreateAgent(index, character, monster, spawnEquipment, equipment, bodyPropertiesValue, bodyPropertiesSeed, isFemale, teamIndex, formationIndex, clothingColor, clothingColor2, agentIndex, equipment2, botControlled, position, movementDirection, networkCommunicator));
							GameNetwork.EndModuleEventAsServer();
							agent.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);
							agent2?.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);

							// Iterate through equipment slots only for human agents
							for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
							{
								for (int j = 0; j < agent.Equipment[equipmentIndex].GetAttachedWeaponsCount(); j++)
								{
									GameNetwork.BeginModuleEventAsServer(networkPeer);
									GameNetwork.WriteMessage(new AttachWeaponToWeaponInAgentEquipmentSlot(agent.Equipment[equipmentIndex].GetAttachedWeapon(j), agent.Index, equipmentIndex, agent.Equipment[equipmentIndex].GetAttachedWeaponFrame(j)));
									GameNetwork.EndModuleEventAsServer();
								}
							}

							// Handle other weapon attachments
							int attachedWeaponsCount2 = agent.GetAttachedWeaponsCount();
							for (int k = 0; k < attachedWeaponsCount2; k++)
							{
								GameNetwork.BeginModuleEventAsServer(networkPeer);
								GameNetwork.WriteMessage(new AttachWeaponToAgent(agent.GetAttachedWeapon(k), agent.Index, agent.GetAttachedWeaponBoneIndex(k), agent.GetAttachedWeaponFrame(k)));
								GameNetwork.EndModuleEventAsServer();
							}

							if (agent2 != null)
							{
								attachedWeaponsCount2 = agent2.GetAttachedWeaponsCount();
								for (int l = 0; l < attachedWeaponsCount2; l++)
								{
									GameNetwork.BeginModuleEventAsServer(networkPeer);
									GameNetwork.WriteMessage(new AttachWeaponToAgent(agent2.GetAttachedWeapon(l), agent2.Index, agent2.GetAttachedWeaponBoneIndex(l), agent2.GetAttachedWeaponFrame(l)));
									GameNetwork.EndModuleEventAsServer();
								}
							}

							EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
							int num5 = ((wieldedItemIndex != EquipmentIndex.None) ? agent.Equipment[wieldedItemIndex].CurrentUsageIndex : 0);
							GameNetwork.BeginModuleEventAsServer(networkPeer);
							GameNetwork.WriteMessage(new SetWieldedItemIndex(agent.Index, false, true, true, wieldedItemIndex, num5));
							GameNetwork.EndModuleEventAsServer();
							GameNetwork.BeginModuleEventAsServer(networkPeer);
							GameNetwork.WriteMessage(new SetWieldedItemIndex(agent.Index, true, true, true, agent.GetWieldedItemIndex(Agent.HandIndex.OffHand), num5));
							GameNetwork.EndModuleEventAsServer();
							MBActionSet actionSet = agent.ActionSet;
							if (actionSet.IsValid)
							{
								AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(actionSet, agent.Character.GetStepSize(), false);
								GameNetwork.BeginModuleEventAsServer(networkPeer);
								GameNetwork.WriteMessage(new SetAgentActionSet(agent.Index, animationSystemData));
								GameNetwork.EndModuleEventAsServer();
								if (!agent.IsActive())
								{
									GameNetwork.BeginModuleEventAsServer(networkPeer);
									GameNetwork.WriteMessage(new MakeAgentDead(agent.Index, state == AgentState.Killed, agent.GetCurrentActionValue(0)));
									GameNetwork.EndModuleEventAsServer();
								}
							}
							else
							{
								Log($"ERROR in Prefix_SendAgentsToPeer: Agent {agent.Name} has invalid action set. Considering it dead.", LogLevel.Error);
								GameNetwork.BeginModuleEventAsServer(networkPeer);
								GameNetwork.WriteMessage(new MakeAgentDead(agent.Index, state == AgentState.Killed, ActionIndexValueCache.act_none));
								GameNetwork.EndModuleEventAsServer();
							}
							nbAgentsSent++;
						}
					}
				}

				Log($"==> Sent {nbAgentsSent} agents and {nbAnimalsSent} animals to {networkPeer?.UserName} ({nbAgentsIgnored} ignored)", LogLevel.Debug);
			}
			catch (Exception e)
			{
				Log($"ERROR in {nameof(Prefix_SendAgentsToPeer)}", LogLevel.Error);
				Log(e.ToString(), LogLevel.Error);
			}

			// Skip the original SendAgentsToPeer since we're handling it here
			return false;
		}

		// Fix for transfer order in multiplayer
		public static bool Prefix_HandleClientEventApplyOrderWithFormationAndNumber(MissionNetworkComponent __instance, ref bool __result, NetworkCommunicator networkPeer, GameNetworkMessage baseMessage)
		{
			ApplyOrderWithFormationAndNumber message = (ApplyOrderWithFormationAndNumber)baseMessage;
			Team teamOfPeer = (Team)(typeof(MissionNetworkComponent).GetMethod("GetTeamOfPeer",
						BindingFlags.Instance | BindingFlags.NonPublic)?
						.Invoke(__instance, new object[] { networkPeer }));
			OrderController orderController = teamOfPeer != null ? teamOfPeer.GetOrderControllerOf(networkPeer.ControlledAgent) : null;

			// Remove check on CountOfUnits to allow transfer to empty formations
			Formation formation = teamOfPeer != null ? teamOfPeer.FormationsIncludingEmpty.SingleOrDefault((f) => /*f.CountOfUnits > 0 && */f.Index == message.FormationIndex) : null;

			// Give control to player
			Log($"Giving control of formation {formation.Index} to {networkPeer.UserName}", LogLevel.Information);
			FormationControlModel.Instance.AssignControlToPlayer(networkPeer.GetComponent<MissionPeer>(), formation.FormationIndex, true);

			int number = message.Number;
			if (teamOfPeer != null && orderController != null && formation != null)
			{
				orderController.SetOrderWithFormationAndNumber(message.OrderType, formation, number);
			}
			__result = true;

			// Return false to skip original method
			return false;
		}
	}
}