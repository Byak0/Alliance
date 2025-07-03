using Alliance.Common.Extensions;
using Alliance.Common.Extensions.PlayerSpawn.Handlers;
using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.PlayerSpawn.Handlers
{
	public class PlayerSpawnClientHandler : PlayerSpawnMenuHandlerBase, IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<SyncPlayerSpawnMenu>(HandlePlayerSpawnMenuOperation);
		}
	}
}
