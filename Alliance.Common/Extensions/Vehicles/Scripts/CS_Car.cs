using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    /// <summary>
    /// Custom script to control cars. 
    /// </summary>
    public class CS_Car : CS_Vehicle
    {
        public float BackWheelRadius = 1f;
        public float FrontWheelRadius = 1f;

        protected GameEntity PivotSteeringWheel;
        protected GameEntity PivotFrontLeft;
        protected GameEntity PivotFrontRight;
        protected GameEntity WheelFrontLeft;
        protected GameEntity WheelFrontRight;
        protected GameEntity WheelsBack;
        protected GameEntity CountOil;
        protected GameEntity CountSpeed;
        protected GameEntity CountRPM;
        protected GameEntity CountVacuum;
        protected GameEntity CountPressure;
        protected GameEntity LightLeft;
        protected GameEntity LightRight;
        protected List<GameEntity> CollisionPoints;

        protected float OilLevel = 80f;
        protected float CountRPMAngle = 0f;
        protected float CountVacuumAngle = 0f;
        protected float CountPressureAngle = 0f;

        protected float CurrentPivotTurnAngleDeg = 0f;
        protected float GroundOffset;

        protected float CurrentDriftTorque = 0f; // The current torque of the drift
        protected float CurrentDriftForce = 0f; // The current force of the drift
        protected float DriftStrafeFactor = 0.25f; // The amount of sideways motion when drifting
        protected float DriftTurnFactor = 0.1f; // The amount of turn rotation when drifting
        protected float CurrentEngineTorque = 300f; // Nm
        protected const float EngineTorqueDefault = 300f; // Nm
        protected float BrakeForce = 3800f; // N
        protected const float BrakeForceDefault = 3800f; // N
        protected List<float> GearRatios = new List<float> { 1.8f, 1.5f, 1.2f, 0.9f, 0.7f }; // Average sports car gear ratios
        protected int CurrentGear = 0;
        protected int Mass = 240;
        protected float Gravity = -9.8f;

        protected bool IsFlying;
        protected bool PreviouslyGoingUp;
        protected bool PreviouslyGoingDown;
        protected float PreviousUpwardRotation;
        protected float PreviousHeightToTerrain;

        private SoundEvent _honkSound;
        private bool _lightOn;

        public bool LightOn
        {
            get
            {
                return _lightOn;
            }
            protected set
            {
                if (value != _lightOn)
                {
                    _lightOn = value;
                    LightLeft.SetVisibilityExcludeParents(value);
                    LightRight.SetVisibilityExcludeParents(value);
                }
            }
        }

        public CS_Car()
        {
            ForwardAccelerationRate = MaxForwardSpeed / TimeToAttainMaxForwardSpeed * 10f;
            BackwardAccelerationRate = MaxBackwardSpeed / TimeToAttainMaxBackwardSpeed;
            UpwardAccelerationRate = MaxUpwardSpeed / TimeToAttainMaxUpwardSpeed;
            DownwardAccelerationRate = MaxDownwardSpeed / TimeToAttainMaxDownwardSpeed;
            TurnRate = MaxTurnAngle / TimeToAttainMaxTurn * 7f;
            CurrentUpwardSpeed = -1f;
        }

        protected override void OnInit()
        {
            base.OnInit();

            List<GameEntity> allChildren = new List<GameEntity>();
            GameEntity.GetChildrenRecursive(ref allChildren);
            CollisionPoints = new List<GameEntity>();

            foreach (GameEntity child in allChildren)
            {
                if (child.HasTag("wheels_back"))
                {
                    WheelsBack = child;
                }
                if (child.HasTag("wheel_front_left"))
                {
                    WheelFrontLeft = child;
                }
                if (child.HasTag("wheel_front_right"))
                {
                    WheelFrontRight = child;
                }
                if (child.HasTag("pivot_steering_wheel"))
                {
                    PivotSteeringWheel = child;
                }
                if (child.HasTag("pivot_front_left"))
                {
                    PivotFrontLeft = child;
                }
                if (child.HasTag("pivot_front_right"))
                {
                    PivotFrontRight = child;
                }
                if (child.HasTag("oil_count"))
                {
                    CountOil = child;
                }
                if (child.HasTag("speed_count"))
                {
                    CountSpeed = child;
                }
                if (child.HasTag("rpm_count"))
                {
                    CountRPM = child;
                }
                if (child.HasTag("first_stage_count"))
                {
                    CountVacuum = child;
                }
                if (child.HasTag("second_stage_count"))
                {
                    CountPressure = child;
                }
                if (child.HasTag("light_left"))
                {
                    LightLeft = child;
                }
                if (child.HasTag("light_right"))
                {
                    LightRight = child;
                }
                if (child.HasTag("collision_point"))
                {
                    CollisionPoints.Add(child);
                }
            }
            if (WheelsBack == null || WheelFrontLeft == null || WheelFrontRight == null || PivotSteeringWheel == null || CountOil == null)
            {
                ValidState = false;
            }
            else
            {
                FollowsTerrainPoints = new List<GameEntity> {
                    WheelsBack,
                    WheelFrontLeft,
                    WheelFrontRight
                };
            }

            GroundOffset = 0;
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (_honkSound != null && _honkSound.IsPlaying())
            {
                _honkSound.SetPosition(GameEntity.GetGlobalFrame().origin);
            }
        }

        public void RequestLight(bool lightOn, bool sync = false)
        {
            if (LightOn != lightOn)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestLight(this, lightOn));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    LightOn = lightOn;
                }
            }
        }

        public virtual void ServerToggleLight(bool lightOn)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncLight(this, lightOn));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            LightOn = lightOn;
        }

        public void RequestHonk(bool sync = false)
        {
            if (sync)
            {
                GameNetwork.BeginModuleEventAsClient();
                GameNetwork.WriteMessage(new CS_VehicleRequestHonk(this));
                GameNetwork.EndModuleEventAsClient();
            }
            else
            {
                // Play honk sound
                _honkSound = SoundEvent.CreateEvent(SoundEvent.GetEventIdFromString("event:/mission/car/car_horn"), Scene);
                _honkSound.SetPosition(GameEntity.GetGlobalFrame().origin);
                _honkSound.Play();
                DelayedStop(_honkSound);
            }
        }

        // Delayed stop to prevent ambient sounds from looping
        private async void DelayedStop(SoundEvent _honkSound)
        {
            await Task.Delay(3000);
            _honkSound.Stop();
        }

        public virtual void ServerSyncHonk()
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncHonk(this));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
        }

        public override void UpdateDirectionAndSpeed(float dt)
        {
            if (PilotAgent == null || OilLevel <= 0)
            {
                Decelerate(dt);
                SlowdownTurn(dt);
                return;
            }

            if (_moveForward) MoveForward(dt);
            if (_moveBackward) MoveBackward(dt);
            if (!_moveForward && !_moveBackward) Decelerate(dt);

            if (_moveUpward) MoveUpward(dt);
            else if (_moveDownward) MoveDownward(dt);

            if (_turnLeft) TurnLeft(dt);
            else if (_turnRight) TurnRight(dt);
            else SlowdownTurn(dt);
        }

        public override void UpdateVehicleMovement(float dt)
        {
            // Update speed and turn based on pilot input
            UpdateDirectionAndSpeed(dt);

            bool moving = CurrentForwardSpeed != 0f || CurrentUpwardSpeed != 0f || CurrentTurnRate != 0f;

            if (!moving) return;

            // Get all car parts
            MatrixFrame carFrame = GameEntity.GetFrame();
            MatrixFrame pivotSteeringWheelFrame = PivotSteeringWheel.GetFrame();
            MatrixFrame pivotFrontLeftFrame = PivotFrontLeft.GetFrame();
            MatrixFrame pivotFrontRightFrame = PivotFrontRight.GetFrame();
            MatrixFrame wheelFrontLeftFrame = WheelFrontLeft.GetFrame();
            MatrixFrame wheelFrontRightFrame = WheelFrontRight.GetFrame();
            MatrixFrame wheelsBackFrame = WheelsBack.GetFrame();
            MatrixFrame countOilFrame = CountOil.GetFrame();
            MatrixFrame countSpeedFrame = CountSpeed.GetFrame();
            MatrixFrame countRPMFrame = CountRPM.GetFrame();
            MatrixFrame countFirstStageFrame = CountVacuum.GetFrame();
            MatrixFrame countSecondStageFrame = CountPressure.GetFrame();

            // Update car properties
            UpdateEngineTorque(dt);
            UpdateBrakeForce(dt);
            UpdateWheelTurn(pivotFrontLeftFrame.rotation.f);

            // Update counters values
            UpdateOilCount(ref countOilFrame, dt);
            UpdateSpeedCount(ref countSpeedFrame);
            UpdateRPMCount(ref countRPMFrame, ref countFirstStageFrame, ref countSecondStageFrame, dt);

            // Update wheels rotation based on speed and angle
            if (CurrentForwardSpeed != 0f)
            {
                float wheelRotationMultiplier = MathF.Clamp(Math.Abs(CurrentForwardSpeed), 0.7f, 1f);
                UpdateWheelsRotation(dt, ref wheelsBackFrame, wheelRotationMultiplier, BackWheelRadius);
                UpdateWheelsRotation(dt, ref wheelFrontLeftFrame, wheelRotationMultiplier - CurrentPivotTurnAngleDeg / 100, FrontWheelRadius);
                UpdateWheelsRotation(dt, ref wheelFrontRightFrame, wheelRotationMultiplier + CurrentPivotTurnAngleDeg / 100, FrontWheelRadius);
            }

            // Update wheels turn on left/right based 
            if (CurrentTurnRate != 0f)
            {
                // Check if the current turn rate is not zero
                bool oppositeDirection = CurrentPivotTurnAngleDeg * CurrentTurnRate < 0;
                float turnRateAngleDeg = CurrentTurnRate * dt;
                float nextPivotTurnAngleDeg = CurrentPivotTurnAngleDeg + turnRateAngleDeg;
                float turnRateRad = MathHelper.ToRadian(turnRateAngleDeg);

                if (oppositeDirection || Math.Abs(nextPivotTurnAngleDeg) <= MaxTurnAngle)
                {
                    CurrentPivotTurnAngleDeg = nextPivotTurnAngleDeg;
                    UpdatePivotFrames(turnRateRad, ref pivotSteeringWheelFrame, ref pivotFrontLeftFrame, ref pivotFrontRightFrame);
                }
            }

            if (CurrentForwardSpeed != 0)
            {
                // Calculate the amount of rotation
                float desiredRotationAngleRad = MathHelper.ToRadian(CurrentPivotTurnAngleDeg);
                float rotationRate = desiredRotationAngleRad * CurrentForwardSpeed * dt / 2;
                float maxRate = Math.Abs(desiredRotationAngleRad);
                rotationRate = MathF.Clamp(rotationRate, -maxRate, maxRate);

                // Apply the rotation to the car
                if (GameNetwork.IsServer)
                {
                    if (IsFlying)
                    {
                        carFrame.Rotate(rotationRate * 0.5f, Vec3.Up);
                    }
                    else
                    {
                        carFrame.Rotate(rotationRate, Vec3.Up);
                    }
                }

                // Reduce turn as the car go forward
                float turnRateAngleDeg = MathHelper.ToDegrees(-rotationRate / 5f);
                float nextPivotTurnAngleDeg = CurrentPivotTurnAngleDeg + turnRateAngleDeg;
                if (Math.Abs(nextPivotTurnAngleDeg) <= MaxTurnAngle)
                {
                    UpdatePivotFrames(-rotationRate / 5f, ref pivotSteeringWheelFrame, ref pivotFrontLeftFrame, ref pivotFrontRightFrame);
                    CurrentPivotTurnAngleDeg = nextPivotTurnAngleDeg;
                }
            }

            // Update car position/rotation based on speed, turn rate, gravity, etc. (Server only)
            if (GameNetwork.IsServer)
            {
                Vec3[] collisionPoints = GetCollisionPoints();
                float heightToTerrain = GetHeightToTerrain(GetCollisionPoints());

                // Assuming points are ordered in clockwise or counterclockwise direction
                float averageBackWheelsZ = WheelsBack.GetGlobalFrame().origin.z;
                float averageFrontWheelsZ = (WheelFrontLeft.GetGlobalFrame().origin.z + WheelFrontRight.GetGlobalFrame().origin.z) / 2;

                // Get car upward rotation to determine if we're going up or down
                float carUpwardRotation = MathHelper.AngleBetweenTwoVectors(carFrame.rotation.u, Vec3.Up);
                float carUpwardRotationDeg = MathHelper.ToDegrees(carUpwardRotation);
                float relativeAngleForTakeOff = 2 - 1.6f * (CurrentForwardSpeed / MaxForwardSpeed);
                bool carGoingUp = averageFrontWheelsZ >= averageBackWheelsZ && carUpwardRotationDeg >= 0;
                bool carGoingDown = averageFrontWheelsZ < averageBackWheelsZ && carUpwardRotationDeg > 0;
                bool takeOffAsc = carGoingUp && PreviousHeightToTerrain + 0.1f < heightToTerrain ||
                    PreviouslyGoingUp && carUpwardRotationDeg < PreviousUpwardRotation - relativeAngleForTakeOff;
                bool takeOffDesc = PreviouslyGoingDown && carUpwardRotationDeg > PreviousUpwardRotation + relativeAngleForTakeOff;
                PreviouslyGoingUp = carGoingUp;
                PreviouslyGoingDown = carGoingDown;

                if (!IsFlying && Math.Abs(CurrentForwardSpeed) > 5f && (takeOffAsc || takeOffDesc))
                {
                    Log($"TakingOff! | {carUpwardRotationDeg} | backWheelsZ={averageBackWheelsZ} | frontWheelsZ={averageFrontWheelsZ} | distToTerrain={heightToTerrain} | relativeAng={relativeAngleForTakeOff}");
                    IsFlying = true;
                    CurrentUpwardSpeed = 0f;
                }
                if (IsFlying && heightToTerrain < -0.03f)
                {
                    Log($"GroundReached! | {carUpwardRotationDeg} | backWheelsZ={averageBackWheelsZ} | frontWheelsZ={averageFrontWheelsZ} | distToTerrain={heightToTerrain}");
                    IsFlying = false;
                    CurrentUpwardSpeed = -1f;
                }
                PreviousUpwardRotation = carUpwardRotationDeg;
                PreviousHeightToTerrain = heightToTerrain;

                // Apply gravity and put car on ground if below
                if (heightToTerrain < -0.03f)
                {
                    carFrame.Elevate(-heightToTerrain);
                }
                else if (heightToTerrain > 0.03f)
                {
                    float adjustment = Math.Max(-heightToTerrain, CurrentUpwardSpeed * dt);
                    carFrame.Elevate(adjustment);
                }

                if (IsFlying)
                {
                    // Increase gravity                    
                    CurrentUpwardSpeed += Gravity * dt;

                    // Update position
                    carFrame.Advance(CurrentForwardSpeed * dt);

                    // Rotate the car based on the slope steepness ?
                    float rotationFactor = Math.Min(-0.005f, 0.04f * -carUpwardRotation);
                    float rollFactor = rotationFactor / 5f * (_turnLeft ? 1 : -1);
                    carFrame.Rotate(rotationFactor, Vec3.Side);
                    carFrame.Rotate(rollFactor, Vec3.Forward);
                }
                else
                {
                    // Propelling
                    carFrame.Advance(CurrentForwardSpeed * dt);

                    // Drifting
                    if (Math.Abs(CurrentForwardSpeed) > 10f && Math.Abs(CurrentPivotTurnAngleDeg) > 1f)
                    {
                        HandleDrifting(dt, ref carFrame);
                    }

                    // Align with terrain
                    if (heightToTerrain < 1f && FollowTerrain && (CurrentForwardSpeed != 0f || CurrentUpwardSpeed != 0f) && GameNetwork.IsServer)
                    {
                        AlignFrameWithGround(ref carFrame, GameEntity, collisionPoints, GroundOffset);
                    }
                }
            }

            // Set local frames (client & server)
            PivotSteeringWheel.SetFrame(ref pivotSteeringWheelFrame);
            PivotFrontLeft.SetFrame(ref pivotFrontLeftFrame);
            PivotFrontRight.SetFrame(ref pivotFrontRightFrame);
            WheelFrontRight.SetFrame(ref wheelFrontRightFrame);
            WheelFrontLeft.SetFrame(ref wheelFrontLeftFrame);
            WheelsBack.SetFrame(ref wheelsBackFrame);
            CountOil.SetFrame(ref countOilFrame);
            CountSpeed.SetFrame(ref countSpeedFrame);
            CountRPM.SetFrame(ref countRPMFrame);
            CountVacuum.SetFrame(ref countFirstStageFrame);
            CountPressure.SetFrame(ref countSecondStageFrame);

            // Sync global frame (server only)
            if (GameNetwork.IsServer)
            {
                SetFrameSynched(ref carFrame);
            }
        }

        private void UpdateRPMCount(ref MatrixFrame countRPMFrame, ref MatrixFrame countVacuumFrame, ref MatrixFrame countPressureFrame, float dt)
        {
            // Constants
            float maxRPM = 5000f;
            float[] maxRPMPerGear = new float[] { 2600, 3200, 3800, 4200, 5000 };
            float shiftDropRatio = 0.5f;

            // Calculate RPM
            float maxSpeedInCurrentGear = MaxForwardSpeed * ((CurrentGear + 1) / (float)GearRatios.Count);
            float maxRPMInCurrentGear = maxRPMPerGear[CurrentGear]; // Adjust max RPM for first 4 gears
            float rpmInCurrentGear = CurrentForwardSpeed / maxSpeedInCurrentGear * maxRPMInCurrentGear;

            // Apply gear shift drop
            if (CurrentGear > 0 && rpmInCurrentGear < shiftDropRatio * maxRPMInCurrentGear)
            {
                rpmInCurrentGear = shiftDropRatio * maxRPMInCurrentGear;
            }

            float estimatedRPM = Math.Max(0, rpmInCurrentGear);
            float rpmAngleTarget = MathHelper.ToRadian(estimatedRPM / maxRPM * 290f);
            CountRPMAngle = MathF.Lerp(CountRPMAngle, rpmAngleTarget, dt * 4);
            countRPMFrame.rotation = MatrixFrame.Identity.rotation;
            countRPMFrame.Rotate(CountRPMAngle, Vec3.Forward);

            // For the Vacuum gauge (1st stage)
            float vacuumReading = 160 * (1 - estimatedRPM / maxRPM);
            float vacuumAngleTarget = MathHelper.ToRadian(vacuumReading / 160 * 240);
            CountVacuumAngle = MathF.Lerp(CountVacuumAngle, vacuumAngleTarget, dt * 4);
            countVacuumFrame.rotation = MatrixFrame.Identity.rotation;
            countVacuumFrame.Rotate(CountVacuumAngle, Vec3.Forward);

            // For the Pressure gauge (2nd stage)
            float pressureReading = 160 * (estimatedRPM / maxRPM);
            float pressureAngleTarget = MathHelper.ToRadian(pressureReading / 160 * 240);
            CountPressureAngle = MathF.Lerp(CountPressureAngle, pressureAngleTarget, dt * 4);
            countPressureFrame.rotation = MatrixFrame.Identity.rotation;
            countPressureFrame.Rotate(CountPressureAngle, Vec3.Forward);
        }

        private void UpdateSpeedCount(ref MatrixFrame countSpeedFrame)
        {
            float speedAngle = MathHelper.ToRadian(2.9f * Math.Abs(CurrentForwardSpeed));
            countSpeedFrame.rotation = MatrixFrame.Identity.rotation;
            countSpeedFrame.Rotate(speedAngle, Vec3.Forward);
        }

        private void UpdateOilCount(ref MatrixFrame countOilFrame, float dt)
        {
            // Reduce oil level based on time spent and speed
            OilLevel = Math.Max(0, OilLevel - dt * CurrentForwardSpeed * 0.001f);
            float oilAngle = MathHelper.ToRadian(2.9625f * (OilLevel - 80));
            countOilFrame.rotation = MatrixFrame.Identity.rotation;
            countOilFrame.Rotate(oilAngle, Vec3.Forward);
        }

        private void UpdateWheelTurn(Vec3 wheelRotation)
        {
            CurrentPivotTurnAngleDeg = MathHelper.ToDegrees(MathHelper.AngleBetweenTwoVectors(wheelRotation, Vec3.Forward));
        }

        public override void MoveForward(float dt)
        {
            // Calculate the engine force
            float engineForce = CurrentEngineTorque / FrontWheelRadius;

            // Calculate the total force exerted on the car
            float totalForce = engineForce;

            // Subtract the brake force if the car is moving
            if (CurrentForwardSpeed < 0)
            {
                totalForce -= BrakeForce * MathF.Sign(CurrentForwardSpeed);
            }

            // Add the drag force (which should always oppose the direction of motion)
            float dragForce = 0.05f * CurrentForwardSpeed * MathF.Abs(CurrentForwardSpeed);
            totalForce -= dragForce;

            // Add the rolling resistance (which should always oppose the direction of motion)
            float rollingResistance = 0.02f * Mass;
            totalForce -= rollingResistance;

            // Adjust the current forward speed
            CurrentForwardSpeed += totalForce / Mass * dt;
            CurrentForwardSpeed = MathF.Min(CurrentForwardSpeed, MaxForwardSpeed);
        }

        public override void MoveBackward(float dt)
        {
            // Calculate the engine force
            float engineForce = -CurrentEngineTorque / FrontWheelRadius;

            // Calculate the total force exerted on the car
            float totalForce = engineForce;

            // Subtract the brake force if the car is moving
            if (CurrentForwardSpeed > 0)
            {
                totalForce -= BrakeForce * MathF.Sign(CurrentForwardSpeed);
            }

            // Add the drag force (which should always oppose the direction of motion)
            float dragForce = 0.3f * CurrentForwardSpeed * MathF.Abs(CurrentForwardSpeed);
            totalForce -= dragForce;

            // Add the rolling resistance (which should always oppose the direction of motion)
            float rollingResistance = 0.01f * Mass * 10;
            totalForce -= rollingResistance;

            // Adjust the current forward speed
            CurrentForwardSpeed += totalForce / Mass * dt;
            CurrentForwardSpeed = MathF.Min(CurrentForwardSpeed, MaxForwardSpeed);
        }

        public override void TurnLeft(float dt)
        {
            // The turning rate decreases as the speed increases
            float relativeSpeed = (float)(0.3 + (0.8 - 0.3) * (CurrentForwardSpeed / MaxForwardSpeed));
            CurrentTurnRate = (MaxTurnAngle - relativeSpeed * MaxTurnAngle) / 2f;
        }

        public override void TurnRight(float dt)
        {
            // The turning rate decreases as the speed increases
            float relativeSpeed = (float)(0.3 + (0.8 - 0.3) * (CurrentForwardSpeed / MaxForwardSpeed));
            CurrentTurnRate = -(MaxTurnAngle - relativeSpeed * MaxTurnAngle) / 2f;
        }

        private void HandleDrifting(float dt, ref MatrixFrame carFrame)
        {
            if (Math.Abs(CurrentPivotTurnAngleDeg) < 5f && !_moveBackward)
            {
                return;
            }

            bool isBraking = CurrentForwardSpeed > 0 && _moveBackward;

            // If the car is braking, increase the drift forces
            float brakeFactor = isBraking ? 2.0f : 1.0f;
            float relativeSpeed = (float)(0.3 + (1 - 0.3) * (CurrentForwardSpeed / MaxForwardSpeed));

            // CurrentDriftTorque is proportional to the current steering angle, and increased if braking
            CurrentDriftTorque = Math.Abs(CurrentPivotTurnAngleDeg) * DriftTurnFactor * relativeSpeed * brakeFactor;

            // CurrentDriftForce is proportional to the current speed, and increased if braking
            CurrentDriftForce = CurrentForwardSpeed * CurrentDriftTorque * DriftStrafeFactor * brakeFactor;

            // If the car is not braking and there is some sideways speed, it should decrease over time
            float speedLoss = 0.05f * CurrentDriftForce * CurrentDriftTorque * dt;
            CurrentForwardSpeed -= speedLoss;

            int side = Math.Sign(CurrentPivotTurnAngleDeg);

            // The strafe amount is proportional to the sideways speed and the time step
            float strafeAmount = CurrentDriftForce * dt * side;

            // The rotate amount is proportional to the torque and the time step
            float rotateAmount = CurrentDriftTorque * dt * side;

            // Update the car's position and rotation
            carFrame.Strafe(strafeAmount);
            carFrame.Rotate(rotateAmount, Vec3.Up);

            //Utility.Log($"Drift - Strafe by {strafeAmount} | Rotate by {rotateAmount} | Speed = {CurrentForwardSpeed} (lost {speedLoss})");
        }

        private void UpdatePivotFrames(float turnRateRad, ref MatrixFrame pivotSteeringWheelFrame, ref MatrixFrame pivotFrontLeftFrame, ref MatrixFrame pivotFrontRightFrame)
        {
            // Rotate the steering wheel more because we don't have assisted direction
            pivotSteeringWheelFrame.Rotate(-turnRateRad * 7f, Vec3.Forward);
            // Rotate front wheels
            pivotFrontLeftFrame.Rotate(turnRateRad, Vec3.Up);
            pivotFrontRightFrame.Rotate(turnRateRad, Vec3.Up);
        }

        public override void SlowdownTurn(float dt)
        {
            if (CurrentTurnRate > 0)
            {
                CurrentTurnRate -= TurnSlowdownRate * dt * 5f;
                if (CurrentTurnRate < 0) CurrentTurnRate = 0;
            }
            else if (CurrentTurnRate < 0)
            {
                CurrentTurnRate += TurnSlowdownRate * dt * 5f;
                if (CurrentTurnRate > 0) CurrentTurnRate = 0;
            }
        }

        public override Vec3[] GetCollisionPoints()
        {
            Vec3[] collisionPoints = new Vec3[CollisionPoints.Count];

            for (int i = 0; i < CollisionPoints.Count; i++)
            {
                collisionPoints[i] = CollisionPoints[i].GetGlobalFrame().origin;
            }

            return collisionPoints;
        }

        public void UpdateWheelsRotation(float dt, ref MatrixFrame wheelFrame, float speedMultiplier = 1f, float wheelRadius = 1f)
        {
            if (wheelFrame == null) return;

            // Adjust the rotation speed based on the wheel size            
            float wheelRotationSpeed = speedMultiplier * (CurrentForwardSpeed + .1f) / wheelRadius;

            // Rotate the wheel frame
            wheelFrame.Rotate(wheelRotationSpeed * dt * -1f, Vec3.Side);
        }

        private void UpdateEngineTorque(float dt)
        {
            // Update engine torque based on speed and gear
            float relativeSpeed = Math.Abs(CurrentForwardSpeed) / MaxForwardSpeed;

            // If the relative speed exceeds the threshold for the current gear and the gear isn't max, shift up
            if (relativeSpeed > (CurrentGear + 1) / (float)GearRatios.Count && CurrentGear < GearRatios.Count - 1)
            {
                ShiftUp();
            }
            // If the relative speed falls below the threshold for the current gear and the gear isn't min, shift down
            else if (relativeSpeed < CurrentGear / (float)GearRatios.Count && CurrentGear > 0)
            {
                ShiftDown();
            }

            // After potential gear shift, update the engine torque
            CurrentEngineTorque = EngineTorqueDefault * GearRatios[CurrentGear];
        }

        private void UpdateBrakeForce(float dt)
        {
            // Update brake force based on speed
            float speedFactor = Math.Abs(Math.Min(CurrentForwardSpeed / MaxForwardSpeed, 0.9f));
            BrakeForce = BrakeForceDefault * (1.0f - speedFactor);
        }

        public void ShiftUp()
        {
            if (CurrentGear < GearRatios.Count - 1)
            {
                CurrentGear++;
                //Utility.Log($"Shift gear up to {CurrentGear}");
            }
        }

        public void ShiftDown()
        {
            if (CurrentGear > 0)
            {
                CurrentGear--;
                //Utility.Log($"Shift gear down to {CurrentGear}");
            }
        }

        public override void AlignFrameWithGround(ref MatrixFrame frame, GameEntity gameEntity, Vec3[] collisionPoints, float groundOffset = 0f)
        {
            // Store the collisionPoint positions
            Vec3.StackArray8Vec3 collisionPointPositions = default;

            // Check if visibility exclusion is enabled and disable it temporarily
            bool wasVisibilityExcluded = gameEntity.GetVisibilityExcludeParents();
            if (wasVisibilityExcluded)
            {
                gameEntity.SetVisibilityExcludeParents(false);
            }

            int numcollisionPoints = 0;

            // Acquire a read lock on the physics and raycast lock of the scene
            using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
            {
                // Calculate collisionPoint positions and ground heights
                foreach (Vec3 collisionPoint in collisionPoints)
                {
                    Vec3 collisionPointPosition = collisionPoint;

                    // Get the ground height at the collisionPoint position
                    collisionPointPosition.z = FlyElevation - groundOffset + Scene.GetGroundHeightAtPositionMT(collisionPointPosition, BodyFlags.CommonCollisionExcludeFlags);

                    // Store the collisionPoint position
                    collisionPointPositions[numcollisionPoints++] = collisionPointPosition;
                }
            }

            // Re-enable visibility exclusion if it was originally enabled
            if (wasVisibilityExcluded)
            {
                gameEntity.SetVisibilityExcludeParents(true);
            }

            // Variables for calculating rotation angles
            float sumX = 0f;
            float sumXY = 0f;
            float sumY = 0f;
            float sumXZ = 0f;
            float sumYZ = 0f;

            // Calculate the average position of the collisionPoint points
            Vec3 averagePosition = default;
            for (int i = 0; i < numcollisionPoints; i++)
            {
                averagePosition += collisionPointPositions[i];
            }
            averagePosition /= numcollisionPoints;

            // Calculate covariance matrix and rotation angles
            for (int j = 0; j < numcollisionPoints; j++)
            {
                Vec3 positionDifference = collisionPointPositions[j] - averagePosition;
                sumX += positionDifference.x * positionDifference.x;
                sumXY += positionDifference.x * positionDifference.y;
                sumY += positionDifference.y * positionDifference.y;
                sumXZ += positionDifference.x * positionDifference.z;
                sumYZ += positionDifference.y * positionDifference.z;
            }

            float determinant = sumX * sumY - sumXY * sumXY;
            float rotationAngleX = (sumYZ * sumXY - sumXZ * sumY) / determinant;
            float rotationAngleY = (sumXY * sumXZ - sumX * sumYZ) / determinant;

            MatrixFrame groundFrame;

            // Set the rotation vectors of the ground frame
            groundFrame.rotation.u = new Vec3(rotationAngleX, rotationAngleY, 1f, -1f);
            groundFrame.rotation.u.Normalize();
            groundFrame.rotation.f = frame.rotation.f;
            groundFrame.rotation.f -= Vec3.DotProduct(frame.rotation.f, groundFrame.rotation.u) * groundFrame.rotation.u;
            groundFrame.rotation.f.Normalize();
            groundFrame.rotation.s = Vec3.CrossProduct(groundFrame.rotation.f, groundFrame.rotation.u);
            groundFrame.rotation.s.Normalize();

            // Update the rotation of the frame
            frame.rotation.u = groundFrame.rotation.u * frame.rotation.u.Length;
            frame.rotation.f = groundFrame.rotation.f * frame.rotation.f.Length;
            frame.rotation.s = groundFrame.rotation.s * frame.rotation.s.Length;
        }

        public float GetHeightToTerrain(Vec3[] collisionPoints, float groundOffset = 0f)
        {
            float heightAdjustmentUnder = 0f;
            float heightAdjustmentOver = 0f;

            // Check if visibility exclusion is enabled and disable it temporarily
            bool wasVisibilityExcluded = GameEntity.GetVisibilityExcludeParents();
            if (wasVisibilityExcluded)
            {
                GameEntity.SetVisibilityExcludeParents(false);
            }

            // Make the vehicle follow the ground
            foreach (Vec3 cp in collisionPoints)
            {
                float terrainHeight = Scene.GetGroundHeightAtPositionMT(cp, BodyFlags.CommonCollisionExcludeFlags);
                float cpHeight = cp.z - groundOffset - terrainHeight;

                // Collision point is under the ground
                if (cpHeight < 0)
                {
                    heightAdjustmentUnder = Math.Min(heightAdjustmentUnder, cpHeight);
                }
                // Collision point is flying
                else if (cpHeight > FlyElevation)
                {
                    heightAdjustmentOver = Math.Max(heightAdjustmentOver, cpHeight - FlyElevation);
                }
            }

            // Re-enable visibility exclusion if it was originally enabled
            if (wasVisibilityExcluded)
            {
                GameEntity.SetVisibilityExcludeParents(true);
            }

            float heightToTerrain = heightAdjustmentOver + heightAdjustmentUnder;

            return heightToTerrain;
        }

        public override void UpdatePilot(Agent agent)
        {
            base.UpdatePilot(agent);

            if (GameNetwork.IsClient)
            {
                PilotAgent.AgentVisuals.SetVisible(false);
            }
        }

        public override void RemovePilot(Agent agent)
        {
            base.RemovePilot(agent);

            if (GameNetwork.IsClient)
            {
                PilotAgent.AgentVisuals.SetVisible(true);
            }
        }
    }
}