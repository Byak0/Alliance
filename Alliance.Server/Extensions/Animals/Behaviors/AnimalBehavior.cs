using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace Alliance.Server.Extensions.Animals.Behaviors
{
	/// <summary>
	/// Manage animal spawning (like in singleplayer).
	/// </summary>
	public class AnimalBehavior : MissionNetwork
	{
		private static int _disabledFaceId = -1;
		private static int _disabledFaceIdForAnimals = 1;

		public override void AfterStart()
		{
			base.AfterStart();
			SpawnHorses();
			SpawnSheeps();
			SpawnCows();
			SpawnHogs();
			SpawnGeese();
			SpawnChicken();
		}

		public static void SpawnSheeps()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_sheep"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				ItemRosterElement itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("sheep"), 0, null);
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Mission mission = Mission.Current;
				ItemRosterElement itemRosterElement2 = itemRosterElement;
				ItemRosterElement itemRosterElement3 = default(ItemRosterElement);
				Vec2 asVec = globalFrame.rotation.f.AsVec2;
				Agent agent = mission.SpawnMonster(itemRosterElement2, itemRosterElement3, globalFrame.origin, asVec, -1);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceId);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceIdForAnimals);
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnCows()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_cow"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				ItemRosterElement itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("cow"), 0, null);
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Mission mission = Mission.Current;
				ItemRosterElement itemRosterElement2 = itemRosterElement;
				ItemRosterElement itemRosterElement3 = default(ItemRosterElement);
				Vec2 asVec = globalFrame.rotation.f.AsVec2;
				Agent agent = mission.SpawnMonster(itemRosterElement2, itemRosterElement3, globalFrame.origin, asVec, -1);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceId);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceIdForAnimals);
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnGeese()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_goose"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				ItemRosterElement itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("goose"), 0, null);
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Mission mission = Mission.Current;
				ItemRosterElement itemRosterElement2 = itemRosterElement;
				ItemRosterElement itemRosterElement3 = default(ItemRosterElement);
				Vec2 asVec = globalFrame.rotation.f.AsVec2;
				Agent agent = mission.SpawnMonster(itemRosterElement2, itemRosterElement3, globalFrame.origin, asVec, -1);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceId);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceIdForAnimals);
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnChicken()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_chicken"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				ItemRosterElement itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("chicken"), 0, null);
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Mission mission = Mission.Current;
				ItemRosterElement itemRosterElement2 = itemRosterElement;
				ItemRosterElement itemRosterElement3 = default(ItemRosterElement);
				Vec2 asVec = globalFrame.rotation.f.AsVec2;
				Agent agent = mission.SpawnMonster(itemRosterElement2, itemRosterElement3, globalFrame.origin, asVec, -1);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceId);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceIdForAnimals);
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static void SpawnHogs()
		{
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_hog"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				ItemRosterElement itemRosterElement = new ItemRosterElement(Game.Current.ObjectManager.GetObject<ItemObject>("hog"), 0, null);
				globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
				Mission mission = Mission.Current;
				ItemRosterElement itemRosterElement2 = itemRosterElement;
				ItemRosterElement itemRosterElement3 = default(ItemRosterElement);
				Vec2 asVec = globalFrame.rotation.f.AsVec2;
				Agent agent = mission.SpawnMonster(itemRosterElement2, itemRosterElement3, globalFrame.origin, asVec, -1);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceId);
				SetAgentExcludeFaceGroupIdAux(agent, _disabledFaceIdForAnimals);
				AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
				SimulateAnimalAnimations(agent);
			}
		}

		public static List<Agent> SpawnHorses()
		{
			List<Agent> list = new List<Agent>();
			foreach (GameEntity gameEntity in Mission.Current.Scene.FindEntitiesWithTag("sp_horse"))
			{
				MatrixFrame globalFrame = gameEntity.GetGlobalFrame();
				string text = gameEntity.Tags[1];
				ItemObject @object = MBObjectManager.Instance.GetObject<ItemObject>(text);
				ItemRosterElement itemRosterElement = new ItemRosterElement(@object, 1, null);
				ItemRosterElement itemRosterElement2 = default(ItemRosterElement);
				if (@object.HasHorseComponent)
				{
					globalFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
					Mission mission = Mission.Current;
					ItemRosterElement itemRosterElement3 = itemRosterElement;
					ItemRosterElement itemRosterElement4 = itemRosterElement2;
					Vec2 asVec = globalFrame.rotation.f.AsVec2;
					Agent agent = mission.SpawnMonster(itemRosterElement3, itemRosterElement4, globalFrame.origin, asVec, -1);
					AnimalSpawnSettings.CheckAndSetAnimalAgentFlags(gameEntity, agent);
					SimulateAnimalAnimations(agent);
					list.Add(agent);
				}
			}
			return list;
		}

		private static void SimulateAnimalAnimations(Agent agent)
		{
			int num = 10 + MBRandom.RandomInt(90);
			for (int i = 0; i < num; i++)
			{
				agent.TickActionChannels(0.1f);
				Vec3 vec = agent.ComputeAnimationDisplacement(0.1f);
				if (vec.LengthSquared > 0f)
				{
					agent.TeleportToPosition(agent.Position + vec);
				}
				agent.AgentVisuals.GetSkeleton().TickAnimations(0.1f, agent.AgentVisuals.GetGlobalFrame(), true);
			}
		}

		private static void SetAgentExcludeFaceGroupIdAux(Agent agent, int _disabledFaceId)
		{
			if (_disabledFaceId != -1)
			{
				agent.SetAgentExcludeStateForFaceGroupId(_disabledFaceId, true);
			}
		}
	}
}
