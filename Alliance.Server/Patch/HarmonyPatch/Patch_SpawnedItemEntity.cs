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
		// As of 1.3 it also requires a client patch to make dropped banner focusable
		public static bool Prefix_OnTickParallel2(
			SpawnedItemEntity __instance, float dt,
			// Accessing private fields of SpawnedItemEntity using Harmony
			int ____usedChannelIndex, ActionIndexCache ____successActionIndex, ActionIndexCache ____progressActionIndex, Timer ____deletionTimer,
			ref bool ____readyToBeDeleted, GameEntity ____ownerGameEntity, MissionWeapon ____weapon,
			ref Vec3 ____fakeSimulationVelocity, Timer ____disablePhysicsTimer, ref GameEntity ____groundEntityWhenDisabled, ref bool ____alreadyMadeWaterDropSound, ref bool ____disableDynamicPhysicsNextFrame)
		{
			// Base method
			for (int i = __instance.GetMovingAgentCount() - 1; i >= 0; i--)
			{
				if (!__instance.GetMovingAgentWithIndex(i).IsActive())
				{
					typeof(UsableMissionObject).GetField("_needsSingleThreadTickOnce", BindingFlags.Instance | BindingFlags.NonPublic)
						.SetValue(__instance, true);
				}
			}

			// Native method
			if (!GameNetwork.IsClientOrReplay)
			{
				if (__instance.HasUser)
				{
					ActionIndexCache currentActionValue = __instance.UserAgent.GetCurrentAction(____usedChannelIndex);
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
							____ownerGameEntity.SetGlobalFrame(globalFrame);
							using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
							{
								if (____ownerGameEntity.Scene.GetGroundHeightAtPosition(globalFrame.origin, BodyFlags.CommonCollisionExcludeFlags) > globalFrame.origin.z + 0.3f)
								{
									// Prepare to disable physics on next frame (OnTick)
									____groundEntityWhenDisabled = (GameEntity)typeof(SpawnedItemEntity).GetMethod("TryFindProperGroundEntityForSpawnedEntity",
																					BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });
									____disableDynamicPhysicsNextFrame = true;
								}
								else
								{
									// Synchronize banner position with clients when falling
									if (GameNetwork.IsServerOrRecorder)
									{
										MatrixFrame frame = ____ownerGameEntity.GetFrame();
										GameNetwork.BeginBroadcastModuleEvent();
										GameNetwork.WriteMessage(new SetMissionObjectFrame(__instance.Id, ref frame));
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
							flag2 = flag || ____ownerGameEntity.IsDynamicBodyStationaryMT();
							if (flag2)
							{
								____groundEntityWhenDisabled = (GameEntity)typeof(SpawnedItemEntity).GetMethod("TryFindProperGroundEntityForSpawnedEntity",
																				BindingFlags.Instance | BindingFlags.NonPublic).Invoke(__instance, new object[] { });
								if (____groundEntityWhenDisabled != null)
								{
									____groundEntityWhenDisabled.WeakEntity.AddChild(__instance.GameEntity, true);
								}
								using (new TWSharedMutexWriteLock(Scene.PhysicsAndRayCastLock))
								{
									if (!____weapon.IsEmpty && !____ownerGameEntity.BodyFlag.HasAnyFlag(BodyFlags.Disabled))
									{
										____ownerGameEntity.DisableDynamicBodySimulationMT();
									}
									else
									{
										____ownerGameEntity.RemovePhysics(false);
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
									MissionObjectId idParent = groundEntityWhenDisabled != null ? groundEntityWhenDisabled.GetFirstScriptOfType<MissionObject>().Id : MissionObjectId.Invalid;
									GameNetwork.WriteMessage(new StopPhysicsAndSetFrameOfMissionObject(id, idParent, ____ownerGameEntity.GetFrame()));
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
								waterLevelAtPositionMT = Mission.Current.GetWaterLevelAtPositionMT(vec7.AsVec2, true);
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

		/* Original method 
		protected internal override void OnTickParallel2(float dt)
		{
			base.OnTickParallel2(dt);
			if (!GameNetwork.IsClientOrReplay)
			{
				if (base.HasUser)
				{
					ActionIndexCache currentAction = base.UserAgent.GetCurrentAction(this._usedChannelIndex);
					if (currentAction == this._successActionIndex)
					{
						base.UserAgent.StopUsingGameObjectMT(base.UserAgent.CanUseObject(this) && !base.UserAgent.IsInWater(), Agent.StopUsingGameObjectFlags.AutoAttachAfterStoppingUsingGameObject);
					}
					else if (currentAction != this._progressActionIndex)
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
							this._ownerGameEntity.SetGlobalFrame(globalFrame, true);
							if (this._ownerGameEntity.Scene.GetGroundHeightAtPosition(globalFrame.origin, BodyFlags.CommonCollisionExcludeFlags) > globalFrame.origin.z + 0.3f)
							{
								this.PhysicsStopped = true;
								return;
							}
						}
						else
						{
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
								this._ownerGameEntity.SetGlobalFrame(globalFrame2, true);
							}
							bool flag = this._disablePhysicsTimer.Check(Mission.Current.CurrentTime);
							if ((flag || this._disablePhysicsTimer.ElapsedTime() > 1f) && (flag || this._ownerGameEntity.IsDynamicBodyStationaryMT()))
							{
								this._groundEntityWhenDisabled = this.TryFindProperGroundEntityForSpawnedEntity();
								this._disableDynamicPhysicsNextFrame = true;
							}
							if (!this.PhysicsStopped && this._disablePhysicsTimer.ElapsedTime() > 0.2f)
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
								Vec3 vec9 = Vec3.Vec3Max(vec3, vec5);
								float waterLevelAtPositionMT = Mission.Current.GetWaterLevelAtPositionMT(vec7.AsVec2, !GameNetwork.IsMultiplayer);
								bool flag2 = vec7.z < waterLevelAtPositionMT;
								bool flag3 = vec8.z < waterLevelAtPositionMT;
								if (flag2)
								{
									this._disablePhysicsTimer.AdjustStartTime(dt * 0.8f);
									float num = waterLevelAtPositionMT - 3.5f;
									if (vec9.z < num)
									{
										this._readyToBeDeleted = true;
									}
									if (!flag3)
									{
										BodyFlags bodyFlags;
										base.GameEntity.Scene.GetGroundHeightAndBodyFlagsAtPosition(globalFrame3.origin, out bodyFlags, BodyFlags.CommonCollisionExcludeFlagsForCombat);
										if (!bodyFlags.HasAnyFlag(BodyFlags.Moveable))
										{
											Vec3 linearVelocityMT = this._ownerGameEntity.GetLinearVelocityMT();
											float num2 = this._ownerGameEntity.Mass * linearVelocityMT.Length;
											if (!this._alreadyMadeWaterDropSound && num2 > 0f)
											{
												num2 *= 0.0625f;
												num2 = MathF.Min(num2, 1f);
												Vec3 vec10 = globalPosition;
												vec10.z = waterLevelAtPositionMT;
												SoundEventParameter soundEventParameter = new SoundEventParameter("Size", num2);
												Mission.Current.MakeSound(ItemPhysicsSoundContainer.SoundCodePhysicsWater, vec10, false, true, -1, -1, ref soundEventParameter);
												this._alreadyMadeWaterDropSound = true;
											}
										}
									}
								}
								if (flag2 != flag3)
								{
									float num3 = (flag2 ? 100f : 1f);
									PhysicsMaterial physicsMaterial = base.GameEntity.GetPhysicsMaterial();
									float num4 = physicsMaterial.GetLinearDamping() * num3;
									float num5 = physicsMaterial.GetAngularDamping() * num3;
									if (num4 > 15f)
									{
										num4 = 15f;
									}
									if (num5 > 15f)
									{
										num5 = 15f;
									}
									base.GameEntity.SetDampingMT(num4, num5);
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
