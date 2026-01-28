using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Common.Extensions.PlayerSpawn.Models;
using Alliance.Common.Extensions.TroopSpawner.Models;
using Alliance.Common.Extensions.TroopSpawner.Utilities;
using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;
using static TaleWorlds.MountAndBlade.MPPerkObject;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModels
{
	/// <summary>
	/// GameModel calculating agents stats.
	/// Apply different multiplier on stats depending on agents AI difficulty or player formation.    
	/// </summary>
	public class ExtendedAgentStatCalculateModel : CustomBattleAgentStatCalculateModel
	{
		AgentStatCalculateModel _previousModel;

		public ExtendedAgentStatCalculateModel(AgentStatCalculateModel previousModel)
		{
			_previousModel = previousModel;
			_previousModel ??= new CustomBattleAgentStatCalculateModel();
		}

		public override int GetEffectiveSkill(Agent agent, SkillObject skill)
		{
			Log($"GetEffectiveSkill={_previousModel.GetEffectiveSkill(agent, skill)} GetSkillBonus={GetSkillBonus(agent)}, Difficulty={AgentsInfoModel.Instance.Agents[agent.Index].Difficulty}");
			return _previousModel.GetEffectiveSkill(agent, skill) + GetSkillBonus(agent);
		}

		public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			_previousModel.UpdateAgentStats(agent, agentDrivenProperties);
		}

		private int GetSkillBonus(Agent agent)
		{
			return (int)((AgentsInfoModel.Instance.Agents[agent.Index].Difficulty - 1f) * 50);
		}
	}
}