using Alliance.Common.Extensions.TroopSpawner.Models;
using HarmonyLib;
using NetworkMessages.FromClient;
using NetworkMessages.FromServer;
using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.Core;
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
						nameof(Prefix_SendAgentsToPeer), BindingFlags.Static | BindingFlags.Public))
				);
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
				// Iterate through all agents in the mission and process accordingly
				foreach (Agent agent in __instance.Mission.AllAgents)
				{
					bool isMount = agent.IsMount;
					bool isAnimal = agent.Monster.FamilyType > 0;

					if (isAnimal && agent.RiderAgent == null)
					{
						// Handling mount agents (animals without riders)
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new CreateFreeMountAgent(agent, agent.Position, agent.GetMovementDirection()));
						GameNetwork.EndModuleEventAsServer();
						agent.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);

						// Sync attached weapons if any (e.g., saddle equipment or tack)
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
					}
					else if (!isMount && !isAnimal)
					{
						// Handling human agents
						Agent agent2 = agent.MountAgent;
						if (agent2 != null && agent2.RiderAgent == null)
						{
							agent2 = null;
						}

						// Ensure necessary components are initialized before creating the agent						
						Equipment spawnEquipment = agent.SpawnEquipment ?? new Equipment();
						MissionEquipment missionEquipment = agent.Equipment ?? new MissionEquipment();

						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new CreateAgent(agent.Index, agent.Character, agent.Monster, spawnEquipment, missionEquipment, agent.BodyPropertiesValue, agent.BodyPropertiesSeed, agent.IsFemale, agent.Team?.TeamIndex ?? (-1), agent.Formation?.Index ?? (-1), agent.ClothingColor1, agent.ClothingColor2, agent2?.Index ?? (-1), agent.MountAgent?.SpawnEquipment, agent.MissionPeer != null && agent.OwningAgentMissionPeer == null, agent.Position, agent.GetMovementDirection(), agent.MissionPeer?.GetNetworkPeer() ?? agent.OwningAgentMissionPeer?.GetNetworkPeer()));
						GameNetwork.EndModuleEventAsServer();
						agent.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);
						agent2?.LockAgentReplicationTableDataWithCurrentReliableSequenceNo(networkPeer);

						// Iterate through equipment slots only for human agents
						for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
						{
							if (agent.Equipment != null && !spawnEquipment[equipmentIndex].IsEmpty)
							{
								for (int j = 0; j < agent.Equipment[equipmentIndex].GetAttachedWeaponsCount(); j++)
								{
									GameNetwork.BeginModuleEventAsServer(networkPeer);
									GameNetwork.WriteMessage(new AttachWeaponToWeaponInAgentEquipmentSlot(agent.Equipment[equipmentIndex].GetAttachedWeapon(j), agent.Index, equipmentIndex, agent.Equipment[equipmentIndex].GetAttachedWeaponFrame(j)));
									GameNetwork.EndModuleEventAsServer();
								}
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

						if (attachedWeaponsCount2 > 0)
						{
							// Handle wielded items
							EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
							int mainHandCurUsageIndex = ((wieldedItemIndex != EquipmentIndex.None) ? agent.Equipment[wieldedItemIndex].CurrentUsageIndex : 0);
							GameNetwork.BeginModuleEventAsServer(networkPeer);
							GameNetwork.WriteMessage(new SetWieldedItemIndex(agent.Index, isLeftHand: false, isWieldedInstantly: true, isWieldedOnSpawn: true, wieldedItemIndex, mainHandCurUsageIndex));
							GameNetwork.EndModuleEventAsServer();

							GameNetwork.BeginModuleEventAsServer(networkPeer);
							GameNetwork.WriteMessage(new SetWieldedItemIndex(agent.Index, isLeftHand: true, isWieldedInstantly: true, isWieldedOnSpawn: true, agent.GetWieldedItemIndex(Agent.HandIndex.OffHand), mainHandCurUsageIndex));
							GameNetwork.EndModuleEventAsServer();
						}
					}
				}

				Log("agents sending end-", LogLevel.Debug);
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