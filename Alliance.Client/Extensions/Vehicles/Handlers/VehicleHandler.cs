using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromServer;
using Alliance.Common.Extensions.Vehicles.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.Vehicles.Handlers
{
    public class VehicleHandler : IHandlerRegister
    {
        public VehicleHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<CS_VehicleSyncForward>(HandleVehicleSyncForward);
            reg.Register<CS_VehicleSyncBackward>(HandleVehicleSyncBackward);
            reg.Register<CS_VehicleSyncUpward>(HandleVehicleSyncUpward);
            reg.Register<CS_VehicleSyncDownward>(HandleVehicleSyncDownward);
            reg.Register<CS_VehicleSyncTurnLeft>(HandleVehicleSyncTurnLeft);
            reg.Register<CS_VehicleSyncTurnRight>(HandleVehicleSyncTurnRight);
            reg.Register<CS_VehicleSyncLight>(HandleVehicleSyncLight);
            reg.Register<CS_VehicleSyncHonk>(HandleVehicleSyncHonk);
        }

        public void HandleVehicleSyncForward(CS_VehicleSyncForward message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestMoveForward(message.Move);
            }
        }

        public void HandleVehicleSyncBackward(CS_VehicleSyncBackward message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestMoveBackward(message.Move);
            }
        }

        public void HandleVehicleSyncUpward(CS_VehicleSyncUpward message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestMoveUpward(message.Move);
            }
        }

        public void HandleVehicleSyncDownward(CS_VehicleSyncDownward message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestMoveDownward(message.Move);
            }
        }

        public void HandleVehicleSyncTurnLeft(CS_VehicleSyncTurnLeft message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestTurnLeft(message.Turn);
            }
        }

        public void HandleVehicleSyncTurnRight(CS_VehicleSyncTurnRight message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestTurnRight(message.Turn);
            }
        }

        public void HandleVehicleSyncLight(CS_VehicleSyncLight message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Car vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Car>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestLight(message.LightOn);
            }
        }

        public void HandleVehicleSyncHonk(CS_VehicleSyncHonk message)
        {
            MissionObject vehicle = Mission.MissionNetworkHelper.GetMissionObjectFromMissionObjectId(message.MissionObjectId);
            CS_Car vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Car>();

            if (vehicleScript != null)
            {
                vehicleScript.RequestHonk();
            }
        }
    }
}
