using Alliance.Common.Extensions;
using Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromClient;
using Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer;
using Alliance.Server.Extensions.FakeArmy.Behaviors;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace Alliance.Server.Extensions.FakeArmy.Handlers
{
	/// <summary>
	/// All request related to FakeArmy send by CLIENT will be handled here
	/// </summary>
	public class FakeArmyHandler : IHandlerRegister
	{
		public void Register(GameNetwork.NetworkMessageHandlerRegisterer reg)
		{
			reg.Register<GetTicketsOfTeamRequest>(GetCurrentTicketOfSide);
			reg.Register<StartFakeArmyMessage>(InitFakeArmyFromCommand);
		}

		/// <summary>
		/// DELETE ME
		/// </summary>
		/// <param name="peer"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public bool InitFakeArmy(NetworkCommunicator peer, StartFakeArmyMessage message)
		{
			// Client request to Init FakeArmy
			FakeArmyBehavior fakeArmyBehavior = Mission.Current.GetMissionBehavior<FakeArmyBehavior>();

			if (fakeArmyBehavior == null)
			{
				Debug.Print("This mission do not contain FakeArmyBehavior. Request aborded");
				return false;
			}

			if (!fakeArmyBehavior.IsReady) return false;

			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new InitFakeArmyMessage(fakeArmyBehavior.PositionToSpawnEmitterForClient, fakeArmyBehavior.GetMaxTickets(), fakeArmyBehavior.GetCurrentTickets()));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);

			return true;
		}

		/// <summary>
		/// Client send command to start FakeArmy
		/// </summary>
		/// <param name="peer"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public bool InitFakeArmyFromCommand(NetworkCommunicator peer, StartFakeArmyMessage message)
		{
			// Client send command to start FakeArmy
			FakeArmyBehavior fakeArmyBehavior = Mission.Current.GetMissionBehavior<FakeArmyBehavior>();

			if (fakeArmyBehavior == null)
			{
				Debug.Print("This mission do not contain FakeArmyBehavior. Init aborded");
				return false;
			}

			var side = peer.ControlledAgent.Team.Side;
			fakeArmyBehavior.Init(side);

			if (!fakeArmyBehavior.IsReady) return false;


			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new InitFakeArmyMessage(fakeArmyBehavior.PositionToSpawnEmitterForClient, fakeArmyBehavior.GetMaxTickets(), fakeArmyBehavior.GetCurrentTickets()));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.IncludeUnsynchronizedClients);

			return true;
		}

		/// <summary>
		/// Client request to get tickets
		/// </summary>
		/// <param name="peer"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public bool GetCurrentTicketOfSide(NetworkCommunicator peer, GetTicketsOfTeamRequest message)
		{
			FakeArmyBehavior fakeArmyBehavior = Mission.Current.GetMissionBehavior<FakeArmyBehavior>();

			if (fakeArmyBehavior == null || !fakeArmyBehavior.IsReady)
			{
				Debug.Print("This mission do not contain FakeArmyBehavior. Request aborded");
				return false;
			}

			GameNetwork.BeginModuleEventAsServer(peer);
			GameNetwork.WriteMessage(new GetTicketsOfTeamAnswerMessage(fakeArmyBehavior.GetCurrentTickets()));
			GameNetwork.EndModuleEventAsServer();

			return true;
		}
	}
}
