using System.Collections.Generic;
using System.Linq;
using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static System.Net.Mime.MediaTypeNames;
using static Alliance.Common.Utilities.Logger;
using  Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using static TaleWorlds.MountAndBlade.MultiplayerClassDivisions;

namespace Alliance.Server.Extensions.AdminMenu.Behaviors
{
	/// <summary>
	/// TeamKillCountBehavior used to follow friendly fire during a round and send info so Admin menu
	/// </summary>
	public class TeamKillCountBehavior : MissionNetwork, IMissionBehavior
	{

		private readonly Dictionary<NetworkCommunicator, (int TkCount, int TkDamage, int TkKill)> _AgentTkCount = new Dictionary<NetworkCommunicator, (int, int, int)>();

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
		}

		public override void OnRemoveBehavior()
		{

			_AgentTkCount.Clear();
			base.OnRemoveBehavior();
		}

		public override void OnAgentHit(Agent victim, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData collisionData)
		{
			// Ignore invalid cases
			if (victim == null || affectorAgent == null || affectorAgent.MissionPeer?.GetNetworkPeer() == null)
				return;
			NetworkCommunicator _AgentNetworkCommunicator = affectorAgent.MissionPeer.GetNetworkPeer();

			if (victim.Team != null && affectorAgent != victim && !affectorAgent.Team.IsEnemyOf(victim.Team))
			{
				// Add Agent in dictionary if not already in
				if (!_AgentTkCount.ContainsKey(_AgentNetworkCommunicator))
					_AgentTkCount[_AgentNetworkCommunicator] = (0, 0, 0);

				int _TkKill = 0;
				if (victim.Health <= 0) _TkKill++;

				var (TkCount, TkDamage, TkKill) = _AgentTkCount[_AgentNetworkCommunicator];
				_AgentTkCount[_AgentNetworkCommunicator] = (TkCount + 1, TkDamage + blow.InflictedDamage, TkKill + _TkKill );

				//Send info to client
				var singleEntry = new Dictionary<NetworkCommunicator, (int, int, int)> { [_AgentNetworkCommunicator] = (TkCount, TkDamage, TkKill) };
				NotifyClientsOfTeamKill(singleEntry);
				Log($"[AdminPanel][TP] Player {affectorAgent.Name} hit teammate {victim.Name} for {blow.InflictedDamage} damage!", LogLevel.Information);
			}
		}

		protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			if (networkPeer.IsAdmin())
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new SyncTk(_AgentTkCount));
				GameNetwork.EndModuleEventAsServer();
		}
		public void NotifyClientsOfTeamKill(Dictionary<NetworkCommunicator, (int TkCount, int TkDamage, int TkKill)> AgentTkCountToSend)
		{
			foreach( var kvp in GameNetwork.NetworkPeers )
			{
				if (kvp.IsAdmin())
					GameNetwork.BeginModuleEventAsServer(kvp);
					GameNetwork.WriteMessage(new SyncTk(_AgentTkCount));
					GameNetwork.EndModuleEventAsServer();
			}
		}
	}
}