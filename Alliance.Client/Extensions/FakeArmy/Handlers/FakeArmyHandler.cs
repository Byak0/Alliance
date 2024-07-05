using Alliance.Common.Extensions;
using Alliance.Common.Extensions.FakeArmy.Behaviors;
using Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer;
using Alliance.Common.Utilities;
using TaleWorlds.MountAndBlade;

namespace Alliance.Client.Extensions.FakeArmy.Handlers
{
	/// <summary>
	/// All request related to FakeArmy send by SERVER will be handled here
	/// </summary>
	public class FakeArmyHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<GetTicketsOfTeamAnswerMessage>(HandleGetTicketsOfTeamMessage);
			reg.Register<InitFakeArmyMessage>(InitFakeArmy);
		}

		/// <summary>
		/// Server request to init FakeArmy
		/// </summary>
		/// <param name="message"></param>
		public void InitFakeArmy(InitFakeArmyMessage message)
		{
			// Server request to init FakeArmy
			FakeArmyBehavior fakeArmyBehavior = Mission.Current.GetMissionBehavior<FakeArmyBehavior>();
			if (fakeArmyBehavior == null)
			{
				Logger.Log("fakeArmyBehavior do not exist. Request aborded");
				return;
			}

			fakeArmyBehavior.Init(message.PositionToSpawnEmitter, message.MaxNumberOfTickets, message.CurrentNumberOfTickets);
		}

		/// <summary>
		/// Server answer to request from client by sending number of tickets
		/// </summary>
		/// <param name="message"></param>
		public void HandleGetTicketsOfTeamMessage(GetTicketsOfTeamAnswerMessage message)
		{
			// Server answered and we have the current number of tickets.
			FakeArmyBehavior fakeArmyBehavior = Mission.Current.GetMissionBehavior<FakeArmyBehavior>();
			if (fakeArmyBehavior == null)
			{
				Logger.Log("fakeArmyBehavior do not exist. Request aborded");
				return;
			}

			fakeArmyBehavior.UpdateTicketValue(message.NbTickets);
		}
	}
}
