using Alliance.Common.Extensions.CustomScripts.Scripts;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    /// <summary>
    /// Basic custom script to control vehicles. 
    /// Can contain a pilot and passengers.
    /// Provides basic movement.
    /// </summary>
    public class CS_Vehicle : UsableMachine
    {
        public string Description = "Vehicle";
        public string PilotInteraction = "Drive";
        public string PassengerInteraction = "Sit";

        public float MaxForwardSpeed = 5f;
        public float TimeToAttainMaxForwardSpeed = 5f;
        public float MaxBackwardSpeed = 2f;
        public float TimeToAttainMaxBackwardSpeed = 2f;
        public float MaxUpwardSpeed = 1f;
        public float TimeToAttainMaxUpwardSpeed = 1f;
        public float MaxDownwardSpeed = 1f;
        public float TimeToAttainMaxDownwardSpeed = 1f;
        public float MaxTurnAngle = 60f;
        public float TimeToAttainMaxTurn = 2f;
        public float DecelerationRate = 0.5f;
        public float TurnSlowdownRate = 10f;

        public bool CanFly = true;
        public bool FollowTerrain = true;

        public bool ValidState { get; protected set; } = true;
        public float ForwardAccelerationRate { get; protected set; } = 0f;
        public float BackwardAccelerationRate { get; protected set; } = 0f;
        public float UpwardAccelerationRate { get; protected set; } = 0f;
        public float DownwardAccelerationRate { get; protected set; } = 0f;
        public float CurrentForwardSpeed { get; protected set; } = 0f;
        public float CurrentUpwardSpeed { get; protected set; } = 0f;
        public float TurnRate { get; protected set; } = 0f;
        public float CurrentTurnRate { get; protected set; } = 0f;
        public float FlyElevation { get; protected set; } = 0f;

        protected bool _moveForward = false;
        protected bool _moveBackward = false;
        protected bool _moveUpward = false;
        protected bool _moveDownward = false;
        protected bool _turnRight = false;
        protected bool _turnLeft = false;
        protected const float MaxFlatTerrainAngle = 0.5f;

        protected List<GameEntity> FollowsTerrainPoints = new List<GameEntity>();

        [EditableScriptComponentVariable(false)]
        public Action<CS_Vehicle, Agent> OnUseEvent;

        [EditableScriptComponentVariable(false)]
        public Action<CS_Vehicle, Agent> OnUseStoppedEvent;

        protected float _lastAgentSync;

        public CS_Vehicle()
        {
            // Calculate the acceleration rate needed to reach MaxSpeed in TimeToAttainMax seconds
            ForwardAccelerationRate = MaxForwardSpeed / TimeToAttainMaxForwardSpeed;
            BackwardAccelerationRate = MaxBackwardSpeed / TimeToAttainMaxBackwardSpeed;
            UpwardAccelerationRate = MaxUpwardSpeed / TimeToAttainMaxUpwardSpeed;
            DownwardAccelerationRate = MaxDownwardSpeed / TimeToAttainMaxDownwardSpeed;
            TurnRate = MaxTurnAngle / TimeToAttainMaxTurn;
        }

        public override UsableMachineAIBase CreateAIBehaviorObject()
        {
            return new CS_VehicleAI(this);
        }

        protected override void OnInit()
        {
            base.OnInit();

            List<GameEntity> allChildren = new List<GameEntity>();
            GameEntity.GetChildrenRecursive(ref allChildren);

            foreach (StandingPoint sp in StandingPoints)
            {
                if (sp is CS_StandingPoint cs_sp && sp.GameEntity.HasTag("pilot"))
                {
                    typeof(UsableMachine).GetProperty("PilotStandingPoint").SetValue(this, cs_sp);
                }
            }

            // Get the children entities that should follow the terrain
            foreach (GameEntity child in allChildren)
            {
                if (child.HasTag("FollowsTerrain"))
                {
                    FollowsTerrainPoints.Add(child);
                }
            }

            if (PilotStandingPoint is CS_StandingPoint standingPoint)
            {
                standingPoint.OnUseEvent += UpdatePilot;
                standingPoint.OnUseStoppedEvent += RemovePilot;
            }
            //List<CS_StandingPoint> standingPoints = GameEntity.CollectObjects<CS_StandingPoint>();
            //PassengersStandingPoints = new List<CS_StandingPoint>();
            //foreach (CS_StandingPoint point in standingPoints)
            //{
            //    if (point.GameEntity.HasTag(PilotStandingPointTag))
            //    {
            //        PilotStandingPoint = point;                    
            //    } 
            //    else
            //    {
            //        PassengersStandingPoints.Add(point);
            //    }
            //}
        }

        public virtual void UpdatePilot(Agent agent)
        {
            OnUseEvent?.Invoke(this, agent);
        }

        public virtual void RemovePilot(Agent agent)
        {
            OnUseStoppedEvent?.Invoke(this, agent);
        }

        public override TickRequirement GetTickRequirement()
        {
            return TickRequirement.Tick | base.GetTickRequirement();
        }

        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            if (!ValidState) return;

            if (GameNetwork.IsClient) CheckPilotInput();
            //if (GameNetwork.IsServer) UpdateVehicleMovement(dt);
            UpdateVehicleMovement(dt);
        }

        public virtual void UpdateVehicleMovement(float dt)
        {
            UpdateDirectionAndSpeed(dt);

            MatrixFrame frame = GameEntity.GetFrame();
            UpdatePositionAndRotation(dt, ref frame);
            SyncFrame(frame);
        }

        public virtual MatrixFrame SyncFrame(MatrixFrame frame)
        {
            if (frame.origin != GameEntity.GetFrame().origin || frame.rotation != GameEntity.GetFrame().rotation)
            {
                SetFrameSynched(ref frame, GameNetwork.IsClient);
            }

            return frame;
        }

        public virtual void UpdatePositionAndRotation(float dt, ref MatrixFrame frame)
        {
            bool moving = CurrentForwardSpeed != 0f || CurrentUpwardSpeed != 0f || CurrentTurnRate != 0f;

            if (CurrentTurnRate != 0f)
            {
                // Rotate the vehicle's frame for yaw
                frame.Rotate((float)(CurrentTurnRate * (Math.PI / 180) * dt), Vec3.Up);
            }

            if (CurrentForwardSpeed != 0f)
            {
                frame.Advance(CurrentForwardSpeed * dt);
            }

            if (CurrentUpwardSpeed != 0f)
            {
                float elevation = CurrentUpwardSpeed * dt;
                frame.Elevate(elevation);
                FlyElevation += elevation;
                if (FlyElevation < 0) FlyElevation = 0;
            }

            if (FollowTerrain && moving)
            {
                Vec3[] collisionPoints = GetCollisionPoints();

                // Adjust the rotation of the vehicle to align with average terrain height at collision points
                AlignFrameWithGround(ref frame, GameEntity, collisionPoints);

                // Adjust the position of the vehicle to prevent wheels from going under the terrain
                AdjustPositionToTerrain(ref frame, collisionPoints);
            }

            _lastAgentSync += dt;
            if (_lastAgentSync > 1f)
            {
                MovePilotAndPassengers();
                _lastAgentSync = 0f;
            }
        }

        public virtual void AlignFrameWithGround(ref MatrixFrame frame, GameEntity gameEntity, Vec3[] collisionPoints, float groundOffset = 0f)
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

        public virtual void AdjustPositionToTerrain(ref MatrixFrame frame, Vec3[] collisionPoints, float groundOffset = 0f)
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
                    heightAdjustmentUnder = -Math.Min(heightAdjustmentUnder, cpHeight);
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

            float adjustment = heightAdjustmentUnder - heightAdjustmentOver;

            if (Math.Abs(adjustment) > 0.05f)
            {
                frame.Elevate(adjustment);
            }
        }

        public virtual Vec3[] GetCollisionPoints()
        {
            Vec3[] collisionPoints = new Vec3[4];

            // Assuming points are ordered in clockwise or counterclockwise direction
            collisionPoints[0] = FollowsTerrainPoints[0].GetGlobalFrame().origin;
            collisionPoints[1] = FollowsTerrainPoints[1].GetGlobalFrame().origin;
            collisionPoints[2] = FollowsTerrainPoints[2].GetGlobalFrame().origin;
            collisionPoints[3] = FollowsTerrainPoints[3].GetGlobalFrame().origin;

            return collisionPoints;
        }

        public virtual void UpdateDirectionAndSpeed(float dt)
        {
            if (PilotAgent == null)
            {
                Decelerate(dt);
                DecelerateVertical(dt);
                SlowdownTurn(dt);
                return;
            }

            if (_moveForward) MoveForward(dt);
            else if (_moveBackward) MoveBackward(dt);
            else Decelerate(dt);

            if (_moveUpward) MoveUpward(dt);
            else if (_moveDownward) MoveDownward(dt);
            else DecelerateVertical(dt);

            if (_turnLeft) TurnLeft(dt);
            else if (_turnRight) TurnRight(dt);
            else SlowdownTurn(dt);
        }

        public virtual void CheckPilotInput()
        {
            if (Agent.Main != null && PilotAgent == Agent.Main)
            {
                if (Mission.Current.InputManager.IsKeyPressed(InputKey.W)) RequestMoveForward(true, true);
                else if (Mission.Current.InputManager.IsKeyReleased(InputKey.W)) RequestMoveForward(false, true);

                if (Mission.Current.InputManager.IsKeyPressed(InputKey.S)) RequestMoveBackward(true, true);
                else if (Mission.Current.InputManager.IsKeyReleased(InputKey.S)) RequestMoveBackward(false, true);

                if (Mission.Current.InputManager.IsKeyPressed(InputKey.A)) RequestTurnLeft(true, true);
                else if (Mission.Current.InputManager.IsKeyReleased(InputKey.A)) RequestTurnLeft(false, true);

                if (Mission.Current.InputManager.IsKeyPressed(InputKey.D)) RequestTurnRight(true, true);
                else if (Mission.Current.InputManager.IsKeyReleased(InputKey.D)) RequestTurnRight(false, true);

                if (Mission.Current.InputManager.IsKeyPressed(InputKey.Space)) RequestMoveUpward(true, true);
                else if (Mission.Current.InputManager.IsKeyReleased(InputKey.Space)) RequestMoveUpward(false, true);

                if (Mission.Current.InputManager.IsKeyPressed(InputKey.LeftControl)) RequestMoveDownward(true, true);
                else if (Mission.Current.InputManager.IsKeyReleased(InputKey.LeftControl)) RequestMoveDownward(false, true);
            }
        }

        public virtual void RequestMoveForward(bool move, bool sync = false)
        {
            if (_moveForward != move)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestForward(this, move));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    _moveForward = move;
                }
            }
        }

        public virtual void ServerMoveForward(bool move)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncForward(this, move));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            _moveForward = move;
        }

        public virtual void RequestMoveBackward(bool move, bool sync = false)
        {
            if (_moveBackward != move)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestBackward(this, move));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    _moveBackward = move;
                }
            }
        }

        public virtual void ServerMoveBackward(bool move)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncBackward(this, move));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            _moveBackward = move;
        }

        public virtual void RequestMoveUpward(bool move, bool sync = false)
        {
            if (_moveUpward != move)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestUpward(this, move));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    _moveUpward = move;
                }
            }
        }

        public virtual void ServerMoveUpward(bool move)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncUpward(this, move));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            _moveUpward = move;
        }

        public virtual void RequestMoveDownward(bool move, bool sync = false)
        {
            if (_moveDownward != move)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestDownward(this, move));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    _moveDownward = move;
                }
            }
        }

        public virtual void ServerMoveDownward(bool move)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncDownward(this, move));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            _moveDownward = move;
        }

        public virtual void RequestTurnLeft(bool turn, bool sync = false)
        {
            if (_turnLeft != turn)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestTurnLeft(this, turn));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    _turnLeft = turn;
                }
            }
        }

        public virtual void ServerTurnLeft(bool turn)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncTurnLeft(this, turn));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            _turnLeft = turn;
        }

        public virtual void RequestTurnRight(bool turn, bool sync = false)
        {
            if (_turnRight != turn)
            {
                if (sync)
                {
                    GameNetwork.BeginModuleEventAsClient();
                    GameNetwork.WriteMessage(new CS_VehicleRequestTurnRight(this, turn));
                    GameNetwork.EndModuleEventAsClient();
                }
                else
                {
                    _turnRight = turn;
                }
            }
        }

        public virtual void ServerTurnRight(bool turn)
        {
            GameNetwork.BeginBroadcastModuleEvent();
            GameNetwork.WriteMessage(new CS_VehicleSyncTurnRight(this, turn));
            GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
            _turnRight = turn;
        }

        public virtual void MoveForward(float dt)
        {
            // Normal forward acceleration
            if (CurrentForwardSpeed >= 0) CurrentForwardSpeed += ForwardAccelerationRate * dt;
            // Braking from backward move
            else CurrentForwardSpeed += ForwardAccelerationRate * dt * 3;

            CurrentForwardSpeed = MathF.Clamp(CurrentForwardSpeed, -MaxBackwardSpeed, MaxForwardSpeed);
        }

        public virtual void MoveBackward(float dt)
        {
            // Normal backward acceleration
            if (CurrentForwardSpeed <= 0) CurrentForwardSpeed -= BackwardAccelerationRate * dt;
            // Braking from forward move
            else CurrentForwardSpeed -= ForwardAccelerationRate * dt * 5;
            CurrentForwardSpeed = MathF.Clamp(CurrentForwardSpeed, -MaxBackwardSpeed, MaxForwardSpeed);
        }

        public virtual void MoveUpward(float dt)
        {
            CurrentUpwardSpeed += UpwardAccelerationRate * dt;
            CurrentUpwardSpeed = MathF.Clamp(CurrentUpwardSpeed, -MaxDownwardSpeed, MaxUpwardSpeed);
        }

        public virtual void MoveDownward(float dt)
        {
            CurrentUpwardSpeed -= DownwardAccelerationRate * dt;
            CurrentUpwardSpeed = MathF.Clamp(CurrentUpwardSpeed, -MaxDownwardSpeed, MaxUpwardSpeed);
        }

        public virtual void TurnRight(float dt)
        {
            CurrentTurnRate -= TurnRate * dt;
            CurrentTurnRate = MathF.Clamp(CurrentTurnRate, -MaxTurnAngle, MaxTurnAngle);
        }

        public virtual void TurnLeft(float dt)
        {
            CurrentTurnRate += TurnRate * dt;
            CurrentTurnRate = MathF.Clamp(CurrentTurnRate, -MaxTurnAngle, MaxTurnAngle);
        }

        public virtual void Decelerate(float dt)
        {
            float decelerationRateToUse = DecelerationRate;

            // When there is no pilot, use a stronger deceleration rate
            if (PilotAgent == null)
            {
                decelerationRateToUse *= 4; // Change this factor as needed
            }

            if (Math.Abs(CurrentForwardSpeed) < MaxForwardSpeed * 0.1 || PilotAgent == null)
            {
                if (CurrentForwardSpeed > 0)
                {
                    CurrentForwardSpeed -= decelerationRateToUse * dt;
                    if (CurrentForwardSpeed < 0) CurrentForwardSpeed = 0;
                }
                else if (CurrentForwardSpeed < 0)
                {
                    CurrentForwardSpeed += decelerationRateToUse * dt;
                    if (CurrentForwardSpeed > 0) CurrentForwardSpeed = 0;
                }
            }
        }

        public virtual void DecelerateVertical(float dt)
        {
            if (CurrentUpwardSpeed > 0)
            {
                CurrentUpwardSpeed -= DecelerationRate * dt;
                if (CurrentUpwardSpeed < 0) CurrentUpwardSpeed = 0;
            }
            else if (CurrentUpwardSpeed < 0)
            {
                CurrentUpwardSpeed += DecelerationRate * dt;
                if (CurrentUpwardSpeed > 0) CurrentUpwardSpeed = 0;
            }
        }

        public virtual void SlowdownTurn(float dt)
        {
            if (CurrentTurnRate > 0)
            {
                CurrentTurnRate -= TurnSlowdownRate * dt;
                if (CurrentTurnRate < 0) CurrentTurnRate = 0;
            }
            else if (CurrentTurnRate < 0)
            {
                CurrentTurnRate += TurnSlowdownRate * dt;
                if (CurrentTurnRate > 0) CurrentTurnRate = 0;
            }
        }

        public virtual void MovePilotAndPassengers()
        {
            //foreach (StandingPoint standingPoint in StandingPoints)
            //{
            //    if (standingPoint.HasUser)
            //    {
            //        Utility.Log($"Teleporting {standingPoint.UserAgent.Name} to {standingPoint.GameEntity.GlobalPosition}", logToAll: true);
            //        standingPoint.UserAgent.TeleportToPosition(standingPoint.GameEntity.GlobalPosition);
            //    }
            //}
        }

        public virtual void SyncVehicle()
        {
        }

        public override void Disable()
        {
            base.Disable();
        }

        public override bool ReadFromNetwork()
        {
            bool flag = base.ReadFromNetwork();
            return flag;
        }

        public override void WriteToNetwork()
        {
            base.WriteToNetwork();
        }

        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            if (usableGameObject.GameEntity.HasTag("Pilot"))
            {
                return new TextObject(PilotInteraction, null);
            }
            else
            {
                return new TextObject(PassengerInteraction, null);
            }
        }

        public override string GetDescriptionText(GameEntity gameEntity = null)
        {
            return Description;
        }
    }

    public static class Vec3Extensions
    {
        public static Vec3 ProjectOnUnitVector(this Vec3 vector, Vec3 unitVector)
        {
            return unitVector * Vec3.DotProduct(vector, unitVector);
        }

        public static Vec3 ProjectOnPlane(this Vec3 vector, Vec3 planeNormal)
        {
            return vector - planeNormal * Vec3.DotProduct(vector, planeNormal);
        }
    }
}
