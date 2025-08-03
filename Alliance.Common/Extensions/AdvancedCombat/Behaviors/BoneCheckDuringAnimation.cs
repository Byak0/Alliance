using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.AdvancedCombat.Behaviors
{
	/// <summary>
	/// Temporary class checking for collision between an agent and its targets. Removed after action end.
	/// </summary>
	public class BoneCheckDuringAnimation : BoneCheck
	{
		private ActionIndexCache _action;
		private float _actionProgressMin;
		private float _actionProgressMax;

		// Constructor callback
		public BoneCheckDuringAnimation(ActionIndexCache action, Agent agent, List<Agent> targets, List<sbyte> boneIds, float actionProgressMin, float actionProgressMax, float boneCollisionRadius, bool stopAfterFirstHit, Action<Agent, Agent, sbyte> onCollisionCallback, Action onExpiration)
			: base(agent, targets, boneIds, actionProgressMax, boneCollisionRadius, stopAfterFirstHit, onCollisionCallback, onExpiration)
		{
			_action = action;
			_actionProgressMin = actionProgressMin;
			_actionProgressMax = actionProgressMax;
		}

		public override bool Tick(float dt)
		{
			// Check if the agent is still performing the action and if there are targets to check against.
			// TODO check, subject to crash under certain conditions (due to agent state ?)
			if (_agent == null || _agent.AgentVisuals == null || !_agent.IsActive() || _agent.Health <= 0 || _targets == null || _targets.Count == 0
				|| _agent.GetCurrentAction(0) != _action
				|| _agent.GetCurrentActionProgress(0) >= _actionProgressMax)
			{
				_onExpiration?.Invoke();
				// Remove this instance
				return false;
			}

			// Check if the action progress is above the minimum threshold.
			if (_agent.GetCurrentActionProgress(0) >= _actionProgressMin)
			{
				if (!CheckBoneCollision())
				{
					_onExpiration?.Invoke();
					// Remove this instance
					return false;
				}
			}

			// Keep this instance alive
			return true;
		}
	}
}

