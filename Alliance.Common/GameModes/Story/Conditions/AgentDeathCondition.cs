using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.GameModes.Story.Behaviors;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if a specific agent (or a number of this specific agent) is dead
	/// </summary>
	public class AgentDeathCondition : Condition
	{
		[ConfigProperty(label: "Character", tooltip: "ID of the character to check death.")]
		public string Character;
		public SideType Side = SideType.All;
		[ConfigProperty(label: "Death quota", tooltip: "How many time character need to die to trigger condition.")]
		public int DeathQuota = 1;
		[ConfigProperty(label: "Repeat", tooltip: "If true, it will reset Death quota each time is it reached.")]
		public bool ConditionMayRepeat;

		private int _deathCount = 0;

		public override void Register(GameEntity gameEntity = null)
		{
			ConditionsBehavior cdtBehavior = Mission.Current.GetMissionBehavior<ConditionsBehavior>();
			if (cdtBehavior == null) return;
			cdtBehavior.UpdateAgentDeathCondition += CheckDeadAgentId;
		}

		public override void Unregister()
		{
			ConditionsBehavior cdtBehavior = Mission.Current.GetMissionBehavior<ConditionsBehavior>();
			if (cdtBehavior == null) return;
			cdtBehavior.UpdateAgentDeathCondition -= CheckDeadAgentId;
		}

		private void CheckDeadAgentId(Agent victim)
		{
			if (victim == null) return;
			if (Character == victim.Character.StringId && (Side == SideType.All || (int)victim.Team.Side == (int)Side))
			{
				_deathCount++;
			}
		}

		private void Reset()
		{
			_deathCount = 0;
		}

		// Check if number of specific agent  is reached to trigger condition
		public override bool Evaluate(ScenarioManager context)
		{
			if (_deathCount >= DeathQuota)
			{
				if (ConditionMayRepeat) Reset();
				return true;
			}
			return false;
		}
	}
}
