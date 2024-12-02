using Alliance.Common.Extensions.RTSCamera.NetworkMessages.FromClient;
using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.RTSCamera.Models
{
    /// <summary>
    /// Model storing camera positions of players.
    /// </summary>
    public class CameraPositionsModel
    {
        public static readonly CameraPositionsModel Instance = new();

        public Dictionary<NetworkCommunicator, Vec3> CameraPositions => _cameraPositions;

        private Dictionary<NetworkCommunicator, Vec3> _cameraPositions = new();

        /// <summary>
        /// Server method - Handle update of camera position by player.
        /// </summary>
        public bool HandleUpdateCameraPosition(NetworkCommunicator peer, RequestUpdateCameraPosition message)
        {
            if (peer != null && message.Position != Vec3.Invalid)
            {
                _cameraPositions[peer] = message.Position;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Client method - Request update of camera position.
        /// </summary>
        public void UpdateCameraPosition(Vec3 position)
        {
            GameNetwork.BeginModuleEventAsClient();
            GameNetwork.WriteMessage(new RequestUpdateCameraPosition(position));
            GameNetwork.EndModuleEventAsClient();
        }
    }
}
