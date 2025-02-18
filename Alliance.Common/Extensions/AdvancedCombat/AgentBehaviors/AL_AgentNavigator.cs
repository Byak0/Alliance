using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.AgentBehaviors
{
	public sealed class AL_AgentNavigator
	{
		public UsableMachine TargetUsableMachine { get; private set; }

		public WorldPosition TargetPosition { get; private set; }

		public Vec2 TargetDirection { get; private set; }

		public GameEntity TargetEntity { get; private set; }

		public Agent TargetAgent { get; private set; }

		public string SpecialTargetTag
		{
			get
			{
				return _specialTargetTag;
			}
			set
			{
				if (value != _specialTargetTag)
				{
					_specialTargetTag = value;
					AL_AgentBehavior activeBehavior = GetActiveBehavior();
					if (activeBehavior != null)
					{
						activeBehavior.OnSpecialTargetChanged();
					}
				}
			}
		}

		private Dictionary<KeyValuePair<sbyte, string>, int> _bodyComponents { get; set; }

		public AL_AgentNavigator.NavigationState _agentState { get; private set; }

		public bool CharacterHasVisiblePrefabs { get; private set; }

		public AL_AgentNavigator(Agent agent)
		{
			_mission = agent.Mission;
			OwnerAgent = agent;
			_prefabNamesForBones = new Dictionary<sbyte, string>();
			_behaviorGroups = new List<AL_AgentBehaviorGroup>();
			_bodyComponents = new Dictionary<KeyValuePair<sbyte, string>, int>();
			SpecialTargetTag = string.Empty;
			TargetUsableMachine = null;
			_checkBehaviorGroupsTimer = new BasicMissionTimer();
			_prevPrefabs = new List<int>();
			CharacterHasVisiblePrefabs = false;
			SetItemsVisibility(true);
			SetSpecialItem();
		}

		public void OnStopUsingGameObject()
		{
			_targetBehavior = null;
			TargetUsableMachine = null;
			_agentState = AL_AgentNavigator.NavigationState.NoTarget;
		}

		public void OnAgentRemoved(Agent agent)
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				if (agentBehaviorGroup.IsActive)
				{
					agentBehaviorGroup.OnAgentRemoved(agent);
				}
			}
		}

		public void SetTarget(Agent targetAgent)
		{
			TargetAgent = targetAgent;
		}

		public void SetTarget(UsableMachine usableMachine, bool isInitialTarget = false)
		{
			if (usableMachine == null)
			{
				UsableMachine targetUsableMachine = TargetUsableMachine;
				if (targetUsableMachine != null)
				{
					((IDetachment)targetUsableMachine).RemoveAgent(OwnerAgent);
				}
				TargetUsableMachine = null;
				OwnerAgent.DisableScriptedMovement();
				OwnerAgent.ClearTargetFrame();
				TargetPosition = WorldPosition.Invalid;
				TargetEntity = null;
				_agentState = AL_AgentNavigator.NavigationState.NoTarget;
				return;
			}
			if (TargetUsableMachine != usableMachine || isInitialTarget)
			{
				TargetPosition = WorldPosition.Invalid;
				_agentState = AL_AgentNavigator.NavigationState.NoTarget;
				UsableMachine targetUsableMachine2 = TargetUsableMachine;
				if (targetUsableMachine2 != null)
				{
					((IDetachment)targetUsableMachine2).RemoveAgent(OwnerAgent);
				}
				if (usableMachine.IsStandingPointAvailableForAgent(OwnerAgent))
				{
					TargetUsableMachine = usableMachine;
					TargetPosition = WorldPosition.Invalid;
					_agentState = AL_AgentNavigator.NavigationState.UseMachine;
					_targetBehavior = TargetUsableMachine.CreateAIBehaviorObject();
					((IDetachment)TargetUsableMachine).AddAgent(OwnerAgent, -1);
					_targetReached = false;
				}
			}
		}

		public void SetTargetFrame(WorldPosition position, float rotation, float rangeThreshold = 1f, float rotationThreshold = -10f, Agent.AIScriptedFrameFlags flags = Agent.AIScriptedFrameFlags.None, bool disableClearTargetWhenTargetIsReached = false)
		{
			if (_agentState != AL_AgentNavigator.NavigationState.NoTarget)
			{
				ClearTarget();
			}
			TargetPosition = position;
			TargetDirection = Vec2.FromRotation(rotation);
			_rangeThreshold = rangeThreshold;
			_rotationScoreThreshold = rotationThreshold;
			_disableClearTargetWhenTargetIsReached = disableClearTargetWhenTargetIsReached;
			if (IsTargetReached())
			{
				TargetPosition = WorldPosition.Invalid;
				_agentState = AL_AgentNavigator.NavigationState.NoTarget;
				return;
			}
			OwnerAgent.SetScriptedPositionAndDirection(ref position, rotation, false, flags);
			_agentState = AL_AgentNavigator.NavigationState.GoToTarget;
		}

		public void ClearTarget()
		{
			SetTarget(null, false);
		}

		public void Tick(float dt, bool isSimulation = false)
		{
			HandleBehaviorGroups(isSimulation);

			TickActiveGroups(dt, isSimulation);

			if (TargetAgent != null)
			{
				GoToTarget();
			}
			else if (TargetUsableMachine != null)
			{
				_targetBehavior.Tick(OwnerAgent, null, null, dt);
			}
			else
			{
				HandleMovement();
			}
			if (TargetUsableMachine != null && isSimulation)
			{
				_targetBehavior.TeleportUserAgentsToMachine(new List<Agent> { OwnerAgent });
			}
		}

		public float GetDistanceToTarget(UsableMachine target)
		{
			float num = 100000f;
			if (target != null && OwnerAgent.CurrentlyUsedGameObject != null)
			{
				num = OwnerAgent.CurrentlyUsedGameObject.GetUserFrameForAgent(OwnerAgent).Origin.GetGroundVec3().Distance(OwnerAgent.Position);
			}
			return num;
		}

		public bool IsTargetReached()
		{
			if (TargetDirection.IsValid && TargetPosition.IsValid)
			{
				float num = Vec2.DotProduct(TargetDirection, OwnerAgent.GetMovementDirection());
				_targetReached = (OwnerAgent.Position - TargetPosition.GetGroundVec3()).LengthSquared < _rangeThreshold * _rangeThreshold && num > _rotationScoreThreshold;
			}
			return _targetReached;
		}

		private void GoToTarget()
		{
			if (TargetAgent.AgentVisuals == null || TargetAgent.Health <= 0 || TargetAgent.IsFadingOut())
			{
				Log($"ERROR, Target invalid in AttackTask - {TargetAgent?.Name}", LogLevel.Error);
				OwnerAgent?.DisableScriptedCombatMovement();
				OwnerAgent?.DisableScriptedMovement();
				TargetAgent = null;
				return;
			}

			WorldPosition pos = OwnerAgent.GetWorldPosition();
			OwnerAgent.SetScriptedTargetEntityAndPosition(TargetAgent.AgentVisuals.GetEntity(), pos, TaleWorlds.MountAndBlade.Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation, false);
			if (OwnerAgent.HasRangedWeapon())
			{
				OwnerAgent.SetScriptedPosition(ref pos, false, TaleWorlds.MountAndBlade.Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget);
			}
		}

		private void HandleMovement()
		{
			if (_agentState == AL_AgentNavigator.NavigationState.GoToTarget && IsTargetReached())
			{
				_agentState = AL_AgentNavigator.NavigationState.AtTargetPosition;
				if (!_disableClearTargetWhenTargetIsReached)
				{
					OwnerAgent.ClearTargetFrame();
				}
			}
		}

		public void HoldAndHideRecentlyUsedMeshes()
		{
			foreach (KeyValuePair<KeyValuePair<sbyte, string>, int> keyValuePair in _bodyComponents)
			{
				if (OwnerAgent.IsSynchedPrefabComponentVisible(keyValuePair.Value))
				{
					OwnerAgent.SetSynchedPrefabComponentVisibility(keyValuePair.Value, false);
					_prevPrefabs.Add(keyValuePair.Value);
				}
			}
		}

		public void RecoverRecentlyUsedMeshes()
		{
			foreach (int num in _prevPrefabs)
			{
				OwnerAgent.SetSynchedPrefabComponentVisibility(num, true);
			}
			_prevPrefabs.Clear();
		}

		public bool CanSeeAgent(Agent otherAgent)
		{
			if ((OwnerAgent.Position - otherAgent.Position).Length < 30f)
			{
				Vec3 eyeGlobalPosition = otherAgent.GetEyeGlobalPosition();
				Vec3 eyeGlobalPosition2 = OwnerAgent.GetEyeGlobalPosition();
				if (MathF.Abs(Vec3.AngleBetweenTwoVectors(otherAgent.Position - OwnerAgent.Position, OwnerAgent.LookDirection)) < 1.5f)
				{
					float num;
					return !Mission.Current.Scene.RayCastForClosestEntityOrTerrain(eyeGlobalPosition2, eyeGlobalPosition, out num, 0.01f, BodyFlags.CommonFocusRayCastExcludeFlags);
				}
			}
			return false;
		}

		public bool IsCarryingSomething()
		{
			return OwnerAgent.GetWieldedItemIndex(Agent.HandIndex.MainHand) >= EquipmentIndex.WeaponItemBeginSlot || OwnerAgent.GetWieldedItemIndex(Agent.HandIndex.OffHand) >= EquipmentIndex.WeaponItemBeginSlot || Enumerable.Any<KeyValuePair<KeyValuePair<sbyte, string>, int>>(_bodyComponents, (KeyValuePair<KeyValuePair<sbyte, string>, int> component) => OwnerAgent.IsSynchedPrefabComponentVisible(component.Value));
		}

		public void SetPrefabVisibility(sbyte realBoneIndex, string prefabName, bool isVisible)
		{
			KeyValuePair<sbyte, string> keyValuePair = new KeyValuePair<sbyte, string>(realBoneIndex, prefabName);
			int num2;
			if (isVisible)
			{
				int num;
				if (!_bodyComponents.TryGetValue(keyValuePair, out num))
				{
					_bodyComponents.Add(keyValuePair, OwnerAgent.AddSynchedPrefabComponentToBone(prefabName, realBoneIndex));
					return;
				}
				if (!OwnerAgent.IsSynchedPrefabComponentVisible(num))
				{
					OwnerAgent.SetSynchedPrefabComponentVisibility(num, true);
					return;
				}
			}
			else if (_bodyComponents.TryGetValue(keyValuePair, out num2) && OwnerAgent.IsSynchedPrefabComponentVisible(num2))
			{
				OwnerAgent.SetSynchedPrefabComponentVisibility(num2, false);
			}
		}

		public bool GetPrefabVisibility(sbyte realBoneIndex, string prefabName)
		{
			KeyValuePair<sbyte, string> keyValuePair = new KeyValuePair<sbyte, string>(realBoneIndex, prefabName);
			int num;
			return _bodyComponents.TryGetValue(keyValuePair, out num) && OwnerAgent.IsSynchedPrefabComponentVisible(num);
		}

		public void SetSpecialItem()
		{
			if (_specialItem != null)
			{
				bool flag = false;
				EquipmentIndex equipmentIndex = EquipmentIndex.None;
				for (EquipmentIndex equipmentIndex2 = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex2 <= EquipmentIndex.Weapon3; equipmentIndex2++)
				{
					if (OwnerAgent.Equipment[equipmentIndex2].IsEmpty)
					{
						equipmentIndex = equipmentIndex2;
					}
					else if (OwnerAgent.Equipment[equipmentIndex2].Item == _specialItem)
					{
						equipmentIndex = equipmentIndex2;
						flag = true;
						break;
					}
				}
				if (equipmentIndex == EquipmentIndex.None)
				{
					OwnerAgent.DropItem(EquipmentIndex.Weapon3, WeaponClass.Undefined);
					equipmentIndex = EquipmentIndex.Weapon3;
				}
				if (!flag)
				{
					ItemObject specialItem = _specialItem;
					ItemModifier itemModifier = null;
					IAgentOriginBase origin = OwnerAgent.Origin;
					MissionWeapon missionWeapon = new MissionWeapon(specialItem, itemModifier, (origin != null) ? origin.Banner : null);
					OwnerAgent.EquipWeaponWithNewEntity(equipmentIndex, ref missionWeapon);
				}
				OwnerAgent.TryToWieldWeaponInSlot(equipmentIndex, Agent.WeaponWieldActionType.Instant, false);
			}
		}

		public void SetItemsVisibility(bool isVisible)
		{
			foreach (KeyValuePair<sbyte, string> keyValuePair in _prefabNamesForBones)
			{
				SetPrefabVisibility(keyValuePair.Key, keyValuePair.Value, isVisible);
			}
			CharacterHasVisiblePrefabs = _prefabNamesForBones.Count > 0 && isVisible;
		}

		public void ForceThink(float inSeconds)
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				agentBehaviorGroup.ForceThink(inSeconds);
			}
		}

		public T AddBehaviorGroup<T>() where T : AL_AgentBehaviorGroup
		{
			T t = GetBehaviorGroup<T>();
			if (t == null)
			{
				t = Activator.CreateInstance(typeof(T), new object[] { this, _mission }) as T;
				if (t != null)
				{
					_behaviorGroups.Add(t);
				}
			}
			return t;
		}

		public T GetBehaviorGroup<T>() where T : AL_AgentBehaviorGroup
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				if (agentBehaviorGroup is T)
				{
					return (T)((object)agentBehaviorGroup);
				}
			}
			return default(T);
		}

		public AL_AgentBehavior GetBehavior<T>() where T : AL_AgentBehavior
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				foreach (AL_AgentBehavior agentBehavior in agentBehaviorGroup.Behaviors)
				{
					if (agentBehavior.GetType() == typeof(T))
					{
						return agentBehavior;
					}
				}
			}
			return null;
		}

		public bool HasBehaviorGroup<T>()
		{
			using (List<AL_AgentBehaviorGroup>.Enumerator enumerator = _behaviorGroups.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.GetType() is T)
					{
						return true;
					}
				}
			}
			return false;
		}

		public void RemoveBehaviorGroup<T>() where T : AL_AgentBehaviorGroup
		{
			for (int i = 0; i < _behaviorGroups.Count; i++)
			{
				if (_behaviorGroups[i] is T)
				{
					_behaviorGroups.RemoveAt(i);
				}
			}
		}

		public void RefreshBehaviorGroups(bool isSimulation)
		{
			_checkBehaviorGroupsTimer.Reset();
			float num = 0f;
			AL_AgentBehaviorGroup agentBehaviorGroup = null;
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup2 in _behaviorGroups)
			{
				float score = agentBehaviorGroup2.GetScore(isSimulation);
				if (score > num)
				{
					num = score;
					agentBehaviorGroup = agentBehaviorGroup2;
				}
			}
			if (num > 0f && agentBehaviorGroup != null && !agentBehaviorGroup.IsActive)
			{
				ActivateGroup(agentBehaviorGroup);
			}
		}

		private void ActivateGroup(AL_AgentBehaviorGroup behaviorGroup)
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				agentBehaviorGroup.IsActive = false;
			}
			behaviorGroup.IsActive = true;
		}

		private void HandleBehaviorGroups(bool isSimulation)
		{
			if (isSimulation || _checkBehaviorGroupsTimer.ElapsedTime > 1f)
			{
				RefreshBehaviorGroups(isSimulation);
			}
		}

		private void TickActiveGroups(float dt, bool isSimulation)
		{
			if (!OwnerAgent.IsActive())
			{
				return;
			}
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				if (agentBehaviorGroup.IsActive)
				{
					agentBehaviorGroup.Tick(dt, isSimulation);
				}
			}
		}

		public AL_AgentBehavior GetActiveBehavior()
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				if (agentBehaviorGroup.IsActive)
				{
					return agentBehaviorGroup.GetActiveBehavior();
				}
			}
			return null;
		}

		public AL_AgentBehaviorGroup GetActiveBehaviorGroup()
		{
			foreach (AL_AgentBehaviorGroup agentBehaviorGroup in _behaviorGroups)
			{
				if (agentBehaviorGroup.IsActive)
				{
					return agentBehaviorGroup;
				}
			}
			return null;
		}

		private const float SeeingDistance = 30f;

		public readonly Agent OwnerAgent;

		private readonly Mission _mission;

		private readonly List<AL_AgentBehaviorGroup> _behaviorGroups;

		private readonly ItemObject _specialItem;

		private UsableMachineAIBase _targetBehavior;

		private bool _targetReached;

		private float _rangeThreshold;

		private float _rotationScoreThreshold;

		private string _specialTargetTag;

		private bool _disableClearTargetWhenTargetIsReached;

		private readonly Dictionary<sbyte, string> _prefabNamesForBones;

		private readonly List<int> _prevPrefabs;

		private readonly BasicMissionTimer _checkBehaviorGroupsTimer;

		public enum NavigationState
		{
			NoTarget,
			GoToTarget,
			AtTargetPosition,
			UseMachine
		}
	}
}
