using Alliance.Common.Extensions.FlagsTracker.Scripts;
using NetworkMessages.FromServer;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Server.Extensions.FlagsTracker.Behaviors
{
	/// <summary>
	/// MissionBehavior used to keep track of flags holded by agents or dropped on the ground.
	/// </summary>
	public class FlagTrackerBehavior : MissionNetwork, IMissionBehavior
	{
		public List<FlagTracker> FlagTrackers { get; protected set; }
		public Dictionary<Agent, FlagTracker> FlagBearers { get; protected set; }
		public Dictionary<SpawnedItemEntity, FlagTracker> FlagsOnGround { get; protected set; }

		public FlagTrackerBehavior() : base()
		{
			FlagTrackers = new List<FlagTracker>();
			FlagBearers = new Dictionary<Agent, FlagTracker>();
			FlagsOnGround = new Dictionary<SpawnedItemEntity, FlagTracker>();
		}

		public override void OnBehaviorInitialize()
		{
			base.OnBehaviorInitialize();
			Mission.OnItemPickUp += OnItemPickup;
			Mission.OnItemDrop += OnItemDrop;
		}

		protected override void OnEndMission()
		{
			base.OnEndMission();
			Mission.OnItemPickUp -= OnItemPickup;
			Mission.OnItemDrop -= OnItemDrop;
		}

		public override void OnAgentBuild(Agent agent, Banner banner)
		{
			// If an agent spawns with a banner, track it
			if (agent.Equipment != null)
			{
				MissionWeapon flag = agent.Equipment[EquipmentIndex.ExtraWeaponSlot];
				if (flag.IsBanner())
				{
					FlagTracker flagTracker = new FlagTracker(agent, flag);
					FlagTrackers.Add(flagTracker);
					FlagBearers.Add(agent, flagTracker);
					Log($"{agent.Team.Side}'s team was granted a {flag.Item.Name}", LogLevel.Information);
				}
			}
		}

		public void OnItemDrop(Agent agent, SpawnedItemEntity item)
		{
			// If a FlagBearer dropped a Banner, update the flag infos
			if (item.IsBanner() && FlagBearers.ContainsKey(agent))
			{
				FlagTracker flagTracker = FlagTrackers.Find(flag => flag == FlagBearers[agent]);
				flagTracker.DropFlag(item);
				FlagBearers.Remove(agent);
				FlagsOnGround.Add(item, flagTracker);
				Log($"{agent.Team.Side}'s flag was dropped", LogLevel.Information);
			}
		}

		public void OnItemPickup(Agent agent, SpawnedItemEntity item)
		{
			// If a Flag is picked up, update the flag infos
			if (item.IsBanner() && FlagsOnGround.ContainsKey(item))
			{
				FlagTracker flagTracker = FlagTrackers.Find(flag => flag == FlagsOnGround[item]);
				flagTracker.PickupFlag(agent);
				FlagsOnGround.Remove(item);
				FlagBearers.Add(agent, flagTracker);
				Log($"Flag was picked up by {agent.Name} for {agent.Team.Side}", LogLevel.Information);
			}
		}
	}

	public class FlagTracker
	{
		public MissionWeapon Flag { get; protected set; }
		public Team Team { get; protected set; }
		public Agent FlagBearer { get; protected set; }
		public SpawnedItemEntity FlagOnGround { get; protected set; }
		public CS_CapturableZone FlagZone { get; protected set; }

		public FlagTracker(Agent agent, MissionWeapon flag)
		{
			Flag = flag;
			Team = agent.Team;
			FlagBearer = agent;

			GameEntity entity = GameEntity.Instantiate(Mission.Current.Scene, "script_capturable_zone", agent.Frame);
			CS_CapturableZone flagZone = entity.GetFirstScriptOfType<CS_CapturableZone>();
			List<MissionObjectId> list = new List<MissionObjectId>();
			GameNetwork.BeginBroadcastModuleEvent();
			GameNetwork.WriteMessage(new CreateMissionObject(flagZone.Id, "script_capturable_zone", agent.Frame, list));
			GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord, null);
			Mission.Current.AddDynamicallySpawnedMissionObjectInfo(new Mission.DynamicallyCreatedEntity("script_capturable_zone", flagZone.Id, agent.Frame, ref list));
			flagZone.SetOwner(agent.Team.Side);
			flagZone.ServerSynchronize();
			FlagZone = flagZone;
		}

		public void DropFlag(SpawnedItemEntity flag)
		{
			FlagBearer = null;
			FlagOnGround = flag;
			FlagZone?.SetBearer(null);
			FlagZone?.ServerSynchronize();
		}

		public void PickupFlag(Agent agent)
		{
			FlagOnGround = null;
			FlagBearer = agent;
			FlagZone?.SetBearer(agent);
			FlagZone?.ServerSynchronize();
		}

		public bool UnderControl()
		{
			return Team.Side == FlagZone.Owner;
		}

		public MatrixFrame GetPosition()
		{
			MatrixFrame position = new MatrixFrame();
			if (FlagBearer != null)
			{
				position.origin = FlagBearer.Position;
			}
			else if (FlagOnGround != null)
			{
				position.origin = FlagOnGround.GameEntity.GlobalPosition;
			}
			else
			{
				Log("Error - Cannot locate flag position", LogLevel.Error);
			}

			return position;
		}
	}
}