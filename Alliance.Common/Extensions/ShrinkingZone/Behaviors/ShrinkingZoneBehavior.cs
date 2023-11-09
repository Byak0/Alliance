using Alliance.Common.Extensions.ShrinkingZone.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.ShrinkingZone.Behaviors
{
    /// <summary>
    /// Handle the shrinking zone. 
    /// People outside of it will take damage.
    /// </summary>
    public class ShrinkingZoneBehavior : MissionNetwork
    {
        const int MIN_EMITTER_COUNT = 2;
        const int MAX_EMITTER_COUNT = 12;
        const string PARTICLE_NAME = "br_zone_smoke";
        const float ROTATION_SPEED = 10f;
        const float EMITTER_Z_OFFSET = 0f;

        public Vec3 ZoneOrigin;
        public float ZoneMaxRadius;
        public int ZoneLifeTime;
        public bool ZoneVisible;

        public float CurrentRadius;
        public float CurrentLifeTime;

        private bool _zoneCreated;
        private List<GameEntity> _emitters;
        private float _currentAngle;

        public ShrinkingZoneBehavior()
        {
        }

        protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
        {
            if (_zoneCreated && GameNetwork.IsServer)
            {
                GameNetwork.BeginModuleEventAsServer(networkPeer);
                GameNetwork.WriteMessage(new SyncShrinkingZone(ZoneOrigin, ZoneMaxRadius, ZoneLifeTime, ZoneVisible));
                GameNetwork.EndModuleEventAsServer();
            }
        }

        public void InitZone(Vec3 zoneOrigin, float zoneRadius, int zoneLifeTime, bool visible)
        {
            ZoneOrigin = zoneOrigin;
            ZoneMaxRadius = zoneRadius;
            ZoneLifeTime = zoneLifeTime;
            ZoneVisible = visible;

            CurrentRadius = ZoneMaxRadius;
            CurrentLifeTime = ZoneLifeTime;
            _zoneCreated = true;

            if (GameNetwork.IsServer)
            {
                GameNetwork.BeginBroadcastModuleEvent();
                GameNetwork.WriteMessage(new SyncShrinkingZone(zoneOrigin, zoneRadius, zoneLifeTime, visible));
                GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None, null);
            }
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();

            ZoneMaxRadius = 100f;
            ZoneOrigin = new Vec3();
            _emitters = new List<GameEntity>();
        }

        public override void OnMissionTick(float dt)
        {
            if (_zoneCreated && CurrentLifeTime > 0)
            {
                CurrentLifeTime -= dt;
                CurrentRadius = ZoneMaxRadius * Math.Max(CurrentLifeTime / ZoneLifeTime, 0.05f);
            }
            if (ZoneVisible && GameNetwork.IsClient)
            {
                UpdateEmittersCount();
                RotateEmitters(dt);
            }
        }

        /// <summary>
        /// Update emitters count based on zone radius.
        /// </summary>
        private void UpdateEmittersCount()
        {
            // Calculate the fraction of the current radius relative to the max radius
            float radiusFraction = CurrentRadius / ZoneMaxRadius;
            // Linearly interpolate between min and max emitter counts based on the radius fraction
            int desiredEmitterCount = MIN_EMITTER_COUNT + (int)Math.Floor((MAX_EMITTER_COUNT - MIN_EMITTER_COUNT) * radiusFraction);

            // Add or remove emitters to match desired count
            while (_emitters.Count < desiredEmitterCount)
            {
                Vec3 position = new Vec3(ZoneOrigin.x, ZoneOrigin.y, Mission.Scene.GetTerrainHeight(new Vec2(ZoneOrigin.x, ZoneOrigin.y)) + EMITTER_Z_OFFSET); // Initial position, to be updated
                GameEntity newEmitter = CreateEmitter(position);
                _emitters.Add(newEmitter);
            }
            while (_emitters.Count > desiredEmitterCount)
            {
                GameEntity lastEmitter = _emitters[_emitters.Count - 1];
                lastEmitter.Remove(0); // Safely remove the entity
                _emitters.RemoveAt(_emitters.Count - 1);
                Log($"Removed emitter", LogLevel.Debug);
            }
        }

        /// <summary>
        /// Rotate emitters around the zone origin.
        /// </summary>
        private void RotateEmitters(float dt)
        {
            if (_emitters.Count == 0) return;

            // Dynamically adjust RotationSpeed based on current emitter count
            float adjustedRotationSpeed = ROTATION_SPEED * MAX_EMITTER_COUNT / _emitters.Count;

            Log($"_currentAngle = {_currentAngle}", LogLevel.Debug);
            // Increase _currentAngle and keep it between 0 and 360
            _currentAngle += adjustedRotationSpeed * dt;
            _currentAngle = Repeat(_currentAngle, 360f);

            float angleStep = 360f / _emitters.Count;

            for (int i = 0; i < _emitters.Count; ++i)
            {
                float angle = _currentAngle + i * angleStep;
                float radianAngle = angle * Mathf.Deg2Rad;

                float x = ZoneOrigin.x + CurrentRadius * Mathf.Cos(radianAngle);
                float y = ZoneOrigin.y + CurrentRadius * Mathf.Sin(radianAngle);

                Vec3 newPosition = new Vec3(x, y, Mission.Scene.GetTerrainHeight(new Vec2(x, y)) + EMITTER_Z_OFFSET);

                MatrixFrame newFrame = new MatrixFrame(Mat3.Identity, newPosition);
                _emitters[i].SetFrame(ref newFrame);
                Log($"Moved emitter {i} to {newPosition}", LogLevel.Debug);
            }
        }

        public GameEntity CreateEmitter(Vec3 position)
        {
            GameEntity emitter = GameEntity.CreateEmpty(Mission.Current.Scene);
            MatrixFrame frame = MatrixFrame.Identity;
            ParticleSystem.CreateParticleSystemAttachedToEntity(PARTICLE_NAME, emitter, ref frame);
            MatrixFrame globalFrame = new MatrixFrame(Mat3.Identity, position);
            emitter.SetGlobalFrame(globalFrame);
            Log($"Created emitter at {position}", LogLevel.Debug);

            return emitter;
        }

        public float Repeat(float value, float max)
        {
            float result = value % max;
            if (result < 0)
            {
                result += max;
            }
            return result;
        }
    }
}