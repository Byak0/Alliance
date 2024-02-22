using System.ComponentModel;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    public class CS_Boat : CS_Vehicle
    {
        private float baseBoatLevel;
        private float maxDefaultSpeed = 5f;
        private BoatPosition boatStatus = BoatPosition.IN_WATER;
        private MatrixFrame starterPosition;

        public CS_Boat()
        {
            MaxDownwardSpeed = 0f;
            MaxUpwardSpeed = 0f;
            MaxTurnAngle = 10;
            ForceDecelerate = true;

            base.CanFly = false;
        }

        void Test(object sender, PropertyChangedEventArgs e)
        {
            Utilities.Logger.Log("CALLED!");
            //TODO Uncomment me if CALLED log is called when round is reset
            //GameEntity.SetLocalPosition(starterPosition.origin);
        }

        protected override void OnInit()
        {
            base.OnInit();
            Mission.Current.OnMissionReset += this.Test;
            starterPosition = GameEntity.GetFrame();
            baseBoatLevel = FollowsTerrainPoints.First().GlobalPosition.Z;
        }

        protected override void OnTick(float dt)
        {
            MatrixFrame frame = GameEntity.GetFrame();
            if (frame.origin.Z > baseBoatLevel + 0.2f)
            {
                boatStatus = BoatPosition.FRONT_LANDED;
            }
            else if (frame.origin.Z <= baseBoatLevel + 0.1f)
            {
                boatStatus = BoatPosition.IN_WATER;
            }
            UpdateBoatOnStatus();
            //Utilities.Logger.Log("TURN = " + CurrentTurnRate);
            //Utilities.Logger.Log("Max = " + MaxTurnAngle);
            base.OnTick(dt);
        }

        private void UpdateBoatOnStatus()
        {
            switch (boatStatus)
            {
                case BoatPosition.FRONT_LANDED:
                    UpdateSpeedIfNeeded(true);
                    break;
                case BoatPosition.BACK_LANDED:
                    UpdateSpeedIfNeeded(false);
                    break;
                case BoatPosition.IN_WATER:
                    GetMaxForwardSpeed = maxDefaultSpeed;
                    GetMaxBackwardSpeed = maxDefaultSpeed;
                    break;
                default:
                    break;
            }
        }

        private void UpdateSpeedIfNeeded(bool v)
        {
            CurrentTurnRate = 0f;

            if (v)
            {
                GetMaxForwardSpeed -= 1;
                GetMaxBackwardSpeed += 1;
            }
            else
            {
                GetMaxForwardSpeed += 1;
                GetMaxBackwardSpeed -= 1;
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
