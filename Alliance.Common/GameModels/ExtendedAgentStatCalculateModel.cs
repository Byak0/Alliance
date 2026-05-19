using Alliance.Common.Core.Configuration.Models;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Common.Extensions.TroopSpawner.Models;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using static Alliance.Common.Utilities.Logger;

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
			return _previousModel.GetEffectiveSkill(agent, skill) + GetSkillBonus(agent);
		}

		public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
		{
			_previousModel.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
		}

		public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			_previousModel.UpdateAgentStats(agent, agentDrivenProperties);

			UpdateArmor(agent, agentDrivenProperties);

			UpdateAgentFormationStats(agent, agentDrivenProperties);
		}

		private static void UpdateArmor(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			Equipment spawnEquipment = agent.SpawnEquipment;
			MissionEquipment equipment = agent.Equipment;
			BasicCharacterObject character = agent.Character;
			MissionPeer missionPeer = agent.MissionPeer ?? agent.OwningAgentMissionPeer;
			bool isPlayer = agent.MissionPeer != null;
			MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent);
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);


			if (Config.Instance.LocalizedArmor)
			{
				// Localized armor values based on equipment (default singleplayer behavior)
				agentDrivenProperties.ArmorHead = spawnEquipment.GetHeadArmorSum();
				agentDrivenProperties.ArmorTorso = spawnEquipment.GetHumanBodyArmorSum();
				agentDrivenProperties.ArmorLegs = spawnEquipment.GetLegArmorSum();
				agentDrivenProperties.ArmorArms = spawnEquipment.GetArmArmorSum();
			}
			else
			{
				// Global armor values based on class (default multiplayer behavior)
				MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(missionPeer);
				for (int i = (int)DrivenProperty.ArmorHead; i < (int)DrivenProperty.ArmorArms; i++)
				{
					DrivenProperty drivenProperty = (DrivenProperty)i;
					float perkBonus = onSpawnPerkHandler?.GetDrivenPropertyBonusOnSpawn(isPlayer, drivenProperty, 0) ?? 0;
					agentDrivenProperties.SetStat(drivenProperty, (float)mPHeroClassForCharacter.ArmorValue + perkBonus);
				}
			}
		}

		/// <summary>
		/// Update agent stats depending on its formation state (for players only).
		/// </summary>
		private void UpdateAgentFormationStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			FormationComponent formationComp = agent.MissionPeer?.GetComponent<FormationComponent>();
			if (formationComp == null)
			{
				return;
			}

			agentDrivenProperties.SwingSpeedMultiplier *= formationComp.MeleeDebuffMultiplier;
			agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier *= formationComp.MeleeDebuffMultiplier;
			agentDrivenProperties.ReloadSpeed *= formationComp.DistanceDebuffMultiplier;
			agentDrivenProperties.BipedalRangedReadySpeedMultiplier *= formationComp.DistanceDebuffMultiplier;
			agentDrivenProperties.BipedalRangedReloadSpeedMultiplier *= formationComp.DistanceDebuffMultiplier;
			agentDrivenProperties.WeaponInaccuracy += 1 - formationComp.AccuracyDebuffMultiplier;
		}

		private int GetSkillBonus(Agent agent)
		{
			return (int)((AgentsInfoModel.Instance.Agents[agent.Index].Difficulty - 1f) * 100);
		}
	}
}