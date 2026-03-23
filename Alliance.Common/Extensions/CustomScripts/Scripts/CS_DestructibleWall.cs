using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.CustomScripts.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Network.Messages;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// This script is based on the native DestructableComponent, making an object destructible.
	/// When destroyed, it can also enable / disable 2 different NavMesh to allow or block AI from using them.
	/// NavigationMeshIdEnabledOnDestroy is disabled at first, and enabled after destruction(e.g. for NavMesh under the wall).
	/// NavigationMeshIdDisabledOnDestroy is enabled at first, and disabled after destruction(e.g. for NavMesh ON the wall).
	/// </summary>
	public class CS_DestructibleWall : SynchedMissionObject
	{
		public delegate void OnHitTakenAndDestroyedDelegate(CS_DestructibleWall target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage);

		public const string CleanStateTag = "operational";

		public static float MaxBlowMagnitude = 20f;

		public string DestructionStates;

		public bool DestroyedByStoneOnly;

		public bool CanBeDestroyedInitially = true;

		public float MaxHitPoint = 100f;

		public bool DestroyOnAnyHit;

		public bool PassHitOnToParent;

		public string ReferenceEntityTag;

		public string HeavyHitParticlesTag;

		public float HeavyHitParticlesThreshold = 5f;

		public string ParticleEffectOnDestroy = "";

		public string SoundEffectOnDestroy = "";

		public float SoundAndParticleEffectHeightOffset;

		public float SoundAndParticleEffectForwardOffset;

		public BattleSideEnum BattleSide = BattleSideEnum.None;

		public int NavigationMeshIdEnabledOnDestroy = -1;

		public int NavigationMeshIdDisabledOnDestroy = -1;

		[EditableScriptComponentVariable(false)]
		public Func<int, int, int, int> OnCalculateDestructionStateIndex;

		private float _hitPoint;

		private string OriginalStateTag = "operational";

		private GameEntity _referenceEntity;

		private GameEntity _previousState;

		private GameEntity _originalState;

		private string[] _destructionStates;

		private int _currentStateIndex;

		private IEnumerable<GameEntity> _heavyHitParticles;

		public float HitPoint
		{
			get
			{
				return _hitPoint;
			}
			set
			{
				if (!_hitPoint.Equals(value))
				{
					_hitPoint = MathF.Max(value, 0f);
					if (GameNetwork.IsServerOrRecorder)
					{
						GameNetwork.BeginBroadcastModuleEvent();
						GameNetwork.WriteMessage(new SyncObjectHitpoints(Id, value));
						GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
					}
				}
			}
		}

		public FocusableObjectType FocusableObjectType => FocusableObjectType.None;

		public bool IsDestroyed => HitPoint <= 0f;

		public GameEntity CurrentState { get; private set; }

		private bool HasDestructionState
		{
			get
			{
				return _destructionStates != null && !_destructionStates.IsEmpty();
			}
		}

		public event Action OnNextDestructionState;

		public event OnHitTakenAndDestroyedDelegate OnDestroyed;

		public event OnHitTakenAndDestroyedDelegate OnHitTaken;

		static CS_DestructibleWall()
		{
		}

		protected override void OnInit()
		{
			base.OnInit();
			//_hitPoint = MaxHitPoint;
			_referenceEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(string.IsNullOrEmpty(ReferenceEntityTag) ? GameEntity : GameEntity.GetFirstChildEntityWithTag(ReferenceEntityTag));
			if (!string.IsNullOrEmpty(DestructionStates))
			{
				_destructionStates = DestructionStates.Replace(" ", string.Empty).Split(new char[] { ',' });
				bool flag = false;
				string[] destructionStates = _destructionStates;
				for (int i = 0; i < destructionStates.Length; i++)
				{
					string item = destructionStates[i];
					if (!string.IsNullOrEmpty(item))
					{
						WeakGameEntity gameEntity = GameEntity.GetChildren().FirstOrDefault((x) => x.Name == item);
						if (gameEntity.IsValid)
						{
							gameEntity.AddBodyFlags(BodyFlags.Moveable, true);
							PhysicsShape bodyShape = gameEntity.GetBodyShape();
							if (bodyShape != null)
							{
								PhysicsShape.AddPreloadQueueWithName(bodyShape.GetName(), gameEntity.GetGlobalScale());
								flag = true;
							}
						}
						else
						{
							GameEntity gameEntity2 = TaleWorlds.Engine.GameEntity.Instantiate(null, item, false, true, "");
							List<GameEntity> list = new List<GameEntity>();
							gameEntity2.GetChildrenRecursive(ref list);
							list.Add(gameEntity2);
							foreach (GameEntity gameEntity3 in list)
							{
								PhysicsShape bodyShape2 = gameEntity3.GetBodyShape();
								if (bodyShape2 != null)
								{
									Vec3 globalScale = gameEntity3.GetGlobalScale();
									Vec3 globalScale2 = _referenceEntity.GetGlobalScale();
									Vec3 vec = new Vec3(globalScale.x * globalScale2.x, globalScale.y * globalScale2.y, globalScale.z * globalScale2.z, -1f);
									PhysicsShape.AddPreloadQueueWithName(bodyShape2.GetName(), vec);
									flag = true;
								}
							}
						}
					}
				}

				// TODO temporary fix to prevent instant destruction and switch states correctly
				MaxHitPoint = _destructionStates.Count();
				_hitPoint = MaxHitPoint;

				if (flag)
				{
					PhysicsShape.ProcessPreloadQueue();
				}
			}
			WeakGameEntity originalState = this.GetOriginalState(GameEntity);
			_originalState = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(originalState.IsValid ? originalState : GameEntity);
			CurrentState = _originalState;
			_originalState.AddBodyFlags(BodyFlags.Moveable, true);
			List<WeakGameEntity> list2 = new List<WeakGameEntity>();
			GameEntity.GetChildrenRecursive(ref list2);
			foreach (WeakGameEntity weakGameEntity2 in Enumerable.Where<WeakGameEntity>(list2, (WeakGameEntity child) => child.BodyFlag.HasAnyFlag(BodyFlags.Dynamic)))
			{
				GameEntityPhysicsExtensions.SetPhysicsState(weakGameEntity2, false, true);
				weakGameEntity2.SetFrameChanged();
			}
			_heavyHitParticles = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(GameEntity).CollectChildrenEntitiesWithTag(HeavyHitParticlesTag);
			GameEntity.SetAnimationSoundActivation(true);

			// At start, disable NavigationMeshIdEnabledOnDestroy
			SetAbilityOfNavmesh(false, true);
		}

		public WeakGameEntity GetOriginalState(WeakGameEntity parent)
		{
			int childCount = parent.ChildCount;
			for (int i = 0; i < childCount; i++)
			{
				WeakGameEntity child = parent.GetChild(i);
				if (!child.HasScriptOfType<DestructableComponent>())
				{
					if (child.HasTag(OriginalStateTag))
					{
						return child;
					}
					WeakGameEntity originalState = GetOriginalState(child);
					if (originalState != null)
					{
						return originalState;
					}
				}
			}
			return WeakGameEntity.Invalid;
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			_referenceEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(string.IsNullOrEmpty(ReferenceEntityTag) ? GameEntity : GameEntity.GetFirstChildEntityWithTag(ReferenceEntityTag));
		}

		protected override void OnEditorVariableChanged(string variableName)
		{
			base.OnEditorVariableChanged(variableName);
			if (variableName.Equals(ReferenceEntityTag))
			{
				_referenceEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(string.IsNullOrEmpty(ReferenceEntityTag) ? GameEntity : GameEntity.GetFirstChildEntityWithTag(ReferenceEntityTag));
			}
		}

		protected override void OnMissionReset()
		{
			base.OnMissionReset();
			Reset();
		}

		public void Reset()
		{
			RestoreEntity();
			_hitPoint = MaxHitPoint;
			_currentStateIndex = 0;
		}

		private void RestoreEntity()
		{
			if (_destructionStates != null)
			{
				for (int i = 0; i < _destructionStates.Length; i++)
				{
					WeakGameEntity gameEntity = GameEntity.GetChildren().FirstOrDefault((x) => x.Name == _destructionStates[i].ToString());
					if (gameEntity.IsValid)
					{
						Skeleton skeleton = gameEntity.Skeleton;
						skeleton?.SetAnimationAtChannel(-1, 0, 1f, -1f, 0f);
					}
				}
			}
			if (CurrentState != _originalState)
			{
				CurrentState.SetVisibilityExcludeParents(false);
				CurrentState = _originalState;
			}
			CurrentState.SetVisibilityExcludeParents(true);
			CurrentState.SetPhysicsState(true, true);
			CurrentState.SetFrameChanged();

			// Restore Navmesh
			SetAbilityOfNavmesh(false, true);
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			if (_referenceEntity != null && _referenceEntity != GameEntity && MBEditor.IsEntitySelected(_referenceEntity))
			{
				new Vec3(-2f, -0.5f, -1f);
				new Vec3(2f, 0.5f, 1f);
				MatrixFrame output = MatrixFrame.Identity;
				GameEntity gameEntity = _referenceEntity;
				while (gameEntity.Parent != null)
				{
					gameEntity = gameEntity.Parent;
				}

				gameEntity.GetMeshBendedFrame(_referenceEntity.GetGlobalFrame(), ref output);
			}
		}

		public void TriggerOnHit(Agent attackerAgent, int inflictedDamage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex, ScriptComponentBehavior attackerScriptComponentBehavior)
		{
			OnHit(attackerAgent, inflictedDamage, impactPosition, impactDirection, weapon, affectorWeaponSlotOrMissileIndex, attackerScriptComponentBehavior, out bool flag, out float num);
		}

		protected override bool OnHit(Agent attackerAgent, int inflictedDamage, Vec3 impactPosition, Vec3 impactDirection, in MissionWeapon weapon, int affectorWeaponSlotOrMissileIndex, ScriptComponentBehavior attackerScriptComponentBehavior, out bool reportDamage, out float modifiedDamage)
		{
			reportDamage = false;
			modifiedDamage = (float)inflictedDamage;
			if (IsDisabled)
			{
				return true;
			}
			MissionWeapon missionWeapon = weapon;
			if (missionWeapon.IsEmpty && !(attackerScriptComponentBehavior is BatteringRam))
			{
				inflictedDamage = 0;
			}
			else if (DestroyedByStoneOnly)
			{
				missionWeapon = weapon;
				WeaponComponentData currentUsageItem = missionWeapon.CurrentUsageItem;
				if (currentUsageItem.WeaponClass != WeaponClass.Stone && currentUsageItem.WeaponClass != WeaponClass.Boulder || !currentUsageItem.WeaponFlags.HasAnyFlag(WeaponFlags.NotUsableWithOneHand))
				{
					inflictedDamage = 0;
				}
			}
			bool isDestroyed = IsDestroyed;
			if (DestroyOnAnyHit)
			{
				inflictedDamage = (int)(MaxHitPoint + 1f);
			}
			if (inflictedDamage > 0 && !isDestroyed)
			{
				// TODO temporary fix to prevent instant destruction and switch states correctly
				inflictedDamage = 1;

				HitPoint -= inflictedDamage;
				if (inflictedDamage > HeavyHitParticlesThreshold)
				{
					BurstHeavyHitParticles();
				}
				int num = CalculateNextDestructionLevel(inflictedDamage);
				if (!IsDestroyed)
				{
					OnHitTakenAndDestroyedDelegate onHitTaken = OnHitTaken;
					if (onHitTaken != null)
					{
						onHitTaken(this, attackerAgent, weapon, attackerScriptComponentBehavior, inflictedDamage);
					}
				}
				else if (IsDestroyed && !isDestroyed)
				{
					//Mission.Current.OnObjectDisabled(this);
					GameEntity.GetFirstScriptOfType<UsableMachine>()?.Disable();
					this?.SetAbilityOfFaces(enabled: false);
					//foreach (MissionBehavior missionBehavior in Mission.Current.MissionBehaviors)
					//{
					//missionBehavior.OnObjectDisabled(this);
					//}

					OnHitTakenAndDestroyedDelegate onHitTaken2 = OnHitTaken;
					if (onHitTaken2 != null)
					{
						onHitTaken2(this, attackerAgent, weapon, attackerScriptComponentBehavior, inflictedDamage);
					}
					OnHitTakenAndDestroyedDelegate onDestroyed = OnDestroyed;
					if (onDestroyed != null)
					{
						onDestroyed(this, attackerAgent, weapon, attackerScriptComponentBehavior, inflictedDamage);
					}
					MatrixFrame globalFrame = GameEntity.GetGlobalFrame();
					globalFrame.origin += globalFrame.rotation.u * SoundAndParticleEffectHeightOffset + globalFrame.rotation.f * SoundAndParticleEffectForwardOffset;
					globalFrame.rotation.Orthonormalize();
					if (ParticleEffectOnDestroy != "")
					{
						Mission.Current.Scene.CreateBurstParticle(ParticleSystemManager.GetRuntimeIdByName(ParticleEffectOnDestroy), globalFrame);
					}
					if (SoundEffectOnDestroy != "")
					{
						//Mission.Current.MakeSound(SoundEvent.GetEventIdFromString(SoundEffectOnDestroy), globalFrame.origin, false, true, (attackerAgent != null) ? attackerAgent.Index : (-1), -1);
						SyncSound();
					}

					// At destruction, enable NavigationMeshIdEnabledOnDestroy and disable NavigationMeshIdDisabledOnDestroy
					SetAbilityOfNavmesh(true, false);
				}

				SetDestructionLevel(num, -1, inflictedDamage, impactPosition, impactDirection);
				reportDamage = true;
			}

			return !PassHitOnToParent;
		}

		public void SyncSound()
		{
			if (GameNetwork.IsClient)
			{
				AudioPlayer.Instance.Play(AudioPlayer.Instance.GetAudioId(SoundEffectOnDestroy), 1f, false, 10000, GameEntity.GetGlobalFrame().origin);
			}
			else if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncSoundDestructible(Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		public void SetAbilityOfNavmesh(bool Navmesh1, bool Navmesh2)
		{
			Log($"Disabling navmesh {NavigationMeshIdEnabledOnDestroy}, enabling navmesh {NavigationMeshIdDisabledOnDestroy}", LogLevel.Debug);

			if (NavigationMeshIdEnabledOnDestroy != -1)
			{
				Scene.SetAbilityOfFacesWithId(NavigationMeshIdEnabledOnDestroy, Navmesh1);
			}
			if (NavigationMeshIdDisabledOnDestroy != -1)
			{
				Scene.SetAbilityOfFacesWithId(NavigationMeshIdDisabledOnDestroy, Navmesh2);
			}

			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncAbilityOfNavmesh(Id, Navmesh1, Navmesh2));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		public void BurstHeavyHitParticles()
		{
			foreach (GameEntity heavyHitParticle in _heavyHitParticles)
			{
				heavyHitParticle.BurstEntityParticle(doChildren: false);
			}

			if (GameNetwork.IsServerOrRecorder)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new BurstAllHeavyHitParticles(Id));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		private int CalculateNextDestructionLevel(int inflictedDamage)
		{
			if (HasDestructionState)
			{
				int num = _destructionStates.Length;
				float num2 = MaxHitPoint / num;
				float num3 = MaxHitPoint;
				int num4 = 0;
				while (num3 - num2 >= HitPoint)
				{
					num3 -= num2;
					num4++;
				}
				Func<int, int, int, int> onCalculateDestructionStateIndex = OnCalculateDestructionStateIndex;
				return onCalculateDestructionStateIndex != null ? onCalculateDestructionStateIndex(num4, inflictedDamage, DestructionStates.Length) : num4;
			}
			if (IsDestroyed)
			{
				return _currentStateIndex + 1;
			}
			return _currentStateIndex;
		}

		public void SetDestructionLevel(int state, int forcedId, float blowMagnitude, Vec3 blowPosition, Vec3 blowDirection, bool noEffects = false)
		{
			if (_currentStateIndex != state)
			{
				float num = MBMath.ClampFloat(blowMagnitude, 1f, DestructableComponent.MaxBlowMagnitude);
				_currentStateIndex = state;
				ReplaceEntityWithBrokenEntity(forcedId);
				if (CurrentState != null)
				{
					foreach (GameEntity gameEntity in from child in CurrentState.GetChildren()
													  where child.BodyFlag.HasAnyFlag(BodyFlags.Dynamic)
													  select child)
					{
						gameEntity.SetPhysicsState(true, true);
						gameEntity.SetFrameChanged();
					}
					if (!GameNetwork.IsDedicatedServer && !noEffects)
					{
						CurrentState.BurstEntityParticle(true);
						ApplyPhysics(num, blowPosition, blowDirection);
					}
					Action onNextDestructionState = OnNextDestructionState;
					if (onNextDestructionState != null)
					{
						onNextDestructionState();
					}
				}
				if (GameNetwork.IsServerOrRecorder)
				{
					if (CurrentState != null)
					{
						MissionObject firstScriptOfType = CurrentState.GetFirstScriptOfType<MissionObject>();
						if (firstScriptOfType != null)
						{
							forcedId = firstScriptOfType.Id.Id;
						}
					}
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new SyncObjectDestructionLevel(Id, state, forcedId, num, blowPosition, blowDirection));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
				}
			}
		}

		private void ApplyPhysics(float blowMagnitude, Vec3 blowPosition, Vec3 blowDirection)
		{
			if (CurrentState != null)
			{
				IEnumerable<GameEntity> enumerable = from child in CurrentState.GetChildren()
													 where child.HasBody() && child.BodyFlag.HasAnyFlag(BodyFlags.Dynamic) && !child.HasScriptOfType<SpawnedItemEntity>()
													 select child;
				int num = enumerable.Count();
				float num2 = num > 1 ? blowMagnitude / num : blowMagnitude;
				foreach (GameEntity gameEntity in enumerable)
				{
					gameEntity.ApplyLocalImpulseToDynamicBody(Vec3.Zero, blowDirection * num2);
					Mission.Current.AddTimerToDynamicEntity(gameEntity, 10f + MBRandom.RandomFloat * 2f);
				}
			}
		}

		private void ReplaceEntityWithBrokenEntity(int forcedId)
		{
			_previousState = CurrentState;
			_previousState.SetVisibilityExcludeParents(false);
			if (HasDestructionState)
			{
				bool flag;
				CurrentState = AddBrokenEntity(_destructionStates[_currentStateIndex - 1], out flag);
				if (flag)
				{
					if (_originalState != GameEntity)
					{
						GameEntity.AddChild(CurrentState.WeakEntity, true);
					}
					if (forcedId != -1)
					{
						MissionObject firstScriptOfType = CurrentState.GetFirstScriptOfType<MissionObject>();
						if (firstScriptOfType != null)
						{
							firstScriptOfType.Id = new MissionObjectId(forcedId, true);
							using (IEnumerator<GameEntity> enumerator = CurrentState.GetChildren().GetEnumerator())
							{
								while (enumerator.MoveNext())
								{
									GameEntity gameEntity = enumerator.Current;
									MissionObject firstScriptOfType2 = gameEntity.GetFirstScriptOfType<MissionObject>();
									if (firstScriptOfType2 != null && firstScriptOfType2.Id.CreatedAtRuntime)
									{
										firstScriptOfType2.Id = new MissionObjectId(++forcedId, true);
									}
								}
								return;
							}
						}
						MBDebug.ShowWarning("Current destruction state doesn't have mission object script component.");
						return;
					}
				}
				else
				{
					MatrixFrame frame = _previousState.GetFrame();
					CurrentState.SetFrame(ref frame);
				}
			}
		}

		protected override bool MovesEntity()
		{
			return true;
		}

		public void PreDestroy()
		{
			OnHitTakenAndDestroyedDelegate onDestroyed = OnDestroyed;
			if (onDestroyed != null)
			{
				onDestroyed(this, null, MissionWeapon.Invalid, null, 0);
			}
			SetVisibleSynched(false, true);
		}

		private GameEntity AddBrokenEntity(string prefab, out bool newCreated)
		{
			if (!string.IsNullOrEmpty(prefab))
			{
				int childCount = base.GameEntity.ChildCount;
				int num = 0;
				WeakGameEntity weakGameEntity = WeakGameEntity.Invalid;
				for (int i = 0; i < childCount; i++)
				{
					WeakGameEntity child = base.GameEntity.GetChild(i);
					if (child.Name == prefab)
					{
						num++;
						if (MBRandom.RandomInt(num) == 0)
						{
							weakGameEntity = child;
						}
					}
				}
				GameEntity gameEntity;
				if (weakGameEntity.IsValid)
				{
					gameEntity = TaleWorlds.Engine.GameEntity.CreateFromWeakEntity(weakGameEntity);
					weakGameEntity.SetVisibilityExcludeParents(true);
					if (!GameNetwork.IsClientOrReplay)
					{
						MissionObject missionObject = Enumerable.FirstOrDefault<MissionObject>(weakGameEntity.GetScriptComponents<MissionObject>());
						if (missionObject != null)
						{
							missionObject.SetAbilityOfFaces(true);
						}
					}
					newCreated = false;
				}
				else
				{
					gameEntity = TaleWorlds.Engine.GameEntity.Instantiate(Mission.Current.Scene, prefab, this._referenceEntity.GetGlobalFrame(), true, "");
					if (gameEntity != null)
					{
						gameEntity.SetMobility(TaleWorlds.Engine.GameEntity.Mobility.Stationary);
					}
					if (base.GameEntity.Parent.IsValid)
					{
						base.GameEntity.Parent.AddChild(gameEntity.WeakEntity, true);
					}
					newCreated = true;
				}
				if (this._referenceEntity.Skeleton != null && gameEntity.Skeleton != null)
				{
					Skeleton skeleton = ((this.CurrentState != this._originalState) ? this.CurrentState : this._referenceEntity).Skeleton;
					int animationIndexAtChannel = skeleton.GetAnimationIndexAtChannel(0);
					float animationParameterAtChannel = skeleton.GetAnimationParameterAtChannel(0);
					if (animationIndexAtChannel != -1)
					{
						gameEntity.Skeleton.SetAnimationAtChannel(animationIndexAtChannel, 0, 1f, -1f, animationParameterAtChannel);
						gameEntity.ResumeSkeletonAnimation();
					}
				}
				// new 1.3 script for coloring ships ? Not useful here
				//WeakGameEntity weakGameEntity2 = base.GameEntity;
				//while (weakGameEntity2 != null)
				//{
				//	ColorAssigner firstScriptOfType = weakGameEntity2.GetFirstScriptOfType<ColorAssigner>();
				//	if (firstScriptOfType != null)
				//	{
				//		firstScriptOfType.SetColor(gameEntity.WeakEntity);
				//		break;
				//	}
				//	weakGameEntity2 = weakGameEntity2.Parent;
				//}
				return gameEntity;
			}
			newCreated = false;
			return null;
		}

		public override void WriteToNetwork()
		{
			base.WriteToNetwork();
			GameNetworkMessage.WriteFloatToPacket(MathF.Max(HitPoint, 0f), CompressionMission.UsableGameObjectHealthCompressionInfo);
			GameNetworkMessage.WriteIntToPacket(_currentStateIndex, CompressionMission.UsableGameObjectDestructionStateCompressionInfo);
			if (_currentStateIndex != 0)
			{
				MissionObject firstScriptOfType = CurrentState.GetFirstScriptOfType<MissionObject>();
				GameNetworkMessage.WriteBoolToPacket(firstScriptOfType != null);
				if (firstScriptOfType != null)
				{
					GameNetworkMessage.WriteMissionObjectIdToPacket(firstScriptOfType.Id);
				}
			}
		}

		public override void OnAfterReadFromNetwork((BaseSynchedMissionObjectReadableRecord, ISynchedMissionObjectReadableRecord) synchedMissionObjectReadableRecord, bool allowVisibilityUpdate = true)
		{
			base.OnAfterReadFromNetwork(synchedMissionObjectReadableRecord);
			CS_DestructibleWallRecord destructableComponentRecord = (CS_DestructibleWallRecord)synchedMissionObjectReadableRecord.Item2;
			HitPoint = destructableComponentRecord.HitPoint;
			if (destructableComponentRecord.DestructionState != 0)
			{
				if (IsDestroyed)
				{
					OnDestroyed?.Invoke(this, null, MissionWeapon.Invalid, null, 0);
				}
				SetDestructionLevel(destructableComponentRecord.DestructionState, destructableComponentRecord.ForceIndex, 0f, Vec3.Zero, Vec3.Zero, true);
			}
		}

		[DefineSynchedMissionObjectTypeForMod(typeof(CS_DestructibleWall))]
		public struct CS_DestructibleWallRecord : ISynchedMissionObjectReadableRecord
		{
			public float HitPoint { get; private set; }

			public int DestructionState { get; private set; }

			public int ForceIndex { get; private set; }

			public bool IsMissionObject { get; private set; }

			public CS_DestructibleWallRecord(float hitPoint, int destructionState, int forceIndex, bool isMissionObject)
			{
				HitPoint = hitPoint;
				DestructionState = destructionState;
				ForceIndex = forceIndex;
				IsMissionObject = isMissionObject;
			}

			public bool ReadFromNetwork(ref bool bufferReadValid)
			{
				HitPoint = GameNetworkMessage.ReadFloatFromPacket(CompressionMission.UsableGameObjectHealthCompressionInfo, ref bufferReadValid);
				DestructionState = GameNetworkMessage.ReadIntFromPacket(CompressionMission.UsableGameObjectDestructionStateCompressionInfo, ref bufferReadValid);
				ForceIndex = -1;
				if (DestructionState != 0)
				{
					IsMissionObject = GameNetworkMessage.ReadBoolFromPacket(ref bufferReadValid);
					if (IsMissionObject)
					{
						ForceIndex = GameNetworkMessage.ReadMissionObjectIdFromPacket(ref bufferReadValid).Id;
					}
				}
				return bufferReadValid;
			}
		}

		public override void AddStuckMissile(GameEntity missileEntity)
		{
			if (CurrentState != null)
			{
				CurrentState.AddChild(missileEntity, false);
				return;
			}
			GameEntity.AddChild(missileEntity.WeakEntity, false);
		}

		protected override bool OnCheckForProblems()
		{
			bool result = base.OnCheckForProblems();
			if ((string.IsNullOrEmpty(ReferenceEntityTag) ? GameEntity : GameEntity.GetChildren().FirstOrDefault((x) => x.HasTag(ReferenceEntityTag))) == null)
			{
				MBEditor.AddEntityWarning(GameEntity, "Reference entity must be assigned. Root entity is " + GameEntity.Root.Name + ", child is " + GameEntity.Name);
				result = true;
			}

			string[] destructionStates = DestructionStates.Replace(" ", string.Empty).Split(',');
			int i;
			for (i = 0; i < destructionStates.Count(); i++)
			{
				if (!string.IsNullOrEmpty(destructionStates[i]) && !(GameEntity.GetChildren().FirstOrDefault((x) => x.Name == destructionStates[i]) != null) && TaleWorlds.Engine.GameEntity.Instantiate(null, destructionStates[i], callScriptCallbacks: false) == null)
				{
					MBEditor.AddEntityWarning(GameEntity, "Destruction state '" + destructionStates[i] + "' is not valid.");
					result = true;
				}
			}

			return result;
		}

		public void OnFocusGain(Agent userAgent)
		{
		}

		public void OnFocusLose(Agent userAgent)
		{
		}

		public TextObject GetInfoTextForBeingNotInteractable(Agent userAgent)
		{
			return null;
		}

		public string GetDescriptionText(GameEntity gameEntity = null)
		{
			int num;
			if (int.TryParse(gameEntity.Name.Split(new char[] { '_' }).Last(), out num))
			{
				string text = gameEntity.Name;
				text = text.Remove(text.Count() - num.ToString().Count());
				text += "x";
				TextObject textObject;
				if (GameTexts.TryGetText("str_destructible_component", out textObject, text))
				{
					return textObject.ToString();
				}
				return "";
			}
			else
			{
				TextObject textObject2;
				if (GameTexts.TryGetText("str_destructible_component", out textObject2, gameEntity.Name))
				{
					return textObject2.ToString();
				}
				return "";
			}
		}

		public TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			int num;
			TextObject textObject;
			if (int.TryParse(Enumerable.Last<string>(gameEntity.Name.Split(new char[] { '_' })), out num))
			{
				string text = gameEntity.Name;
				text = text.Remove(text.Length - num.ToString().Length);
				text += "x";
				if (GameTexts.TryGetText("str_destructible_component", out textObject, text))
				{
					return textObject;
				}
			}
			if (GameTexts.TryGetText("str_destructible_component", out textObject, gameEntity.Name))
			{
				return textObject;
			}
			return null;
		}
	}
}