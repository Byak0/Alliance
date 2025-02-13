using Alliance.Common.Extensions.FormationEnforcer.Component;
using NetworkMessages.FromClient;
using System;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.ExNativeUI.MainAgentController.MissionViews
{
	[OverrideView(typeof(MissionMainAgentController))]
	public class AL_MainAgentController : MissionMainAgentController
	{
		public event MissionMainAgentController.OnLockedAgentChangedDelegate OnLockedAgentChanged;

		public event MissionMainAgentController.OnPotentialLockedAgentChangedDelegate OnPotentialLockedAgentChanged;

		public new bool IsPlayerAiming
		{
			get
			{
				if (_isPlayerAiming)
				{
					return true;
				}
				if (Mission.MainAgent == null)
				{
					return false;
				}
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				if (Input != null)
				{
					flag2 = Input.IsGameKeyDown(9);
				}
				if (Mission.MainAgent != null)
				{
					if (Mission.MainAgent.WieldedWeapon.CurrentUsageItem != null)
					{
						flag = Mission.MainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon || Mission.MainAgent.WieldedWeapon.CurrentUsageItem.IsAmmo;
					}
					flag3 = Mission.MainAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.AttackMask);
				}
				return flag && flag2 && flag3;
			}
		}

		public new Agent LockedAgent
		{
			get
			{
				return _lockedAgent;
			}
			private set
			{
				if (_lockedAgent != value)
				{
					_lockedAgent = value;
					MissionMainAgentController.OnLockedAgentChangedDelegate onLockedAgentChanged = OnLockedAgentChanged;
					if (onLockedAgentChanged == null)
					{
						return;
					}
					onLockedAgentChanged(value);
				}
			}
		}

		public new Agent PotentialLockTargetAgent
		{
			get
			{
				return _potentialLockTargetAgent;
			}
			private set
			{
				if (_potentialLockTargetAgent != value)
				{
					_potentialLockTargetAgent = value;
					MissionMainAgentController.OnPotentialLockedAgentChangedDelegate onPotentialLockedAgentChanged = OnPotentialLockedAgentChanged;
					if (onPotentialLockedAgentChanged == null)
					{
						return;
					}
					onPotentialLockedAgentChanged(value);
				}
			}
		}

		public AL_MainAgentController()
		{
			InteractionComponent = new MissionMainAgentInteractionComponent(this);
			CustomLookDir = Vec3.Zero;
			IsChatOpen = false;
		}

		public override void EarlyStart()
		{
			base.EarlyStart();
			Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(new Action<MissionPlayerToggledOrderViewEvent>(OnPlayerToggleOrder));
			Mission.OnMainAgentChanged += new PropertyChangedEventHandler(Mission_OnMainAgentChanged);
			MissionMultiplayerGameModeBaseClient missionBehavior = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
			if (((missionBehavior != null) ? missionBehavior.RoundComponent : null) != null)
			{
				missionBehavior.RoundComponent.OnRoundStarted += Disable;
				missionBehavior.RoundComponent.OnPreparationEnded += Enable;
			}
			ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
			UpdateLockTargetOption();
		}

		public override void OnMissionScreenFinalize()
		{
			base.OnMissionScreenFinalize();
			Mission.OnMainAgentChanged -= new PropertyChangedEventHandler(Mission_OnMainAgentChanged);
			Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(new Action<MissionPlayerToggledOrderViewEvent>(OnPlayerToggleOrder));
			MissionMultiplayerGameModeBaseClient missionBehavior = Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
			if (((missionBehavior != null) ? missionBehavior.RoundComponent : null) != null)
			{
				missionBehavior.RoundComponent.OnRoundStarted -= Disable;
				missionBehavior.RoundComponent.OnPreparationEnded -= Enable;
			}
			ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		}

		public override bool IsReady()
		{
			bool flag = true;
			if (Mission.MainAgent != null)
			{
				flag = Mission.MainAgent.AgentVisuals.CheckResources(true);
			}
			return flag;
		}

		private void Mission_OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Mission.MainAgent != null)
			{
				_isPlayerAgentAdded = true;
				_strafeModeActive = false;
				_autoDismountModeActive = false;
			}
		}

		public override void OnPreMissionTick(float dt)
		{
			base.OnPreMissionTick(dt);
			if (MissionScreen == null)
			{
				return;
			}
			if (Mission.MainAgent == null && GameNetwork.MyPeer != null)
			{
				MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
				if (component != null)
				{
					if (component.HasSpawnedAgentVisuals)
					{
						AgentVisualsMovementCheck();
					}
					else if (component.FollowedAgent != null)
					{
						RequestToSpawnAsBotCheck();
					}
				}
			}
			Agent mainAgent = Mission.MainAgent;
			if (mainAgent != null && mainAgent.State == AgentState.Active && !MissionScreen.IsCheatGhostMode && !Mission.MainAgent.IsAIControlled && !IsDisabled && _activated)
			{
				InteractionComponent.FocusTick();
				InteractionComponent.FocusedItemHealthTick();
				ControlTick();
				InteractionComponent.FocusStateCheckTick();
				LookTick(dt);
				return;
			}
			LockedAgent = null;
		}

		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (InteractionComponent.CurrentFocusedObject == affectedAgent || affectedAgent == Mission.MainAgent)
			{
				InteractionComponent.ClearFocus();
			}
		}

		public override void OnAgentDeleted(Agent affectedAgent)
		{
			if (InteractionComponent.CurrentFocusedObject == affectedAgent)
			{
				InteractionComponent.ClearFocus();
			}
		}

		public override void OnClearScene()
		{
			InteractionComponent.OnClearScene();
		}

		private void LookTick(float dt)
		{
			if (!IsDisabled)
			{
				Agent mainAgent = Mission.MainAgent;
				if (mainAgent != null)
				{
					if (_isPlayerAgentAdded)
					{
						_isPlayerAgentAdded = false;
						mainAgent.LookDirectionAsAngle = mainAgent.MovementDirectionAsAngle;
					}
					if (Mission.ClearSceneTimerElapsedTime >= 0f)
					{
						Vec3 vec;
						if (LockedAgent != null)
						{
							float num = 0f;
							float agentScale = LockedAgent.AgentScale;
							float agentScale2 = mainAgent.AgentScale;
							if (!LockedAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
							{
								num += LockedAgent.Monster.BodyCapsulePoint1.z * agentScale;
							}
							else if (LockedAgent.HasMount)
							{
								num += (LockedAgent.MountAgent.Monster.RiderCameraHeightAdder + LockedAgent.MountAgent.Monster.BodyCapsulePoint1.z + LockedAgent.MountAgent.Monster.BodyCapsuleRadius) * LockedAgent.MountAgent.AgentScale + LockedAgent.Monster.CrouchEyeHeight * agentScale;
							}
							else if (LockedAgent.CrouchMode || LockedAgent.IsSitting())
							{
								num += (LockedAgent.Monster.CrouchEyeHeight + 0.2f) * agentScale;
							}
							else
							{
								num += (LockedAgent.Monster.StandingEyeHeight + 0.2f) * agentScale;
							}
							if (!mainAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
							{
								num -= LockedAgent.Monster.BodyCapsulePoint1.z * agentScale2;
							}
							else if (mainAgent.HasMount)
							{
								num -= (mainAgent.MountAgent.Monster.RiderCameraHeightAdder + mainAgent.MountAgent.Monster.BodyCapsulePoint1.z + mainAgent.MountAgent.Monster.BodyCapsuleRadius) * mainAgent.MountAgent.AgentScale + mainAgent.Monster.CrouchEyeHeight * agentScale2;
							}
							else if (mainAgent.CrouchMode || mainAgent.IsSitting())
							{
								num -= (mainAgent.Monster.CrouchEyeHeight + 0.2f) * agentScale2;
							}
							else
							{
								num -= (mainAgent.Monster.StandingEyeHeight + 0.2f) * agentScale2;
							}
							if (LockedAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
							{
								num -= 0.3f * agentScale;
							}
							num = MBMath.Lerp(_lastLockedAgentHeightDifference, num, MathF.Min(8f * dt, 1f), 1E-05f);
							_lastLockedAgentHeightDifference = num;
							vec = (LockedAgent.VisualPosition + ((LockedAgent.MountAgent != null) ? (LockedAgent.MountAgent.GetMovementDirection().ToVec3(0f) * LockedAgent.MountAgent.Monster.RiderBodyCapsuleForwardAdder) : Vec3.Zero) + new Vec3(0f, 0f, num, -1f) - (mainAgent.VisualPosition + ((mainAgent.MountAgent != null) ? (mainAgent.MountAgent.GetMovementDirection().ToVec3(0f) * mainAgent.MountAgent.Monster.RiderBodyCapsuleForwardAdder) : Vec3.Zero))).NormalizedCopy();
						}
						else if (CustomLookDir.IsNonZero)
						{
							vec = CustomLookDir;
						}
						else
						{
							Mat3 identity = Mat3.Identity;
							identity.RotateAboutUp(MissionScreen.CameraBearing);
							identity.RotateAboutSide(MissionScreen.CameraElevation);
							vec = identity.f;
						}
						if (!MissionScreen.IsViewingCharacter() && !mainAgent.IsLookDirectionLocked && mainAgent.MovementLockedState != AgentMovementLockedState.FrameLocked)
						{
							mainAgent.LookDirection = vec;
						}
						mainAgent.HeadCameraMode = Mission.CameraIsFirstPerson;
					}
				}
			}
		}

		private void AgentVisualsMovementCheck()
		{
			if (Input.IsGameKeyReleased(13))
			{
				BreakAgentVisualsInvulnerability();
			}
		}

		public void BreakAgentVisualsInvulnerability()
		{
			if (GameNetwork.IsClient)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new AgentVisualsBreakInvulnerability());
				GameNetwork.EndModuleEventAsClient();
				return;
			}
			Mission.Current.GetMissionBehavior<SpawnComponent>().SetEarlyAgentVisualsDespawning(GameNetwork.MyPeer.GetComponent<MissionPeer>(), true);
		}

		private void RequestToSpawnAsBotCheck()
		{
			if (Input.IsGameKeyPressed(13))
			{
				if (GameNetwork.IsClient)
				{
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new RequestToSpawnAsBot());
					GameNetwork.EndModuleEventAsClient();
					return;
				}
				if (GameNetwork.MyPeer.GetComponent<MissionPeer>().HasSpawnTimerExpired)
				{
					GameNetwork.MyPeer.GetComponent<MissionPeer>().WantsToSpawnAsBot = true;
				}
			}
		}

		private Agent FindTargetedLockableAgent(Agent player)
		{
			Vec3 direction = MissionScreen.CombatCamera.Direction;
			Vec3 vec = direction;
			Vec3 position = MissionScreen.CombatCamera.Position;
			Vec3 visualPosition = player.VisualPosition;
			float num = new Vec3(position.x, position.y, 0f, -1f).Distance(new Vec3(visualPosition.x, visualPosition.y, 0f, -1f));
			Vec3 vec2 = position * (1f - num) + (position + direction) * num;
			float num2 = 0f;
			Agent agent = null;
			foreach (Agent agent2 in Mission.Agents)
			{
				if ((agent2.IsMount && agent2.RiderAgent != null && agent2.RiderAgent.IsEnemyOf(player)) || (!agent2.IsMount && agent2.IsEnemyOf(player)))
				{
					Vec3 vec3 = agent2.GetChestGlobalPosition() - vec2;
					float num3 = vec3.Normalize();
					if (num3 < 20f)
					{
						float num4 = Vec2.DotProduct(vec.AsVec2.Normalized(), vec3.AsVec2.Normalized());
						float num5 = Vec2.DotProduct(new Vec2(vec.AsVec2.Length, vec.z), new Vec2(vec3.AsVec2.Length, vec3.z));
						if (num4 > 0.95f && num5 > 0.95f)
						{
							float num6 = num4 * num4 * num4 / MathF.Pow(num3, 0.15f);
							if (num6 > num2)
							{
								num2 = num6;
								agent = agent2;
							}
						}
					}
				}
			}
			if (agent != null && agent.IsMount && agent.RiderAgent != null)
			{
				return agent.RiderAgent;
			}
			return agent;
		}

		private void ControlTick()
		{
			if (MissionScreen != null && MissionScreen.IsPhotoModeEnabled)
			{
				return;
			}
			if (IsChatOpen)
			{
				return;
			}
			Agent mainAgent = Mission.MainAgent;
			bool flag = false;
			if (LockedAgent != null && (!Mission.Agents.ContainsQ(LockedAgent) || !LockedAgent.IsActive() || LockedAgent.Position.DistanceSquared(mainAgent.Position) > 625f || Input.IsGameKeyReleased(26) || Input.IsGameKeyDown(25) || (Mission.Mode != MissionMode.Battle && Mission.Mode != MissionMode.Stealth) || (!mainAgent.WieldedWeapon.IsEmpty && mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) || MissionScreen == null || MissionScreen.GetSpectatingData(MissionScreen.CombatCamera.Frame.origin).CameraType != SpectatorCameraTypes.LockToMainPlayer))
			{
				LockedAgent = null;
				flag = true;
			}
			if (Mission.Mode == MissionMode.Conversation)
			{
				mainAgent.MovementFlags = (Agent.MovementControlFlag)0U;
				mainAgent.MovementInputVector = Vec2.Zero;
				return;
			}
			if (Mission.ClearSceneTimerElapsedTime >= 0f && mainAgent.State == AgentState.Active)
			{
				bool flag2 = false;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				Vec2 vec = new Vec2(Input.GetGameKeyAxis("MovementAxisX"), Input.GetGameKeyAxis("MovementAxisY"));
				if (_autoDismountModeActive)
				{
					if (!Input.IsGameKeyDown(0) && mainAgent.MountAgent != null)
					{
						if (mainAgent.GetCurrentVelocity().y > 0f)
						{
							vec.y = -1f;
						}
					}
					else
					{
						_autoDismountModeActive = false;
					}
				}
				if (MathF.Abs(vec.x) < 0.2f)
				{
					vec.x = 0f;
				}
				if (MathF.Abs(vec.y) < 0.2f)
				{
					vec.y = 0f;
				}
				if (vec.IsNonZero())
				{
					float rotationInRadians = vec.RotationInRadians;
					if (rotationInRadians > -0.7853982f && rotationInRadians < 0.7853982f)
					{
						flag3 = true;
					}
					else if (rotationInRadians < -2.3561945f || rotationInRadians > 2.3561945f)
					{
						flag5 = true;
					}
					else if (rotationInRadians < 0f)
					{
						flag2 = true;
					}
					else
					{
						flag4 = true;
					}
				}
				mainAgent.EventControlFlags = (Agent.EventControlFlag)0U;
				mainAgent.MovementFlags = (Agent.MovementControlFlag)0U;
				mainAgent.MovementInputVector = Vec2.Zero;
				if (!MissionScreen.IsRadialMenuActive && !Mission.IsOrderMenuOpen)
				{
					if (Input.IsGameKeyPressed(14))
					{
						if (mainAgent.MountAgent == null || mainAgent.MovementVelocity.LengthSquared > 0.09f)
						{
							mainAgent.EventControlFlags |= Agent.EventControlFlag.Jump;
						}
						else
						{
							mainAgent.EventControlFlags |= Agent.EventControlFlag.Rear;
						}
					}
					if (Input.IsGameKeyPressed(13))
					{
						mainAgent.MovementFlags |= Agent.MovementControlFlag.Action;
					}
				}
				if (mainAgent.MountAgent != null && mainAgent.GetCurrentVelocity().y < 0.5f && (Input.IsGameKeyDown(3) || Input.IsGameKeyDown(2)))
				{
					if (Input.IsGameKeyPressed(16))
					{
						_strafeModeActive = true;
					}
				}
				else
				{
					_strafeModeActive = false;
				}
				Agent.MovementControlFlag movementControlFlag = _lastMovementKeyPressed;
				if (Input.IsGameKeyPressed(0))
				{
					movementControlFlag = Agent.MovementControlFlag.Forward;
				}
				else if (Input.IsGameKeyPressed(1))
				{
					movementControlFlag = Agent.MovementControlFlag.Backward;
				}
				else if (Input.IsGameKeyPressed(2))
				{
					movementControlFlag = Agent.MovementControlFlag.StrafeLeft;
				}
				else if (Input.IsGameKeyPressed(3))
				{
					movementControlFlag = Agent.MovementControlFlag.StrafeRight;
				}
				if (movementControlFlag != _lastMovementKeyPressed)
				{
					_lastMovementKeyPressed = movementControlFlag;
					Game game = Game.Current;
					if (game != null)
					{
						game.EventManager.TriggerEvent<MissionPlayerMovementFlagsChangeEvent>(new MissionPlayerMovementFlagsChangeEvent(_lastMovementKeyPressed));
					}
				}
				if (!Input.GetIsMouseActive())
				{
					bool flag6 = true;
					if (flag3)
					{
						movementControlFlag = Agent.MovementControlFlag.Forward;
					}
					else if (flag5)
					{
						movementControlFlag = Agent.MovementControlFlag.Backward;
					}
					else if (flag4)
					{
						movementControlFlag = Agent.MovementControlFlag.StrafeLeft;
					}
					else if (flag2)
					{
						movementControlFlag = Agent.MovementControlFlag.StrafeRight;
					}
					else
					{
						flag6 = false;
					}
					if (flag6)
					{
						Mission.SetLastMovementKeyPressed(movementControlFlag);
					}
				}
				else
				{
					Mission.SetLastMovementKeyPressed(_lastMovementKeyPressed);
				}
				if (Input.IsGameKeyPressed(0))
				{
					if (_lastForwardKeyPressTime + 0.3f > Time.ApplicationTime)
					{
						mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
						mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionUp;
					}
					_lastForwardKeyPressTime = Time.ApplicationTime;
				}
				if (Input.IsGameKeyPressed(1))
				{
					if (_lastBackwardKeyPressTime + 0.3f > Time.ApplicationTime)
					{
						mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
						mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionDown;
					}
					_lastBackwardKeyPressTime = Time.ApplicationTime;
				}
				if (Input.IsGameKeyPressed(2))
				{
					if (_lastLeftKeyPressTime + 0.3f > Time.ApplicationTime)
					{
						mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
						mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionLeft;
					}
					_lastLeftKeyPressTime = Time.ApplicationTime;
				}
				if (Input.IsGameKeyPressed(3))
				{
					if (_lastRightKeyPressTime + 0.3f > Time.ApplicationTime)
					{
						mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
						mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionRight;
					}
					_lastRightKeyPressTime = Time.ApplicationTime;
				}
				if (_isTargetLockEnabled)
				{
					if (Input.IsGameKeyDown(26) && LockedAgent == null && !Input.IsGameKeyDown(25) && (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Stealth) && (mainAgent.WieldedWeapon.IsEmpty || !mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) && !GameNetwork.IsMultiplayer)
					{
						float applicationTime = Time.ApplicationTime;
						if (_lastLockKeyPressTime <= 0f)
						{
							_lastLockKeyPressTime = applicationTime;
						}
						if (applicationTime > _lastLockKeyPressTime + 0.3f)
						{
							PotentialLockTargetAgent = FindTargetedLockableAgent(mainAgent);
						}
					}
					else
					{
						PotentialLockTargetAgent = null;
					}
					if (LockedAgent == null && !flag && Input.IsGameKeyReleased(26) && !GameNetwork.IsMultiplayer)
					{
						_lastLockKeyPressTime = 0f;
						if (!Input.IsGameKeyDown(25) && (Mission.Mode == MissionMode.Battle || Mission.Mode == MissionMode.Stealth) && (mainAgent.WieldedWeapon.IsEmpty || !mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) && MissionScreen != null && MissionScreen.GetSpectatingData(MissionScreen.CombatCamera.Frame.origin).CameraType == SpectatorCameraTypes.LockToMainPlayer)
						{
							LockedAgent = FindTargetedLockableAgent(mainAgent);
						}
					}
				}
				if (mainAgent.MountAgent != null && !_strafeModeActive)
				{
					if (flag2 || vec.x > 0f)
					{
						mainAgent.MovementFlags |= Agent.MovementControlFlag.TurnRight;
					}
					else if (flag4 || vec.x < 0f)
					{
						mainAgent.MovementFlags |= Agent.MovementControlFlag.TurnLeft;
					}
				}
				mainAgent.MovementInputVector = vec;
				if (!MissionScreen.MouseVisible && !MissionScreen.IsRadialMenuActive && !_isPlayerOrderOpen && mainAgent.CombatActionsEnabled)
				{
					WeaponComponentData currentUsageItem = mainAgent.WieldedWeapon.CurrentUsageItem;
					bool flag7 = currentUsageItem != null && currentUsageItem.WeaponFlags.HasAllFlags(WeaponFlags.StringHeldByHand);
					WeaponComponentData currentUsageItem2 = mainAgent.WieldedWeapon.CurrentUsageItem;
					if (currentUsageItem2 != null && currentUsageItem2.IsRangedWeapon)
					{
						bool isConsumable = mainAgent.WieldedWeapon.CurrentUsageItem.IsConsumable;
					}
					WeaponComponentData currentUsageItem3 = mainAgent.WieldedWeapon.CurrentUsageItem;
					bool flag8 = currentUsageItem3 != null && currentUsageItem3.IsRangedWeapon && !mainAgent.WieldedWeapon.CurrentUsageItem.IsConsumable && !mainAgent.WieldedWeapon.CurrentUsageItem.WeaponFlags.HasAllFlags(WeaponFlags.StringHeldByHand);
					bool flag9 = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableAlternateAiming) != 0f && (flag7 || flag8);
					if (flag9)
					{
						HandleRangedWeaponAttackAlternativeAiming(mainAgent);
					}
					else if (Input.IsGameKeyDown(9))
					{
						mainAgent.MovementFlags |= mainAgent.AttackDirectionToMovementFlag(mainAgent.GetAttackDirection());
					}
					if (!flag9 && Input.IsGameKeyDown(10))
					{
						if (ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.ControlBlockDirection) == 2f && MissionGameModels.Current.AutoBlockModel != null)
						{
							Agent.UsageDirection blockDirection = MissionGameModels.Current.AutoBlockModel.GetBlockDirection(Mission);
							if (blockDirection == Agent.UsageDirection.AttackLeft)
							{
								mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendRight;
							}
							else if (blockDirection == Agent.UsageDirection.AttackRight)
							{
								mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendLeft;
							}
							else if (blockDirection == Agent.UsageDirection.AttackUp)
							{
								mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendUp;
							}
							else if (blockDirection == Agent.UsageDirection.AttackDown)
							{
								mainAgent.MovementFlags |= Agent.MovementControlFlag.DefendDown;
							}
						}
						else
						{
							mainAgent.MovementFlags |= mainAgent.GetDefendMovementFlag();
						}
					}
				}
				if (!MissionScreen.IsRadialMenuActive && !Mission.IsOrderMenuOpen)
				{
					if (Input.IsGameKeyPressed(16) && (mainAgent.KickClear() || mainAgent.MountAgent != null))
					{
						mainAgent.EventControlFlags |= Agent.EventControlFlag.Kick;
					}
					if (Input.IsGameKeyPressed(18))
					{
						mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.WeaponItemBeginSlot, Agent.WeaponWieldActionType.WithAnimation, false);
					}
					else if (Input.IsGameKeyPressed(19))
					{
						mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon1, Agent.WeaponWieldActionType.WithAnimation, false);
					}
					else if (Input.IsGameKeyPressed(20))
					{
						mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon2, Agent.WeaponWieldActionType.WithAnimation, false);
					}
					else if (Input.IsGameKeyPressed(21))
					{
						mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon3, Agent.WeaponWieldActionType.WithAnimation, false);
					}
					else if (Input.IsGameKeyPressed(11) && _lastWieldNextPrimaryWeaponTriggerTime + 0.2f < Time.ApplicationTime)
					{
						_lastWieldNextPrimaryWeaponTriggerTime = Time.ApplicationTime;
						mainAgent.WieldNextWeapon(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
					}
					else if (Input.IsGameKeyPressed(12) && _lastWieldNextOffhandWeaponTriggerTime + 0.2f < Time.ApplicationTime)
					{
						_lastWieldNextOffhandWeaponTriggerTime = Time.ApplicationTime;
						mainAgent.WieldNextWeapon(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
					}
					else if (Input.IsGameKeyPressed(23))
					{
						mainAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
					}
					// Handle alternative weapon usage
					if (Input.IsGameKeyPressed(17) || _weaponUsageToggleRequested)
					{
						WeaponComponentData currentUsageItem = mainAgent.WieldedWeapon.CurrentUsageItem;
						if (_weaponUsageToggleRequested)
						{
							mainAgent.EventControlFlags |= Agent.EventControlFlag.ToggleAlternativeWeapon;
							_weaponUsageToggleRequested = false;
						}
						// If mounted or equipped with polearm, check if player is in formation before allowing alternative weapon usage (couch)
						if (FormationComponent.Main == null || FormationComponent.Main.State != FormationState.Rambo || !mainAgent.HasMount && !(currentUsageItem != null && currentUsageItem.IsPolearm && !mainAgent.WieldedWeapon.IsAnyConsumable()))
						{
							mainAgent.EventControlFlags |= Agent.EventControlFlag.ToggleAlternativeWeapon;
						}
						else
						{
							Log("Impossible de couch hors formation !", LogLevel.Warning);
						}
					}
					if (Input.IsGameKeyPressed(30))
					{
						mainAgent.EventControlFlags |= (mainAgent.WalkMode ? Agent.EventControlFlag.Run : Agent.EventControlFlag.Walk);
					}
					if (mainAgent.MountAgent != null)
					{
						if (Input.IsGameKeyPressed(15) || _autoDismountModeActive)
						{
							if (mainAgent.GetCurrentVelocity().y < 0.5f && mainAgent.MountAgent.GetCurrentActionType(0) != Agent.ActionCodeType.Rear)
							{
								mainAgent.EventControlFlags |= Agent.EventControlFlag.Dismount;
								return;
							}
							if (Input.IsGameKeyPressed(15))
							{
								_autoDismountModeActive = true;
								mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
								mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionDown;
								return;
							}
						}
					}
					else if (Input.IsGameKeyPressed(15))
					{
						mainAgent.EventControlFlags |= (mainAgent.CrouchMode ? Agent.EventControlFlag.Stand : Agent.EventControlFlag.Crouch);
					}
				}
			}
		}

		private void HandleRangedWeaponAttackAlternativeAiming(Agent player)
		{
			if (Input.GetKeyState(InputKey.ControllerLTrigger).x > 0.2f)
			{
				if (Input.GetKeyState(InputKey.ControllerRTrigger).x < 0.6f)
				{
					player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
				}
				_isPlayerAiming = true;
				return;
			}
			if (_isPlayerAiming)
			{
				player.MovementFlags |= Agent.MovementControlFlag.DefendUp;
				_isPlayerAiming = false;
			}
		}

		private void HandleTriggeredWeaponAttack(Agent player)
		{
			if (Input.GetKeyState(InputKey.ControllerRTrigger).x <= 0.2f)
			{
				if (_isPlayerAiming)
				{
					_playerShotMissile = false;
					_isPlayerAiming = false;
					player.MovementFlags |= Agent.MovementControlFlag.DefendUp;
				}
				return;
			}
			if (!_isPlayerAiming && player.WieldedWeapon.MaxAmmo > 0 && player.WieldedWeapon.Ammo == 0)
			{
				player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
				return;
			}
			if (!_playerShotMissile && Input.GetKeyState(InputKey.ControllerRTrigger).x < 0.99f)
			{
				player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
				_isPlayerAiming = true;
				return;
			}
			_isPlayerAiming = true;
			_playerShotMissile = true;
		}

		public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
		{
			return otherAgent.IsMount && otherAgent.IsActive();
		}

		public void Disable()
		{
			_activated = false;
		}

		public void Enable()
		{
			_activated = true;
		}

		private void OnPlayerToggleOrder(MissionPlayerToggledOrderViewEvent obj)
		{
			_isPlayerOrderOpen = obj.IsOrderEnabled;
		}

		public void OnWeaponUsageToggleRequested()
		{
			_weaponUsageToggleRequested = true;
		}

		private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType optionType)
		{
			if (optionType == ManagedOptions.ManagedOptionsType.LockTarget)
			{
				UpdateLockTargetOption();
			}
		}

		private void UpdateLockTargetOption()
		{
			_isTargetLockEnabled = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.LockTarget) == 1f;
			LockedAgent = null;
			PotentialLockTargetAgent = null;
			_lastLockKeyPressTime = 0f;
			_lastLockedAgentHeightDifference = 0f;
		}

		private const float _minValueForAimStart = 0.2f;

		private const float _maxValueForAttackEnd = 0.6f;

		private float _lastForwardKeyPressTime;

		private float _lastBackwardKeyPressTime;

		private float _lastLeftKeyPressTime;

		private float _lastRightKeyPressTime;

		private float _lastWieldNextPrimaryWeaponTriggerTime;

		private float _lastWieldNextOffhandWeaponTriggerTime;

		private bool _activated = true;

		private bool _strafeModeActive;

		private bool _autoDismountModeActive;

		private bool _isPlayerAgentAdded;

		private bool _isPlayerAiming;

		private bool _playerShotMissile;

		private bool _isPlayerOrderOpen;

		private bool _isTargetLockEnabled;

		private Agent.MovementControlFlag _lastMovementKeyPressed = Agent.MovementControlFlag.Forward;

		private Agent _lockedAgent;

		private Agent _potentialLockTargetAgent;

		private float _lastLockKeyPressTime;

		private float _lastLockedAgentHeightDifference;

		public readonly MissionMainAgentInteractionComponent InteractionComponent;

		public bool IsChatOpen;

		private bool _weaponUsageToggleRequested;

		public delegate void OnLockedAgentChangedDelegate(Agent newAgent);

		public delegate void OnPotentialLockedAgentChangedDelegate(Agent newPotentialAgent);
	}
}
