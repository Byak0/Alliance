using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Core
{
	public static class ServerCoreMsg
	{
		/// <summary>
		/// Sends message to targetPeer in order to request target to move its camera to wanted location
		/// </summary>
		/// <param name="targetFrame">Wanted frame</param>
		/// <param name="targetPeer">Client where its camera should set its matrixFrame</param>
		public static void SendClientCameraPosition(MatrixFrame targetFrame, NetworkCommunicator targetPeer)
		{
			GameNetwork.BeginModuleEventAsServer(targetPeer);
			GameNetwork.WriteMessage(new SetClientCameraFrame(targetFrame));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
