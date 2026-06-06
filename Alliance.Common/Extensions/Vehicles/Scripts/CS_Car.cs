using Alliance.Common.Extensions.Audio;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer;
using Alliance.Common.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
	/// <summary>
	/// Car controller. Handles visual feedback (particles, sounds, dashboard counters, pilot hands).
	/// All physics simulation are delegated to <see cref="ArcadeCarPhysics"/>
	/// </summary>
	public class CS_Car : CS_Vehicle
	{
		// Public parameters (can be tuned on prefab)
		public float BackWheelRadius = 0.35f;
		public float FrontWheelRadius = 0.35f;
		public string TireSmokeParticle = "prt_food_smoke";
		public string EngineSound = "Alliance/Native/Ambient/Car engine.mp3";
		public string TireScreechSound = "Alliance/Native/Ambient/Car tire screech.mp3";
		public string HonkSound = "Alliance/Native/Alert/Car horn.mp3";

		// Physics bridge
		private ArcadeCarPhysics _arcadePhysics;
		private bool _arcadePhysicsInitialized;
		private Vec3 _previousFrameOrigin;
		private float _smoothedSteerAngle;

		// Entity pivots
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

		// Sounds
		private CachedSound _engineSound;
		private CachedSound _tireScreechSound;
		private bool _tireScreechActive;
		private float _simulatedRPM = 800f;
		private float _enginePhaseAccumulator;

		// Visuals for wheels / smoke
		private Mat3 _pivotFrontLeftInitialRot;
		private Mat3 _pivotFrontRightInitialRot;
		private Mat3 _pivotSteeringWheelInitialRot;
		private Vec3 _pivotFLInitialOrigin;
		private Vec3 _pivotFRInitialOrigin;
		private Vec3 _wheelsBackInitialOrigin;
		private GameEntity[] _wheelSmokeEntities;
		private ParticleSystem[] _wheelSmokeParticles;
		private float[] _wheelSmokeIntensity;

		// Dashboard / gauges
		protected float OilLevel = 80f;
		protected float CountRPMAngle = 0f;
		protected float CountVacuumAngle = 0f;
		protected float CountPressureAngle = 0f;
		protected float CurrentPivotTurnAngleDeg = 0f;

		// Gearbox
		protected List<float> GearRatios = new List<float> { 1.2f, 1.1f, 1.0f, 0.9f, 0.8f };
		protected int CurrentGear = 0;
		private int _lastGear = -1;

		private bool _lightOn;
		public bool LightOn
		{
			get => _lightOn;
			protected set
			{
				if (value == _lightOn) return;
				_lightOn = value;
				LightLeft.SetVisibilityExcludeParents(value);
				LightRight.SetVisibilityExcludeParents(value);
			}
		}

		public CS_Car()
		{
		}

		public override void SetForwardSpeed(float speed)
		{
			base.SetForwardSpeed(speed);

			if (_arcadePhysics == null) return;
			Vec3 fwd = _arcadePhysics.CarFrame.rotation.f;
			float currentFwdComponent = Vec3.DotProduct(_arcadePhysics.Velocity, fwd);
			_arcadePhysics.Velocity += fwd * (speed - currentFwdComponent);
		}

		protected override void OnInit()
		{
			base.OnInit();

			CollisionPoints = new List<WeakGameEntity>();
			List<WeakGameEntity> allChildren = new List<WeakGameEntity>();
			GameEntity.GetChildrenRecursive(ref allChildren);

			foreach (WeakGameEntity child in allChildren)
			{
				if (child.HasTag("wheels_back")) WheelsBack = child;
				else if (child.HasTag("wheel_front_left")) WheelFrontLeft = child;
				else if (child.HasTag("wheel_front_right")) WheelFrontRight = child;
				else if (child.HasTag("pivot_steering_wheel")) PivotSteeringWheel = child;
				else if (child.HasTag("pivot_front_left")) PivotFrontLeft = child;
				else if (child.HasTag("pivot_front_right")) PivotFrontRight = child;
				else if (child.HasTag("oil_count")) CountOil = child;
				else if (child.HasTag("speed_count")) CountSpeed = child;
				else if (child.HasTag("rpm_count")) CountRPM = child;
				else if (child.HasTag("first_stage_count")) CountVacuum = child;
				else if (child.HasTag("second_stage_count")) CountPressure = child;
				else if (child.HasTag("light_left")) LightLeft = child;
				else if (child.HasTag("light_right")) LightRight = child;
				else if (child.HasTag("collision_point")) CollisionPoints.Add(child);
			}

			if (WheelsBack == null || WheelFrontLeft == null || WheelFrontRight == null || PivotSteeringWheel == null || CountOil == null)
			{
				ValidState = false;
			}
			else
			{
				FollowsTerrainPoints = new List<WeakGameEntity> { WheelsBack, WheelFrontLeft, WheelFrontRight };
			}

			if (ValidState)
			{
				InitializePhysics();
				InitializeWheelSmoke();
			}

			_previousFrameOrigin = GameEntity.GetFrame().origin;
		}

		private void InitializePhysics()
		{
			_arcadePhysics = new ArcadeCarPhysics();

			// Capture initial pivot positions from model (suspension fully extended)
			_pivotFrontLeftInitialRot = PivotFrontLeft.GetFrame().rotation;
			_pivotFrontRightInitialRot = PivotFrontRight.GetFrame().rotation;
			_pivotSteeringWheelInitialRot = PivotSteeringWheel.GetFrame().rotation;
			_pivotFLInitialOrigin = PivotFrontLeft.GetFrame().origin;
			_pivotFRInitialOrigin = PivotFrontRight.GetFrame().origin;
			_wheelsBackInitialOrigin = WheelsBack.GetFrame().origin;

			// Initialize physics wheels
			MatrixFrame globalCarFrame = GameEntity.GetGlobalFrame();
			Vec3 flLocal = globalCarFrame.TransformToLocal(WheelFrontLeft.GetGlobalFrame().origin);
			Vec3 frLocal = globalCarFrame.TransformToLocal(WheelFrontRight.GetGlobalFrame().origin);
			Vec3 backLocal = globalCarFrame.TransformToLocal(WheelsBack.GetGlobalFrame().origin);
			_arcadePhysics.InitializeWheels(
				new[] { flLocal, frLocal, new Vec3(flLocal.x, backLocal.y, backLocal.z), new Vec3(frLocal.x, backLocal.y, backLocal.z) },
				new[] { FrontWheelRadius, FrontWheelRadius, BackWheelRadius, BackWheelRadius }
			);
		}

		// Override vehicle movement update to delegate to ArcadeCarPhysics and handle visuals/sounds
		public override void UpdateVehicleMovement(float dt)
		{
			if (_arcadePhysics == null) return;

			ProcessDriverInput(dt);

			_arcadePhysics.ComputePhysics(dt, GameEntity);
			CurrentForwardSpeed = _arcadePhysics.ForwardSpeed;

			MatrixFrame carFrame = _arcadePhysics.CarFrame;

			if (GameNetwork.IsServer)
			{
				if (CollisionEnabled && MathF.Abs(CurrentForwardSpeed) > 0.1f)
				{
					if (CheckMultiPointCollision(carFrame, MathF.Abs(CurrentForwardSpeed * dt) + 0.5f, out CollisionResult collision))
					{
						OnCollision(ref carFrame, collision);
					}
				}

				SetFrameSynched(ref carFrame, false);
			}
#if DEBUG
			else if (Mission.Current?.InputManager?.IsKeyDown(InputKey.LeftAlt) ?? false)
			{
				_arcadePhysics.RenderDebug();
			}
#endif

			_previousFrameOrigin = carFrame.origin;


			// Visual updates (both client and server)
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

			UpdateOilCount(ref countOilFrame, dt);
			UpdateSpeedCount(ref countSpeedFrame);
			UpdateRPMCount(ref countRPMFrame, ref countFirstStageFrame, ref countSecondStageFrame, dt);

			// Always update wheel rotation based on actual speed (no threshold)
			UpdateWheelsRotation(dt, ref wheelsBackFrame, BackWheelRadius);
			UpdateWheelsRotation(dt, ref wheelFrontLeftFrame, FrontWheelRadius);
			UpdateWheelsRotation(dt, ref wheelFrontRightFrame, FrontWheelRadius);

			UpdatePivotFrames(MathHelper.ToRadian(CurrentPivotTurnAngleDeg), ref pivotSteeringWheelFrame, ref pivotFrontLeftFrame, ref pivotFrontRightFrame);
			UpdateSuspensionVisuals(ref pivotFrontLeftFrame, ref pivotFrontRightFrame, ref wheelsBackFrame);

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

			UpdateWheelSmoke(dt);
			UpdateSound(dt);
			UpdatePilotHands();
		}

		private void ProcessDriverInput(float dt)
		{
			UpdateCurrentGear(dt);
			float gearMultiplier = GearRatios[CurrentGear];

			float targetSteer = 0f;
			_arcadePhysics.EngineForce = 0f;
			_arcadePhysics.BrakeForce = 0f;
			
			if (PilotAgent != null)
			{
				if (_moveForward)
					_arcadePhysics.EngineForce += _arcadePhysics.MaxEngineForce * gearMultiplier;

				if (_moveBackward)
				{
					if (_arcadePhysics.ForwardSpeed > 1.5f)
						_arcadePhysics.BrakeForce += _arcadePhysics.MaxBrakeForce;
					else
						_arcadePhysics.EngineForce += -_arcadePhysics.MaxEngineForce * 0.4f * gearMultiplier;
				}

				// Speed-dependent steering				
				float speed = MathF.Abs(_arcadePhysics.ForwardSpeed);
				float bonusSteerForBrake = _arcadePhysics.ForwardSpeed > 0 ? _arcadePhysics.BrakeForce / _arcadePhysics.MaxBrakeForce : 0;
				float steerFactor = 1f / (1f + speed * 0.12f) + bonusSteerForBrake * .08f;
				steerFactor = MathF.Max(0.12f, steerFactor);
				
				if (_turnLeft) targetSteer = _arcadePhysics.MaxSteerAngle * steerFactor;
				else if (_turnRight) targetSteer = -_arcadePhysics.MaxSteerAngle * steerFactor;
			}

			_smoothedSteerAngle += (targetSteer - _smoothedSteerAngle) * MathF.Min(dt * 10f, 1f);
			_arcadePhysics.SteerAngle = _smoothedSteerAngle;
			CurrentPivotTurnAngleDeg = _arcadePhysics.SteerAngle;
		}

		#region Interactions
		public override void UpdatePilot(Agent agent)
		{
			base.UpdatePilot(agent);
			StartEngineSound();
		}

		public override void RemovePilot(Agent agent)
		{
			StopAllSounds();
			base.RemovePilot(agent);
		}

		protected override void ResetMovement(MatrixFrame frame)
		{
			base.ResetMovement(frame);

			if (_arcadePhysics == null)
			{
				return;
			}

			_arcadePhysics.CarFrame = frame;
			_arcadePhysics.Velocity = Vec3.Zero;
			_arcadePhysics.AngularVelocity = Vec3.Zero;
			_arcadePhysics.EngineForce = 0f;
			_arcadePhysics.BrakeForce = 0f;
			_arcadePhysics.SteerAngle = 0f;
			_arcadePhysics.Yaw = MathF.Atan2(-frame.rotation.f.x, frame.rotation.f.y);
			_smoothedSteerAngle = 0f;
			CurrentPivotTurnAngleDeg = 0f;
		}

		public void RequestLight(bool lightOn, bool sync = false)
		{
			if (LightOn == lightOn) return;
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
				Vec3 soundPosition = GameEntity.GetFrame().origin;
				AudioPlayer.Instance.Play(AudioPlayer.Instance.GetAudioId(HonkSound), 1f, true, 100, soundPosition);
			}
		}

		public virtual void ServerSyncHonk()
		{
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CS_VehicleSyncHonk(Id));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
		}
		#endregion

		#region Collision
		protected override Vec3[] GetCollisionCheckPoints(MatrixFrame vehicleFrame)
		{
			return new Vec3[]
			{
				vehicleFrame.origin + vehicleFrame.rotation.f * 2.0f + vehicleFrame.rotation.u * 1f, // center forward
				vehicleFrame.origin + vehicleFrame.rotation.f * 2.0f + vehicleFrame.rotation.s * 1.0f + vehicleFrame.rotation.u * 1f, //sides
				vehicleFrame.origin + vehicleFrame.rotation.f * 2.0f - vehicleFrame.rotation.s * 1.0f + vehicleFrame.rotation.u * 1f,
				vehicleFrame.origin + vehicleFrame.rotation.u * 1f, // center backward
			};
		}

		/// <summary>
		/// Override collision behavior for cars: deflect velocity by adding normal, lose speed based on impact angle.
		/// </summary>
		protected override void OnCollision(ref MatrixFrame frame, CollisionResult collision)
		{
			if (_arcadePhysics == null)
			{
				base.OnCollision(ref frame, collision);
				return;
			}

			// Low-speed collision: just stop
			if (CurrentForwardSpeed < 10f)
			{
				_arcadePhysics.Velocity = Vec3.Zero;
				_arcadePhysics.AngularVelocity = Vec3.Zero;
				CurrentForwardSpeed = 0f;
				frame.origin += collision.CollisionNormal * 0.1f;
				_arcadePhysics.CarFrame = frame;
				if (GameNetwork.IsServer) ServerSyncSpeed();
				return;
			}

			Vec3 velocity = _arcadePhysics.Velocity;
			float speed = velocity.Length;

			Vec3 normal = collision.CollisionNormal;
			normal.z = 0;
			if (normal.LengthSquared < 0.001f)
			{
				normal = collision.ImpactDirection;
			}
			else
			{
				normal.Normalize();
			}

			float normalComponent = Vec3.DotProduct(velocity, normal);

			// Already moving away from obstacle
			if (normalComponent >= 0)
			{
				return;
			}

			// Calculate impact angle (0 = glancing, 1 = head-on)
			Vec3 velocityDir = velocity.NormalizedCopy();
			float impactAngle = MathF.Abs(Vec3.DotProduct(velocityDir, normal));

			// Deflect velocity by adding normal vector (pushes car away from wall)
			Vec3 deflectedVelocity = velocity + normal * speed * 0.6f;

			// Speed loss proportional to impact angle: glancing keeps speed, head-on loses most
			float speedRetention = MathF.Lerp(0.95f, 0.25f, impactAngle);
			Vec3 newVelocity = deflectedVelocity.NormalizedCopy() * (speed * speedRetention);

			_arcadePhysics.Velocity = newVelocity;

			// Rotate car to face new velocity direction
			Vec3 newVelocityDir = newVelocity.NormalizedCopy();
			float newYaw = MathF.Atan2(-newVelocityDir.x, newVelocityDir.y);
			_arcadePhysics.Yaw = newYaw;

			frame.rotation.f = new Vec3(-MathF.Sin(newYaw), MathF.Cos(newYaw), 0);
			frame.rotation.s = new Vec3(MathF.Cos(newYaw), MathF.Sin(newYaw), 0);
			frame.rotation.u = new Vec3(0, 0, 1);

			CurrentForwardSpeed = Vec3.DotProduct(newVelocity, frame.rotation.f);

			// Push away from obstacle
			frame.origin += normal * 0.1f;

			_arcadePhysics.CarFrame = frame;
			if (GameNetwork.IsServer) ServerSyncSpeed();
		}
		#endregion

		#region Particles Effects
		private void InitializeWheelSmoke()
		{
			if (string.IsNullOrEmpty(TireSmokeParticle)) return;

			_wheelSmokeEntities = new GameEntity[4];
			_wheelSmokeParticles = new ParticleSystem[4];
			_wheelSmokeIntensity = new float[4];
			Vec3 origin = GameEntity.GetGlobalFrame().origin;

			for (int i = 0; i < 4; i++)
			{
				_wheelSmokeEntities[i] = TaleWorlds.Engine.GameEntity.CreateEmpty(Scene);
				MatrixFrame localFrame = MatrixFrame.Identity;
				_wheelSmokeParticles[i] = ParticleSystem.CreateParticleSystemAttachedToEntity(TireSmokeParticle, _wheelSmokeEntities[i], ref localFrame);
				_wheelSmokeParticles[i].SetRuntimeEmissionRateMultiplier(0f);
				_wheelSmokeEntities[i].SetGlobalFrame(new MatrixFrame(Mat3.Identity, origin));
			}
		}

		private void UpdateWheelSmoke(float dt)
		{
			if (_wheelSmokeParticles == null) return;

			MatrixFrame globalFrame = GameEntity.GetGlobalFrame();
			float speed = Math.Abs(CurrentForwardSpeed);
			float lateralFactor = Math.Abs(CurrentPivotTurnAngleDeg) / 10f * MathF.Clamp(speed / 5f, 0f, 1f);

			for (int i = 0; i < 4; i++)
			{
				bool grounded = _arcadePhysics.Wheels[i].IsGrounded;

				float intensity = 0f;
				if (grounded)
				{
					if (speed > 3f) intensity += MathF.Clamp((speed - 3f) / 25f, 0f, 0.15f);
					if (i >= 2 && _moveForward && speed < 3f) intensity += 0.7f;
					if (_moveBackward && speed > 4f) intensity += MathF.Clamp(speed / 15f, 0f, 0.8f);
					if (lateralFactor > 0.1f) intensity += MathF.Clamp(lateralFactor * 0.6f, 0f, 0.7f);
				}

				intensity = MathF.Clamp(intensity, 0f, 1f);
				float lerpRate = intensity > _wheelSmokeIntensity[i] ? 8f : 4f;
				_wheelSmokeIntensity[i] += (intensity - _wheelSmokeIntensity[i]) * MathF.Min(dt * lerpRate, 1f);
				if (_wheelSmokeIntensity[i] < 0.01f) _wheelSmokeIntensity[i] = 0f;

				Vec3 smokePos;
				if (grounded)
					smokePos = _arcadePhysics.Wheels[i].GroundHitPoint;
				else
				{
					smokePos = globalFrame.TransformToParent(_arcadePhysics.Wheels[i].CurrentPosition);
					smokePos.z -= _arcadePhysics.Wheels[i].WheelRadius;
					_wheelSmokeIntensity[i] *= 0.5f; // reduce intensity when in air
				}

				if (speed > 1f) smokePos += globalFrame.rotation.f * MathF.Clamp(speed * 0.3f, 0f, 10f);

				_wheelSmokeEntities[i].SetGlobalFrame(new MatrixFrame(Mat3.Identity, smokePos));
				_wheelSmokeParticles[i].SetRuntimeEmissionRateMultiplier(_wheelSmokeIntensity[i] * 10f);
			}
		}
		#endregion Effect

		#region Dashboard / Gauges
		private void UpdateRPMCount(ref MatrixFrame countRPMFrame, ref MatrixFrame countVacuumFrame, ref MatrixFrame countPressureFrame, float dt)
		{
			float maxRPM = 5000f;
			float[] maxRPMPerGear = { 2600f, 3200f, 3800f, 4200f, 5000f };
			float maxSpeedInGear = MaxForwardSpeed * ((CurrentGear + 1) / (float)GearRatios.Count);
			float estimatedRPM = Math.Max(0, CurrentForwardSpeed / maxSpeedInGear * maxRPMPerGear[CurrentGear]);
			if (CurrentGear > 0 && estimatedRPM < 0.5f * maxRPMPerGear[CurrentGear]) estimatedRPM = 0.5f * maxRPMPerGear[CurrentGear];

			CountRPMAngle = MathF.Lerp(CountRPMAngle, MathHelper.ToRadian(estimatedRPM / maxRPM * 290f), dt * 4);
			countRPMFrame.rotation = MatrixFrame.Identity.rotation;
			countRPMFrame.Rotate(CountRPMAngle, Vec3.Forward);

			CountVacuumAngle = MathF.Lerp(CountVacuumAngle, MathHelper.ToRadian((1 - estimatedRPM / maxRPM) * 240f), dt * 4);
			countVacuumFrame.rotation = MatrixFrame.Identity.rotation;
			countVacuumFrame.Rotate(CountVacuumAngle, Vec3.Forward);

			CountPressureAngle = MathF.Lerp(CountPressureAngle, MathHelper.ToRadian((estimatedRPM / maxRPM) * 240f), dt * 4);
			countPressureFrame.rotation = MatrixFrame.Identity.rotation;
			countPressureFrame.Rotate(CountPressureAngle, Vec3.Forward);
		}

		private void UpdateSpeedCount(ref MatrixFrame countSpeedFrame)
		{
			countSpeedFrame.rotation = MatrixFrame.Identity.rotation;
			countSpeedFrame.Rotate(MathHelper.ToRadian(2.9f * Math.Abs(CurrentForwardSpeed)), Vec3.Forward);
		}

		private void UpdateOilCount(ref MatrixFrame countOilFrame, float dt)
		{
			OilLevel = Math.Max(0, OilLevel - dt * CurrentForwardSpeed * 0.001f);
			countOilFrame.rotation = MatrixFrame.Identity.rotation;
			countOilFrame.Rotate(MathHelper.ToRadian(2.9625f * (OilLevel - 80)), Vec3.Forward);
		}
		#endregion

		#region Wheels Visuals
		private void UpdateSuspensionVisuals(ref MatrixFrame pivotFrontLeftFrame, ref MatrixFrame pivotFrontRightFrame, ref MatrixFrame wheelsBackFrame)
		{
			if (!_arcadePhysicsInitialized) return;

			// delta = compression relative to full extension + offset so equilibrium (50%) matches model pos
			float wheelVisualOffset = _arcadePhysics.EquilibriumFloat;
			float flDelta = _arcadePhysics.Wheels[0].SuspensionCompression - _arcadePhysics.Wheels[0].SuspensionRestLength + wheelVisualOffset;
			float frDelta = _arcadePhysics.Wheels[1].SuspensionCompression - _arcadePhysics.Wheels[1].SuspensionRestLength + wheelVisualOffset;
			float rlDelta = _arcadePhysics.Wheels[2].SuspensionCompression - _arcadePhysics.Wheels[2].SuspensionRestLength + wheelVisualOffset;
			float rrDelta = _arcadePhysics.Wheels[3].SuspensionCompression - _arcadePhysics.Wheels[3].SuspensionRestLength + wheelVisualOffset;

			pivotFrontLeftFrame.origin = new Vec3(_pivotFLInitialOrigin.x, _pivotFLInitialOrigin.y, _pivotFLInitialOrigin.z + flDelta);
			pivotFrontRightFrame.origin = new Vec3(_pivotFRInitialOrigin.x, _pivotFRInitialOrigin.y, _pivotFRInitialOrigin.z + frDelta);
			wheelsBackFrame.origin = new Vec3(_wheelsBackInitialOrigin.x, _wheelsBackInitialOrigin.y, _wheelsBackInitialOrigin.z + (rlDelta + rrDelta) * 0.5f);
		}

		private void UpdatePivotFrames(float absoluteAngleRad, ref MatrixFrame pivotSteeringWheelFrame, ref MatrixFrame pivotFrontLeftFrame, ref MatrixFrame pivotFrontRightFrame)
		{
			pivotFrontLeftFrame.rotation = _pivotFrontLeftInitialRot;
			pivotFrontLeftFrame.Rotate(absoluteAngleRad, Vec3.Up);
			pivotFrontRightFrame.rotation = _pivotFrontRightInitialRot;
			pivotFrontRightFrame.Rotate(absoluteAngleRad, Vec3.Up);
			pivotSteeringWheelFrame.rotation = _pivotSteeringWheelInitialRot;
			pivotSteeringWheelFrame.Rotate(-absoluteAngleRad * 5f, Vec3.Forward);
		}

		public void UpdateWheelsRotation(float dt, ref MatrixFrame wheelFrame, float wheelRadius)
		{
			// Rotate wheel based on actual car speed - always in sync with movement
			wheelFrame.Rotate(CurrentForwardSpeed / wheelRadius * dt * -1f, Vec3.Side);
		}

		private void UpdatePilotHands()
		{
			if (PilotAgent == null) return;

			MatrixFrame wheelFrame = PivotSteeringWheel.GetGlobalFrame();
			float wheelRadius = 0.27f;
			float handDepth = 0.1f;

			float leftHandAngle = MathHelper.ToRadian(-70f);
			Vec3 leftRadial = MathF.Cos(leftHandAngle) * wheelFrame.rotation.u + MathF.Sin(leftHandAngle) * wheelFrame.rotation.s;
			Vec3 leftHandPos = wheelFrame.origin + wheelRadius * leftRadial + handDepth * wheelFrame.rotation.f;
			Vec3 leftTangent = Vec3.CrossProduct(wheelFrame.rotation.f, leftRadial).NormalizedCopy();
			Mat3 leftRotation;
			leftRotation.u = -leftTangent;
			leftRotation.s = leftTangent;
			leftRotation.f = wheelFrame.rotation.s;
			leftRotation.Orthonormalize();

			float rightHandAngle = MathHelper.ToRadian(70f);
			Vec3 rightRadial = MathF.Cos(rightHandAngle) * wheelFrame.rotation.u + MathF.Sin(rightHandAngle) * wheelFrame.rotation.s;
			Vec3 rightHandPos = wheelFrame.origin + wheelRadius * rightRadial + handDepth * wheelFrame.rotation.f;
			Vec3 rightTangent = Vec3.CrossProduct(wheelFrame.rotation.f, rightRadial).NormalizedCopy();
			Mat3 rightRotation;
			rightRotation.u = -rightTangent;
			rightRotation.s = -rightTangent;
			rightRotation.f = -wheelFrame.rotation.s;
			rightRotation.Orthonormalize();

			PilotAgent.SetHandInverseKinematicsFrame(new MatrixFrame(leftRotation, leftHandPos), new MatrixFrame(rightRotation, rightHandPos));
		}
		#endregion

		#region Car gear
		private void UpdateCurrentGear(float dt)
		{
			float relativeSpeed = Math.Abs(CurrentForwardSpeed) / MaxForwardSpeed;
			if (relativeSpeed > (CurrentGear + 1) / (float)GearRatios.Count && CurrentGear < GearRatios.Count - 1)
				ShiftUp();
			else if (relativeSpeed < CurrentGear / (float)GearRatios.Count && CurrentGear > 0)
				ShiftDown();
		}

		public void ShiftUp()
		{
			if (CurrentGear < GearRatios.Count - 1) CurrentGear++;
		}

		public void ShiftDown()
		{
			if (CurrentGear > 0) CurrentGear--;
		}
		#endregion

		#region Sounds
		private void StartEngineSound()
		{
			if (GameNetwork.IsServer || string.IsNullOrEmpty(EngineSound)) return;
			StopEngineSound();
			Vec3 carPos = GameEntity.GetGlobalFrame().origin;
			_engineSound = AudioPlayer.Instance.PlayLooping(EngineSound, 0.3f, 100, carPos);
			if (_engineSound != null) _engineSound.CrossfadeSeconds = 0.2f;
		}

		private void StopEngineSound()
		{
			if (_engineSound != null) { _engineSound.Stop(); _engineSound = null; }
		}

		private void StopAllSounds()
		{
			StopEngineSound();
			if (_tireScreechSound != null) { _tireScreechSound.Stop(); _tireScreechSound = null; }
			_tireScreechActive = false;
		}

		private void UpdateSound(float dt)
		{
			if (GameNetwork.IsServer) return;
			Vec3 carPos = GameEntity.GetGlobalFrame().origin;
			UpdateEngineSound(carPos, dt);
			UpdateTireScreechSound(carPos);
		}

		private void UpdateEngineSound(Vec3 carPos, float dt)
		{
			if (string.IsNullOrEmpty(EngineSound) || PilotAgent == null) return;

			if (_engineSound == null || _engineSound.IsComplete)
			{
				StartEngineSound();
				return;
			}

			_engineSound.SetSoundOrigin(carPos);

			const float maxRPM = 5000f;
			const float idleRPM = 800f;
			float[] gearMaxRPM = { 2600f, 3200f, 3800f, 4200f, 5000f };

			if (CurrentGear != _lastGear)
			{
				if (CurrentGear > _lastGear && _lastGear >= 0) _simulatedRPM *= 0.55f;
				_lastGear = CurrentGear;
			}

			float gearSpeedMin = MaxForwardSpeed * (CurrentGear / (float)GearRatios.Count);
			float gearSpeedMax = MaxForwardSpeed * ((CurrentGear + 1) / (float)GearRatios.Count);
			float gearMinRPM = CurrentGear == 0 ? idleRPM : gearMaxRPM[CurrentGear - 1] * 0.55f;
			float gearProgress = MathF.Clamp(
				(Math.Abs(CurrentForwardSpeed) - gearSpeedMin) / MathF.Max(gearSpeedMax - gearSpeedMin, 0.1f), 0f, 1f);

			float targetRPM = _moveForward
				? gearMinRPM + (gearMaxRPM[CurrentGear] - gearMinRPM) * gearProgress
				: idleRPM + (gearMinRPM - idleRPM) * MathF.Clamp(Math.Abs(CurrentForwardSpeed) / 5f, 0f, 1f);

			float lerpRate = _simulatedRPM < targetRPM ? 3f : 5f;
			_simulatedRPM += (targetRPM - _simulatedRPM) * MathF.Min(dt * lerpRate, 1f);
			_simulatedRPM = MathF.Clamp(_simulatedRPM, idleRPM, maxRPM);

			float rpmRatio = (_simulatedRPM - idleRPM) / (maxRPM - idleRPM);
			_enginePhaseAccumulator += dt * (3f + rpmRatio * 5f);
			float wobble = MathF.Sin(_enginePhaseAccumulator) * 0.02f + MathF.Sin(_enginePhaseAccumulator * 0.27f) * 0.012f;
			_engineSound.PlaybackRate = 0.75f + rpmRatio * 1.5f + wobble;

			float volume = 0.6f + rpmRatio * 0.4f;
			if (_moveForward) volume = MathF.Min(volume + 0.15f, 1f);
			_engineSound.SetVolume(volume);
		}

		private void UpdateTireScreechSound(Vec3 carPos)
		{
			if (string.IsNullOrEmpty(TireScreechSound)) return;

			bool shouldScreech = PilotAgent != null && _moveBackward && CurrentForwardSpeed > 30f;

			if (shouldScreech)
			{
				if (_tireScreechSound == null || _tireScreechSound.IsComplete)
				{
					_tireScreechSound = AudioPlayer.Instance.PlayLooping(TireScreechSound, .3f, 100, carPos);
					if (_tireScreechSound != null) _tireScreechSound.Loop = false;
				}
				else
				{
					_tireScreechSound.SetSoundOrigin(carPos);
				}
				_tireScreechActive = true;
			}
			else if (_tireScreechActive)
			{
				_tireScreechActive = false;
			}
		}
		#endregion
	}
}
