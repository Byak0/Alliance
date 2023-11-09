using HarmonyLib;
using NetworkMessages.FromServer;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Server.Patch.HarmonyPatch
{
    class Patch_SpawnedItemEntity
    {
        private static readonly Harmony Harmony = new Harmony(SubModule.ModuleId + nameof(Patch_SpawnedItemEntity));

        private static bool _patched;
        public static bool Patch()
        {
            try
            {
                if (_patched)
                    return false;
                _patched = true;
                Harmony.Patch(
                    typeof(SpawnedItemEntity).GetMethod("OnTickParallel2",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    prefix: new HarmonyMethod(typeof(Patch_SpawnedItemEntity).GetMethod(
                        nameof(Prefix_OnTickParallel2), BindingFlags.Static | BindingFlags.Public)));
            }
            catch (Exception e)
            {
                Log($"Alliance - ERROR in {nameof(Patch_SpawnedItemEntity)}", LogLevel.Error);
                Log(e.ToString(), LogLevel.Error);
                return false;
            }

            return true;
        }

        // Fix banners flying above the ground when dropped
        public static bool Prefix_OnTickParallel2(SpawnedItemEntity __instance, float dt, int ____usedChannelIndex,
            ActionIndexCache ____successActionIndex, ActionIndexCache ____progressActionIndex, Timer ____deletionTimer,
            ref bool ____readyToBeDeleted, GameEntity ____ownerGameEntity, MissionWeapon ____weapon,
            ref Vec3 ____fakeSimulationVelocity, Timer ____disablePhysicsTimer, GameEntity ____groundEntityWhenDisabled)
        {
            for (int i = __instance.GetMovingAgentCount() - 1; i >= 0; i--)
            {
                if (!__instance.GetMovingAgentWithIndex(i).IsActive())
                {
                    typeof(UsableMissionObject).GetField("_needsSingleThreadTickOnce", BindingFlags.Instance | BindingFlags.NonPublic)
                        .SetValue(__instance, true);
                }
            }

            if (!GameNetwork.IsClientOrReplay)
            {
                if (__instance.HasUser)
                {
                    ActionIndexValueCache currentActionValue = __instance.UserAgent.GetCurrentActionValue(____usedChannelIndex);
                    if (currentActionValue == ____successActionIndex)
                    {
                        __instance.UserAgent.StopUsingGameObjectMT(__instance.UserAgent.CanUseObject(__instance), Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
                    }
                    else if (currentActionValue != ____progressActionIndex)
                    {
                        __instance.UserAgent.StopUsingGameObjectMT(false, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
                    }
                }
                else if (__instance.HasLifeTime && ____deletionTimer.Check(Mission.Current.CurrentTime))
                {
                    ____readyToBeDeleted = true;
                }

                Traverse PhysicsStoppedTraverse = Traverse.Create(__instance).Property("PhysicsStopped");

                if (!(bool)PhysicsStoppedTraverse.GetValue())
                {
                    if (____ownerGameEntity != null)
                    {
                        if (____weapon.IsBanner())
                        {
                            MatrixFrame globalFrame = ____ownerGameEntity.GetGlobalFrame();
                            ____fakeSimulationVelocity.z = ____fakeSimulationVelocity.z - dt * 9.8f;
                            globalFrame.origin += ____fakeSimulationVelocity * dt;
                            ____ownerGameEntity.SetGlobalFrameMT(globalFrame);
                            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                            {
                                if (____ownerGameEntity.Scene.GetGroundHeightAtPositionMT(globalFrame.origin, BodyFlags.CommonCollisionExcludeFlags) > globalFrame.origin.z + 0.3f)
                                {
                                    PhysicsStoppedTraverse.SetValue(true);
                                    Log($"Alliance - Synchronizing entity {____weapon.Item.Name}", LogLevel.Error);
                                    // Synchronize physics with clients
                                    //if (GameNetwork.IsServerOrRecorder)
                                    //{
                                    //    MissionObjectId id = __instance.Id;
                                    //    GameNetwork.BeginBroadcastModuleEvent();
                                    //    GameNetwork.WriteMessage(new StopPhysicsAndSetFrameOfMissionObject(id, null, ____ownerGameEntity.GetFrame()));
                                    //    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
                                    //}
                                }
                                else
                                {
                                    // Synchronize banner position with clients when falling
                                    if (GameNetwork.IsServerOrRecorder)
                                    {
                                        MatrixFrame frame = ____ownerGameEntity.GetFrame();
                                        GameNetwork.BeginBroadcastModuleEvent();
                                        GameNetwork.WriteMessage(new SetMissionObjectFrame(__instance, ref frame));
                                        GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
                                    }
                                }
                            }
                            return false;
                        }

                        Vec3 globalPosition = ____ownerGameEntity.GlobalPosition;
                        if (globalPosition.z <= CompressionBasic.PositionCompressionInfo.GetMinimumValue() + 5f)
                        {
                            ____readyToBeDeleted = true;
                        }
                        if (!____ownerGameEntity.BodyFlag.HasAnyFlag(BodyFlags.Dynamic))
                        {
                            PhysicsStoppedTraverse.SetValue(true);
                            return false;
                        }
                        MatrixFrame globalFrame2 = ____ownerGameEntity.GetGlobalFrame();
                        if (!globalFrame2.rotation.IsUnit())
                        {
                            globalFrame2.rotation.Orthonormalize();
                            ____ownerGameEntity.SetGlobalFrame(globalFrame2);
                        }
                        bool flag = ____disablePhysicsTimer.Check(Mission.Current.CurrentTime);
                        if (flag || ____disablePhysicsTimer.ElapsedTime() > 1f)
                        {
                            bool flag2;
                            using (new TWSharedMutexUpgradeableReadLock(Scene.PhysicsAndRayCastLock))
                            {
                                flag2 = flag || ____ownerGameEntity.IsDynamicBodyStationaryMT();
                                if (flag2)
                                {
                                    ____groundEntityWhenDisabled = (GameEntity)typeof(SpawnedItemEntity).GetMethod("TryFindProperGroundEntityForSpawnedEntity",
                                                                                    BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });

                                    if (____groundEntityWhenDisabled != null)
                                    {
                                        ____groundEntityWhenDisabled.AddChild(__instance.GameEntity, true);
                                    }
                                    using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
                                    {
                                        if (!____weapon.IsEmpty && !____ownerGameEntity.BodyFlag.HasAnyFlag(BodyFlags.Disabled))
                                        {
                                            ____ownerGameEntity.DisableDynamicBodySimulationMT();
                                        }
                                        else
                                        {
                                            ____ownerGameEntity.RemovePhysicsMT(false);
                                        }
                                    }
                                }
                            }
                            if (flag2)
                            {
                                typeof(SpawnedItemEntity).GetMethod("ClampEntityPositionForStoppingIfNeeded", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });

                                PhysicsStoppedTraverse.SetValue(true);
                                if ((!__instance.IsDeactivated || ____groundEntityWhenDisabled != null) && !____weapon.IsEmpty && GameNetwork.IsServerOrRecorder)
                                {
                                    GameNetwork.BeginBroadcastModuleEvent();
                                    MissionObjectId id = __instance.Id;
                                    GameEntity groundEntityWhenDisabled = ____groundEntityWhenDisabled;
                                    GameNetwork.WriteMessage(new StopPhysicsAndSetFrameOfMissionObject(id, groundEntityWhenDisabled != null ? groundEntityWhenDisabled.GetFirstScriptOfType<MissionObject>() : null, ____ownerGameEntity.GetFrame()));
                                    GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
                                }
                            }
                        }
                        if (!(bool)PhysicsStoppedTraverse.GetValue())
                        {
                            Vec3 vec;
                            Vec3 vec2;
                            ____ownerGameEntity.GetPhysicsMinMax(true, out vec, out vec2, true);
                            MatrixFrame globalFrame3 = ____ownerGameEntity.GetGlobalFrame();
                            MatrixFrame previousGlobalFrame = ____ownerGameEntity.GetPreviousGlobalFrame();
                            Vec3 vec3 = globalFrame3.TransformToParent(vec);
                            Vec3 vec4 = previousGlobalFrame.TransformToParent(vec);
                            Vec3 vec5 = globalFrame3.TransformToParent(vec2);
                            Vec3 vec6 = previousGlobalFrame.TransformToParent(vec2);
                            Vec3 vec7 = Vec3.Vec3Min(vec3, vec5);
                            Vec3 vec8 = Vec3.Vec3Min(vec4, vec6);
                            float waterLevelAtPositionMT;
                            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                            {
                                waterLevelAtPositionMT = Mission.Current.GetWaterLevelAtPositionMT(vec7.AsVec2);
                            }
                            bool flag3 = vec7.z < waterLevelAtPositionMT;
                            if (vec8.z >= waterLevelAtPositionMT && flag3)
                            {
                                Vec3 linearVelocityMT;
                                using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
                                {
                                    linearVelocityMT = ____ownerGameEntity.GetLinearVelocityMT();
                                }
                                float num = ____ownerGameEntity.Mass * linearVelocityMT.Length;
                                if (num > 0f)
                                {
                                    num *= 0.0625f;
                                    num = MathF.Min(num, 1f);
                                    Vec3 vec9 = globalPosition;
                                    vec9.z = waterLevelAtPositionMT;
                                    SoundEventParameter soundEventParameter = new SoundEventParameter("Size", num);
                                    Mission.Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsWater, vec9, true, false, -1, -1, ref soundEventParameter);
                                    return false;
                                }
                            }
                        }
                    }
                    else
                    {
                        PhysicsStoppedTraverse.SetValue(true);
                    }
                }
            }
            return false;
        }

        // Reverse patch to access base implementation of UsableMissionObject
        //class ReversePatch_UsableMissionObject
        //{
        //    public static void OnTickParallel2(UsableMissionObject instance, float dt)
        //    {
        //        // Should never show if reverse patch works
        //        Log("Vulcain - ReversePatch_UsableMissionObject failed!", 0, Debug.DebugColor.Red);
        //    }
        //}

        /* Original method 
         * 
		protected internal override void OnTickParallel2(float dt)
		{
			base.OnTickParallel2(dt);
			if (!GameNetwork.IsClientOrReplay)
			{
				if (base.HasUser)
				{
					ActionIndexValueCache currentActionValue = base.UserAgent.GetCurrentActionValue(this._usedChannelIndex);
					if (currentActionValue == this._successActionIndex)
					{
						base.UserAgent.StopUsingGameObjectMT(base.UserAgent.CanUseObject(this), Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
					else if (currentActionValue != this._progressActionIndex)
					{
						base.UserAgent.StopUsingGameObjectMT(false, Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
				}
				else if (this.HasLifeTime && this._deletionTimer.Check(Mission.Current.CurrentTime))
				{
					this._readyToBeDeleted = true;
				}
				if (!this.PhysicsStopped)
				{
					if (this._ownerGameEntity != null)
					{
						if (this._weapon.IsBanner())
						{
							MatrixFrame globalFrame = this._ownerGameEntity.GetGlobalFrame();
							this._fakeSimulationVelocity.z = this._fakeSimulationVelocity.z - dt * 9.8f;
							globalFrame.origin += this._fakeSimulationVelocity * dt;
							this._ownerGameEntity.SetGlobalFrameMT(globalFrame);
							using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
							{
								if (this._ownerGameEntity.Scene.GetGroundHeightAtPositionMT(globalFrame.origin, BodyFlags.CommonCollisionExcludeFlags) > globalFrame.origin.z + 0.3f)
								{
									this.PhysicsStopped = true;
								}
								return;
							}
						}
						Vec3 globalPosition = this._ownerGameEntity.GlobalPosition;
						if (globalPosition.z <= CompressionBasic.PositionCompressionInfo.GetMinimumValue() + 5f)
						{
							this._readyToBeDeleted = true;
						}
						if (!this._ownerGameEntity.BodyFlag.HasAnyFlag(BodyFlags.Dynamic))
						{
							this.PhysicsStopped = true;
							return;
						}
						MatrixFrame globalFrame2 = this._ownerGameEntity.GetGlobalFrame();
						if (!globalFrame2.rotation.IsUnit())
						{
							globalFrame2.rotation.Orthonormalize();
							this._ownerGameEntity.SetGlobalFrame(globalFrame2);
						}
						bool flag = this._disablePhysicsTimer.Check(Mission.Current.CurrentTime);
						if (flag || this._disablePhysicsTimer.ElapsedTime() > 1f)
						{
							bool flag2;
							using (new TWSharedMutexUpgradeableReadLock(Scene.PhysicsAndRayCastLock))
							{
								flag2 = flag || this._ownerGameEntity.IsDynamicBodyStationaryMT();
								if (flag2)
								{
									this._groundEntityWhenDisabled = this.TryFindProperGroundEntityForSpawnedEntity();
									if (this._groundEntityWhenDisabled != null)
									{
										this._groundEntityWhenDisabled.AddChild(base.GameEntity, true);
									}
									using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
									{
										if (!this._weapon.IsEmpty && !this._ownerGameEntity.BodyFlag.HasAnyFlag(BodyFlags.Disabled))
										{
											this._ownerGameEntity.DisableDynamicBodySimulationMT();
										}
										else
										{
											this._ownerGameEntity.RemovePhysicsMT(false);
										}
									}
								}
							}
							if (flag2)
							{
								this.ClampEntityPositionForStoppingIfNeeded();
								this.PhysicsStopped = true;
								if ((!base.IsDeactivated || this._groundEntityWhenDisabled != null) && !this._weapon.IsEmpty && GameNetwork.IsServerOrRecorder)
								{
									GameNetwork.BeginBroadcastModuleEvent();
									MissionObjectId id = base.Id;
									GameEntity groundEntityWhenDisabled = this._groundEntityWhenDisabled;
									GameNetwork.WriteMessage(new StopPhysicsAndSetFrameOfMissionObject(id, (groundEntityWhenDisabled != null) ? groundEntityWhenDisabled.GetFirstScriptOfType<MissionObject>() : null, this._ownerGameEntity.GetFrame()));
									GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
								}
							}
						}
						if (!this.PhysicsStopped)
						{
							Vec3 vec;
							Vec3 vec2;
							this._ownerGameEntity.GetPhysicsMinMax(true, out vec, out vec2, true);
							MatrixFrame globalFrame3 = this._ownerGameEntity.GetGlobalFrame();
							MatrixFrame previousGlobalFrame = this._ownerGameEntity.GetPreviousGlobalFrame();
							Vec3 vec3 = globalFrame3.TransformToParent(vec);
							Vec3 vec4 = previousGlobalFrame.TransformToParent(vec);
							Vec3 vec5 = globalFrame3.TransformToParent(vec2);
							Vec3 vec6 = previousGlobalFrame.TransformToParent(vec2);
							Vec3 vec7 = Vec3.Vec3Min(vec3, vec5);
							Vec3 vec8 = Vec3.Vec3Min(vec4, vec6);
							float waterLevelAtPositionMT;
							using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
							{
								waterLevelAtPositionMT = Mission.Current.GetWaterLevelAtPositionMT(vec7.AsVec2);
							}
							bool flag3 = vec7.z < waterLevelAtPositionMT;
							if (vec8.z >= waterLevelAtPositionMT && flag3)
							{
								Vec3 linearVelocityMT;
								using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
								{
									linearVelocityMT = this._ownerGameEntity.GetLinearVelocityMT();
								}
								float num = this._ownerGameEntity.Mass * linearVelocityMT.Length;
								if (num > 0f)
								{
									num *= 0.0625f;
									num = MathF.Min(num, 1f);
									Vec3 vec9 = globalPosition;
									vec9.z = waterLevelAtPositionMT;
									SoundEventParameter soundEventParameter = new SoundEventParameter("Size", num);
									Mission.Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsWater, vec9, true, false, -1, -1, ref soundEventParameter);
									return;
								}
							}
						}
					}
					else
					{
						this.PhysicsStopped = true;
					}
				}
			}
		}*/
    }
}
