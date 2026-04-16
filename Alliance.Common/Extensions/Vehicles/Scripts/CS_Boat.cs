using System;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    public class CS_Boat : CS_Vehicle
    {
        private float baseBoatLevel;
        private float defaultForwardSpeed;
        private float defaultBackwardSpeed;
        private BoatPosition boatStatus = BoatPosition.IN_WATER;
        private MatrixFrame starterPosition;

        private const float MinSpeed = 0f;
        private const float MaxSpeed = 10f;

        private float ClampedForwardSpeed
        {
            get => MaxForwardSpeed;
            set => MaxForwardSpeed = MathF.Clamp(value, MinSpeed, MaxSpeed);
        }

        private float ClampedBackwardSpeed
        {
            get => MaxBackwardSpeed;
            set => MaxBackwardSpeed = MathF.Clamp(value, MinSpeed, MaxSpeed);
        }

        public CS_Boat()
        {
            MaxDownwardSpeed = 0f;
            MaxUpwardSpeed = 0f;
            MaxTurnAngle = 10;
            ForceDecelerate = true;
            CanFly = false;
        }

        void ResetPosition(object sender, PropertyChangedEventArgs e)
        {
            SyncFrame(starterPosition);
        }

		public override void AfterMissionStart()
		{
			base.AfterMissionStart();
			// Store initial speeds from parent to restore them when boat is in water
			defaultForwardSpeed = MaxForwardSpeed;
			defaultBackwardSpeed = MaxBackwardSpeed;
			Mission.Current.OnMissionReset += ResetPosition;
			starterPosition = GameEntity.GetGlobalFrame();
			baseBoatLevel = FollowsTerrainPoints.First().GlobalPosition.Z;
		}

        protected override void OnTick(float dt)
        {
            MatrixFrame frame = GameEntity.GetGlobalFrame();
            if (frame.origin.Z > baseBoatLevel + 0.09f)
            {
                boatStatus = BoatPosition.FRONT_LANDED;
            }
            else if (frame.origin.Z <= baseBoatLevel + 0.07f)
            {
                boatStatus = BoatPosition.IN_WATER;
            }
            UpdateBoatOnStatus();
            base.OnTick(dt);
        }

		public override float GetGroundHeight(Vec3 cp)
		{
			return MathF.Max(
				Scene.GetGroundHeightAtPosition(cp, BodyFlags.CommonCollisionExcludeFlags),
				Scene.GetWaterLevelAtPosition(cp.AsVec2, true, true)
			);
		}

		private void UpdateBoatOnStatus()
        {
            switch (boatStatus)
            {
                case BoatPosition.FRONT_LANDED:
                    UpdateSpeedIfNeeded(isFrontLanded: true);
                    break;
                case BoatPosition.BACK_LANDED:
                    UpdateSpeedIfNeeded(isFrontLanded: false);
                    break;
                case BoatPosition.IN_WATER:
                    MaxForwardSpeed = defaultForwardSpeed;
                    MaxBackwardSpeed = defaultBackwardSpeed;
                    DecelerationRate = 0.5f;
                    break;
                default:
                    break;
            }
        }

        private void UpdateSpeedIfNeeded(bool isFrontLanded)
        {
            CurrentTurnRate = 0f;
            if (MaxForwardSpeed == 0)
            {
                DecelerationRate = 8f;
            }

            if (isFrontLanded)
            {
                ClampedForwardSpeed -= 1;
                ClampedBackwardSpeed += 1;
            }
            else
            {
                ClampedForwardSpeed += 1;
                ClampedBackwardSpeed -= 1;
            }
        }

		private enum BoatPosition
        {
            IN_WATER,
            FRONT_LANDED,
            BACK_LANDED
        }
    }
}
