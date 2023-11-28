using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_Mission
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_Mission));

        private static bool _patched;

        static List<int> Indexes = new List<int>();
        static List<UIntPtr> AgentPtrs = new List<UIntPtr>();
        static List<UIntPtr> PositionPtrs = new List<UIntPtr>();
        static List<UIntPtr> IndexPtrs = new List<UIntPtr>();
        static List<UIntPtr> FlagsPtrs = new List<UIntPtr>();
        static List<UIntPtr> StatePtrs = new List<UIntPtr>();

        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(Mission).GetMethod("CreateAgent",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_Mission).GetMethod(
                        nameof(Prefix_CreateAgent), BindingFlags.Static | BindingFlags.Public)));
                //Harmony.Patch(
                //    typeof(Mission).GetMethod(nameof(Mission.SpawnWeaponAsDropFromAgentAux),
                //        BindingFlags.Instance | BindingFlags.Public),
                //    prefix: new HarmonyMethod(typeof(Patch_Mission).GetMethod(
                //        nameof(Prefix_SpawnWeaponAsDropFromAgentAux), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_Mission)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Replace SpawnWeaponAsDropFromAgentAux and disable log spam
        //public static bool Prefix_SpawnWeaponAsDropFromAgentAux(Mission __instance, Agent agent, EquipmentIndex equipmentIndex, ref Vec3 velocity, ref Vec3 angularVelocity, Mission.WeaponSpawnFlags spawnFlags, int forcedSpawnIndex)
        //{
        //    MissionWeapon weapon = agent.Equipment[equipmentIndex];

        //    // Check if the weapon is a banner
        //    if (weapon.IsBanner())
        //    {
        //        // Set the initial velocity of the banner based on the agent's velocity
        //        velocity = agent.Velocity + new Vec3(0f, 0f, -2f);
        //    }

        //    agent.AgentVisuals.GetSkeleton().ForceUpdateBoneFrames();
        //    agent.PrepareWeaponForDropInEquipmentSlot(equipmentIndex, (spawnFlags & Mission.WeaponSpawnFlags.WithHolster) > Mission.WeaponSpawnFlags.None);
        //    GameEntity weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
        //    weaponEntityFromEquipmentSlot.CreateAndAddScriptComponent(typeof(SpawnedItemEntity).Name);
        //    SpawnedItemEntity firstScriptOfType = weaponEntityFromEquipmentSlot.GetFirstScriptOfType<SpawnedItemEntity>();
        //    if (forcedSpawnIndex >= 0)
        //    {
        //        firstScriptOfType.Id = new MissionObjectId(forcedSpawnIndex, true);
        //    }
        //    float maximumValue = CompressionMission.SpawnedItemVelocityCompressionInfo.GetMaximumValue();
        //    float maximumValue2 = CompressionMission.SpawnedItemAngularVelocityCompressionInfo.GetMaximumValue();
        //    if (velocity.LengthSquared > maximumValue * maximumValue)
        //    {
        //        velocity = velocity.NormalizedCopy() * maximumValue;
        //    }
        //    if (angularVelocity.LengthSquared > maximumValue2 * maximumValue2)
        //    {
        //        angularVelocity = angularVelocity.NormalizedCopy() * maximumValue2;
        //    }
        //    MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
        //    if (GameNetwork.IsServerOrRecorder)
        //    {
        //        GameNetwork.BeginBroadcastModuleEvent();
        //        GameNetwork.WriteMessage(new SpawnWeaponAsDropFromAgent(agent.Index, equipmentIndex, velocity, angularVelocity, spawnFlags, firstScriptOfType.Id.Id));
        //        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
        //    }

        //    typeof(Mission).GetMethod("SpawnWeaponAux",
        //                BindingFlags.Instance | BindingFlags.NonPublic)?
        //                .Invoke(__instance, new object[] { weaponEntityFromEquipmentSlot, missionWeapon, spawnFlags, velocity, angularVelocity, true });

        //    if (!GameNetwork.IsClientOrReplay)
        //    {
        //        for (int i = 0; i < missionWeapon.GetAttachedWeaponsCount(); i++)
        //        {
        //            if (missionWeapon.GetAttachedWeapon(i).Item.ItemFlags.HasAnyFlag(ItemFlags.CanBePickedUpFromCorpse))
        //            {
        //                __instance.SpawnAttachedWeaponOnSpawnedWeapon(firstScriptOfType, i, -1);
        //            }
        //        }
        //    }
        //    agent.OnWeaponDrop(equipmentIndex);

        //    foreach (MissionBehavior missionBehavior in __instance.MissionBehaviors)
        //    {
        //        missionBehavior.OnItemDrop(agent, firstScriptOfType);
        //    }

        //    // Return false to skip original method
        //    return false;
        //}

        /* Original method 
         * 
		public void SpawnWeaponAsDropFromAgentAux(Agent agent, EquipmentIndex equipmentIndex, ref Vec3 velocity, ref Vec3 angularVelocity, Mission.WeaponSpawnFlags spawnFlags, int forcedSpawnIndex)
		{
			Log("SpawnWeaponAsDropFromAgentAux started.", 0, Debug.DebugColor.White, 17592186044416UL);
			if (agent == null)
			{
				Log("SpawnWeaponAsDropFromAgentAux agent is null", 0, Debug.DebugColor.White, 17592186044416UL);
			}
			agent.AgentVisuals.GetSkeleton().ForceUpdateBoneFrames();
			Log("SpawnWeaponAsDropFromAgentAux 1.", 0, Debug.DebugColor.White, 17592186044416UL);
			agent.PrepareWeaponForDropInEquipmentSlot(equipmentIndex, (spawnFlags & Mission.WeaponSpawnFlags.WithHolster) > Mission.WeaponSpawnFlags.None);
			Log("SpawnWeaponAsDropFromAgentAux 2.", 0, Debug.DebugColor.White, 17592186044416UL);
			GameEntity weaponEntityFromEquipmentSlot = agent.GetWeaponEntityFromEquipmentSlot(equipmentIndex);
			Log("SpawnWeaponAsDropFromAgentAux 3.", 0, Debug.DebugColor.White, 17592186044416UL);
			weaponEntityFromEquipmentSlot.CreateAndAddScriptComponent(typeof(SpawnedItemEntity).Name);
			Log("SpawnWeaponAsDropFromAgentAux 4.", 0, Debug.DebugColor.White, 17592186044416UL);
			SpawnedItemEntity firstScriptOfType = weaponEntityFromEquipmentSlot.GetFirstScriptOfType<SpawnedItemEntity>();
			Log("SpawnWeaponAsDropFromAgentAux 5.", 0, Debug.DebugColor.White, 17592186044416UL);
			if (forcedSpawnIndex >= 0)
			{
				firstScriptOfType.Id = new MissionObjectId(forcedSpawnIndex, true);
				Log("SpawnWeaponAsDropFromAgentAux 5,5.", 0, Debug.DebugColor.White, 17592186044416UL);
			}
			float maximumValue = CompressionMission.SpawnedItemVelocityCompressionInfo.GetMaximumValue();
			float maximumValue2 = CompressionMission.SpawnedItemAngularVelocityCompressionInfo.GetMaximumValue();
			if (velocity.LengthSquared > maximumValue * maximumValue)
			{
				velocity = velocity.NormalizedCopy() * maximumValue;
			}
			if (angularVelocity.LengthSquared > maximumValue2 * maximumValue2)
			{
				angularVelocity = angularVelocity.NormalizedCopy() * maximumValue2;
			}
			MissionWeapon missionWeapon = agent.Equipment[equipmentIndex];
			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SpawnWeaponAsDropFromAgent(agent, equipmentIndex, velocity, angularVelocity, spawnFlags, firstScriptOfType.Id.Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
			}
			this.SpawnWeaponAux(weaponEntityFromEquipmentSlot, missionWeapon, spawnFlags, velocity, angularVelocity, true);
			Log("SpawnWeaponAsDropFromAgentAux 6.", 0, Debug.DebugColor.White, 17592186044416UL);
			if (!GameNetwork.IsClientOrReplay)
			{
				for (int i = 0; i < missionWeapon.GetAttachedWeaponsCount(); i++)
				{
					if (missionWeapon.GetAttachedWeapon(i).Item.ItemFlags.HasAnyFlag(ItemFlags.CanBePickedUpFromCorpse))
					{
						this.SpawnAttachedWeaponOnSpawnedWeapon(firstScriptOfType, i, -1);
					}
				}
			}
			agent.OnWeaponDrop(equipmentIndex);
			Log("SpawnWeaponAsDropFromAgentAux 7.", 0, Debug.DebugColor.White, 17592186044416UL);
			foreach (MissionBehavior missionBehavior in this._missionBehaviorList)
			{
				missionBehavior.OnItemDrop(agent, firstScriptOfType);
			}
			Log("SpawnWeaponAsDropFromAgentAux ended.", 0, Debug.DebugColor.White, 17592186044416UL);
		}*/

        // Replace CreateAgent and try to prevent crash caused by AccessViolationException...
        public static bool Prefix_CreateAgent(Mission __instance, ref Agent __result, Monster monster, bool isFemale, int instanceNo, Agent.CreationType creationType, float stepSize, int forcedAgentIndex, int weight, BasicCharacterObject characterObject)
        {
            try
            {
                AnimationSystemData animationSystemData = monster.FillAnimationSystemData(stepSize, false, isFemale);
                AgentCapsuleData agentCapsuleData = monster.FillCapsuleData();
                AgentSpawnData agentSpawnData = monster.FillSpawnData(null);

                // Struct definition
                //var AgentCreationResult = __instance.GetType().GetField("AgentCreationResult");
                // Retrieve struct of Mission
                //object agentCreationResult = AgentCreationResult.GetValue(__instance);

                // Original call which may return duplicate pointers ?
                //Mission.AgentCreationResult agentCreationResult = __instance.CreateAgentInternal(monster.Flags, forcedAgentIndex, isFemale, ref agentSpawnData, ref agentCapsuleData, ref animationSystemData, instanceNo);

                object[] args = new object[] { monster.Flags, forcedAgentIndex, isFemale, agentSpawnData, agentCapsuleData, animationSystemData, instanceNo };
                object agentCreationResult = typeof(Mission).GetMethod("CreateAgentInternal",
                            BindingFlags.Instance | BindingFlags.NonPublic)?
                            .Invoke(__instance, args);

                // Retrieve data from AgentCreationResult struct
                int index = (int)agentCreationResult.GetType().GetField("Index", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agentCreationResult);
                UIntPtr agentPtr = (UIntPtr)agentCreationResult.GetType().GetField("AgentPtr", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agentCreationResult);
                UIntPtr positionPtr = (UIntPtr)agentCreationResult.GetType().GetField("PositionPtr", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agentCreationResult);
                UIntPtr indexPtr = (UIntPtr)agentCreationResult.GetType().GetField("IndexPtr", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agentCreationResult);
                UIntPtr flagsPtr = (UIntPtr)agentCreationResult.GetType().GetField("FlagsPtr", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agentCreationResult);
                UIntPtr statePtr = (UIntPtr)agentCreationResult.GetType().GetField("StatePtr", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(agentCreationResult);
                Log("DEBUG - index: " + index + " | agentPtr: " + agentPtr + " | positionPtr: " + positionPtr + " | indexPtr: " + indexPtr + " | flagsPtr: " + flagsPtr + " | statePtr: " + statePtr, LogLevel.Debug);

                bool duplicateIndex = false;
                bool duplicateAgentPtrs = false;
                bool duplicatePositionPtrs = false;
                bool duplicateIndexPtrs = false;
                bool duplicateFlagsPtrs = false;
                bool duplicateStatePtrs = false;

                if (Indexes.Contains(index))
                {
                    duplicateIndex = true;
                }
                if (AgentPtrs.Contains(agentPtr))
                {
                    duplicateAgentPtrs = true;
                }
                if (PositionPtrs.Contains(positionPtr))
                {
                    duplicatePositionPtrs = true;
                }
                if (IndexPtrs.Contains(indexPtr))
                {
                    duplicateIndexPtrs = true;
                }
                if (FlagsPtrs.Contains(flagsPtr))
                {
                    duplicateFlagsPtrs = true;
                }
                if (StatePtrs.Contains(statePtr))
                {
                    duplicateStatePtrs = true;
                }
                Indexes.Add(index);
                AgentPtrs.Add(agentPtr);
                PositionPtrs.Add(positionPtr);
                IndexPtrs.Add(indexPtr);
                FlagsPtrs.Add(flagsPtr);
                StatePtrs.Add(statePtr);

                // If duplicate pointers from engine
                if (duplicateAgentPtrs || duplicatePositionPtrs || duplicateIndexPtrs || duplicateFlagsPtrs || duplicateStatePtrs)
                {
                    Log("Error in patch Mission.Prefix_CreateAgent, duplicate pointers found :\n" +
                                "duplicateIndex : " + duplicateIndex + " (" + index + ")" + " x" + Indexes.FindAll(i => i == index).Count +
                                " | duplicateAgentPtrs : " + duplicateAgentPtrs + " (" + agentPtr + ")" + " x" + AgentPtrs.FindAll(ptr => ptr == agentPtr).Count +
                                " | duplicatePositionPtrs : " + duplicatePositionPtrs + " (" + positionPtr + ")" + " x" + PositionPtrs.FindAll(ptr => ptr == positionPtr).Count +
                                " | duplicateIndexPtrs : " + duplicateIndexPtrs + " (" + indexPtr + ")" + " x" + IndexPtrs.FindAll(ptr => ptr == indexPtr).Count +
                                " | duplicateFlagsPtrs : " + duplicateFlagsPtrs + " (" + flagsPtr + ")" + " x" + FlagsPtrs.FindAll(ptr => ptr == flagsPtr).Count +
                                " | duplicateStatePtrs : " + duplicateStatePtrs + " (" + statePtr + ")" + " x" + StatePtrs.FindAll(ptr => ptr == statePtr).Count
                                , LogLevel.Error);
                    //return false;
                }

                // Original call
                //Agent agent = new Agent(__instance, agentCreationResult, creationType, monster);            
                //ConstructorInfo agentConstructor = typeof(Agent).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(Mission), typeof(Object), typeof(Agent.CreationType), typeof(Monster) }, null);
                //Agent agent = (Agent)agentConstructor.Invoke(new object[] { __instance, agentCreationResult, creationType, monster });
                Agent agent = (Agent)typeof(Agent).Assembly.CreateInstance(
                    typeof(Agent).FullName, false,
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    null, new object[] { __instance, agentCreationResult, creationType, monster }, null, null);

                agent.Character = characterObject;
                foreach (MissionBehavior missionBehavior in __instance.MissionBehaviors)
                {
                    missionBehavior.OnAgentCreated(agent);
                }
                __result = agent;
            }
            catch (Exception ex)
            {
                Log("Error in patch Mission.Prefix_CreateAgent :\n" + ex.Message, LogLevel.Error);
                Log(ex.ToString(), LogLevel.Error);
                string report = "Error spawning " + characterObject.Name + ": " + ex.Message;
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new ServerMessage(report, false));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            }

            // Return false to skip original method
            return false;
        }

        /* Original method
         * 
        private Agent CreateAgent(Monster monster, bool isFemale, int instanceNo, Agent.CreationType creationType, float stepSize, int forcedAgentIndex, int weight, BasicCharacterObject characterObject)
		{
			AnimationSystemData animationSystemData = monster.FillAnimationSystemData(stepSize, false, isFemale);
			AgentCapsuleData agentCapsuleData = monster.FillCapsuleData();
			AgentSpawnData agentSpawnData = monster.FillSpawnData(null);
			Mission.AgentCreationResult agentCreationResult = this.CreateAgentInternal(monster.Flags, forcedAgentIndex, isFemale, ref agentSpawnData, ref agentCapsuleData, ref animationSystemData, instanceNo);
			Agent agent = new Agent(this, agentCreationResult, creationType, monster);
			agent.Character = characterObject;
			foreach (MissionBehavior missionBehavior in this.MissionBehaviors)
			{
				missionBehavior.OnAgentCreated(agent);
			}
			return agent;
		}*/
    }
}
