using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions.AdminMenu.NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

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
				_AgentTkCount[_AgentNetworkCommunicator] = (TkCount + 1, TkDamage + blow.InflictedDamage, TkKill + _TkKill);

				//Send info to client
				var singleEntry = new Dictionary<NetworkCommunicator, (int, int, int)> { [_AgentNetworkCommunicator] = (TkCount, TkDamage, TkKill) };
				NotifyClientsOfTeamKill(singleEntry);
				Log($"[AdminPanel][TP] Player {affectorAgent.Name} hit teammate {victim.Name} for {blow.InflictedDamage} damage!", LogLevel.Information);
			}
		}

		protected override void HandleNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			if (!networkPeer.IsAdmin()) return;

			// Split the dictionary if necessary
			if (_AgentTkCount.Count > 100)
			{
				Dictionary<NetworkCommunicator, (int TkCount, int TkDamage, int TkKill)> partialDict = new();
				int counter = 0;
				foreach (var kvp in _AgentTkCount)
				{
					partialDict[kvp.Key] = kvp.Value;
					counter++;
					if (counter >= 100)
					{
						GameNetwork.BeginModuleEventAsServer(networkPeer);
						GameNetwork.WriteMessage(new SyncTk(partialDict));
						GameNetwork.EndModuleEventAsServer();
						partialDict.Clear();
						counter = 0;
					}
				}
				// Send remaining entries
				if (partialDict.Count > 0)
				{
					GameNetwork.BeginModuleEventAsServer(networkPeer);
					GameNetwork.WriteMessage(new SyncTk(partialDict));
					GameNetwork.EndModuleEventAsServer();
				}
			}
			else
			{
				GameNetwork.BeginModuleEventAsServer(networkPeer);
				GameNetwork.WriteMessage(new SyncTk(_AgentTkCount));
				GameNetwork.EndModuleEventAsServer();
			}
		}

		public void NotifyClientsOfTeamKill(Dictionary<NetworkCommunicator, (int TkCount, int TkDamage, int TkKill)> AgentTkCountToSend)
		{
			foreach (var kvp in GameNetwork.NetworkPeers)
			{
				if (kvp.IsAdmin())
				{
					GameNetwork.BeginModuleEventAsServer(kvp);
					GameNetwork.WriteMessage(new SyncTk(AgentTkCountToSend));
					GameNetwork.EndModuleEventAsServer();
				}
			}
		}
	}
}