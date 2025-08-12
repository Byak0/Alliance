using Alliance.Common.Extensions;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.NetworkMessages.FromServer;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.TroopSpawner.Handlers
{
	public class AgentsInfoHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<AgentsInfoMessage>(HandleServerEventAgentsInfoMessage);
		}

		public void HandleServerEventAgentsInfoMessage(AgentsInfoMessage message)
		{
			Agent agent = Mission.MissionNetworkHelper.GetAgentFromIndex(message.AgentIndex);
			switch (message.DataType)
			{
				case AgentDataType.All:
					AgentsInfoModel.Instance.AddAgentInfo(agent, message.Difficulty, message.Lives, message.SpeakingRange);
					Log($"Add agent {agent.Index} infos => Diff: {message.Difficulty}, speakRange: {message.SpeakingRange}, lives: {message.Difficulty}", LogLevel.Debug);
					break;
				case AgentDataType.Difficulty:
					AgentsInfoModel.Instance.UpdateAgentDifficulty(agent, message.Difficulty);
					Log($"Update agent {agent.Index} difficulty : {message.Difficulty}", LogLevel.Debug);
					break;
				case AgentDataType.SpeakingRange:
					AgentsInfoModel.Instance.UpdateAgentSpeakingRange(agent, message.SpeakingRange);
					Log($"Update agent {agent.Index} speaking range : {message.SpeakingRange}", LogLevel.Debug);
					break;
				case AgentDataType.Lives:
					AgentsInfoModel.Instance.UpdateAgentLives(agent, message.Lives);
					Log($"Update agent {agent.Index} lives : {message.Lives}", LogLevel.Debug);
					break;
			}
			agent.UpdateAgentProperties();
		}
	}
}