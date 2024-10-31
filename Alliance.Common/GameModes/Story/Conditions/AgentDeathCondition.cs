using Alliance.Common.GameModes.Story.Behaviors;
using Alliance.Common.GameModes.Story.Utilities;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.GameModes.Story.Conditions
{
	/// <summary>
	/// Check if agents entered a zone.
	/// </summary>
	public class AgentDeathCondition : Condition
	{

		[ScenarioEditor(label: "Character", tooltip: "ID of the character to check death.")]
		public string Character;
		[ScenarioEditor(label: "Death quota", tooltip: "How many time character need to die to trigger condition.")]
		public int DeathQuota = 1;
		[ScenarioEditor(label: "Repeat", tooltip: "If true, it will trigger each time Death quota is reached once again.")]
		public bool Repeat;


		private Agent _DeadAgent;
		private int _DeathCount = 0 ;

		public override void Register()
		{
			//Mission.Current.GetMissionBehavior<RespawnBehavior>().RespawnPlayer(playerSelected);
			ConditionsBehavior cdtBehavior = Mission.Current.GetMissionBehavior<ConditionsBehavior>();
			if (cdtBehavior == null) return;
			cdtBehavior.UpdateAgentDeathCondition += CheckdeadagentId;
		}

		public override void Unregister()
		{
			ConditionsBehavior cdtBehavior = Mission.Current.GetMissionBehavior<ConditionsBehavior>();
			if (cdtBehavior == null) return;
			cdtBehavior.UpdateAgentDeathCondition -= CheckdeadagentId;
		}

		private void CheckdeadagentId(Agent victim)
		{
			_DeadAgent = victim;
			if (Character == _DeadAgent.Character.StringId)
			{
				_DeathCount += 1;
				_DeadAgent = default;
			}
		}

		private void Reset()
		{
			if ( Repeat == true ) 
			_DeathCount = 0;
		}

		public override bool Evaluate(ScenarioManager context)
		{
			if (_DeathCount == DeathQuota)
			{
				_DeathCount += 1;
				Reset();
				return true;
			}
			else return false;
		}
	}
}
