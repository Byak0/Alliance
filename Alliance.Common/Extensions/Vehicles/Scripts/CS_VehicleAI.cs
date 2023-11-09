using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.Vehicles.Scripts
{
    public sealed class CS_VehicleAI : UsableMachineAIBase
    {
        public CS_VehicleAI(CS_Vehicle vehicle)
            : base(vehicle)
        {
        }

        private CS_Vehicle Vehicle
        {
            get
            {
                return UsableMachine as CS_Vehicle;
            }
        }

        public override bool HasActionCompleted
        {
            get
            {
                return Vehicle.IsDeactivated;
            }
        }
    }
}
