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

		public Dictionary<NetworkCommunicator, MatrixFrame> CameraPositions => _cameraPositions;

		private Dictionary<NetworkCommunicator, MatrixFrame> _cameraPositions = new();

		/// <summary>
		/// Server method - Handle update of camera position by player.
		/// </summary>
		public bool HandleUpdateCameraPosition(NetworkCommunicator peer, RequestUpdateCameraPosition message)
		{
			if (peer != null && message.MatrixFrame != MatrixFrame.Zero)
			{
				_cameraPositions[peer] = message.MatrixFrame;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Client method - Request update of camera position.
		/// </summary>
		public void UpdateCameraPosition(MatrixFrame matrixFrame)
		{
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestUpdateCameraPosition(matrixFrame));
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
