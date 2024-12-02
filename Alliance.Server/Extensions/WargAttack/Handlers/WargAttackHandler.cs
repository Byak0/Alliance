using Alliance.Common.Extensions;
using Alliance.Common.Extensions.WargAttack.NetworkMessages.FromClient;
using Alliance.Server.Extensions.WargAttack.Utilities;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.WargAttack.Handlers
{
	public class WargAttackHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestWargAttack>(HandleWargAttack);
		}

		public bool HandleWargAttack(NetworkCommunicator peer, RequestWargAttack attack)
		{
			Agent warg = peer.ControlledAgent?.MountAgent;

			if (warg == null)
			{
				return false;
			}

			warg.WargAttack();

			return true;
		}
	}
}