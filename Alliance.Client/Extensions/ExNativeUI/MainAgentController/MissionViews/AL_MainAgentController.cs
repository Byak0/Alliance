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

        public bool IsDisabled { get; set; }

        public Vec3 CustomLookDir { get; set; }

        public bool IsPlayerAiming
        {
            get
            {
                if (this._isPlayerAiming)
                {
                    return true;
                }
                if (base.Mission.MainAgent == null)
                {
                    return false;
                }
                bool flag = false;
                bool flag2 = false;
                bool flag3 = false;
                if (base.Input != null)
                {
                    flag2 = base.Input.IsGameKeyDown(9);
                }
                if (base.Mission.MainAgent != null)
                {
                    if (base.Mission.MainAgent.WieldedWeapon.CurrentUsageItem != null)
                    {
                        flag = base.Mission.MainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon || base.Mission.MainAgent.WieldedWeapon.CurrentUsageItem.IsAmmo;
                    }
                    flag3 = base.Mission.MainAgent.MovementFlags.HasAnyFlag(Agent.MovementControlFlag.AttackMask);
                }
                return flag && flag2 && flag3;
            }
        }

        public Agent LockedAgent
        {
            get
            {
                return this._lockedAgent;
            }
            private set
            {
                if (this._lockedAgent != value)
                {
                    this._lockedAgent = value;
                    MissionMainAgentController.OnLockedAgentChangedDelegate onLockedAgentChanged = this.OnLockedAgentChanged;
                    if (onLockedAgentChanged == null)
                    {
                        return;
                    }
                    onLockedAgentChanged(value);
                }
            }
        }

        public Agent PotentialLockTargetAgent
        {
            get
            {
                return this._potentialLockTargetAgent;
            }
            private set
            {
                if (this._potentialLockTargetAgent != value)
                {
                    this._potentialLockTargetAgent = value;
                    MissionMainAgentController.OnPotentialLockedAgentChangedDelegate onPotentialLockedAgentChanged = this.OnPotentialLockedAgentChanged;
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
            this.InteractionComponent = new MissionMainAgentInteractionComponent(this);
            this.CustomLookDir = Vec3.Zero;
            this.IsChatOpen = false;
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            Game.Current.EventManager.RegisterEvent<MissionPlayerToggledOrderViewEvent>(new Action<MissionPlayerToggledOrderViewEvent>(this.OnPlayerToggleOrder));
            base.Mission.OnMainAgentChanged += new PropertyChangedEventHandler(this.Mission_OnMainAgentChanged);
            MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            if (((missionBehavior != null) ? missionBehavior.RoundComponent : null) != null)
            {
                missionBehavior.RoundComponent.OnRoundStarted += this.Disable;
                missionBehavior.RoundComponent.OnPreparationEnded += this.Enable;
            }
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(this.OnManagedOptionChanged));
            this.UpdateLockTargetOption();
        }

        public override void OnMissionScreenFinalize()
        {
            base.OnMissionScreenFinalize();
            base.Mission.OnMainAgentChanged -= new PropertyChangedEventHandler(this.Mission_OnMainAgentChanged);
            Game.Current.EventManager.UnregisterEvent<MissionPlayerToggledOrderViewEvent>(new Action<MissionPlayerToggledOrderViewEvent>(this.OnPlayerToggleOrder));
            MissionMultiplayerGameModeBaseClient missionBehavior = base.Mission.GetMissionBehavior<MissionMultiplayerGameModeBaseClient>();
            if (((missionBehavior != null) ? missionBehavior.RoundComponent : null) != null)
            {
                missionBehavior.RoundComponent.OnRoundStarted -= this.Disable;
                missionBehavior.RoundComponent.OnPreparationEnded -= this.Enable;
            }
            ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(this.OnManagedOptionChanged));
        }

        public override bool IsReady()
        {
            bool flag = true;
            if (base.Mission.MainAgent != null)
            {
                flag = base.Mission.MainAgent.AgentVisuals.CheckResources(true);
            }
            return flag;
        }

        private void Mission_OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
        {
            if (base.Mission.MainAgent != null)
            {
                this._isPlayerAgentAdded = true;
                this._strafeModeActive = false;
                this._autoDismountModeActive = false;
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            base.OnPreMissionTick(dt);
            if (base.MissionScreen == null)
            {
                return;
            }
            if (base.Mission.MainAgent == null && GameNetwork.MyPeer != null)
            {
                MissionPeer component = GameNetwork.MyPeer.GetComponent<MissionPeer>();
                if (component != null)
                {
                    if (component.HasSpawnedAgentVisuals)
                    {
                        this.AgentVisualsMovementCheck();
                    }
                    else if (component.FollowedAgent != null)
                    {
                        this.RequestToSpawnAsBotCheck();
                    }
                }
            }
            Agent mainAgent = base.Mission.MainAgent;
            if (mainAgent != null && mainAgent.State == AgentState.Active && !base.MissionScreen.IsCheatGhostMode && !base.Mission.MainAgent.IsAIControlled && !this.IsDisabled && this._activated)
            {
                this.InteractionComponent.FocusTick();
                this.InteractionComponent.FocusedItemHealthTick();
                this.ControlTick();
                this.InteractionComponent.FocusStateCheckTick();
                this.LookTick(dt);
                return;
            }
            this.LockedAgent = null;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (this.InteractionComponent.CurrentFocusedObject == affectedAgent || affectedAgent == base.Mission.MainAgent)
            {
                this.InteractionComponent.ClearFocus();
            }
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            if (this.InteractionComponent.CurrentFocusedObject == affectedAgent)
            {
                this.InteractionComponent.ClearFocus();
            }
        }

        public override void OnClearScene()
        {
            this.InteractionComponent.OnClearScene();
        }

        private void LookTick(float dt)
        {
            if (!this.IsDisabled)
            {
                Agent mainAgent = base.Mission.MainAgent;
                if (mainAgent != null)
                {
                    if (this._isPlayerAgentAdded)
                    {
                        this._isPlayerAgentAdded = false;
                        mainAgent.LookDirectionAsAngle = mainAgent.MovementDirectionAsAngle;
                    }
                    if (base.Mission.ClearSceneTimerElapsedTime >= 0f)
                    {
                        Vec3 vec;
                        if (this.LockedAgent != null)
                        {
                            float num = 0f;
                            float agentScale = this.LockedAgent.AgentScale;
                            float agentScale2 = mainAgent.AgentScale;
                            if (!this.LockedAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
                            {
                                num += this.LockedAgent.Monster.BodyCapsulePoint1.z * agentScale;
                            }
                            else if (this.LockedAgent.HasMount)
                            {
                                num += (this.LockedAgent.MountAgent.Monster.RiderCameraHeightAdder + this.LockedAgent.MountAgent.Monster.BodyCapsulePoint1.z + this.LockedAgent.MountAgent.Monster.BodyCapsuleRadius) * this.LockedAgent.MountAgent.AgentScale + this.LockedAgent.Monster.CrouchEyeHeight * agentScale;
                            }
                            else if (this.LockedAgent.CrouchMode || this.LockedAgent.IsSitting())
                            {
                                num += (this.LockedAgent.Monster.CrouchEyeHeight + 0.2f) * agentScale;
                            }
                            else
                            {
                                num += (this.LockedAgent.Monster.StandingEyeHeight + 0.2f) * agentScale;
                            }
                            if (!mainAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
                            {
                                num -= this.LockedAgent.Monster.BodyCapsulePoint1.z * agentScale2;
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
                            if (this.LockedAgent.GetAgentFlags().HasAnyFlag(AgentFlag.IsHumanoid))
                            {
                                num -= 0.3f * agentScale;
                            }
                            num = MBMath.Lerp(this._lastLockedAgentHeightDifference, num, MathF.Min(8f * dt, 1f), 1E-05f);
                            this._lastLockedAgentHeightDifference = num;
                            vec = (this.LockedAgent.VisualPosition + ((this.LockedAgent.MountAgent != null) ? (this.LockedAgent.MountAgent.GetMovementDirection().ToVec3(0f) * this.LockedAgent.MountAgent.Monster.RiderBodyCapsuleForwardAdder) : Vec3.Zero) + new Vec3(0f, 0f, num, -1f) - (mainAgent.VisualPosition + ((mainAgent.MountAgent != null) ? (mainAgent.MountAgent.GetMovementDirection().ToVec3(0f) * mainAgent.MountAgent.Monster.RiderBodyCapsuleForwardAdder) : Vec3.Zero))).NormalizedCopy();
                        }
                        else if (this.CustomLookDir.IsNonZero)
                        {
                            vec = this.CustomLookDir;
                        }
                        else
                        {
                            Mat3 identity = Mat3.Identity;
                            identity.RotateAboutUp(base.MissionScreen.CameraBearing);
                            identity.RotateAboutSide(base.MissionScreen.CameraElevation);
                            vec = identity.f;
                        }
                        if (!base.MissionScreen.IsViewingCharacter() && !mainAgent.IsLookDirectionLocked && mainAgent.MovementLockedState != AgentMovementLockedState.FrameLocked)
                        {
                            mainAgent.LookDirection = vec;
                        }
                        mainAgent.HeadCameraMode = base.Mission.CameraIsFirstPerson;
                    }
                }
            }
        }

        private void AgentVisualsMovementCheck()
        {
            if (base.Input.IsGameKeyReleased(13))
            {
                this.BreakAgentVisualsInvulnerability();
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
            if (base.Input.IsGameKeyPressed(13))
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
            Vec3 direction = base.MissionScreen.CombatCamera.Direction;
            Vec3 vec = direction;
            Vec3 position = base.MissionScreen.CombatCamera.Position;
            Vec3 visualPosition = player.VisualPosition;
            float num = new Vec3(position.x, position.y, 0f, -1f).Distance(new Vec3(visualPosition.x, visualPosition.y, 0f, -1f));
            Vec3 vec2 = position * (1f - num) + (position + direction) * num;
            float num2 = 0f;
            Agent agent = null;
            foreach (Agent agent2 in base.Mission.Agents)
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
            if (base.MissionScreen != null && base.MissionScreen.IsPhotoModeEnabled)
            {
                return;
            }
            if (this.IsChatOpen)
            {
                return;
            }
            Agent mainAgent = base.Mission.MainAgent;
            bool flag = false;
            if (this.LockedAgent != null && (!base.Mission.Agents.ContainsQ(this.LockedAgent) || !this.LockedAgent.IsActive() || this.LockedAgent.Position.DistanceSquared(mainAgent.Position) > 625f || base.Input.IsGameKeyReleased(26) || base.Input.IsGameKeyDown(25) || (base.Mission.Mode != MissionMode.Battle && base.Mission.Mode != MissionMode.Stealth) || (!mainAgent.WieldedWeapon.IsEmpty && mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) || base.MissionScreen == null || base.MissionScreen.GetSpectatingData(base.MissionScreen.CombatCamera.Frame.origin).CameraType != SpectatorCameraTypes.LockToMainPlayer))
            {
                this.LockedAgent = null;
                flag = true;
            }
            if (base.Mission.Mode == MissionMode.Conversation)
            {
                mainAgent.MovementFlags = (Agent.MovementControlFlag)0U;
                mainAgent.MovementInputVector = Vec2.Zero;
                return;
            }
            if (base.Mission.ClearSceneTimerElapsedTime >= 0f && mainAgent.State == AgentState.Active)
            {
                bool flag2 = false;
                bool flag3 = false;
                bool flag4 = false;
                bool flag5 = false;
                Vec2 vec = new Vec2(base.Input.GetGameKeyAxis("MovementAxisX"), base.Input.GetGameKeyAxis("MovementAxisY"));
                if (this._autoDismountModeActive)
                {
                    if (!base.Input.IsGameKeyDown(0) && mainAgent.MountAgent != null)
                    {
                        if (mainAgent.GetCurrentVelocity().y > 0f)
                        {
                            vec.y = -1f;
                        }
                    }
                    else
                    {
                        this._autoDismountModeActive = false;
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
                if (!base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen)
                {
                    if (base.Input.IsGameKeyPressed(14))
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
                    if (base.Input.IsGameKeyPressed(13))
                    {
                        mainAgent.MovementFlags |= Agent.MovementControlFlag.Action;
                    }
                }
                if (mainAgent.MountAgent != null && mainAgent.GetCurrentVelocity().y < 0.5f && (base.Input.IsGameKeyDown(3) || base.Input.IsGameKeyDown(2)))
                {
                    if (base.Input.IsGameKeyPressed(16))
                    {
                        this._strafeModeActive = true;
                    }
                }
                else
                {
                    this._strafeModeActive = false;
                }
                Agent.MovementControlFlag movementControlFlag = this._lastMovementKeyPressed;
                if (base.Input.IsGameKeyPressed(0))
                {
                    movementControlFlag = Agent.MovementControlFlag.Forward;
                }
                else if (base.Input.IsGameKeyPressed(1))
                {
                    movementControlFlag = Agent.MovementControlFlag.Backward;
                }
                else if (base.Input.IsGameKeyPressed(2))
                {
                    movementControlFlag = Agent.MovementControlFlag.StrafeLeft;
                }
                else if (base.Input.IsGameKeyPressed(3))
                {
                    movementControlFlag = Agent.MovementControlFlag.StrafeRight;
                }
                if (movementControlFlag != this._lastMovementKeyPressed)
                {
                    this._lastMovementKeyPressed = movementControlFlag;
                    Game game = Game.Current;
                    if (game != null)
                    {
                        game.EventManager.TriggerEvent<MissionPlayerMovementFlagsChangeEvent>(new MissionPlayerMovementFlagsChangeEvent(this._lastMovementKeyPressed));
                    }
                }
                if (!base.Input.GetIsMouseActive())
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
                        base.Mission.SetLastMovementKeyPressed(movementControlFlag);
                    }
                }
                else
                {
                    base.Mission.SetLastMovementKeyPressed(this._lastMovementKeyPressed);
                }
                if (base.Input.IsGameKeyPressed(0))
                {
                    if (this._lastForwardKeyPressTime + 0.3f > Time.ApplicationTime)
                    {
                        mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
                        mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionUp;
                    }
                    this._lastForwardKeyPressTime = Time.ApplicationTime;
                }
                if (base.Input.IsGameKeyPressed(1))
                {
                    if (this._lastBackwardKeyPressTime + 0.3f > Time.ApplicationTime)
                    {
                        mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
                        mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionDown;
                    }
                    this._lastBackwardKeyPressTime = Time.ApplicationTime;
                }
                if (base.Input.IsGameKeyPressed(2))
                {
                    if (this._lastLeftKeyPressTime + 0.3f > Time.ApplicationTime)
                    {
                        mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
                        mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionLeft;
                    }
                    this._lastLeftKeyPressTime = Time.ApplicationTime;
                }
                if (base.Input.IsGameKeyPressed(3))
                {
                    if (this._lastRightKeyPressTime + 0.3f > Time.ApplicationTime)
                    {
                        mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
                        mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionRight;
                    }
                    this._lastRightKeyPressTime = Time.ApplicationTime;
                }
                if (this._isTargetLockEnabled)
                {
                    if (base.Input.IsGameKeyDown(26) && this.LockedAgent == null && !base.Input.IsGameKeyDown(25) && (base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Stealth) && (mainAgent.WieldedWeapon.IsEmpty || !mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) && !GameNetwork.IsMultiplayer)
                    {
                        float applicationTime = Time.ApplicationTime;
                        if (this._lastLockKeyPressTime <= 0f)
                        {
                            this._lastLockKeyPressTime = applicationTime;
                        }
                        if (applicationTime > this._lastLockKeyPressTime + 0.3f)
                        {
                            this.PotentialLockTargetAgent = this.FindTargetedLockableAgent(mainAgent);
                        }
                    }
                    else
                    {
                        this.PotentialLockTargetAgent = null;
                    }
                    if (this.LockedAgent == null && !flag && base.Input.IsGameKeyReleased(26) && !GameNetwork.IsMultiplayer)
                    {
                        this._lastLockKeyPressTime = 0f;
                        if (!base.Input.IsGameKeyDown(25) && (base.Mission.Mode == MissionMode.Battle || base.Mission.Mode == MissionMode.Stealth) && (mainAgent.WieldedWeapon.IsEmpty || !mainAgent.WieldedWeapon.CurrentUsageItem.IsRangedWeapon) && base.MissionScreen != null && base.MissionScreen.GetSpectatingData(base.MissionScreen.CombatCamera.Frame.origin).CameraType == SpectatorCameraTypes.LockToMainPlayer)
                        {
                            this.LockedAgent = this.FindTargetedLockableAgent(mainAgent);
                        }
                    }
                }
                if (mainAgent.MountAgent != null && !this._strafeModeActive)
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
                if (!base.MissionScreen.MouseVisible && !base.MissionScreen.IsRadialMenuActive && !this._isPlayerOrderOpen && mainAgent.CombatActionsEnabled)
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
                        this.HandleRangedWeaponAttackAlternativeAiming(mainAgent);
                    }
                    else if (base.Input.IsGameKeyDown(9))
                    {
                        mainAgent.MovementFlags |= mainAgent.AttackDirectionToMovementFlag(mainAgent.GetAttackDirection());
                    }
                    if (!flag9 && base.Input.IsGameKeyDown(10))
                    {
                        if (ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.ControlBlockDirection) == 2f && MissionGameModels.Current.AutoBlockModel != null)
                        {
                            Agent.UsageDirection blockDirection = MissionGameModels.Current.AutoBlockModel.GetBlockDirection(base.Mission);
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
                if (!base.MissionScreen.IsRadialMenuActive && !base.Mission.IsOrderMenuOpen)
                {
                    if (base.Input.IsGameKeyPressed(16) && (mainAgent.KickClear() || mainAgent.MountAgent != null))
                    {
                        mainAgent.EventControlFlags |= Agent.EventControlFlag.Kick;
                    }
                    if (base.Input.IsGameKeyPressed(18))
                    {
                        mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.WeaponItemBeginSlot, Agent.WeaponWieldActionType.WithAnimation, false);
                    }
                    else if (base.Input.IsGameKeyPressed(19))
                    {
                        mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon1, Agent.WeaponWieldActionType.WithAnimation, false);
                    }
                    else if (base.Input.IsGameKeyPressed(20))
                    {
                        mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon2, Agent.WeaponWieldActionType.WithAnimation, false);
                    }
                    else if (base.Input.IsGameKeyPressed(21))
                    {
                        mainAgent.TryToWieldWeaponInSlot(EquipmentIndex.Weapon3, Agent.WeaponWieldActionType.WithAnimation, false);
                    }
                    else if (base.Input.IsGameKeyPressed(11) && this._lastWieldNextPrimaryWeaponTriggerTime + 0.2f < Time.ApplicationTime)
                    {
                        this._lastWieldNextPrimaryWeaponTriggerTime = Time.ApplicationTime;
                        mainAgent.WieldNextWeapon(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                    }
                    else if (base.Input.IsGameKeyPressed(12) && this._lastWieldNextOffhandWeaponTriggerTime + 0.2f < Time.ApplicationTime)
                    {
                        this._lastWieldNextOffhandWeaponTriggerTime = Time.ApplicationTime;
                        mainAgent.WieldNextWeapon(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
                    }
                    else if (base.Input.IsGameKeyPressed(23))
                    {
                        mainAgent.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                    }
                    if (base.Input.IsGameKeyPressed(17) || this._weaponUsageToggleRequested)
                    {
                        if (this._weaponUsageToggleRequested)
                        {
                            mainAgent.EventControlFlags |= Agent.EventControlFlag.ToggleAlternativeWeapon;
                            this._weaponUsageToggleRequested = false;
                        }
                        if (FormationComponent.Main == null || FormationComponent.Main.State != FormationState.Rambo || !mainAgent.HasMount)
                        {
                            mainAgent.EventControlFlags |= Agent.EventControlFlag.ToggleAlternativeWeapon;
                        }
                        else
                        {
                            Log("Impossible de couch hors formation !", LogLevel.Warning);
                        }

                    }
                    if (base.Input.IsGameKeyPressed(30))
                    {
                        mainAgent.EventControlFlags |= (mainAgent.WalkMode ? Agent.EventControlFlag.Run : Agent.EventControlFlag.Walk);
                    }
                    if (mainAgent.MountAgent != null)
                    {
                        if (base.Input.IsGameKeyPressed(15) || this._autoDismountModeActive)
                        {
                            if (mainAgent.GetCurrentVelocity().y < 0.5f && mainAgent.MountAgent.GetCurrentActionType(0) != Agent.ActionCodeType.Rear)
                            {
                                mainAgent.EventControlFlags |= Agent.EventControlFlag.Dismount;
                                return;
                            }
                            if (base.Input.IsGameKeyPressed(15))
                            {
                                this._autoDismountModeActive = true;
                                mainAgent.EventControlFlags &= ~(Agent.EventControlFlag.DoubleTapToDirectionUp | Agent.EventControlFlag.DoubleTapToDirectionDown | Agent.EventControlFlag.DoubleTapToDirectionRight);
                                mainAgent.EventControlFlags |= Agent.EventControlFlag.DoubleTapToDirectionDown;
                                return;
                            }
                        }
                    }
                    else if (base.Input.IsGameKeyPressed(15))
                    {
                        mainAgent.EventControlFlags |= (mainAgent.CrouchMode ? Agent.EventControlFlag.Stand : Agent.EventControlFlag.Crouch);
                    }
                }
            }
        }

        private void HandleRangedWeaponAttackAlternativeAiming(Agent player)
        {
            if (base.Input.GetKeyState(InputKey.ControllerLTrigger).x > 0.2f)
            {
                if (base.Input.GetKeyState(InputKey.ControllerRTrigger).x < 0.6f)
                {
                    player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
                }
                this._isPlayerAiming = true;
                return;
            }
            if (this._isPlayerAiming)
            {
                player.MovementFlags |= Agent.MovementControlFlag.DefendUp;
                this._isPlayerAiming = false;
            }
        }

        private void HandleTriggeredWeaponAttack(Agent player)
        {
            if (base.Input.GetKeyState(InputKey.ControllerRTrigger).x <= 0.2f)
            {
                if (this._isPlayerAiming)
                {
                    this._playerShotMissile = false;
                    this._isPlayerAiming = false;
                    player.MovementFlags |= Agent.MovementControlFlag.DefendUp;
                }
                return;
            }
            if (!this._isPlayerAiming && player.WieldedWeapon.MaxAmmo > 0 && player.WieldedWeapon.Ammo == 0)
            {
                player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
                return;
            }
            if (!this._playerShotMissile && base.Input.GetKeyState(InputKey.ControllerRTrigger).x < 0.99f)
            {
                player.MovementFlags |= player.AttackDirectionToMovementFlag(player.GetAttackDirection());
                this._isPlayerAiming = true;
                return;
            }
            this._isPlayerAiming = true;
            this._playerShotMissile = true;
        }

        public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
        {
            return otherAgent.IsMount && otherAgent.IsActive();
        }

        public void Disable()
        {
            this._activated = false;
        }

        public void Enable()
        {
            this._activated = true;
        }

        private void OnPlayerToggleOrder(MissionPlayerToggledOrderViewEvent obj)
        {
            this._isPlayerOrderOpen = obj.IsOrderEnabled;
        }

        public void OnWeaponUsageToggleRequested()
        {
            this._weaponUsageToggleRequested = true;
        }

        private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType optionType)
        {
            if (optionType == ManagedOptions.ManagedOptionsType.LockTarget)
            {
                this.UpdateLockTargetOption();
            }
        }

        private void UpdateLockTargetOption()
        {
            this._isTargetLockEnabled = ManagedOptions.GetConfig(ManagedOptions.ManagedOptionsType.LockTarget) == 1f;
            this.LockedAgent = null;
            this.PotentialLockTargetAgent = null;
            this._lastLockKeyPressTime = 0f;
            this._lastLockedAgentHeightDifference = 0f;
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
