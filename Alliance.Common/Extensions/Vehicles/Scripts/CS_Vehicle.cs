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
		public bool CollisionEnabled = true;

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
		protected bool ForceDecelerate = false;

		protected List<WeakGameEntity> FollowsTerrainPoints = new List<WeakGameEntity>();

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

			List<WeakGameEntity> allChildren = new List<WeakGameEntity>();
			GameEntity.GetChildrenRecursive(ref allChildren);

			foreach (StandingPoint sp in StandingPoints)
			{
				if (sp is CS_StandingPoint cs_sp && sp.GameEntity.HasTag("pilot"))
				{
					typeof(UsableMachine).GetProperty("PilotStandingPoint").SetValue(this, cs_sp);
				}
			}

			if (PilotStandingPoint is CS_StandingPoint standingPoint)
			{
				standingPoint.OnUseEvent += UpdatePilot;
				standingPoint.OnUseStoppedEvent += RemovePilot;
			}

			// Get the children entities that should follow the terrain
			foreach (WeakGameEntity child in allChildren)
			{
				if (child.HasTag("FollowsTerrain"))
				{
					FollowsTerrainPoints.Add(child);
				}
			}
		}

		public virtual void UpdatePilot(Agent agent)
		{
			ResetVehicleForPilot();
			OnUseEvent?.Invoke(this, agent);
		}

		public virtual void RemovePilot(Agent agent)
		{
			OnUseStoppedEvent?.Invoke(this, agent);
		}

		protected virtual void ResetVehicleForPilot()
		{
			if (GameNetwork.IsClient && !GameNetwork.IsServerOrRecorder)
			{
				return;
			}

			MatrixFrame frame = GameEntity.GetFrame();
			if (!IsUpsideDown(frame))
			{
				return;
			}

			FlipVehicle(ref frame);
			ResetMovement(frame);
			SyncFrame(frame);
		}

		protected virtual bool IsUpsideDown(MatrixFrame frame)
		{
			return Vec3.DotProduct(frame.rotation.u, Vec3.Up) < .25f;
		}

		protected virtual void FlipVehicle(ref MatrixFrame frame)
		{
			Vec3[] collisionPoints = GetCollisionPoints();
			if (collisionPoints.Length >= 3)
			{
				AlignFrameWithGround(ref frame, GameEntity, collisionPoints);
				AdjustPositionToTerrain(ref frame, collisionPoints);
				return;
			}

			AlignFrameUpright(ref frame);
		}

		protected virtual void AlignFrameUpright(ref MatrixFrame frame)
		{
			Vec3 forward = frame.rotation.f - Vec3.Up * Vec3.DotProduct(frame.rotation.f, Vec3.Up);
			if (forward.LengthSquared < 0.001f)
			{
				forward = frame.rotation.s - Vec3.Up * Vec3.DotProduct(frame.rotation.s, Vec3.Up);
			}

			if (forward.LengthSquared < 0.001f)
			{
				forward = Vec3.Forward;
			}

			forward.Normalize();

			Vec3 side = Vec3.CrossProduct(forward, Vec3.Up);
			if (side.LengthSquared < 0.001f)
			{
				side = new Vec3(1f, 0f, 0f);
			}
			side.Normalize();

			frame.rotation.f = forward * frame.rotation.f.Length;
			frame.rotation.s = side * frame.rotation.s.Length;
			frame.rotation.u = Vec3.Up * frame.rotation.u.Length;
		}

		protected virtual void ResetMovement(MatrixFrame frame)
		{
			_moveForward = false;
			_moveBackward = false;
			_moveUpward = false;
			_moveDownward = false;
			_turnRight = false;
			_turnLeft = false;
			ForceDecelerate = false;
			CurrentForwardSpeed = 0f;
			CurrentUpwardSpeed = 0f;
			CurrentTurnRate = 0f;
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
				// Check for obstacle before moving
				CollisionResult collision;
				if (CollisionEnabled && CheckMultiPointCollision(frame, Math.Abs(CurrentForwardSpeed * dt) + 0.5f, out collision))
				{
					OnCollision(ref frame, collision);
				}
				else
				{
					frame.Advance(CurrentForwardSpeed * dt);
				}
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
		}

		public virtual void AlignFrameWithGround(ref MatrixFrame frame, WeakGameEntity gameEntity, Vec3[] collisionPoints, float groundOffset = 0f)
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
					collisionPointPosition.z = FlyElevation - groundOffset + GetGroundHeight(collisionPointPosition);

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
				float terrainHeight = GetGroundHeight(cp);
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

		public virtual float GetGroundHeight(Vec3 cp)
		{
			return Scene.GetGroundHeightAtPosition(cp, BodyFlags.CommonCollisionExcludeFlags);
		}

		public virtual Vec3[] GetCollisionPoints()
		{
			Vec3[] collisionPoints = new Vec3[FollowsTerrainPoints.Count];
			for (int i = 0; i < FollowsTerrainPoints.Count; i++)
			{
				collisionPoints[i] = FollowsTerrainPoints[i].GetGlobalFrame().origin;
			}

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
					GameNetwork.WriteMessage(new CS_VehicleRequestForward(Id, move));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncForward(Id, move));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			_moveForward = move;
		}

		/// <summary>
		/// Broadcast the current forward speed to all clients (e.g. after collision).
		/// </summary>
		public virtual void ServerSyncSpeed()
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CS_VehicleSyncSpeed(Id, CurrentForwardSpeed));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}

		/// <summary>
		/// Set the forward speed directly (used by client when receiving speed sync).
		/// </summary>
		public virtual void SetForwardSpeed(float speed)
		{
			CurrentForwardSpeed = speed;
		}

		public virtual void RequestMoveBackward(bool move, bool sync = false)
		{
			if (_moveBackward != move)
			{
				if (sync)
				{
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new CS_VehicleRequestBackward(Id, move));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncBackward(Id, move));
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
					GameNetwork.WriteMessage(new CS_VehicleRequestUpward(Id, move));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncUpward(Id, move));
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
					GameNetwork.WriteMessage(new CS_VehicleRequestDownward(Id, move));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncDownward(Id, move));
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
					GameNetwork.WriteMessage(new CS_VehicleRequestTurnLeft(Id, turn));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncTurnLeft(Id, turn));
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
					GameNetwork.WriteMessage(new CS_VehicleRequestTurnRight(Id, turn));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncTurnRight(Id, turn));
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

			if ((Math.Abs(CurrentForwardSpeed) < MaxForwardSpeed * 0.1 || PilotAgent == null) || ForceDecelerate)
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
		}

		public virtual void SyncVehicle()
		{
		}

		public override void Disable()
		{
			base.Disable();
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

		public override TextObject GetDescriptionText(WeakGameEntity gameEntity)
		{
			return new TextObject(Description);
		}

		#region Collision Detection

		/// <summary>
		/// Result of a collision check containing hit info.
		/// </summary>
		public struct CollisionResult
		{
			public bool HasCollision;
			public Vec3 CollisionPoint;
			public float Distance;
			public Vec3 ImpactDirection;
			public Vec3 CollisionNormal;
		}

		/// <summary>
		/// Returns collision check points based on vehicle frame. 
		/// Override to customize per vehicle type.
		/// </summary>
		protected virtual Vec3[] GetCollisionCheckPoints(MatrixFrame vehicleFrame)
		{
			// Default: front and back center points
			return new Vec3[]
			{
				vehicleFrame.origin + vehicleFrame.rotation.f * 2.0f + vehicleFrame.rotation.u * 0.5f,
				vehicleFrame.origin - vehicleFrame.rotation.f * 1.0f + vehicleFrame.rotation.u * 0.5f,
			};
		}

		/// <summary>
		/// Estimates the surface normal at a collision point by sampling surrounding points.
		/// Uses 4-point cross pattern to calculate gradient via finite difference method.
		/// </summary>
		protected Vec3 EstimateNormalAtPoint(Vec3 hitPoint, Vec3 rayDirection)
		{
			float sampleRadius = 0.2f;
			float searchDepth = 0.5f;

			Vec3 up = Vec3.Up;
			Vec3 forward = rayDirection - up * Vec3.DotProduct(rayDirection, up);
			if (forward.LengthSquared < 0.001f) forward = Vec3.Forward;
			forward.Normalize();

			Vec3 right = Vec3.CrossProduct(up, forward);
			right.Normalize();

			Vec3[] sampleOffsets = new Vec3[4]
			{
				forward * sampleRadius,
				-forward * sampleRadius,
				right * sampleRadius,
				-right * sampleRadius
			};

			Vec3[] sampleHeights = new Vec3[4];
			int validSamples = 0;

			for (int i = 0; i < 4; i++)
			{
				Vec3 sampleCenter = hitPoint + sampleOffsets[i];
				Vec3 rayStart = sampleCenter + up * (searchDepth * 0.5f);
				Vec3 rayEnd = sampleCenter - up * (searchDepth * 0.5f);

				float hitDist;
				bool hit = Mission.Current.Scene.RayCastForClosestEntityOrTerrain(
					rayStart, rayEnd, out hitDist, 0.01f, BodyFlags.CommonCollisionExcludeFlags);

				if (hit)
				{
					Vec3 sampleHit = rayStart + (rayEnd - rayStart).NormalizedCopy() * hitDist;
					sampleHeights[i] = sampleHit;
					validSamples++;
				}
				else
				{
					sampleHeights[i] = hitPoint;
				}
			}

			if (validSamples < 3)
			{
				return Vec3.Up;
			}

			Vec3 dx = sampleHeights[2] - sampleHeights[3];
			Vec3 dy = sampleHeights[0] - sampleHeights[1];

			Vec3 estimatedNormal = Vec3.CrossProduct(dy, dx);
			if (estimatedNormal.LengthSquared < 0.001f)
			{
				return Vec3.Up;
			}

			estimatedNormal.Normalize();
			if (Vec3.DotProduct(estimatedNormal, Vec3.Up) < 0)
			{
				estimatedNormal = -estimatedNormal;
			}

			return estimatedNormal;
		}

		/// <summary>
		/// Checks for collisions from multiple points in the movement direction.
		/// </summary>
		protected bool CheckMultiPointCollision(MatrixFrame vehicleFrame, float moveDistance, out CollisionResult result)
		{
			result = default;
			float minDistance = float.MaxValue;

			// Check if visibility exclusion is enabled and disable it temporarily
			bool wasVisibilityExcluded = GameEntity.GetVisibilityExcludeParents();
			if (wasVisibilityExcluded)
			{
				GameEntity.SetVisibilityExcludeParents(false);
			}

			Vec3[] checkPoints = GetCollisionCheckPoints(vehicleFrame);
			Vec3 moveDirection = vehicleFrame.rotation.f * Math.Sign(CurrentForwardSpeed);
			float checkDistance = moveDistance + 2.0f;

			for (int i = 0; i < checkPoints.Length; i++)
			{
				Vec3 checkPoint = checkPoints[i];
				Vec3 targetPoint = checkPoint + moveDirection * checkDistance;

				float collisionDistance;
				Vec3 collisionPoint;
				WeakGameEntity hitEntity;

				bool hit = Scene.RayCastForClosestEntityOrTerrain(
					checkPoint,
					targetPoint,
					out collisionDistance,
					out collisionPoint,
					out hitEntity,
					0.01f,
					BodyFlags.CommonCollisionExcludeFlags
				);

				uint arrowColor = 0xFFFF0000;

				if (!hit)
				{
					continue;
				}

				bool isSelf = hitEntity != null && IsPartOfThisVehicle(hitEntity);
				float heightDifference = collisionPoint.z - checkPoint.z;
				bool isTerrain = heightDifference < -0.2f;

				if (isSelf || isTerrain)
				{
					continue;
				}

				// Valid obstacle found
				if (collisionDistance < minDistance && collisionDistance < checkDistance * 0.8f)
				{
					minDistance = collisionDistance;
					Vec3 rayDirection = (collisionPoint - checkPoint).NormalizedCopy();
					Vec3 estimatedNormal = EstimateNormalAtPoint(collisionPoint, rayDirection);

					Vec3 impactDir = (vehicleFrame.origin - collisionPoint).NormalizedCopy();
					impactDir.z = 0;
					impactDir = impactDir.NormalizedCopy();

					result = new CollisionResult
					{
						HasCollision = true,
						CollisionPoint = collisionPoint,
						Distance = collisionDistance,
						ImpactDirection = impactDir,
						CollisionNormal = estimatedNormal
					};
				}
			}

			// Re-enable visibility exclusion if it was originally enabled
			if (wasVisibilityExcluded)
			{
				GameEntity.SetVisibilityExcludeParents(true);
			}

			return result.HasCollision;
		}

		/// <summary>
		/// Checks if entity is part of this vehicle hierarchy.
		/// </summary>
		protected bool IsPartOfThisVehicle(WeakGameEntity entity)
		{
			if (entity == null || GameEntity == null) return false;

			WeakGameEntity current = entity;
			while (current != null)
			{
				if (current == GameEntity) return true;
				current = current.Parent;
			}

			return false;
		}

		/// <summary>
		/// Called when a collision is detected. Override to customize response.
		/// Default behavior: simple stop and small pushback.
		/// </summary>
		protected virtual void OnCollision(ref MatrixFrame frame, CollisionResult collision)
		{
			frame.origin += collision.ImpactDirection * 0.1f;
			CurrentForwardSpeed = 0f;
			if (GameNetwork.IsServer) ServerSyncSpeed();
		}

		#endregion
	}
}
