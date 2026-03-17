using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
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

		protected WeakGameEntity PivotSteeringWheel;
		protected WeakGameEntity PivotFrontLeft;
		protected WeakGameEntity PivotFrontRight;
		protected WeakGameEntity WheelFrontLeft;
		protected WeakGameEntity WheelFrontRight;
		protected WeakGameEntity WheelsBack;
		protected WeakGameEntity CountOil;
		protected WeakGameEntity CountSpeed;
		protected WeakGameEntity CountRPM;
		protected WeakGameEntity CountVacuum;
		protected WeakGameEntity CountPressure;
		protected WeakGameEntity LightLeft;
		protected WeakGameEntity LightRight;
		protected List<WeakGameEntity> CollisionPoints;

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

			List<WeakGameEntity> allChildren = new List<WeakGameEntity>();
			GameEntity.GetChildrenRecursive(ref allChildren);
			CollisionPoints = new List<WeakGameEntity>();

			foreach (WeakGameEntity child in allChildren)
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
				FollowsTerrainPoints = new List<WeakGameEntity> {
					WheelsBack,
					WheelFrontLeft,
					WheelFrontRight
				};
			}

			GroundOffset = 0;
		}

		public void RequestLight(bool lightOn, bool sync = false)
		{
			if (LightOn != lightOn)
			{
				if (sync)
				{
					GameNetwork.BeginModuleEventAsClient();
					GameNetwork.WriteMessage(new CS_VehicleRequestLight(Id, lightOn));
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
			GameNetwork.WriteMessage(new CS_VehicleSyncLight(Id, lightOn));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			LightOn = lightOn;
		}

		public void RequestHonk(bool sync = false)
		{
			if (sync)
			{
				GameNetwork.BeginModuleEventAsClient();
				GameNetwork.WriteMessage(new CS_VehicleRequestHonk(Id));
				GameNetwork.EndModuleEventAsClient();
			}
			else
			{
				// Play honk sound
				Vec3 soundPosition = GameEntity.GetFrame().origin;
				AudioPlayer.Instance.Play(AudioPlayer.Instance.GetAudioId("Native/Alert/Car horn.mp3"), 1f, true, 100, soundPosition);
			}
		}

		public virtual void ServerSyncHonk()
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CS_VehicleSyncHonk(Id));
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

		private bool CheckMultiPointCollision(MatrixFrame carFrame, float moveDistance, out Vec3 nearestCollision)
		{
			nearestCollision = Vec3.Zero;
			float minDistance = float.MaxValue;
			bool hasAnyCollision = false;

			Vec3[] collisionCheckPoints = new Vec3[]
			{
				carFrame.origin + carFrame.rotation.f * 2.0f + carFrame.rotation.u * 0.5f, // Avant centre
				carFrame.origin + carFrame.rotation.f * 2.0f + carFrame.rotation.s * 1.0f + carFrame.rotation.u * 0.5f, // Avant droit
				carFrame.origin + carFrame.rotation.f * 2.0f - carFrame.rotation.s * 1.0f + carFrame.rotation.u * 0.5f, // Avant gauche
				carFrame.origin - carFrame.rotation.f * 1.0f + carFrame.rotation.u * 0.5f, // Arrière centre
			};

			Vec3 moveDirection = carFrame.rotation.f * Math.Sign(CurrentForwardSpeed);
			float checkDistance = moveDistance + 2.0f;

			for (int i = 0; i < collisionCheckPoints.Length; i++)
			{
				Vec3 checkPoint = collisionCheckPoints[i];
				Vec3 targetPoint = checkPoint + moveDirection * checkDistance;
				
				MBDebug.RenderDebugDirectionArrow(checkPoint, Vec3.Up, 0xFF00FF00, true);

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
					BodyFlags.CommonFocusRayCastExcludeFlags
				);

				if (hit)
				{
					uint arrowColor = 0xFFFF0000;
					
					bool isSelf = hitEntity != null && IsPartOfThisVehicle(hitEntity);
					float heightDifference = collisionPoint.z - checkPoint.z;
					
					// Un obstacle (mur) est à la même hauteur ou plus haut (heightDifference >= -0.2)
					bool isTerrain = heightDifference < -0.2f;  // Le sol est en bas
					bool isObstacle = !isTerrain && !isSelf;     // Ni sol ni soi-même = obstacle

					if (isSelf)
					{
						arrowColor = 0xFFFFFF00; // Jaune
						//Log($"  Point {i}: Hit self (distance: {collisionDistance:F2}m) - IGNORED", LogLevel.Debug);
					}
					else if (isTerrain)
					{
						arrowColor = 0xFF888888; // Gris
						//Log($"  Point {i}: Hit terrain at {collisionDistance:F2}m (height: {heightDifference:F2}m below) - IGNORED", LogLevel.Debug);
					}
					else if (isObstacle)
					{
						arrowColor = 0xFFFF0000; // Rouge
						//Log($"  Point {i}: OBSTACLE DETECTED at {collisionDistance:F2}m (height diff: {heightDifference:F2}m)", LogLevel.Information);
						
						if (collisionDistance < minDistance && collisionDistance < checkDistance * 0.8f)
						{
							minDistance = collisionDistance;
							nearestCollision = collisionPoint;
							hasAnyCollision = true;
						}
					}

					Vec3 rayDirection = (collisionPoint - checkPoint).NormalizedCopy();
					MBDebug.RenderDebugDirectionArrow(checkPoint, rayDirection, arrowColor, false);
					MBDebug.RenderDebugSphere(collisionPoint, 0.2f, arrowColor, false);
				}
				else
				{
					MBDebug.RenderDebugDirectionArrow(checkPoint, moveDirection, 0xFF00FF00, false);
					Log($"  Point {i}: Clear path", LogLevel.Debug);
				}
			}

			if (hasAnyCollision)
			{
				Log($"[Collision] BLOCKED! Nearest obstacle at {minDistance:F2}m", LogLevel.Warning);
				MBDebug.RenderDebugSphere(nearestCollision, 0.5f, 0xFFFF0000, false);
				MBDebug.RenderDebugDirectionArrow(nearestCollision, Vec3.Up, 0xFFFF0000, false);
			}

			return hasAnyCollision;
		}

		private bool IsPartOfThisVehicle(WeakGameEntity entity)
		{
			if (entity == null || GameEntity == null) return false;
			
			// Check if entity is part of vehicle or vehicle itself
			WeakGameEntity current = entity;
			while (current != null)
			{
				if (current == GameEntity) return true;
				current = current.Parent;
			}
			
			return false;
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


			if (PilotAgent != null && (GameNetwork.IsClient || GameNetwork.IsServer))
			{
				Vec3 nearestCollision;
				CheckMultiPointCollision(carFrame, Math.Abs(CurrentForwardSpeed * dt) + 0.5f, out nearestCollision);
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
					Log($"TakingOff! | {carUpwardRotationDeg} | backWheelsZ={averageBackWheelsZ} | frontWheelsZ={averageFrontWheelsZ} | distToTerrain={heightToTerrain} | relativeAng={relativeAngleForTakeOff}", LogLevel.Debug);
					IsFlying = true;
					CurrentUpwardSpeed = 0f;
				}
				if (IsFlying && heightToTerrain < -0.03f)
				{
					Log($"GroundReached! | {carUpwardRotationDeg} | backWheelsZ={averageBackWheelsZ} | frontWheelsZ={averageFrontWheelsZ} | distToTerrain={heightToTerrain}", LogLevel.Debug);
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

				// Check for obstacle before moving
				Vec3 moveDirection = carFrame.rotation.f;
				float moveDistance = Math.Abs(CurrentForwardSpeed * dt);

				if (CurrentForwardSpeed != 0)
				{
					Vec3 collisionPoint;
					if (CheckMultiPointCollision(carFrame, moveDistance + 0.5f, out collisionPoint))
					{
						// Calculer la normale de surface (direction de rebond)
						Vec3 impactDirection = (carFrame.origin - collisionPoint).NormalizedCopy();
						impactDirection.z = 0; // Garder horizontal
						impactDirection = impactDirection.NormalizedCopy();
						
						// Calculer l'angle d'impact
						float impactAngle = Vec3.DotProduct(moveDirection, -impactDirection);
						impactAngle = MathF.Clamp(impactAngle, 0f, 1f); // 0 = latéral, 1 = frontal
						
						// Force de rebond basée sur vitesse et angle d'impact
						float impactForce = MathF.Abs(CurrentForwardSpeed) * impactAngle;
						float bounceDistance = impactForce * 0.15f; // Conversion force -> distance
						bounceDistance = MathF.Clamp(bounceDistance, 0.1f, 1.0f);
						
						// Appliquer le rebond
						carFrame.origin += impactDirection * bounceDistance;
						
						// Calculer nouvelle vitesse avec perte d'énergie
						float energyLoss = 0.7f; // 70% d'énergie perdue
						float newSpeed = -CurrentForwardSpeed * (1f - energyLoss) * (1f - impactAngle * 0.5f);
						
						// Pour impact latéral, ajouter rotation
						if (impactAngle < 0.7f) // Impact pas complètement frontal
						{
							Vec3 rotationAxis = Vec3.CrossProduct(moveDirection, impactDirection);
							float spinIntensity = (1f - impactAngle) * impactForce * 0.05f;
							carFrame.Rotate(spinIntensity, rotationAxis);
						}
						
						CurrentForwardSpeed = newSpeed;
						
						// TODO ? Synchroniser avec clients via message réseau
						//if (GameNetwork.IsServer && MathF.Abs(impactForce) > 5f) // Seulement impacts significatifs
						//{
						//	SyncCollisionBounce(impactDirection, bounceDistance, newSpeed);
						//}
						
						Log($"[Collision] Impact {impactAngle:F2} force={impactForce:F1} bounce={bounceDistance:F2}m speed={newSpeed:F2}", LogLevel.Debug);
					}
					else
					{
						// No collision, move normally
						if (IsFlying)
						{
							CurrentUpwardSpeed += Gravity * dt;
							carFrame.Advance(CurrentForwardSpeed * dt);

							float rotationFactor = Math.Min(-0.005f, 0.04f * -carUpwardRotation);
							float rollFactor = rotationFactor / 5f * (_turnLeft ? 1 : -1);
							carFrame.Rotate(rotationFactor, Vec3.Side);
							carFrame.Rotate(rollFactor, Vec3.Forward);
						}
						else
						{
							carFrame.Advance(CurrentForwardSpeed * dt);

							if (Math.Abs(CurrentForwardSpeed) > 10f && Math.Abs(CurrentPivotTurnAngleDeg) > 1f)
							{
								HandleDrifting(dt, ref carFrame);
							}

							if (heightToTerrain < 1f && FollowTerrain && (CurrentForwardSpeed != 0f || CurrentUpwardSpeed != 0f))
							{
								AlignFrameWithGround(ref carFrame, GameEntity, collisionPoints, GroundOffset);
							}
						}
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

			UpdatePilotHands();

			// Sync global frame (server only)
			if (GameNetwork.IsServer)
			{
				SetFrameSynched(ref carFrame);
			}
		}

		private void UpdatePilotHands()
		{
			if (PilotAgent == null) return;

			MatrixFrame wheelFrame = PivotSteeringWheel.GetGlobalFrame();

			float wheelRadius = 0.27f;
			float handDepth = 0.1f;
			float leftHandAngle = MathHelper.ToRadian(-70f);  // ~9h
			float rightHandAngle = MathHelper.ToRadian(70f);  // ~3h

			// Left hand position on the wheel
			Vec3 leftRadial = MathF.Cos(leftHandAngle) * wheelFrame.rotation.u + MathF.Sin(leftHandAngle) * wheelFrame.rotation.s;
			Vec3 leftOffset = wheelRadius * leftRadial;
			Vec3 leftHandPos = wheelFrame.origin + leftOffset + handDepth * wheelFrame.rotation.f;
			Vec3 leftTangent = Vec3.CrossProduct(wheelFrame.rotation.f, leftRadial).NormalizedCopy();

			Mat3 leftRotation;
			leftRotation.u = -leftTangent;
			leftRotation.s = leftTangent;
			leftRotation.f = wheelFrame.rotation.s;
			leftRotation.Orthonormalize();

			MatrixFrame leftHand = new MatrixFrame(leftRotation, leftHandPos);

			// Right hand position on the wheel
			Vec3 rightRadial = MathF.Cos(rightHandAngle) * wheelFrame.rotation.u + MathF.Sin(rightHandAngle) * wheelFrame.rotation.s;
			Vec3 rightOffset = wheelRadius * rightRadial;
			Vec3 rightHandPos = wheelFrame.origin + rightOffset + handDepth * wheelFrame.rotation.f;

			Vec3 rightTangent = Vec3.CrossProduct(wheelFrame.rotation.f, rightRadial).NormalizedCopy();

			Mat3 rightRotation;
			rightRotation.u = -rightTangent;
			rightRotation.s = -rightTangent;
			rightRotation.f = -wheelFrame.rotation.s;
			rightRotation.Orthonormalize();

			MatrixFrame rightHand = new MatrixFrame(rightRotation, rightHandPos);

			PilotAgent.SetHandInverseKinematicsFrame(leftHand, rightHand);
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

		public override void AlignFrameWithGround(ref MatrixFrame frame, WeakGameEntity gameEntity, Vec3[] collisionPoints, float groundOffset = 0f)
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
					collisionPointPosition.z = FlyElevation - groundOffset + Scene.GetGroundHeightAtPosition(collisionPointPosition, BodyFlags.CommonCollisionExcludeFlags);

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
				float terrainHeight = Scene.GetGroundHeightAtPosition(cp, BodyFlags.CommonCollisionExcludeFlags);
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
	}
}