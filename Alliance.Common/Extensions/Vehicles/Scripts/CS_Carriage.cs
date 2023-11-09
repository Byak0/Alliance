using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    /// <summary>
    /// Custom script to control horse-drawn carriages. 
    /// </summary>
    public class CS_Carriage : CS_Vehicle
    {
        public float BackWheelRadius = 1f;
        public float FrontWheelRadius = 1f;

        protected GameEntity PivotFrontMain;
        protected GameEntity PivotFrontLeft;
        protected GameEntity PivotFrontRight;
        protected GameEntity WheelBackLeft;
        protected GameEntity WheelBackRight;
        protected GameEntity WheelFrontLeft;
        protected GameEntity WheelFrontRight;
        protected List<GameEntity> HorsePositions = new List<GameEntity>();
        protected Dictionary<GameEntity, Agent> HorseAtPosition = new Dictionary<GameEntity, Agent>();

        protected float CurrentInversedPivotTurnRate = 0f;
        protected float CurrentPivotTurnAngleDeg = 0f;
        protected float CurrentPivotTurnAngleInRadian = 0f;
        protected float GroundOffset;
        protected float CachedHorseOccupancyRate = -1;

        private bool _isHorseOccupancyRateDirty = true;
        private bool _init = false;
        private float _timeSinceLastSync;
        private float _timeSinceLastHorseTP;

        public CS_Carriage()
        {
        }

        protected override void OnInit()
        {
            base.OnInit();

            List<GameEntity> allChildren = new List<GameEntity>();
            GameEntity.GetChildrenRecursive(ref allChildren);

            foreach (GameEntity child in allChildren)
            {
                if (child.HasTag("horse_position"))
                {
                    HorsePositions.Add(child);
                    HorseAtPosition[child] = null;
                }
                if (child.HasTag("wheel_back_left"))
                {
                    WheelBackLeft = child;
                }
                if (child.HasTag("wheel_back_right"))
                {
                    WheelBackRight = child;
                }
                if (child.HasTag("wheel_front_left"))
                {
                    WheelFrontLeft = child;
                }
                if (child.HasTag("wheel_front_right"))
                {
                    WheelFrontRight = child;
                }
                if (child.HasTag("pivot_front_main"))
                {
                    PivotFrontMain = child;
                }
                if (child.HasTag("pivot_front_left"))
                {
                    PivotFrontLeft = child;
                }
                if (child.HasTag("pivot_front_right"))
                {
                    PivotFrontRight = child;
                }
            }
            if (WheelBackLeft == null || WheelBackRight == null || WheelFrontLeft == null || WheelFrontRight == null || PivotFrontMain == null)
            {
                ValidState = false;
            }
            else
            {
                FollowsTerrainPoints = new List<GameEntity> {
                    WheelBackLeft,
                    WheelBackRight,
                    WheelFrontLeft,
                    WheelFrontRight
                };
            }

            GroundOffset = Math.Max(BackWheelRadius, FrontWheelRadius);
        }

        public void SpawnHorses()
        {
            // Get the children entities that should follow the terrain
            foreach (GameEntity child in HorsePositions)
            {
                _isHorseOccupancyRateDirty = true; // Mark the occupancy rate to be updated
                HorseAtPosition[child] = SpawnHorse(child.GetGlobalFrame().origin, GameEntity.GetGlobalFrame().rotation.f.AsVec2);
                Log($"I've spawned horse at {child.GetGlobalFrame().origin}", LogLevel.Debug);
            }
        }

        public virtual Agent SpawnHorse(Vec3 position, Vec2 direction)
        {
            Agent horse = null;

            string horseId = "mp_empire_horse_agile";
            string reinsId = "mp_imperial_riding_harness";

            ItemObject horseItem = Game.Current.ObjectManager.GetObject<ItemObject>(horseId);
            ItemObject reinsItem = Game.Current.ObjectManager.GetObject<ItemObject>(reinsId);

            // Ensure the ItemObject is a horse
            if (horseItem.IsMountable)
            {
                EquipmentElement horseEquipmentElement = new EquipmentElement(horseItem);
                EquipmentElement harnessEquipmentElement = new EquipmentElement(reinsItem);

                // Spawn the horse agent
                horse = Mission.Current.SpawnMonster(horseEquipmentElement, new EquipmentElement(), position, direction);
                horse.HealthLimit = 1000;
                horse.Health = 1000;
            }

            return horse;
        }

        protected override void OnTick(float dt)
        {
            if (!ValidState) return;

            base.OnTick(dt);

            if (!MBNetwork.IsClient) return;
        }

        public override void ServerMoveForward(bool move)
        {
            move &= GetHorseOccupancyRate() > 0;
            base.ServerMoveForward(move);
        }

        public override void ServerMoveBackward(bool move)
        {
            move &= GetHorseOccupancyRate() > 0;
            base.ServerMoveBackward(move);
        }

        public override void ServerTurnLeft(bool turn)
        {
            turn &= GetHorseOccupancyRate() > 0;
            base.ServerTurnLeft(turn);
        }

        public override void ServerTurnRight(bool turn)
        {
            turn &= GetHorseOccupancyRate() > 0;
            base.ServerTurnRight(turn);
        }

        public override void UpdateVehicleMovement(float dt)
        {
            UpdateDirectionAndSpeed(dt);

            bool moving = CurrentForwardSpeed != 0f || CurrentUpwardSpeed != 0f || CurrentTurnRate != 0f;

            // Get frames
            MatrixFrame carriageFrame = GameEntity.GetFrame();
            MatrixFrame carriageGlobalFrame = GameEntity.GetGlobalFrame();
            MatrixFrame pivotFrame = PivotFrontMain.GetFrame();
            MatrixFrame pivotGlobalFrame = PivotFrontMain.GetGlobalFrame();
            //MatrixFrame pivotLeftFrame = PivotFrontLeft.GetFrame();
            //MatrixFrame pivotRightFrame = PivotFrontRight.GetFrame();
            MatrixFrame wheelBackLeft = WheelBackLeft.GetFrame();
            MatrixFrame wheelBackRight = WheelBackRight.GetFrame();
            MatrixFrame wheelFrontLeft = WheelFrontLeft.GetFrame();
            MatrixFrame wheelFrontRight = WheelFrontRight.GetFrame();

            // Refresh pivot turn angle
            Vec3 front = pivotGlobalFrame.rotation.f;
            Vec3 back = carriageGlobalFrame.rotation.f;
            CurrentPivotTurnAngleInRadian = MathHelper.AngleBetweenTwoVectors(front, back);
            CurrentPivotTurnAngleDeg = MathHelper.ToDegrees(CurrentPivotTurnAngleInRadian);

            bool oppositeDirection = CurrentPivotTurnAngleDeg * CurrentTurnRate < 0;
            float turnRateAngleDeg = CurrentTurnRate * dt;
            float nextPivotTurnAngleDeg = CurrentPivotTurnAngleDeg + turnRateAngleDeg;
            float turnRateRad = MathHelper.ToRadian(turnRateAngleDeg);

            // Turn pivot left or right depending on turn rate
            if (CurrentTurnRate != 0f)
            {
                // Prevent turn if max angle will be reached
                if (oppositeDirection || Math.Abs(nextPivotTurnAngleDeg) <= MaxTurnAngle)
                {
                    // Rotate the pivotFrame around its own axis                    
                    pivotFrame.Rotate(turnRateRad, Vec3.Up);
                    //pivotLeftFrame.Rotate(turnRateRad, Vec3.Up);
                    //pivotRightFrame.Rotate(turnRateRad, Vec3.Up);

                    CurrentPivotTurnAngleDeg = nextPivotTurnAngleDeg;
                    //Utility.Log($"pivotFrame rotating by {turnRateRad} ({Utility.ToDegrees(turnRateRad)}), currentAngle={CurrentPivotTurnAngleDeg} (max={MaxTurnAngle})", logToAll: true);

                    // TODO refaire le calcul en prenant en compte la distance parcourue par chaque roue
                    if (CurrentForwardSpeed == 0f)
                    {
                        if (turnRateAngleDeg >= 0)
                        {
                            UpdateWheelsRotation(dt, ref wheelFrontLeft, -(100 * turnRateAngleDeg) * FrontWheelRadius, FrontWheelRadius);
                            UpdateWheelsRotation(dt, ref wheelFrontRight, 100 * turnRateAngleDeg * FrontWheelRadius, FrontWheelRadius);
                        }
                        else
                        {
                            UpdateWheelsRotation(dt, ref wheelFrontLeft, -(100 * turnRateAngleDeg) * FrontWheelRadius, FrontWheelRadius);
                            UpdateWheelsRotation(dt, ref wheelFrontRight, 100 * turnRateAngleDeg * FrontWheelRadius, FrontWheelRadius);
                        }
                    }
                }
            }

            if (CurrentForwardSpeed != 0f)
            {
                // Calculate the translation distance and rotation angle
                float desiredRotationAngleRad = MathHelper.ToRadian(CurrentPivotTurnAngleDeg);
                float translationDistance = CurrentForwardSpeed * dt + -desiredRotationAngleRad * CurrentForwardSpeed * dt / 2;

                // Update wheels and pivot rotations
                float wheelRotationMultiplier = MathF.Clamp(Math.Abs(CurrentForwardSpeed), 0.7f, 1f);
                UpdateWheelsRotation(dt, ref wheelBackLeft, wheelRotationMultiplier, BackWheelRadius);
                UpdateWheelsRotation(dt, ref wheelBackRight, wheelRotationMultiplier, BackWheelRadius);
                UpdateWheelsRotation(dt, ref wheelFrontLeft, wheelRotationMultiplier - CurrentPivotTurnAngleDeg / 100, FrontWheelRadius);
                UpdateWheelsRotation(dt, ref wheelFrontRight, wheelRotationMultiplier + CurrentPivotTurnAngleDeg / 100, FrontWheelRadius);

                if (GameNetwork.IsServer) carriageFrame.Advance(translationDistance);

                float rotationRate = desiredRotationAngleRad * CurrentForwardSpeed * dt / 2;
                float maxRate = Math.Abs(desiredRotationAngleRad);
                rotationRate = MathF.Clamp(rotationRate, -maxRate, maxRate);

                //Utility.Log($"Rotating the carriage frame by {rotationRate} ({Utility.ToDegrees(rotationRate)}deg) ({desiredRotationAngleRad}/{translationDistance}/{CurrentForwardSpeed})", logToAll: true);

                // Rotate the carriage frame around its own axis
                if (GameNetwork.IsServer) carriageFrame.Rotate(rotationRate, Vec3.Up);

                // Prevent turn if max angle will be reached
                if (Math.Abs(nextPivotTurnAngleDeg) <= MaxTurnAngle)
                {
                    // Rotate the pivot frame inversely to keep its original orientation
                    pivotFrame.Rotate(-rotationRate, Vec3.Up);
                    //pivotLeftFrame.Rotate(-rotationRate, Vec3.Up);
                    //pivotRightFrame.Rotate(-rotationRate, Vec3.Up);
                }
            }

            if (FollowTerrain && moving && GameNetwork.IsServer)
            {
                Vec3[] collisionPoints = GetCollisionPoints();

                AlignFrameWithGround(ref carriageFrame, GameEntity, collisionPoints, GroundOffset);
                AdjustPositionToTerrain(ref carriageFrame, collisionPoints, GroundOffset);
            }

            if (CurrentUpwardSpeed != 0f && GameNetwork.IsServer)
            {
                carriageFrame.Elevate(CurrentUpwardSpeed * dt);
            }

            // Set local frames
            WheelBackRight.SetFrame(ref wheelBackRight);
            WheelBackLeft.SetFrame(ref wheelBackLeft);
            WheelFrontRight.SetFrame(ref wheelFrontRight);
            WheelFrontLeft.SetFrame(ref wheelFrontLeft);
            PivotFrontMain.SetFrame(ref pivotFrame);
            //PivotFrontLeft.SetFrame(ref pivotLeftFrame);
            //PivotFrontRight.SetFrame(ref pivotRightFrame);

            if (GameNetwork.IsServer)
            {
                // Sync global frame
                _timeSinceLastSync += dt;
                if (_timeSinceLastSync > 0.015f)
                {
                    _timeSinceLastSync = 0f;
                    SetFrameSynched(ref carriageFrame);
                }

                if (!_init && PilotAgent != null)
                {
                    SpawnHorses();
                    _init = true;
                }

                MoveHorses(dt);

                _lastAgentSync += dt;
                if (_lastAgentSync > 1f)
                {
                    MovePilotAndPassengers();
                    _lastAgentSync = 0f;
                }
            }
        }

        public override Vec3[] GetCollisionPoints()
        {
            Vec3[] collisionPoints = new Vec3[4];

            // Assuming points are ordered in clockwise or counterclockwise direction
            collisionPoints[0] = WheelBackLeft.GetGlobalFrame().origin;
            collisionPoints[1] = WheelFrontLeft.GetGlobalFrame().origin;
            collisionPoints[2] = WheelFrontRight.GetGlobalFrame().origin;
            collisionPoints[3] = WheelBackRight.GetGlobalFrame().origin;

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

        private void MoveHorses(float dt)
        {
            foreach (GameEntity horsePos in HorsePositions)
            {
                if (HorseAtPosition[horsePos] != null)
                {
                    if (HorseAtPosition[horsePos].Health <= 0)
                    {
                        HorseAtPosition[horsePos] = null;
                        _isHorseOccupancyRateDirty = true;
                        continue;
                    }
                    MatrixFrame horseTargetFrame = horsePos.GetGlobalFrame();
                    MatrixFrame horseTPFrame = horsePos.GetGlobalFrame();
                    horseTargetFrame.Advance(10f + CurrentForwardSpeed * 5f);
                    horseTPFrame.Advance(CurrentForwardSpeed / 10f);
                    WorldPosition target = horseTargetFrame.origin.ToWorldPosition(Scene);
                    // Set scripted target position with variable distance to trick the horse into making movement animations
                    HorseAtPosition[horsePos].SetScriptedPosition(ref target, false);
                    _timeSinceLastHorseTP += dt;
                    if (_timeSinceLastHorseTP > 0.015f)
                    {
                        _timeSinceLastHorseTP = 0f;
                        // Teleport the horse at the exact position we want him to be
                        HorseAtPosition[horsePos].TeleportToPosition(horseTPFrame.origin);
                    }
                    // Make the horse look in the right direction
                    HorseAtPosition[horsePos].SetMovementDirection(PivotFrontMain.GetGlobalFrame().rotation.f.AsVec2);
                }
            }
        }

        public float GetHorseOccupancyRate()
        {
            if (_isHorseOccupancyRateDirty)
            {
                int occupiedHorsePositions = HorseAtPosition.Values.Count(agent => agent != null && agent.Health > 0);
                CachedHorseOccupancyRate = HorsePositions.Count > 0 ? (float)occupiedHorsePositions / HorsePositions.Count : 0f;
                _isHorseOccupancyRateDirty = false;
            }

            return CachedHorseOccupancyRate;
        }

        public virtual void MoveHorse(Agent horseAgent, Vec2 direction)
        {
            // Set the movement input vector
            horseAgent.MovementInputVector = direction;

            // Set the movement control flags based on the direction
            if (direction.x > 0f)
            {
                horseAgent.MovementFlags = Agent.MovementControlFlag.Forward;
            }
            else if (direction.x < 0f)
            {
                horseAgent.MovementFlags = Agent.MovementControlFlag.Backward;
            }
            else if (direction.y > 0f)
            {
                horseAgent.MovementFlags = Agent.MovementControlFlag.TurnRight;
            }
            else if (direction.y < 0f)
            {
                horseAgent.MovementFlags = Agent.MovementControlFlag.TurnLeft;
            }
            else
            {
                // If no direction is specified, stop the agent
                horseAgent.MovementFlags = 0U;
                horseAgent.MovementInputVector = Vec2.Zero;
            }
        }
    }
}
