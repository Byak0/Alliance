using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions;
using Alliance.Common.Extensions.AdvancedCombat.NetworkMessages.FromClient;
using Alliance.Common.Extensions.AdvancedCombat.Utilities;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.AdvancedCombat.Handlers
{
	public class AdvancedCombatHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestSpecialAttack>(HandleSpecialAttack);
		}

		/// <summary>
		/// Handle special attack request from client, depending on the agent type.
		/// </summary>	
		public bool HandleSpecialAttack(NetworkCommunicator peer, RequestSpecialAttack attack)
		{
			Agent playerAgent = peer.ControlledAgent;

			if (playerAgent == null) return false;

			if (playerAgent.IsTroll())
			{
				// todo special troll attack
			}
			else if (playerAgent.IsEnt())
			{
				// todo special ent attack
			}
			else if (playerAgent.HasMount && playerAgent.MountAgent.IsWarg())
			{
				playerAgent.MountAgent.WargAttack();
			}
			else
			{
				return false;
			}

			return true;
		}
	}
}