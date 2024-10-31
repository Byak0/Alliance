using Alliance.Common.GameModes.Story.Scripts;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Behaviors
{
	/// <summary>
	/// Behavior keeping track of useful informations for conditions 
	/// </summary>
	public class ConditionsBehavior : MissionNetwork, IMissionBehavior
	{
		public override void OnBehaviorInitialize()
		{
			List<GameEntity> triggerActionEntities = new List<GameEntity>();
			Mission.Current.Scene.GetAllEntitiesWithScriptComponent<AL_TriggerAction>(ref triggerActionEntities);

			foreach (GameEntity triggerActionEntity in triggerActionEntities)
			{
				foreach (AL_TriggerAction triggerAction in triggerActionEntity.GetScriptComponents<AL_TriggerAction>())
				{
					triggerAction.Init();
				}
			}
		}

		public event Action<Agent> UpdateAgentDeathCondition;

		public override void OnAgentRemoved(Agent victim, Agent affectorAgent, AgentState agentState, KillingBlow blow)
		{
			if (victim?.Team != null)
			{
				UpdateAgentDeathCondition?.Invoke(victim);
			}
		}
	}
}