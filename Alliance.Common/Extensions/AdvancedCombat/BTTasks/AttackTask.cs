using Alliance.Common.Extensions.AdvancedCombat.BTBlackBoards;
using BehaviorTrees;
using BehaviorTrees.Nodes;
using System.Threading;
using System.Threading.Tasks;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.AdvancedCombat.BTTasks
{
	public class AttackTask : BTTask, IBTCombatBlackboard
	{
		BTBlackboardValue<Agent> agent;
		public BTBlackboardValue<Agent> Agent { get => agent; set => agent = value; }

		BTBlackboardValue<Agent> target;
		public BTBlackboardValue<Agent> Target { get => target; set => target = value; }

		public AttackTask() : base()
		{
		}

		public override async Task<bool> Execute(CancellationToken cancellationToken)
		{
			Agent self = Agent.GetValue();
			Agent targetAgent = Target.GetValue();

			if (self == null)
			{
				Log($"ERROR, Agent is null in AttackTask", LogLevel.Error);
				return false;
			}

			if (targetAgent == null || targetAgent.Health <= 0 || targetAgent.IsFadingOut())
			{
				Log($"ERROR, Target invalid in AttackTask - {targetAgent?.Name}", LogLevel.Error);
				Target.SetValue(null);
				self?.DisableScriptedCombatMovement();
				self?.DisableScriptedMovement();
				return false;
			}

			WorldPosition pos = self.GetWorldPosition();
			self.SetScriptedTargetEntityAndPosition(targetAgent.AgentVisuals.GetEntity(), pos, TaleWorlds.MountAndBlade.Agent.AISpecialCombatModeFlags.IgnoreAmmoLimitForRangeCalculation, false);
			if (self.HasRangedWeapon())
			{
				self.SetScriptedPosition(ref pos, false, TaleWorlds.MountAndBlade.Agent.AIScriptedFrameFlags.RangerCanMoveForClearTarget);
			}

			return true;
		}
	}
}
