using Alliance.Common.Extensions;
using Alliance.Common.Extensions.Vehicles.NetworkMessages.FromClient;
using Alliance.Common.Extensions.Vehicles.Scripts;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.Vehicles.Handlers
{
    public class VehicleHandler : IHandlerRegister
    {
        public VehicleHandler()
        {
        }

        public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
        {
            reg.Register<CS_VehicleRequestForward>(HandleRequestForward);
            reg.Register<CS_VehicleRequestBackward>(HandleRequestBackward);
            reg.Register<CS_VehicleRequestUpward>(HandleRequestUpward);
            reg.Register<CS_VehicleRequestDownward>(HandleRequestDownward);
            reg.Register<CS_VehicleRequestTurnLeft>(HandleRequestTurnLeft);
            reg.Register<CS_VehicleRequestTurnRight>(HandleRequestTurnRight);
            reg.Register<CS_VehicleRequestLight>(HandleRequestLight);
            reg.Register<CS_VehicleRequestHonk>(HandleRequestHonk);
        }

        public bool HandleRequestForward(NetworkCommunicator peer, CS_VehicleRequestForward message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerMoveForward(message.Move);
                return true;
            }
        }

        public bool HandleRequestBackward(NetworkCommunicator peer, CS_VehicleRequestBackward message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerMoveBackward(message.Move);
                return true;
            }
        }

        public bool HandleRequestUpward(NetworkCommunicator peer, CS_VehicleRequestUpward message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerMoveUpward(message.Move);
                return true;
            }
        }

        public bool HandleRequestDownward(NetworkCommunicator peer, CS_VehicleRequestDownward message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerMoveDownward(message.Move);
                return true;
            }
        }

        public bool HandleRequestTurnLeft(NetworkCommunicator peer, CS_VehicleRequestTurnLeft message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerTurnLeft(message.Turn);
                return true;
            }
        }

        public bool HandleRequestTurnRight(NetworkCommunicator peer, CS_VehicleRequestTurnRight message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Vehicle vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Vehicle>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerTurnRight(message.Turn);
                return true;
            }
        }

        public bool HandleRequestLight(NetworkCommunicator peer, CS_VehicleRequestLight message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Car vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Car>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerToggleLight(message.LightOn);
                return true;
            }
        }

        public bool HandleRequestHonk(NetworkCommunicator peer, CS_VehicleRequestHonk message)
        {
            MissionObject vehicle = message.MissionObjectId;
            CS_Car vehicleScript = vehicle.GameEntity.GetFirstScriptOfType<CS_Car>();

            if (peer.ControlledAgent == null || vehicleScript == null || peer.ControlledAgent != vehicleScript.PilotAgent)
            {
                return false;
            }
            else
            {
                vehicleScript.ServerSyncHonk();
                return true;
            }
        }
    }
}
