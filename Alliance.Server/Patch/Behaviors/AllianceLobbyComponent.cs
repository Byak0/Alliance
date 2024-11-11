using NetworkMessages.FromServer;
using System;
using System.Linq;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.DedicatedCustomServer;
using static Alliance.Common.Utilities.Logger;
using BotData = NetworkMessages.FromServer.BotData;

namespace Alliance.Server.Patch.Behaviors
{
	public class AllianceLobbyComponent : MissionCustomGameServerComponent
	{
		protected override void OnBotDies(Agent botAgent, MissionPeer affectorPeer, MissionPeer assistorPeer)
		{
			if (assistorPeer != null)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new KillDeathCountChange(assistorPeer.GetNetworkPeer(), affectorPeer?.GetNetworkPeer(), assistorPeer.KillCount, assistorPeer.AssistCount, assistorPeer.DeathCount, assistorPeer.Score));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}

			if (botAgent == null)
			{
				return;
			}

			MissionScoreboardComponent _missionScoreboardComponent = Mission.GetMissionBehavior<MissionScoreboardComponent>();
			MissionScoreboardComponent.MissionScoreboardSide sideSafe = _missionScoreboardComponent.GetSideSafe(botAgent.Team.Side);

			TaleWorlds.MountAndBlade.BotData botScores = sideSafe.BotScores;

			// Remove bot from alive count in every case
			botScores.AliveCount--;
			if (botScores.AliveCount >= 2048 || botScores.AliveCount < 0)
			{
				Log($"ERROR in AllianceLobbyComponent : AliveCount out of bounds({botScores.AliveCount})", LogLevel.Error);
			}

			if (botAgent.Formation?.PlayerOwner != null)
			{
				NetworkCommunicator networkCommunicator = GameNetwork.NetworkPeers.SingleOrDefault((x) => x.GetComponent<MissionPeer>() != null && x.GetComponent<MissionPeer>().ControlledFormation == botAgent.Formation);
				if (networkCommunicator != null)
				{
					MissionPeer component = networkCommunicator.GetComponent<MissionPeer>();
					typeof(MissionPeer).GetProperty(nameof(MissionPeer.DeathCount)).SetValue(component, component.DeathCount + 1);
					// Check that BotsUnderControlAlive doesn't go under 0 to prevent client crash
					component.BotsUnderControlAlive = Math.Max(component.BotsUnderControlAlive - 1, 0);
					_missionScoreboardComponent.PlayerPropertiesChanged(networkCommunicator);
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new KillDeathCountChange(networkCommunicator, affectorPeer?.GetNetworkPeer(), component.KillCount, component.AssistCount, component.DeathCount, component.Score));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
					GameNetwork.BeginBroadcastModuleEvent();
					GameNetwork.WriteMessage(new BotsControlledChange(networkCommunicator, component.BotsUnderControlAlive, component.BotsUnderControlTotal));
					GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
				}
			}
			else
			{
				botScores.DeathCount++;
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new BotData(sideSafe.Side, botScores.KillCount, botScores.AssistCount, botScores.DeathCount, botScores.AliveCount));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.None);
			}

			_missionScoreboardComponent.BotPropertiesChanged(botAgent.Team.Side);
		}
	}
}
