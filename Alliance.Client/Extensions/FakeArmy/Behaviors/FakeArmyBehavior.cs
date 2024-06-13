using Alliance.Common.Extensions.FakeArmy.NetworkMessages.FromServer;
using System;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.FakeArmy.Behaviors
{
	public class FakeArmyBehavior : MissionNetwork
	{
		private static readonly string FAKE_ARMY_EMITER_NAME = "uruk_army_walking";

		private int nbTickedOfTeam = Int32.MaxValue;
		private int nbTicketMax = Int32.MaxValue;
		private GameEntity particuleEntity;
		private ParticleSystem particule;
		private bool isInit = false;
		private float timer;

		public int NbTickets
		{
			private get
			{
				return nbTickedOfTeam;
			}

			set
			{
				nbTickedOfTeam = value;
				UpdateParticuleIntensity();
			}
		}

		public FakeArmyBehavior() { }

		public void Init(Vec3 position, int maxTickets, int currentTickets)
		{
			if (isInit)
			{
				Log($"Behavior already init. Update of current tickets.", LogLevel.Debug);
				NbTickets = currentTickets;
				return;
			}

			if (maxTickets == 0)
			{
				Log("Something went wrong. MaxTicket CANT BE EQUAL 0", LogLevel.Error);
				return;
			}

			isInit = true;
			nbTicketMax = maxTickets;
			particuleEntity = GameEntity.CreateEmpty(Mission.Current.Scene);
			MatrixFrame frame = MatrixFrame.Identity;
			particule = ParticleSystem.CreateParticleSystemAttachedToEntity(FAKE_ARMY_EMITER_NAME, particuleEntity, ref frame);
			MatrixFrame globalFrame = new MatrixFrame(Mat3.Identity, position);
			particuleEntity.SetGlobalFrame(globalFrame);
			Log($"Created emitter at {position}", LogLevel.Debug);

			NbTickets = currentTickets;
			//PeriodiclyUpdateFakeArmy();
		}

		public override void OnMissionTick(float dt)
		{
			if (!isInit) return;

			timer += dt;
			if (timer <= 15) return;
			timer = 0;

			Log("Ask server to refresh nbTickedOfTeam");
			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new GetTicketsOfTeamRequest());
			GameNetwork.EndModuleEventAsClient();
		}

		/// <summary>
		/// By default, intensity is equal 1 which mean emitter will emit at max capacity.
		/// </summary>
		/// <param name="intensityMultiplier"></param>
		public void UpdateParticuleIntensity()
		{
			float ratio = NbTickets / (float)nbTicketMax;
			float newIntensity = ratio;

			if (ratio < 0.8) newIntensity = ratio * 0.6f;
			if (newIntensity < 0.1) newIntensity = 0;
			if (newIntensity > 1) newIntensity = 1;
			Log("newIntensity = " + newIntensity);
			particule.SetRuntimeEmissionRateMultiplier(newIntensity);
		}

		public void UpdateTicketValue(int currentTickets)
		{
			NbTickets = currentTickets;
		}

		//public async void PeriodiclyUpdateFakeArmy()
		//{
		//	await Task.Delay(15000);

		//	Log("Ask server to refresh nbTickedOfTeam");

		//	GameNetwork.BeginModuleEventAsClient();
		//	GameNetwork.WriteMessage(new GetTicketsOfTeamRequest());
		//	GameNetwork.EndModuleEventAsClient();
		//	//PeriodiclyUpdateFakeArmy();
		//}
	}
}
