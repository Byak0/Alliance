using Alliance.Common.Core.Security.Extension;
using Alliance.Common.Extensions;
using Alliance.Common.GameModes.Story.NetworkMessages.FromClient;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.GameModes.Story.Handlers
{
	public class StoryHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<RequestStopScenario>(HandleRequestStopScenario);
			reg.Register<RequestSetWinner>(HandleRequestSetWinner);
		}

		public bool HandleRequestStopScenario(NetworkCommunicator peer, RequestStopScenario message)
		{
			if (!peer.IsAdmin())
			{
				Log($"ATTENTION : {peer.UserName} is requesting to stop the scenario despite not being admin !", LogLevel.Error);
				return false;
			}
			Log($"{peer.UserName} is stopping scenario !", LogLevel.Information);
			ScenarioManagerServer.Instance.StopScenario();
			return true;
		}

		public bool HandleRequestSetWinner(NetworkCommunicator peer, RequestSetWinner message)
		{
			if (!peer.IsAdmin())
			{
				Log($"ATTENTION : {peer.UserName} is requesting to set the winner despite not being admin !", LogLevel.Error);
				return false;
			}
			Log($"{peer.UserName} is forcing win for {message.Winner} !", LogLevel.Information);
			ScenarioManagerServer.Instance.SetWinner(message.Winner);
			return true;
		}
	}
}
