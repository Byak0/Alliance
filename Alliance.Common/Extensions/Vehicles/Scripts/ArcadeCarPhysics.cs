﻿﻿using Alliance.Common.Utilities;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
	/// <summary>
	/// "Arcade" physics for 4-wheeled vehicles.
	/// Simulates gravity, suspension, lateral grip, rolling resistance, aerodynamic drag, slope compensation, intertia...
	/// Wheel layout: 0=FL, 1=FR, 2=RL, 3=RR.
	/// </summary>
	public class ArcadeCarPhysics
	{
		public struct Wheel
		{
			public Vec3 InitialPosition;       // Local mount point relative to car center
			public Vec3 CurrentPosition;
			public float DistanceToGround;     // Updated each frame by ground detection
			public float SuspensionRestLength; // Max suspension travel (m)
			public float SuspensionStiffness;  // Spring rate (N/m)
			public float SuspensionDamping;    // Damper rate (N·s/m)
			public float WheelRadius;
			public bool IsGrounded;
			public float SuspensionCompression;
			public Vec3 GroundHitPoint;
			public Vec3 GroundNormal;
			public Vec3 LocalForces;
		}

		public Wheel[] Wheels = new Wheel[4];
		public Vec3[] TerrainContacts = new Vec3[4];

		// Rigid body state
		public MatrixFrame CarFrame = MatrixFrame.Identity;
		public Vec3 Velocity = Vec3.Zero;
		public float Yaw = 0f;

		// Physical properties
		public int NbTractionWheels = 4;
		public float Mass = 1600f;
		public float Gravity = -20f;
		public float MaxEngineForce = 14000f;
		public float MaxBrakeForce = 20000f;
		public float MaxSpeed = 120f;

		// Engine / braking inputs (set each frame by the controller)
		public float EngineForce = 0f;
		public float BrakeForce = 0f;
		
		// Steering
		public float SteerAngle = 0f;
		public float MaxSteerAngle = 30f;

		// Resistance
		public float RollingResistanceCoeff = 0.015f;
		public float DragCoefficient = 1.4f;

		// Lateral grip strength (m/s²): how fast velocity re-aligns with heading after a turn.
		// Lower = more understeer/drift; higher = snappy tracking.
		public float LateralGripStrength = 25f; // Higher base grip for tight tracking when not drifting
		public float BrakeDriftGripMultiplier = 0.003f; // Extremely low - lateral slide persists much longer
		public float BrakeDriftYawBoost = 1.2f; // Subtle yaw boost - let lateral slide do the turning
		public float BrakeDriftMinSpeed = 10f; // Minimum speed to start drifting
		public float BrakeDriftFullSpeed = 100f; // Speed at which drift reaches maximum
		public float DriftIntensityMultiplier = 2.8f; // Multiply intensity of drift when braking
		public float EngineBrakeDrag = 0.8f; // Viscous drag coefficient when coasting (engine braking)

		// Smoothed drift factor - decays gradually when brake is released
		private float _smoothedDriftFactor = 0f;
		public float DriftRecoveryRate = .15f; // How fast grip returns after releasing brake (higher = faster)

		// Airborne pitch rate (rad/s): how fast the car nose-dives toward the velocity direction.
		// 0 = frozen orientation; ~1.5 = visible pitch on big jumps.
		public float AirborneRotationRate = 1.5f;

		// World-space angular velocity (rad/s): x=pitch, y=roll, z=yaw
		public Vec3 AngularVelocity = Vec3.Zero;
		// Higher = more rotational inertia (harder to spin/pitch/roll)
		public float AngularInertia = 1200f;
		// Bleeds angular velocity each frame (tire friction, suspension)
		public float AngularDamping = 6f;
		
		// Debug vectors (updated each frame, used by RenderDebug)
		public Vec3 GravityVector = Vec3.Zero;
		public Vec3 ForwardVector = Vec3.Zero;
		public Vec3 VelocityVector = Vec3.Zero;
		public Vec3 ResistanceVector = Vec3.Zero;

		// Derived properties
		public Vec3 Forward => RotateZ(Vec3.Forward, Yaw);
		public float ForwardSpeed => Vec3.DotProduct(Velocity, Forward);

		// Ground alignment smoothing
		private Vec3 _smoothedNormal = Vec3.Up;
		// Distance between front and rear axles (m)
		private float _wheelbase = 2f;
		// Equilibrium float height above terrain when suspension is at rest
		private float _equilibriumFloat = 0.025f;

		/// <summary>Equilibrium float height (m) — used by visuals for suspension offset.</summary>
		public float EquilibriumFloat => _equilibriumFloat;

		/// <summary>
		/// Initialize all 4 wheels from model data. Must be called before any physics step.
		/// </summary>
		public void InitializeWheels(Vec3[] positions, float[] radii)
		{
			if(positions.Length != 4 || radii.Length != 4)
			{
				Log($"Can't initialize wheels", LogLevel.Error);
				return;
			}
			for(int i = 0; i < 4; i ++)
			{
				Wheels[i] = new Wheel()
				{
					InitialPosition = positions[i],
					CurrentPosition = positions[i],
					WheelRadius = radii[i]
				};
			}
			// Wheelbase = distance between front and rear axles along Y
			_wheelbase = MathF.Max(0.5f, MathF.Abs(positions[0].y - positions[2].y));
		}

		/// <summary>
		/// Advance physics to the next frame.
		/// </summary>
		public void ComputePhysics(float dt, WeakGameEntity carEntity)
		{
			bool wasVisible = carEntity.GetVisibilityExcludeParents();
			if (wasVisible) carEntity.SetVisibilityExcludeParents(false);

			CarFrame = carEntity.GetGlobalFrame();
			Yaw = MathF.Atan2(-CarFrame.rotation.f.x, CarFrame.rotation.f.y);
			for (int i = 0; i < 4; i++) DetectWheelGround(i);

			// Accumulate forces/torques, integrate: updates Velocity, AngularVelocity, CarFrame
			ComputeVelocity(dt);
			// Debug vectors
			ForwardVector = CarFrame.rotation.f;
			GravityVector = new Vec3(0f, 0f, Gravity);
			VelocityVector = Velocity;

			// Terrain contact constraint: snap position, align pitch/roll to terrain
			ApplyGroundConstraint(dt);

			// Keep Yaw in sync with the final orientation
			Yaw = MathF.Atan2(-CarFrame.rotation.f.x, CarFrame.rotation.f.y);

			if (wasVisible) carEntity.SetVisibilityExcludeParents(true);
		}

		private void ComputeVelocity(float dt)
		{
			_smoothedNormal = Vec3.Lerp(_smoothedNormal, AverageGroundNormal(), MathF.Min(dt * 12f, 1f));
			Vec3 groundNormal = _smoothedNormal;

			Vec3 fwd = CarFrame.rotation.f;
			fwd -= groundNormal * Vec3.DotProduct(fwd, groundNormal);
			fwd.Normalize();

			Vec3 right = Vec3.CrossProduct(fwd, groundNormal);
			if (right.LengthSquared > 0.001f) right.Normalize();

			int groundedTraction = 0;
			for (int i = 0; i < NbTractionWheels; i++)
				if (Wheels[i].DistanceToGround < 0.05f) groundedTraction++;

			Vec3 force = Vec3.Zero;
			Vec3 torque = Vec3.Zero;

			if (groundedTraction == 0)
			{
				// ── Airborne ─────────────────────────────────────────────────────────
				force += new Vec3(0f, 0f, Gravity * Mass);

				float velLen = Velocity.Length;
				if (velLen > 1f && AirborneRotationRate > 0f)
				{
					Vec3 velDir = Velocity * (1f / velLen);
					Vec3 side = CarFrame.rotation.s;

					// Pitch: drive angular velocity toward velocity direction
					float pitchAngle = MathF.Atan2(
						Vec3.DotProduct(Vec3.CrossProduct(CarFrame.rotation.f, velDir), side),
						Vec3.DotProduct(CarFrame.rotation.f, velDir));
					float pitchDelta = MathF.Clamp(pitchAngle, -AirborneRotationRate * dt, AirborneRotationRate * dt);
					float targetPitchOmega = pitchDelta / dt;
					torque += side * ((targetPitchOmega - Vec3.DotProduct(AngularVelocity, side)) * AngularInertia / dt);

					// Roll: lean toward lateral velocity
					float lateralVel = Vec3.DotProduct(Velocity, side);
					float lateralRatio = MathF.Clamp(lateralVel / velLen, -0.5f, 0.5f);
					Vec3 tiltedUp = Vec3.Up + side * lateralRatio;
					if (tiltedUp.LengthSquared > 0.001f)
					{
						tiltedUp.Normalize();
						Vec3 targetS = Vec3.CrossProduct(CarFrame.rotation.f, tiltedUp);
						if (targetS.LengthSquared > 0.001f)
						{
							targetS.Normalize();
							float rollAngle = MathF.Atan2(
								Vec3.DotProduct(Vec3.CrossProduct(CarFrame.rotation.s, targetS), CarFrame.rotation.f),
								Vec3.DotProduct(CarFrame.rotation.s, targetS));
							float rollDelta = MathF.Clamp(rollAngle, -AirborneRotationRate * 0.7f * dt, AirborneRotationRate * 0.7f * dt);
							float targetRollOmega = rollDelta / dt;
							torque += CarFrame.rotation.f * ((targetRollOmega - Vec3.DotProduct(AngularVelocity, CarFrame.rotation.f)) * AngularInertia / dt);
						}
					}
				}

				EngineForce = 0f;
				BrakeForce = 0f;
			}
			else
			{
				// ── Grounded ─────────────────────────────────────────────────────────

				// Velocity constraint: remove the into-slope component (normal reaction force)
				Velocity -= groundNormal * Vec3.DotProduct(Velocity, groundNormal);

				float tractionFactor = (float)groundedTraction / NbTractionWheels;

				// Engine: traction drops proportionally on steep slopes (0% at ~60° incline)
				float slopeUpDot = MathF.Clamp(Vec3.DotProduct(groundNormal, Vec3.Up), 0f, 1f);
				float slopeTraction = MathF.Clamp((slopeUpDot - 0.5f) / 0.5f, 0.4f, 1f);
				force += fwd * (EngineForce * tractionFactor * slopeTraction);

				// Slope gravity: tangential component only (normal already cancelled above)
				if (groundNormal.x * groundNormal.x + groundNormal.y * groundNormal.y > 0.001f)
				{
					Vec3 gravityForce = new Vec3(0f, 0f, Gravity * Mass);
					force += gravityForce - groundNormal * Vec3.DotProduct(gravityForce, groundNormal);
				}

				// Slope compensation: counter lateral gravity proportional to forward speed.
				// At rest the car slides naturally; at speed tires provide lateral grip.
				force += ComputeSlopeCompensation(groundNormal, right, Vec3.DotProduct(Velocity, fwd));

				float brakeRatio = MathF.Clamp(BrakeForce / MathF.Max(MaxBrakeForce, 0.001f), 0f, 1f) * DriftIntensityMultiplier;
				float steerRatio = MathF.Abs(MaxSteerAngle) > 0.001f
					? MathF.Clamp(MathF.Abs(SteerAngle) / MathF.Abs(MaxSteerAngle), 0f, 1f)
					: 0f;

				// Speed ratio: 0% at minSpeed, 100% at fullSpeed
				// Example: minSpeed=8, fullSpeed=20
				// At 8 m/s: 0%, at 14 m/s: 50%, at 20+ m/s: 100%
				float currentSpeed = MathF.Abs(Vec3.DotProduct(Velocity, fwd));
				float speedRatio = MathF.Clamp((currentSpeed - BrakeDriftMinSpeed) / (BrakeDriftFullSpeed - BrakeDriftMinSpeed), 0f, 1f);

				// Brake is required, steer+speed modulate intensity
				float targetDriftFactor = brakeRatio * MathF.Sqrt(steerRatio * speedRatio);
				targetDriftFactor = MathF.Clamp(targetDriftFactor, 0f, 1f); // Safety clamp to keep physics stable

				// Smooth drift factor - increases instantly but decays gradually
				if (targetDriftFactor > _smoothedDriftFactor)
					_smoothedDriftFactor = targetDriftFactor; // Enter drift instantly
				else
					_smoothedDriftFactor += (targetDriftFactor - _smoothedDriftFactor) * MathF.Min(dt * DriftRecoveryRate, 1f); // Exit drift gradually

				// At low speeds with no input, snap drift factor to zero for full grip (no slip)
				if (currentSpeed < 3f && brakeRatio < 0.1f && steerRatio < 0.1f)
					_smoothedDriftFactor = 0f;

				_smoothedDriftFactor = MathF.Clamp(_smoothedDriftFactor, 0f, 1f); // Safety clamp

				float lateralGrip = MathF.Lerp(LateralGripStrength, LateralGripStrength * BrakeDriftGripMultiplier, _smoothedDriftFactor);
				lateralGrip = MathF.Max(lateralGrip, 0.05f); // Minimum grip to prevent instability

				// Lateral grip: impulse clamped to not overshoot in one frame
				float vLat = Vec3.DotProduct(Velocity, right);
				float latAccel = MathF.Clamp(-vLat * lateralGrip, -MathF.Abs(vLat) / dt, MathF.Abs(vLat) / dt);
				force += right * (latAccel * Mass);

				// Braking: clamped impulse
				float vFwd = Vec3.DotProduct(Velocity, fwd);
				if (BrakeForce > 0f && MathF.Abs(vFwd) > 0.01f)
				{
					float brakeAccel = MathF.Clamp(-MathF.Sign(vFwd) * BrakeForce / Mass, -MathF.Abs(vFwd) / dt, MathF.Abs(vFwd) / dt);
					force += fwd * (brakeAccel * Mass);
				}

				// Rolling resistance + aerodynamic drag (always active when grounded)
				ResistanceVector = ComputeResistanceForces(groundedTraction, Velocity.Length);
				force += ResistanceVector;

				// Engine-braking / drivetrain drag when coasting (s⁻¹ viscous drag coefficient).
				// Keep this low — rolling resistance already handles passive deceleration.
				if (MathF.Abs(EngineForce) < 0.1f)
				{
					vFwd = Vec3.DotProduct(Velocity, fwd);
					float decelAccel = MathF.Clamp(-vFwd * EngineBrakeDrag, -MathF.Abs(vFwd) / dt, MathF.Abs(vFwd) / dt);
					force += fwd * (decelAccel * Mass);
				}

				// Steering: drive AngularVelocity.z to bicycle-model yaw rate in one frame
				float steerRad = MathHelper.ToRadian(SteerAngle);
				vFwd = Vec3.DotProduct(Velocity, fwd);
				float targetYawOmega = (MathF.Abs(vFwd) > 0.5f && MathF.Abs(steerRad) > 0.001f)
					? vFwd * MathF.Tan(steerRad) / _wheelbase
					: 0f;
				targetYawOmega *= 1f + BrakeDriftYawBoost * _smoothedDriftFactor;
				float currentYawOmega = Vec3.DotProduct(AngularVelocity, Vec3.Up);
				torque += Vec3.Up * ((targetYawOmega - currentYawOmega) * AngularInertia / dt);

				// Save whether there's engine input BEFORE resetting (for low-speed snap check)
				bool hasEngineInput = MathF.Abs(EngineForce) > 0.1f;
				EngineForce = 0f;
				BrakeForce = 0f;

				// Low-speed snap to zero: prevent creeping when car should be stopped
				// Only apply when there's NO engine input (coasting to a stop)
				float horizontalSpeed = new Vec2(Velocity.x, Velocity.y).Length;
				if (horizontalSpeed < 0.5f && !hasEngineInput)
				{
					// Gradually kill horizontal velocity
					Velocity = new Vec3(Velocity.x * 0.85f, Velocity.y * 0.85f, Velocity.z);

					// Hard snap at very low speeds
					if (horizontalSpeed < 0.05f)
					{
						Velocity = new Vec3(0f, 0f, Velocity.z);
						AngularVelocity = new Vec3(AngularVelocity.x, AngularVelocity.y, 0f);  // Stop yaw rotation too
					}
				}
			}

			ApplyForce(force, torque, dt);

			// Low-speed snap also applies when airborne (after grounded section)
			if (groundedTraction == 0)
			{
				float horizontalSpeed = new Vec2(Velocity.x, Velocity.y).Length;
				if (horizontalSpeed < 0.05f)
				{
					Velocity = new Vec3(0f, 0f, Velocity.z);
					AngularVelocity = new Vec3(AngularVelocity.x, AngularVelocity.y, 0f);
				}

				// Hard speed cap — no force needed, just clamp
				float vFwdPost = Vec3.DotProduct(Velocity, fwd);
				if (vFwdPost > MaxSpeed)
					Velocity -= fwd * (vFwdPost - MaxSpeed);
				else if (vFwdPost < -MaxSpeed * 0.4f)
					Velocity -= fwd * (vFwdPost + MaxSpeed * 0.4f);
			}
		}

		private Vec3 AverageGroundNormal()
		{
			Vec3 normal = Vec3.Zero;
			int count = 0;
			for (int i = 0; i < 4; i++)
			{
				if (Wheels[i].DistanceToGround >= 0.1f) continue;
				normal += Wheels[i].GroundNormal;
				count++;
			}
			if (count == 0) return Vec3.Up;
			normal *= (1f / count);
			if (normal.LengthSquared < 0.001f) return Vec3.Up;
			normal.Normalize();
			return normal;
		}

		/// <summary>
		/// Terrain contact constraint: correct wheel penetration and align body to terrain.
		/// Position-only fix — orientation is set via AlignFrameToPoints (constraint, not dynamics).
		/// After alignment, AngularVelocity pitch/roll are reconciled so they don't fight the constraint.
		/// Yaw (z) is preserved — it is owned by steering torque in IntegrateState.
		/// </summary>
		private void ApplyGroundConstraint(float dt)
		{
			for (int i = 0; i < 4; i++) DetectWheelGround(i);

			bool anySnapUp = false;
			bool anyCorrection = false;
			float totalCorrectionZ = 0f;
			Vec3[] targettedWheelPosition = new Vec3[4];
			int airborneCount = 0;
			for (int i = 0; i < 4; i++)
			{
				if (Wheels[i].DistanceToGround > 0.05f) airborneCount++;
			}				

			for (int i = 0; i < 4; i++)
			{
				Wheel wheel = Wheels[i];
				float wheelCorrectionZ = 0f;

				if (wheel.DistanceToGround < -0.05f)
				{
					// Underground: snap up
					wheelCorrectionZ = -wheel.DistanceToGround;
					anyCorrection = true;
					anySnapUp = true;
				}
				else if (wheel.DistanceToGround > 0.05f && airborneCount < 4)
				{
					// Apply less gravity to front wheels to ease takeoff
					float wheelRatio = (i < 2) ? .4f : 1f;					
					float grav = Gravity * dt * wheelRatio * (airborneCount / 4f);
					wheelCorrectionZ = MathF.Max(-wheel.DistanceToGround, grav);
					anyCorrection = true;
				}
				totalCorrectionZ += wheelCorrectionZ;
				targettedWheelPosition[i] = CarFrame.TransformToParent(wheel.CurrentPosition) + new Vec3(0f, 0f, wheelCorrectionZ);
			}

			if (!anyCorrection) return;

			float avgCorrection = totalCorrectionZ / 4f;

			AlignFrameToPoints(ref CarFrame,
				targettedWheelPosition[0],
				targettedWheelPosition[1],
				targettedWheelPosition[2],
				targettedWheelPosition[3],
				avgCorrection);

			// Only reset pitch/roll angular velocity on hard snap-up, not gentle terrain following
			if (anySnapUp)
			{
				float yawOmega = Vec3.DotProduct(AngularVelocity, Vec3.Up);
				AngularVelocity = Vec3.Up * yawOmega;
			}
		}

		/// <summary>
		/// Align a MatrixFrame so its orientation matches the plane defined by 4 points.
		/// Points layout: 0=FL, 1=FR, 2=RL, 3=RR.
		/// Origin is set to the centroid of the 4 points.
		/// </summary>
		public static void AlignFrameToPoints(ref MatrixFrame frame, Vec3 fl, Vec3 fr, Vec3 rl, Vec3 rr, float moveUp)
		{
			// Longitudinal axis: rear center → front center
			Vec3 frontCenter = (fl + fr) * 0.5f;
			Vec3 rearCenter = (rl + rr) * 0.5f;
			Vec3 longitudinal = frontCenter - rearCenter;

			// Lateral axis: left center → right center
			Vec3 leftCenter = (fl + rl) * 0.5f;
			Vec3 rightCenter = (fr + rr) * 0.5f;
			Vec3 lateral = rightCenter - leftCenter;

			// Normal to the plane
			Vec3 up = Vec3.CrossProduct(lateral, longitudinal);
			if (up.LengthSquared < 0.0001f) up = Vec3.Up;
			up.Normalize();

			// Re-derive axes for perfect orthogonality
			Vec3 forward = longitudinal - up * Vec3.DotProduct(longitudinal, up);
			if (forward.LengthSquared < 0.0001f) forward = frame.rotation.f;
			forward.Normalize();

			Vec3 right = Vec3.CrossProduct(forward, up);
			right.Normalize();

			// Push origin along plane normal instead of global Z
			frame.origin += Vec3.Up * moveUp;
			frame.rotation.f = forward;
			frame.rotation.s = right;
			frame.rotation.u = up;
		}

		/// <summary>
		/// Counter the lateral component of slope gravity, scaled by forward speed.
		/// A stationary car slides naturally; at speed tires build lateral grip.
		/// </summary>
		private Vec3 ComputeSlopeCompensation(Vec3 groundNormal, Vec3 right, float forwardSpeed)
		{
			Vec3 gravityForce = new Vec3(0f, 0f, Gravity * Mass);
			// Tangential gravity on the slope plane
			Vec3 tangentialGravity = gravityForce - groundNormal * Vec3.DotProduct(gravityForce, groundNormal);
			// Lateral component: sideways to car forward
			float lateralGrav = Vec3.DotProduct(tangentialGravity, right);
			// Ramps from 0 (stationary) to 70% (≥ 6 m/s): no ghost-hold at rest
			float speedFactor = MathF.Min(.7f + MathF.Abs(forwardSpeed) / 6f, 1f);
			return right * (-lateralGrav * speedFactor);
		}

		/// <summary>
		/// Rolling resistance + aerodynamic drag opposing velocity.
		/// Higher rolling resistance at low speeds for quicker stops.
		/// </summary>
		private Vec3 ComputeResistanceForces(int groundedCount, float speed)
		{
			if (groundedCount == 0) return Vec3.Zero;
			Vec3 velDir = new Vec3(Velocity.x, Velocity.y, 0);
			if (velDir.LengthSquared < 0.0001f) return Vec3.Zero;
			velDir.Normalize();
			float weight = Mass * MathF.Abs(Gravity);

			// Increase rolling resistance at low speeds for quicker stops
			float effectiveCoeff = speed < 3f 
				? RollingResistanceCoeff * (1f + (3f - speed) * 0.5f)  // Up to 2.5x at very low speed
				: RollingResistanceCoeff;

			Vec3 rolling = velDir * (-effectiveCoeff * weight);
			Vec3 aero = velDir * (-DragCoefficient * speed * speed);
			return rolling + aero;
		}

		/// <summary>
		/// Detect ground contact for a single wheel and update its state.
		/// Uses raycasts to detect both terrain and entities, estimating normals from surface gradient.
		/// </summary>
		private void DetectWheelGround(int i)
		{
			Wheel wheel = Wheels[i];
			Vec3 wheelContactPoint = CarFrame.TransformToParent(wheel.CurrentPosition);
			wheelContactPoint.z -= wheel.WheelRadius;

			// Raycast from above wheel position down to detect entities or terrain
			Vec3 rayStart = wheelContactPoint + Vec3.Up * 0.5f;
			Vec3 rayEnd = wheelContactPoint - Vec3.Up * 1.0f;
			float rayLength = 1.5f;

			bool hitSomething = Mission.Current.Scene.RayCastForClosestEntityOrTerrain(
				rayStart, rayEnd, out float hitDistance, 0.01f, BodyFlags.CommonCollisionExcludeFlags);

			if (hitSomething && hitDistance < rayLength)
			{
				// Hit entity or terrain - calculate hit point
				Vec3 rayDir = (rayEnd - rayStart).NormalizedCopy();
				Vec3 hitPoint = rayStart + rayDir * hitDistance;

				// Estimate surface normal from surrounding points
				Vec3 estimatedNormal = EstimateNormalAtPoint(hitPoint, rayDir);

				wheel.DistanceToGround = wheelContactPoint.z - hitPoint.z;
				wheel.GroundHitPoint = hitPoint;
				wheel.GroundNormal = estimatedNormal;
				TerrainContacts[i] = hitPoint;
			}
			else
			{
				// Fallback: No raycast hit - use terrain detection
				float terrainZ = Mission.Current.Scene.GetGroundHeightAtPosition(wheelContactPoint, BodyFlags.CommonCollisionExcludeFlags);
				wheel.DistanceToGround = wheelContactPoint.z - terrainZ;
				wheel.GroundHitPoint = new Vec3(wheelContactPoint.x, wheelContactPoint.y, terrainZ);
				wheel.GroundNormal = Mission.Current.Scene.GetNormalAt(wheelContactPoint.AsVec2);
				if (wheel.GroundNormal.LengthSquared < 0.01f) wheel.GroundNormal = Vec3.Up;
				TerrainContacts[i] = new Vec3(wheelContactPoint.x, wheelContactPoint.y, terrainZ);
			}

			// Update wheel state
			float fullExtension = wheel.SuspensionRestLength + wheel.WheelRadius;
			float compression = fullExtension - wheel.DistanceToGround;
			wheel.IsGrounded = wheel.DistanceToGround < 0.05f;

			if (wheel.IsGrounded)
			{
				wheel.SuspensionCompression = MathF.Clamp(compression, 0, wheel.SuspensionRestLength);
			}
			else
			{
				wheel.SuspensionCompression = 0;
			}

			Wheels[i] = wheel;
		}

		/// <summary>
		/// Estimate surface normal at a point by sampling surrounding points with raycasts.
		/// Uses 4-point cross pattern to calculate gradient (finite difference method).
		/// </summary>
		private Vec3 EstimateNormalAtPoint(Vec3 centerPoint, Vec3 rayDir)
		{
			float sampleRadius = 0.15f; // 15cm sample radius for normal estimation
			float maxSampleDepth = 0.3f; // Max depth to search for surface

			// Sample 4 points around center in a cross pattern (forward, back, left, right)
			Vec3 right = Vec3.CrossProduct(Vec3.Up, rayDir).NormalizedCopy();
			if (right.LengthSquared < 0.01f) right = Vec3.Side; // Fallback if rayDir is vertical
			Vec3 forward = Vec3.CrossProduct(right, Vec3.Up).NormalizedCopy();

			Vec3[] sampleOffsets = new Vec3[4]
			{
				forward * sampleRadius,   // Front
				-forward * sampleRadius,  // Back
				right * sampleRadius,     // Right
				-right * sampleRadius     // Left
			};

			Vec3[] sampleHeights = new Vec3[4];
			int validSamples = 0;

			for (int j = 0; j < 4; j++)
			{
				Vec3 samplePos = centerPoint + sampleOffsets[j];
				Vec3 sampleRayStart = samplePos + Vec3.Up * 0.3f;
				Vec3 sampleRayEnd = samplePos - Vec3.Up * maxSampleDepth;

				float sampleDistance;
				bool hitSample = Mission.Current.Scene.RayCastForClosestEntityOrTerrain(
					sampleRayStart, sampleRayEnd, out sampleDistance, 0.01f, BodyFlags.CommonCollisionExcludeFlags);

				if (hitSample && sampleDistance < (0.3f + maxSampleDepth))
				{
					Vec3 sampleHitPoint = sampleRayStart + (sampleRayEnd - sampleRayStart).NormalizedCopy() * sampleDistance;
					sampleHeights[j] = sampleHitPoint;
					validSamples++;
				}
				else
				{
					// No hit - use center point as fallback
					sampleHeights[j] = centerPoint;
				}
			}

			// Calculate normal using cross product of gradient vectors
			// If we have at least 2 valid samples, we can estimate a normal
			if (validSamples >= 2)
			{
				Vec3 dx = sampleHeights[2] - sampleHeights[3]; // Right - Left
				Vec3 dy = sampleHeights[0] - sampleHeights[1]; // Front - Back

				Vec3 estimatedNormal = Vec3.CrossProduct(dy, dx);

				if (estimatedNormal.LengthSquared > 0.001f)
				{
					estimatedNormal.Normalize();

					// Ensure normal points upward (dot product with Vec3.Up should be positive)
					if (Vec3.DotProduct(estimatedNormal, Vec3.Up) < 0f)
						estimatedNormal = -estimatedNormal;

					return estimatedNormal;
				}
			}

			// Fallback: Not enough samples or invalid gradient - use Vec3.Up
			return Vec3.Up;
		}

		/// <summary>
		/// Rotate a vector around the Z (up) axis by the given angle in radians.
		/// </summary>
		private static Vec3 RotateZ(Vec3 v, float angle)
		{
			float c = MathF.Cos(angle);
			float s = MathF.Sin(angle);
			return new Vec3(c * v.x - s * v.y, s * v.x + c * v.y, v.z);
		}

		/// <summary>
		/// Render debug arrows and text for physics visualization.
		/// </summary>
		public void RenderDebug()
		{
			uint green = 0xFF00FF00;
			uint yellow = 0xFFFFFF00;
			uint white = 0xFFFFFFFF;
			uint cyan = 0xFF00FFFF;
			uint red = 0xFFFF00FF;
			string[] wheelLabels = { "FL", "FR", "RL", "RR" };

			for (int i = 0; i < 4; i++)
			{
				Wheel wheel = Wheels[i];
				Vec3 pos = CarFrame.TransformToParent(wheel.CurrentPosition);
				uint color = white;
				if (wheel.DistanceToGround > 0.05f) color = green;
				else if (wheel.DistanceToGround < -0.05f) color = red;

				string info = $"{wheelLabels[i]} H:{wheel.DistanceToGround:F3} {(wheel.IsGrounded ? "" : "AIRBORN")}";
				MBDebug.RenderDebugText3D(pos + Vec3.Up * 0.3f, info, color, 0);
				MBDebug.RenderDebugDirectionArrow(pos, wheel.GroundNormal, cyan);
				MBDebug.RenderDebugDirectionArrow(pos, wheel.LocalForces, yellow);
			}
			Vec3 carTop = CarFrame.origin;
			MBDebug.RenderDebugDirectionArrow(carTop, ForwardVector, cyan);
			MBDebug.RenderDebugDirectionArrow(carTop, _smoothedNormal, white);
			

			// Velocity direction (orange): diverges from ForwardVector when drifting
			float speed = ForwardSpeed;
			if (speed > 0.5f)
			{
				uint orange = 0xFFFF8800;
				MBDebug.RenderDebugDirectionArrow(carTop, VelocityVector * (1f / speed), orange);
			}

			// Slip angle: signed angle between heading and velocity
			float slipAngle = 0f;
			if (speed > 0.5f)
			{
				Vec3 velDir = VelocityVector * (1f / speed);
				float dot = MathF.Clamp(Vec3.DotProduct(ForwardVector, velDir), -1f, 1f);
				float cross = Vec3.DotProduct(Vec3.CrossProduct(ForwardVector, velDir), Vec3.Up);
				slipAngle = MathF.Atan2(cross, dot) * (180f / MathF.PI);
			}
			string carInfo = $"Spd:{speed:F3} Slip:{slipAngle:F3}° Eng:{EngineForce:F3} Brk:{BrakeForce:F3} Str:{SteerAngle:F3} AngVz:{AngularVelocity.z:F3}";
			MBDebug.RenderDebugText3D(carTop, carInfo, cyan, 0);
		}

		/// <summary>
		/// Apply linear force → Velocity → Position  AND  torque → AngularVelocity → Orientation.
		/// </summary>
		private void ApplyForce(Vec3 linearForce, Vec3 torque, float dt)
		{
			// ── Linear ────────────────────────────────────────────────────────────
			Velocity += (linearForce / Mass) * dt;
			CarFrame.origin += Velocity * dt;

			// ── Angular ───────────────────────────────────────────────────────────
			AngularVelocity += (torque / AngularInertia) * dt;
			AngularVelocity *= MathF.Max(0f, 1f - AngularDamping * dt);

			// Integrate orientation:  dR/dt = ω × R
			Vec3 omega = AngularVelocity * dt;
			Vec3 f = CarFrame.rotation.f;
			Vec3 s = CarFrame.rotation.s;

			Vec3 newF = f + Vec3.CrossProduct(omega, f); newF.Normalize();
			Vec3 newS = s + Vec3.CrossProduct(omega, s); newS.Normalize();
			Vec3 newU = Vec3.CrossProduct(newS, newF); newU.Normalize();

			CarFrame.rotation.f = newF;
			CarFrame.rotation.s = newS;
			CarFrame.rotation.u = newU;
		}
	}
}
