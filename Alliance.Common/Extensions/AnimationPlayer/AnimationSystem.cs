using Alliance.Common.Extensions.AnimationPlayer.Models;
using Alliance.Common.Extensions.AnimationPlayer.NetworkMessages.FromServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AnimationPlayer
{
	/// <summary>
	/// Handle animation of agents and their synchronization.    
	/// </summary>
	public class AnimationSystem
	{
		// Store every animations
		public Dictionary<int, List<MBActionSet>> IndexToActionSetDictionary;
		public Dictionary<int, ActionIndexCache> IndexToActionDictionary;
		public Dictionary<int, float> IndexToDurationDictionary;
		public List<Animation> DefaultAnimations;

		private static AnimationSystem _instance;
		private bool _initialized;

		public static AnimationSystem Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = new AnimationSystem();
				}
				return _instance;
			}
		}

		public AnimationSystem()
		{
		}

		public void Init()
		{
			if (_initialized)
			{
				return;
			}

			IndexToActionSetDictionary = new Dictionary<int, List<MBActionSet>>();
			IndexToActionDictionary = new Dictionary<int, ActionIndexCache>();
			IndexToDurationDictionary = new Dictionary<int, float>();
			DefaultAnimations = new List<Animation>();

			// Initialize all Actions and their respective ActionSet
			int totalActions = MBAnimation.GetNumActionCodes();
			int uniqueAnimationIndex = 0;

			// Add the default act_none to all action_sets
			ActionIndexCache defaultActionIndex = ActionIndexCache.act_none;
			List<MBActionSet> allActionSets = new List<MBActionSet>();
			for (int j = 0; j < MBActionSet.GetNumberOfActionSets(); j++)
			{
				allActionSets.Add(MBActionSet.GetActionSetWithIndex(j));
			}
			IndexToActionDictionary.Add(uniqueAnimationIndex, defaultActionIndex);
			IndexToActionSetDictionary.Add(uniqueAnimationIndex, allActionSets);
			uniqueAnimationIndex++;

			// Add other actions and their compatible action_sets
			for (int i = 0; i < totalActions; i++)
			{
				string actionName = MBAnimation.GetActionNameWithCode(i);
				ActionIndexCache actionIndex = ActionIndexCache.Create(actionName);
				List<MBActionSet> actionSets = new List<MBActionSet>();
				for (int j = 0; j < MBActionSet.GetNumberOfActionSets(); j++)
				{
					MBActionSet actionSet = MBActionSet.GetActionSetWithIndex(j);
					if (MBActionSet.CheckActionAnimationClipExists(actionSet, actionIndex))
					{
						actionSets.Add(actionSet);
					}
				}
				IndexToActionDictionary.Add(uniqueAnimationIndex, actionIndex);
				IndexToActionSetDictionary.Add(uniqueAnimationIndex, actionSets);
				uniqueAnimationIndex++;
			}

			// Initialize default store
			AnimationDefaultStore.Instance.Init();
			// Compare default store to number of actions to determine whether we can use it or it needs a refresh
			bool refreshDefaultDurations = AnimationDefaultStore.Instance.DefaultDurations.Count != IndexToActionSetDictionary.Count;
			if (refreshDefaultDurations)
			{
				Log($"WARNING - The animations needs to be refreshed, this can lead to crash due to native instability", LogLevel.Warning);
				AnimationDefaultStore.Instance.DefaultDurations = new List<float>();
			}

			for (int i = 0; i < uniqueAnimationIndex; i++)
			{
				if (refreshDefaultDurations)
				{
					//!\\ This call is randomly prone to AccessViolationException
					float duration = MBActionSet.GetActionAnimationDuration(IndexToActionSetDictionary[i].First(), IndexToActionDictionary[i]);
					AnimationDefaultStore.Instance.DefaultDurations.Add(duration);
				}
				IndexToDurationDictionary.Add(i, AnimationDefaultStore.Instance.DefaultDurations[i]);
				DefaultAnimations.Add(new Animation(i, IndexToActionDictionary[i], IndexToActionSetDictionary[i], "", 1f, AnimationDefaultStore.Instance.DefaultDurations[i]));
			}

			if (refreshDefaultDurations)
			{
				AnimationDefaultStore.Instance.Serialize();
				Log($"WARNING - The animations were refreshed successfully, please save Animations/DefaultAnimations.xml", LogLevel.Warning);
			}

			Log("Alliance - Loaded " + Instance.DefaultAnimations.Count + " animations.", LogLevel.Debug);

			AnimationUserStore.Instance.Init();

			_initialized = true;
		}

		/// <summary>
		/// Play animation on specified agent.
		/// </summary>
		/// <param name="synchronize">Set to true to synchronize with all clients</param>
		public void PlayAnimation(Agent agent, Animation animation, bool synchronize = false)
		{
			Log($"Alliance - Playing animation {animation.Name} for player {agent.Name}", LogLevel.Debug);
			// TODO : add part to hold item in hand       

			bool correctActionSet = true;
			// If animation requires a different ActionSet, apply the correct one to agent
			if (GameNetwork.IsServer && !animation.ActionSets.Contains(agent.ActionSet) && agent.ActionSet.IsValid)
			{
				try
				{
					Log($"Alliance - ActionSet of {agent.Name} is incorrect : {agent.ActionSet.GetName()} instead of required {animation.ActionSets.First().GetName()} ({animation.ActionSets.Count} total sets compatible)", LogLevel.Warning);
					MBActionSet newActionSet = MBActionSet.InvalidActionSet;

					if (agent.ActionSet.GetName().Contains("human"))
					{
						newActionSet = animation.ActionSets.Find(actionSet => actionSet.GetName().Contains("human"));
					}
					else
					{
						newActionSet = animation.ActionSets.First();
					}

					if (newActionSet.IsValid)
					{
						AnimationSystemData animationSystemData = agent.Monster.FillAnimationSystemData(newActionSet, agent.Character.GetStepSize(), false);
						agent.SetActionSet(ref animationSystemData);
						Log($"Alliance - Fixed ActionSet of {agent.Name} to {agent.ActionSet.GetName()}", LogLevel.Warning);
					}
					else
					{
						Log($"Alliance - Didn't find any alternative valid ActionSet for {agent.Name} ({agent.ActionSet.GetName()})", LogLevel.Error);
						correctActionSet = false;
					}
				}
				catch (Exception ex)
				{
					Log($"Alliance - Failed to fix ActionSet of {agent.Name} ({agent.ActionSet.GetName()})", LogLevel.Error);
					Log(ex.Message, LogLevel.Error);
					correctActionSet = false;
				}
			}

			if (correctActionSet)
			{
				try
				{
					agent.SetActionChannel(0, animation.Action, true, 0UL, 0f, animation.Speed, -0.2f, 0.4f, 0f, false, -0.2f, 0, true);
					if (GameNetwork.IsServer && synchronize)
					{
						GameNetwork.BeginBroadcastModuleEvent();
						GameNetwork.WriteMessage(new SyncAnimation(agent, animation.Index, animation.Speed));
						GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
					}
				}
				catch (Exception ex)
				{
					Log($"Alliance - Failed to play animation {animation.Action?.Name} on agent {agent.Name}", LogLevel.Error);
					Log(ex.Message, LogLevel.Error);
				}
			}
		}

		/// <summary>
		/// Play animation on specified formation with a random wait time for each agent.
		/// </summary>
		/// <param name="synchronize">Set to true to synchronize with all clients</param>
		public void PlayAnimationForFormationAsync(Formation formation, Animation animation, bool synchronize = false)
		{
			foreach (Agent agent in formation.GetUnitsWithoutDetachedOnes())
			{
				PlayAnimationAsync(agent, animation, false);
			}
			if (GameNetwork.IsServer && synchronize)
			{
				GameNetwork.BeginBroadcastModuleEvent();
				GameNetwork.WriteMessage(new SyncAnimationFormation(formation, animation.Index, animation.Speed));
				GameNetwork.EndBroadcastModuleEvent(GameNetwork.EventBroadcastFlags.AddToMissionRecord);
			}
		}

		/// <summary>
		/// Play animation on specified agent with a random wait time.
		/// </summary>
		/// <param name="synchronize">Set to true to synchronize with all clients</param>
		private async void PlayAnimationAsync(Agent agent, Animation animation, bool synchronize = false)
		{
			await Task.Delay(MBRandom.RandomInt(0, 500));
			PlayAnimation(agent, animation, synchronize);
		}
	}
}
