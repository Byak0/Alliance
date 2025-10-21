using Alliance.Common.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// Dynamic pile: raises cones with volume, spins + heartbeat,
	/// and simulates a texture sweep by driving VectorArgument2.Z (0..1)
	/// on all entities tagged "sweep", synchronized to the heartbeat.
	/// </summary>
	public class CS_DynamicPile : MissionObject
	{
		public bool Enabled = false;

		// ----- Pile geometry -----
		public float ConeMaxHeight = 7.77f;
		public float Cone2MaxHeight = 4.1f;
		public float ConeMaxVolume = 20000f;
		public float MaxVolumePerSecond = 10f;
		public float EstimateGravity = 8f;

		// ----- Heart spin -----
		public float HeartSpinBase = 0.2f;
		public float HeartSpinSpeedMax = 6.0f;
		public float HeartSpinAccel = 6.0f;
		public float HeartSpinBrake = 4.0f;

		// ----- Heartbeat -----
		public float HeartBPMIdle = 40f;   // beats per minute when idle
		public float HeartBPMActive = 80f;  // beats per minute when active
		public float HeartBeat1Width = 0.065f;// fraction of cycle (0..1), main beat width
		public float HeartBeat2Width = 0.050f;// fraction of cycle (0..1), second beat width
		public float HeartBeat2Delay = 0.18f; // fraction after beat1 (0..1)
		public float HeartBeat2Gain = 0.6f;  // second beat strength vs first (0..1)
		public float HeartBeatAmpIdle = 0.04f; // ±4% when idle
		public float HeartBeatAmpActive = 0.10f; // ±10% when active
		public float HeartBeatAccel = 6.0f;  // acceleration into active state
		public float HeartBeatBrake = 4.0f;  // brake into idle state

		// ----- Sweep simulation -----
		public float SweepScaleX = 0.5f;
		public float SweepScaleY = 1.0f;
		public float SweepOffsetX = 0.0f; // drift offset added to sweep (SweepMin..SweepMax)
		public float SweepOffsetY = 0.0f;
		public float SweepMin = 0.0f;
		public float SweepMax = 1.0f;

		// Editor buttons
		[EditableScriptComponentVariable(true)] public SimpleButton INIT;
		[EditableScriptComponentVariable(true)] public SimpleButton RESET;
		[EditableScriptComponentVariable(true)] public SimpleButton ADD;
		[EditableScriptComponentVariable(true)] public SimpleButton REMOVE;

		// ----- Volume state -----
		public float CurrentVolume = 0f;
		/// <summary>
		/// To be used only in EDITOR !!!
		/// </summary>
		public float VolumeToAdd = 0f;
		public float VolumeTargetted
		{
			get
			{
				return _volumeTargetted;
			}
		}
		private float _volumeTargetted;
		private float _lastVolumeAdded;
		private float _currentVolumeEstimate = 0f;
		private float _volumeDisplayed = 0f;

		// ----- Entities -----
		private WeakGameEntity _coneEntity;
		private MatrixFrame _initialConeFrame;
		private float _initialConeZ;
		private float _currentConeZ;

		private WeakGameEntity _cone2Entity;
		private MatrixFrame _initialCone2Frame;
		private float _initialCone2Z;
		private float _currentCone2Z;

		private WeakGameEntity _particleEntity;
		private ParticleSystem _particleSystem;
		private MatrixFrame _initialParticleFrame;

		private WeakGameEntity _heartEntity;
		private MatrixFrame _initialHeartFrame;

		private List<WeakGameEntity> _entitiesToSweep;

		private CS_TextPanel _textPanel;
		private NumberFormatInfo _labelFormat;

		// ----- Rise timing -----
		private float _riseDelayRequired = 0f;
		private float _riseDelayTimer = 0f;
		private bool _isRising = false;

		// ----- Emitter -----
		private float _currentEmitterRate = 0f; // 0..1 normalized
		private float _emitterLifeTimer = 0f;

		// ----- Spin state -----
		private float _heartSpinVel = 0f;
		private float _heartSpinAng = 0f;

		// ----- Heartbeat state -----
		private float _beatPhase = 0f;  // wraps [0..1)
		private float _beatHz = 1f;     // beats per second (smoothed)
		private float _beatAmp = 0f;    // amplitude (smoothed)
		private float _heartBeatScale = 1f;

		private const float _Eps = 1e-5f;

		public CS_DynamicPile() { }

		public void Init()
		{
			InitChildren();
			_beatPhase = 0f;
			_labelFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
			_labelFormat.NumberGroupSeparator = " ";
			_labelFormat.NumberDecimalDigits = 0;
		}

		/// <summary>
		/// Set the current volume immediately, without animation.		
		/// </summary>
		public void SetVolume(float newVolume)
		{
			CurrentVolume = newVolume;
			_currentVolumeEstimate = CurrentVolume;
			_volumeTargetted = CurrentVolume;
			UpdateCones();
			UpdateTextLabel();
		}

		/// <summary>
		/// Set target volume, which will make the pile rise according to settings.
		/// </summary>
		public void SetVolumeTarget(float targetVolume)
		{
			Enabled = true;
			RecomputeRiseDelay();
			_volumeTargetted = targetVolume;
		}

		public void Reset()
		{
			CurrentVolume = 0f;
			_currentVolumeEstimate = 0f;
			_volumeTargetted = 0f;
			_currentEmitterRate = 0f;
			_riseDelayTimer = 0f;
			_isRising = false;

			_heartSpinVel = 0f;
			_heartSpinAng = 0f;

			_beatPhase = 0f;
			UpdateCones();
		}

		protected override void OnInit()
		{
			base.OnInit();
			Init();
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			Init();
		}

		private void InitChildren()
		{
			_coneEntity = GameEntity.GetFirstChildEntityWithTag("pile");
			if (_coneEntity != null)
			{
				_initialConeFrame = _coneEntity.GetGlobalFrame();
				_initialConeZ = _initialConeFrame.origin.Z;
				_currentConeZ = _initialConeZ;
			}

			_cone2Entity = GameEntity.GetFirstChildEntityWithTag("pile2");
			if (_cone2Entity != null)
			{
				_initialCone2Frame = _cone2Entity.GetGlobalFrame();
				_initialCone2Z = _initialCone2Frame.origin.Z;
				_currentCone2Z = _initialCone2Z;
			}

			_particleEntity = GameEntity.GetFirstChildEntityWithTag("particle");
			if (_particleEntity != null)
			{
				_initialParticleFrame = _particleEntity.GetGlobalFrame();
				var comp = _particleEntity.GetComponentAtIndex(0, TaleWorlds.Engine.GameEntity.ComponentType.ParticleSystemInstanced);
				if (comp is ParticleSystem ps)
				{
					_particleSystem = ps;
					_particleSystem.SetRuntimeEmissionRateMultiplier(_currentEmitterRate);
				}
			}

			_heartEntity = GameEntity.GetFirstChildEntityWithTag("heart");
			if (_heartEntity != null) _initialHeartFrame = _heartEntity.GetGlobalFrame();

			_entitiesToSweep = GameEntity.CollectChildrenEntitiesWithTag("sweep");

			_textPanel = GameEntity.GetFirstScriptInFamilyDescending<CS_TextPanel>();
		}

		protected override void OnEditorVariableChanged(string variableName)
		{
			switch (variableName)
			{
				case nameof(INIT): Init(); break;
				case nameof(RESET): Reset(); break;
				case nameof(ADD): AddVolume(); break;
				case nameof(REMOVE): RemoveVolume(); break;
			}
		}

		public override TickRequirement GetTickRequirement()
		{
			return TickRequirement.Tick | base.GetTickRequirement();
		}

		protected override void OnTick(float dt)
		{
			base.OnTick(dt);
			Tick(dt);
		}

		protected override void OnEditorTick(float dt)
		{
			base.OnEditorTick(dt);
			Tick(dt);
		}

		private void Tick(float dt)
		{
			if (!Enabled) return;

			if (_volumeTargetted != CurrentVolume)
			{
				if (!_isRising)
				{
					_riseDelayTimer += dt;
					if (_riseDelayTimer >= _riseDelayRequired) _isRising = true;
				}

				_emitterLifeTimer += dt;
				UpdateEmitterRate();
				UpdateVolumeEstimate(dt);

				UpdateHeartSpin(dt);
				UpdateHeartBeat(dt);
				UpdateBranchSweeps();
				UpdateParticleAndHeart(dt);

				if (_isRising)
				{
					UpdateVolume(dt);
					UpdateCones();
				}
			}
			else
			{
				_riseDelayTimer = 0f;
				_isRising = false;
				_emitterLifeTimer = 0f;
				_currentVolumeEstimate = CurrentVolume;

				UpdateTextLabel();

				UpdateHeartSpin(dt);
				UpdateHeartBeat(dt);
				UpdateBranchSweeps();

				if (_currentEmitterRate != 0f) _currentEmitterRate = 0f;
				UpdateParticleAndHeart(dt);
			}
		}

		private void UpdateTextLabel()
		{
			if (_textPanel != null && _volumeDisplayed != CurrentVolume)
			{
				_volumeDisplayed = CurrentVolume;

				_textPanel.UpdateText((CurrentVolume * 1000f).ToString("N", _labelFormat) + " E");
				_textPanel.Render();
			}
		}

		// Editor only command - Simulate adding volume
		private void AddVolume()
		{
			RecomputeRiseDelay();
			_volumeTargetted += VolumeToAdd;
			if (_volumeTargetted > ConeMaxVolume) _volumeTargetted = ConeMaxVolume;
		}

		// Editor only command - Simulate removing volume
		private void RemoveVolume()
		{
			CurrentVolume = _volumeTargetted;
			_volumeTargetted -= VolumeToAdd;
			if (_volumeTargetted < 0f) _volumeTargetted = 0f;
		}

		private void RecomputeRiseDelay()
		{
			float pileTopZ = _coneEntity.GetGlobalFrame().origin.z + ConeMaxHeight;
			float emitterZ = _particleEntity.GetGlobalFrame().origin.z;
			float dz = emitterZ - pileTopZ;
			_riseDelayRequired = dz <= 0f ? 0f : Math.Max(0f, (float)Math.Sqrt(2f * dz / EstimateGravity));
			_riseDelayTimer = 0f;
			_isRising = false;
		}

		private void UpdateCones()
		{
			float h = ConeMaxHeight * (float)Math.Pow(CurrentVolume / Math.Max(1f, ConeMaxVolume), 0.5f);

			if (_coneEntity != null)
			{
				_currentConeZ = _initialConeZ + h;
				MatrixFrame f = _coneEntity.GetGlobalFrame();
				f.origin = new Vec3(f.origin.x, f.origin.y, _currentConeZ);
				_coneEntity.SetGlobalFrame(f);

			}

			if (_cone2Entity != null)
			{
				float pct = (_currentConeZ - _initialConeZ) / Math.Max(_Eps, ConeMaxHeight);
				_currentCone2Z = _initialCone2Z + Cone2MaxHeight * pct;
				MatrixFrame f = _cone2Entity.GetGlobalFrame();
				f.origin = new Vec3(f.origin.x, f.origin.y, _currentCone2Z);
				_cone2Entity.SetGlobalFrame(f);
			}
		}

		private void UpdateParticleAndHeart(float dt)
		{
			float t = _currentVolumeEstimate / Math.Max(1f, ConeMaxVolume);
			float elevation = (float)(ConeMaxHeight * 2f * Math.Pow(t, 0.5f));

			float initScaleP = _initialParticleFrame.GetScale().x;
			float initScaleH = _initialHeartFrame.GetScale().x;
			Vec3 delta = _initialHeartFrame.origin - _initialParticleFrame.origin;

			// Particle
			MatrixFrame particleFrame = _initialParticleFrame;
			particleFrame.origin = new Vec3(particleFrame.origin.x, particleFrame.origin.y, particleFrame.origin.z + elevation);
			float sp = MathF.Lerp(initScaleP, .5f, t);
			particleFrame = EntityUtils.SetScale(particleFrame, new Vec3(sp, sp, sp));
			particleFrame.Rotate(_heartSpinAng, Vec3.Up);
			if (_particleEntity.IsValid) _particleEntity.SetGlobalFrame(in particleFrame);

			// Heart
			if (_heartEntity != null)
			{
				MatrixFrame heartFrame = _initialHeartFrame;
				float baseScaleH = MathF.Lerp(initScaleH, .5f, t);
				float relScale = baseScaleH / Math.Max(1e-6f, initScaleH);
				Vec3 offset = delta * relScale;

				// Maintain a relative distance above the particles
				heartFrame.origin = particleFrame.origin + offset;

				float finalScaleH = baseScaleH * _heartBeatScale;
				heartFrame = EntityUtils.SetScale(heartFrame, new Vec3(finalScaleH, finalScaleH, finalScaleH));
				heartFrame.Rotate(_heartSpinAng, Vec3.Up);
				_heartEntity.SetGlobalFrame(in heartFrame);
			}

			_particleSystem?.SetRuntimeEmissionRateMultiplier(_currentEmitterRate);
		}

		private void UpdateHeartSpin(float dt)
		{
			bool active = _currentEmitterRate > 0.01f || _currentVolumeEstimate != _volumeTargetted;
			float estimatedVolumePercentage = _currentVolumeEstimate / Math.Max(1f, ConeMaxVolume);
			float tEase = EaseInOut(MathF.Clamp(estimatedVolumePercentage, 0f, 1f));
			float emitBoost = MathF.Clamp(_currentEmitterRate, 0f, 1f);
			float boost = Math.Max(tEase, emitBoost * 0.75f);

			float targetVel = HeartSpinBase + (HeartSpinSpeedMax - HeartSpinBase) * boost;
			float target = active ? targetVel : HeartSpinBase;
			float resp = active ? HeartSpinAccel : HeartSpinBrake;

			_heartSpinVel = SmoothTowards(_heartSpinVel, target, resp, dt);
			_heartSpinAng += _heartSpinVel * dt;
			if (_heartSpinAng > Math.PI * 4f) _heartSpinAng -= (float)(Math.PI * 4f);
		}

		private void UpdateHeartBeat(float dt)
		{
			bool active = _isRising || _currentEmitterRate > 0.01f || _currentVolumeEstimate != _volumeTargetted;

			// Targets: frequency from BPM, amplitude from state
			float targetHz = (active ? HeartBPMActive : HeartBPMIdle) / 60f; // beats/sec
			float targetAmp = active ? HeartBeatAmpActive : HeartBeatAmpIdle;

			// Smooth towards targets
			float resp = active ? HeartBeatAccel : HeartBeatBrake;
			_beatHz = SmoothTowards(_beatHz, targetHz, resp, dt);
			_beatAmp = SmoothTowards(_beatAmp, targetAmp, resp, dt);

			// Advance wrapped phase in [0..1)
			_beatPhase += Math.Max(0f, _beatHz) * dt;
			if (_beatPhase >= 1f) _beatPhase -= (float)Math.Floor(_beatPhase);

			// Build a "lub-dub" pulse: one strong + one smaller, short delay later
			// Use wrapped Gaussians so pulses are sharp, with quiet time between.
			float p1 = GaussianPulseWrapped(_beatPhase, 0.00f, HeartBeat1Width); // main beat
			float p2 = GaussianPulseWrapped(_beatPhase, HeartBeat2Delay, HeartBeat2Width) * HeartBeat2Gain; // second beat

			// Shaping to make the hit feel snappier
			float pulse = p1 + p2; // ≈ 0 most of the time, spikes on beats
			pulse = PowSaturate(pulse, 0.6f); // <1 → sharper peaks, >1 → softer

			// Final scale multiplier (center at 1.0, only expands on beats)
			_heartBeatScale = 1f + _beatAmp * pulse;  // e.g. 1.00 → 1.10 at peaks
		}

		private void UpdateBranchSweeps()
		{
			// Map to [Min..Max]
			float sweepX = SweepOffsetX + MathF.Lerp(SweepMin, SweepMax, _beatPhase);

			foreach (WeakGameEntity entity in _entitiesToSweep)
			{
				MetaMesh mm = entity.GetMetaMesh(0);
				mm?.SetVectorArgument2(SweepScaleX, SweepScaleY, sweepX, SweepOffsetY);
			}
		}

		private void UpdateVolume(float dt)
		{
			float maxPerTick = MaxVolumePerSecond * dt;
			float delta = _volumeTargetted - CurrentVolume;
			_lastVolumeAdded = Math.Min(delta, maxPerTick);
			CurrentVolume += _lastVolumeAdded;
		}

		private void UpdateVolumeEstimate(float dt)
		{
			float maxPerTick = MaxVolumePerSecond * dt;
			float delta = _volumeTargetted - _currentVolumeEstimate;
			_lastVolumeAdded = Math.Min(delta, maxPerTick);
			_currentVolumeEstimate += _lastVolumeAdded;
		}

		private void UpdateEmitterRate()
		{
			if (MaxVolumePerSecond <= 0) return;

			float currentVolume = Math.Max(CurrentVolume, _currentVolumeEstimate);

			float avgRate = Math.Min(_volumeTargetted - currentVolume, MaxVolumePerSecond) / MaxVolumePerSecond;

			// Don’t drop if it's been less than a second, to have correct amount emitted
			if (avgRate < _currentEmitterRate && _emitterLifeTimer < 1f) return;
			_currentEmitterRate = avgRate;
			_emitterLifeTimer = 0f;
		}

		private static float GaussianPulseWrapped(float phase01, float center01, float width01)
		{
			// Short, wrapped distance on the unit circle
			float distance = phase01 - center01;
			float wrappedDistance = distance - (float)Math.Floor(distance);
			wrappedDistance = Math.Abs(wrappedDistance);
			wrappedDistance = Math.Min(wrappedDistance, 1f - wrappedDistance); // shortest distance around the circle

			// Gaussian with sigma ~ width01/2 (tweak factor 2.0 for sharpness)
			float sigma = Math.Max(1e-4f, width01 * 0.5f);
			float g = (float)Math.Exp(-(wrappedDistance * wrappedDistance) / (2f * sigma * sigma));
			return g;
		}

		// Raises v to power k but keeps 0..1 clamped
		private static float PowSaturate(float v, float k)
		{
			v = v < 0f ? 0f : (v > 1f ? 1f : v);
			return (float)Math.Pow(v, k);
		}

		private static float SmoothTowards(float current, float target, float responsiveness, float dt)
		{
			double k = Math.Max(0.0, responsiveness);
			double a = 1.0 - Math.Exp(-k * dt);
			return current + (float)((target - current) * a);
		}

		private static float EaseInOut(float x)
		{
			x = MathF.Clamp(x, 0f, 1f);
			return (x < 0.5f) ? 4f * x * x * x : 1f - (float)Math.Pow(-2f * x + 2f, 3f) / 2f;
		}
	}
}
