using Alliance.Common.Extensions.SAE.Models;
using Alliance.Common.Extensions.SAE.NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.SAE
{
	internal static class SaeMsg
	{
		public static void RequestClientToCreateMarkers(NetworkCommunicator target, List<SaeMarkerWithIdAndPos> groupList)
		{
			GameNetwork.BeginModuleEventAsServer(target);
			GameNetwork.WriteMessage(new SaeCreateMarkersNetworkServerMessage(groupList));
			GameNetwork.EndModuleEventAsServer();
		}
	}
}
