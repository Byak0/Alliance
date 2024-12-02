using Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer;
using Alliance.Server.GameModes.Story.Behaviors;
using Alliance.Server.GameModes.Story.Behaviors.SpawningStrategy;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using Logger = Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.FakeArmy.Behaviors
{
	public class FakeArmyBehavior : MissionNetwork
	{
		private bool isReady = false;
		private SpawningStrategyBase spawningStrategy;
		private BattleSideEnum side = BattleSideEnum.None;

		/// <summary>
		/// Should only be set by handler when ADMIN request to start fake army
		/// </summary>
		public bool IsReady { get { return isReady; } private set { isReady = value; } }
		public Vec3 PositionToSpawnEmitterForClient { get; private set; }
		public int MaxTicket { get; private set; }

		public FakeArmyBehavior() { }

		protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
		{
			if (!isReady) return;

			GameNetwork.BeginModuleEventAsServer(networkPeer);
			GameNetwork.WriteMessage(new InitFakeArmyMessage(PositionToSpawnEmitterForClient, GetMaxTickets(), GetCurrentTickets()));
			GameNetwork.EndModuleEventAsServer();
		}

		/// <summary>
		/// <b>IMPORTANT</b> currently, tickets will be checked depending on the 
		/// side of the player who called the console command to init the fake army
		/// </summary>
		/// <param name="side"></param>
		public void Init(BattleSideEnum side)
		{
			if (IsReady)
			{
				return;
			};

			var toto = Mission.Current.GetMissionBehavior<ScenarioBehavior>()?.SpawningBehavior?.SpawningStrategy;

			if (toto == null || toto is not SpawningStrategyBase spawningStrategyBase)
			{
				Logger.Log("This mission do not contain SpawningStrategyBase behavior. FakeArmy can't be init");
				return;
			}

			spawningStrategy = spawningStrategyBase;

			IsReady = true;

			int nbTickets = spawningStrategy.GetRemainingLives(side);
			this.side = side;

			MaxTicket = nbTickets;
			PositionToSpawnEmitterForClient = new Vec3(282, 913, 2.13f);
		}

		public int GetMaxTickets() { return MaxTicket; }

		/// <summary>
		/// Return current amount of lives for this side
		/// </summary>
		/// <param name="side"></param>
		public int GetCurrentTickets()
		{
			return spawningStrategy.GetRemainingLives(side);
		}
	}
}
