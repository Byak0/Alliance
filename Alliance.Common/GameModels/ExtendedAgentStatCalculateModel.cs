using Alliance.Common.Core.Utils;
using Alliance.Common.Extensions.FormationEnforcer.Component;
using Alliance.Common.Extensions.TroopSpawner.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.GameModels
{
	/// <summary>
	/// GameModel calculating agents stats.
	/// Apply different multiplier on stats depending on agents AI difficulty or player formation.    
	/// </summary>
	public class ExtendedAgentStatCalculateModel : CustomBattleAgentStatCalculateModel
	{
		public override float GetDifficultyModifier()
		{
			return 0.5f;
		}

		private float GetSkillDifficultyModifier(Agent agent)
		{
			return AgentsInfoModel.Instance.Agents[agent.Index].Difficulty + 0.5f;
		}

		/// <summary>
		/// Gives bonus skill points depending on agent difficulty.
		/// [Easy][Normal][Hard][Very Hard][Bannerlord]
		/// [-25 ][   0  ][+25 ][   +50   ][   +75   ]
		/// </summary>
		private int GetSkillBonus(Agent agent)
		{
			return (int)(AgentsInfoModel.Instance.Agents[agent.Index].Difficulty - 1f) * 50;
		}

		public override bool CanAgentRideMount(Agent agent, Agent targetMount)
		{
			return agent.CheckSkillForMounting(targetMount);
		}

		public override float GetKnockDownResistance(Agent agent, StrikeType strikeType = StrikeType.Invalid)
		{
			float num = agent.Character.KnockdownResistance;
			if (agent.HasMount)
			{
				num += 0.1f;
			}
			else if (strikeType == StrikeType.Thrust)
			{
				num += 0.25f;
			}
			return num;
		}

		protected new float CalculateAILevel(Agent agent, int relevantSkillLevel)
		{
			float difficultyModifier = AgentsInfoModel.Instance.Agents[agent.Index].Difficulty;
			return MBMath.ClampFloat(relevantSkillLevel / 300f * difficultyModifier, 0f, 1f);
		}

		private int GetSkillValueForItem(Agent agent, ItemObject primaryItem)
		{
			return Math.Max(agent.Character.GetSkillValue(primaryItem != null ? primaryItem.RelevantSkill : DefaultSkills.Athletics) + GetSkillBonus(agent), 0);
		}

		public override int GetEffectiveSkill(Agent agent, SkillObject skill)
		{
			return Math.Max(agent.Character.GetSkillValue(skill) + GetSkillBonus(agent), 0);
		}

		public override int GetEffectiveSkillForWeapon(Agent agent, WeaponComponentData weapon)
		{
			int adjustedSkill = GetEffectiveSkill(agent, weapon.RelevantSkill);

			if (adjustedSkill > 0 && weapon.IsRangedWeapon)
			{
				MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent);
				if (perkHandler != null)
				{
					adjustedSkill = MathF.Ceiling(adjustedSkill * (perkHandler.GetRangedAccuracy() + 1f));
				}
			}

			return MBMath.ClampInt(adjustedSkill, 0, 500);
		}

		public override void InitializeAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData)
		{
			agentDrivenProperties.ArmorEncumbrance = spawnEquipment.GetTotalWeightOfArmor(agent.IsHuman);
			if (!agent.IsHuman)
			{
				InitializeHorseAgentStats(agent, spawnEquipment, agentDrivenProperties);
				return;
			}

			// Check if this character is setup for multiplayer
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
			if (mPHeroClassForCharacter != null)
			{
				InitializeHumanAgentStats(agent, agentDrivenProperties, agentBuildData, mPHeroClassForCharacter);
			}
			// Otherwise rely on CustomBattle implementation
			else
			{
				base.InitializeAgentStats(agent, spawnEquipment, agentDrivenProperties, agentBuildData);
			}
		}

		public override void UpdateAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			if (agent.IsHuman)
			{
				// Check if this character is setup for multiplayer
				MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
				if (mPHeroClassForCharacter != null)
				{
					UpdateHumanAgentStats(agent, agentDrivenProperties, mPHeroClassForCharacter);
				}
				// Otherwise rely on CustomBattle implementation
				else
				{
					base.UpdateAgentStats(agent, agentDrivenProperties);
				}
			}
			else if (agent.IsMount)
			{
				UpdateMountAgentStats(agent, agentDrivenProperties);
			}

			UpdateAgentFormationStats(agent, agentDrivenProperties);

			if (agent.IsTroll())
			{
				UpdateStatsForTroll(agent, agentDrivenProperties);
			}
			else if (agent.IsEnt())
			{
				UpdateStatsForEnt(agent, agentDrivenProperties);
			}
			else if (agent.IsWarg())
			{
				UpdateStatsForWarg(agent, agentDrivenProperties);
			}
			else if (agent.IsDwarf())
			{
				UpdateStatsForDwarf(agent, agentDrivenProperties);
			}
		}

		private void UpdateStatsForDwarf(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			agentDrivenProperties.MaxSpeedMultiplier *= 0.9f;
		}

		private void UpdateStatsForWarg(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			agentDrivenProperties.MountManeuver *= 0.8f;
			agentDrivenProperties.TopSpeedReachDuration *= 2f;
			agentDrivenProperties.MountDashAccelerationMultiplier *= 3f;
		}

		private void UpdateStatsForTroll(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			// MaxSpeed is multiplied with native_parameters.xml/bipedal_speed_multiplier.
			agentDrivenProperties.MaxSpeedMultiplier *= 1.7f;
			agentDrivenProperties.TopSpeedReachDuration *= 2f;
			agentDrivenProperties.CombatMaxSpeedMultiplier *= 0.5f;
			agentDrivenProperties.AIDecideOnAttackChance = 1f;
			agentDrivenProperties.AiDecideOnAttackContinueAction = 1f;
			agentDrivenProperties.AiDecideOnAttackingContinue = 1f;
			agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = 1f;
		}

		private void UpdateStatsForEnt(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			// MaxSpeed is multiplied with native_parameters.xml/bipedal_speed_multiplier.
			agentDrivenProperties.MaxSpeedMultiplier *= 2f;
			agentDrivenProperties.CombatMaxSpeedMultiplier *= 1.5f;
			agentDrivenProperties.AIParryOnAttackAbility = 0f;
			agentDrivenProperties.AIParryOnAttackingContinueAbility = 0f;
			agentDrivenProperties.AIParryOnDecideAbility = 0f;
			agentDrivenProperties.AiParryDecisionChangeValue = 0f;
			agentDrivenProperties.AIAttackOnParryChance = 1f;
			agentDrivenProperties.AIBlockOnDecideAbility = 0f;
			agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = 0f;
			agentDrivenProperties.AiMovementDelayFactor = 0.5f;
			agentDrivenProperties.AiDecideOnAttackContinueAction = 1f;
			agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = 30f;
			agentDrivenProperties.AiTryChamberAttackOnDecide = 0f;
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

		private void UpdateMountAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties)
		{
			MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent.RiderAgent);
			EquipmentElement mountElement = agent.SpawnEquipment[EquipmentIndex.ArmorItemEndSlot];
			EquipmentElement harness = agent.SpawnEquipment[EquipmentIndex.HorseHarness];
			agentDrivenProperties.MountManeuver = mountElement.GetModifiedMountManeuver(in harness) * (1f + (perkHandler?.GetMountManeuver() ?? 0f));
			agentDrivenProperties.MountSpeed = (mountElement.GetModifiedMountSpeed(in harness) + 1) * 0.22f * (1f + (perkHandler?.GetMountSpeed() ?? 0f));
			int ridingSkill = 100;
			if (agent.RiderAgent != null)
			{
				ridingSkill = Math.Max(agent.RiderAgent.Character.GetSkillValue(DefaultSkills.Riding) + GetSkillBonus(agent.RiderAgent), 100);
			}
			agentDrivenProperties.TopSpeedReachDuration = Game.Current.BasicModels.RidingModel.CalculateAcceleration(in mountElement, in harness, ridingSkill);
			agentDrivenProperties.MountSpeed *= 1f + ridingSkill * 0.0032f;
			agentDrivenProperties.MountManeuver *= 1f + ridingSkill * 0.0035f;
			float num2 = mountElement.Weight / 2f + (harness.IsEmpty ? 0f : harness.Weight);
			agentDrivenProperties.MountDashAccelerationMultiplier = !(num2 > 200f) ? 1f : num2 < 300f ? 1f - (num2 - 200f) / 111f : 0.1f;

			if (agent.Monster.StringId == "warg")
			{
				agentDrivenProperties.MountManeuver *= 0.8f;
				agentDrivenProperties.TopSpeedReachDuration *= 2f;
				agentDrivenProperties.MountDashAccelerationMultiplier *= 3f;
			}
		}

		private void UpdateHumanAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties, MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter)
		{
			Equipment spawnEquipment = agent.SpawnEquipment;
			agentDrivenProperties.ArmorHead = spawnEquipment.GetHeadArmorSum();
			agentDrivenProperties.ArmorTorso = spawnEquipment.GetHumanBodyArmorSum();
			agentDrivenProperties.ArmorLegs = spawnEquipment.GetLegArmorSum();
			agentDrivenProperties.ArmorArms = spawnEquipment.GetArmArmorSum();
			MPPerkObject.MPPerkHandler perkHandler = MPPerkObject.GetPerkHandler(agent);
			BasicCharacterObject character = agent.Character;
			MissionEquipment equipment = agent.Equipment;
			float totalWeightOfWeapons = equipment.GetTotalWeightOfWeapons();
			totalWeightOfWeapons *= 1f + (perkHandler?.GetEncumbrance(isOnBody: true) ?? 0f);
			EquipmentIndex wieldedItemIndex = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
			EquipmentIndex wieldedItemIndex2 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
			if (wieldedItemIndex != EquipmentIndex.None)
			{
				ItemObject item = equipment[wieldedItemIndex].Item;
				WeaponComponent weaponComponent = item.WeaponComponent;
				float realWeaponLength = weaponComponent.PrimaryWeapon.GetRealWeaponLength();
				float num = (weaponComponent.GetItemType() == ItemObject.ItemTypeEnum.Bow ? 4f : 1.5f) * item.Weight * MathF.Sqrt(realWeaponLength);
				num *= 1f + (perkHandler?.GetEncumbrance(isOnBody: false) ?? 0f);
				totalWeightOfWeapons += num;
			}

			if (wieldedItemIndex2 != EquipmentIndex.None)
			{
				ItemObject item2 = equipment[wieldedItemIndex2].Item;
				float num2 = 1.5f * item2.Weight;
				num2 *= 1f + (perkHandler?.GetEncumbrance(isOnBody: false) ?? 0f);
				totalWeightOfWeapons += num2;
			}

			agentDrivenProperties.WeaponsEncumbrance = totalWeightOfWeapons;
			EquipmentIndex wieldedItemIndex3 = agent.GetWieldedItemIndex(Agent.HandIndex.MainHand);
			WeaponComponentData weaponComponentData = wieldedItemIndex3 != EquipmentIndex.None ? equipment[wieldedItemIndex3].CurrentUsageItem : null;
			ItemObject primaryItem = wieldedItemIndex3 != EquipmentIndex.None ? equipment[wieldedItemIndex3].Item : null;
			EquipmentIndex wieldedItemIndex4 = agent.GetWieldedItemIndex(Agent.HandIndex.OffHand);
			WeaponComponentData secondaryItem = wieldedItemIndex4 != EquipmentIndex.None ? equipment[wieldedItemIndex4].CurrentUsageItem : null;
			agentDrivenProperties.SwingSpeedMultiplier = 0.93f + 0.0007f * GetSkillValueForItem(agent, primaryItem);
			agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
			agentDrivenProperties.HandlingMultiplier = agentDrivenProperties.SwingSpeedMultiplier;
			agentDrivenProperties.ShieldBashStunDurationMultiplier = 1f;
			agentDrivenProperties.KickStunDurationMultiplier = 1f;
			agentDrivenProperties.ReloadSpeed = 0.96f + 0.0004f * GetSkillValueForItem(agent, primaryItem);
			agentDrivenProperties.MissileSpeedMultiplier = 1f;
			agentDrivenProperties.ReloadMovementPenaltyFactor = 1f;
			SetAllWeaponInaccuracy(agent, agentDrivenProperties, (int)wieldedItemIndex3, weaponComponentData);

			// Native speed value
			float movespeedMultiplier = 1f;
			movespeedMultiplier = mPHeroClassForCharacter.IsTroopCharacter(agent.Character) ? mPHeroClassForCharacter.TroopMovementSpeedMultiplier : mPHeroClassForCharacter.HeroMovementSpeedMultiplier;
			agentDrivenProperties.MaxSpeedMultiplier = 1.05f * (movespeedMultiplier * (100f / (100f + totalWeightOfWeapons)));

			int ridingSkill = GetEffectiveSkill(agent, DefaultSkills.Riding);
			bool weaponIsBow = false;
			bool weaponIsWideGrip = false;
			if (weaponComponentData != null)
			{
				WeaponComponentData weaponComponentData2 = weaponComponentData;
				int effectiveSkillForWeapon = GetEffectiveSkillForWeapon(agent, weaponComponentData2);
				if (perkHandler != null)
				{
					agentDrivenProperties.MissileSpeedMultiplier *= perkHandler.GetThrowingWeaponSpeed(weaponComponentData) + 1f;
				}

				if (weaponComponentData2.IsRangedWeapon)
				{
					int thrustSpeed = weaponComponentData2.ThrustSpeed;
					if (!agent.HasMount)
					{
						float num4 = MathF.Max(0f, 1f - effectiveSkillForWeapon / 500f);
						agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.125f * num4;
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.1f * num4;
					}
					else
					{
						float num5 = MathF.Max(0f, (1f - effectiveSkillForWeapon / 500f) * (1f - ridingSkill / 1800f));
						agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = 0.025f * num5;
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = 0.06f * num5;
					}

					agentDrivenProperties.WeaponMaxMovementAccuracyPenalty = MathF.Max(0f, agentDrivenProperties.WeaponMaxMovementAccuracyPenalty);
					agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty = MathF.Max(0f, agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty);
					if (weaponComponentData2.RelevantSkill == DefaultSkills.Bow)
					{
						float value = (thrustSpeed - 60f) / 75f;
						value = MBMath.ClampFloat(value, 0f, 1f);
						agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 6f;
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 4.5f / MBMath.Lerp(0.75f, 2f, value);
					}
					else if (weaponComponentData2.RelevantSkill == DefaultSkills.Throwing)
					{
						float value2 = (thrustSpeed - 85f) / 17f;
						value2 = MBMath.ClampFloat(value2, 0f, 1f);
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 1.5f * MBMath.Lerp(1.5f, 0.8f, value2);
					}
					else if (weaponComponentData2.RelevantSkill == DefaultSkills.Crossbow)
					{
						agentDrivenProperties.WeaponMaxMovementAccuracyPenalty *= 2.5f;
						agentDrivenProperties.WeaponMaxUnsteadyAccuracyPenalty *= 1.2f;
					}

					// Improve range
					//if ((weaponComponentData2.WeaponClass & (WeaponClass.Crossbow | WeaponClass.Bow | WeaponClass.Musket | WeaponClass.Pistol)) != 0)
					//{        
					//    agentDrivenProperties.WeaponInaccuracy *= 0.6f;
					//}

					if (weaponComponentData2.WeaponClass == WeaponClass.Bow)
					{
						weaponIsBow = true;
						agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.3f + (95.75f - thrustSpeed) * 0.005f;
						float value3 = (thrustSpeed - 60f) / 75f;
						value3 = MBMath.ClampFloat(value3, 0f, 1f);
						agentDrivenProperties.WeaponUnsteadyBeginTime = 0.1f + effectiveSkillForWeapon * 0.01f * MBMath.Lerp(1f, 2f, value3);
						if (agent.IsAIControlled)
						{
							agentDrivenProperties.WeaponUnsteadyBeginTime *= 4f;
						}

						agentDrivenProperties.WeaponUnsteadyEndTime = 2f + agentDrivenProperties.WeaponUnsteadyBeginTime;
						agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
					}
					else if (weaponComponentData2.WeaponClass == WeaponClass.Javelin || weaponComponentData2.WeaponClass == WeaponClass.ThrowingAxe || weaponComponentData2.WeaponClass == WeaponClass.ThrowingKnife)
					{
						agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.2f + (89f - thrustSpeed) * 0.009f;
						agentDrivenProperties.WeaponUnsteadyBeginTime = 2.5f + effectiveSkillForWeapon * 0.01f;
						agentDrivenProperties.WeaponUnsteadyEndTime = 10f + agentDrivenProperties.WeaponUnsteadyBeginTime;
						agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.025f;
						if (weaponComponentData2.WeaponClass == WeaponClass.ThrowingAxe)
						{
							agentDrivenProperties.WeaponInaccuracy *= 6.6f;
						}
					}
					else
					{
						agentDrivenProperties.WeaponBestAccuracyWaitTime = 0.1f;
						agentDrivenProperties.WeaponUnsteadyBeginTime = 0f;
						agentDrivenProperties.WeaponUnsteadyEndTime = 0f;
						agentDrivenProperties.WeaponRotationalAccuracyPenaltyInRadians = 0.1f;
					}
				}
				else if (weaponComponentData2.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
				{
					weaponIsWideGrip = true;
					agentDrivenProperties.WeaponUnsteadyBeginTime = 1f + effectiveSkillForWeapon * 0.005f;
					agentDrivenProperties.WeaponUnsteadyEndTime = 3f + effectiveSkillForWeapon * 0.01f;
				}
			}

			agentDrivenProperties.AttributeShieldMissileCollisionBodySizeAdder = 0.3f;
			float num6 = agent.MountAgent?.GetAgentDrivenPropertyValue(DrivenProperty.AttributeRiding) ?? 1f;
			agentDrivenProperties.AttributeRiding = ridingSkill * num6;
			agentDrivenProperties.AttributeHorseArchery = MissionGameModels.Current.StrikeMagnitudeModel.CalculateHorseArcheryFactor(character);
			agentDrivenProperties.BipedalRangedReadySpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReadySpeedMultiplier);
			agentDrivenProperties.BipedalRangedReloadSpeedMultiplier = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalRangedReloadSpeedMultiplier);
			if (perkHandler != null)
			{
				for (int i = 55; i < 84; i++)
				{
					DrivenProperty drivenProperty = (DrivenProperty)i;
					if ((drivenProperty != DrivenProperty.WeaponUnsteadyBeginTime && drivenProperty != DrivenProperty.WeaponUnsteadyEndTime || weaponIsBow || weaponIsWideGrip) && (drivenProperty != DrivenProperty.WeaponRotationalAccuracyPenaltyInRadians || weaponIsBow))
					{
						float stat = agentDrivenProperties.GetStat(drivenProperty);
						agentDrivenProperties.SetStat(drivenProperty, stat + perkHandler.GetDrivenPropertyBonus(drivenProperty, stat));
					}
				}
			}

			SetAiRelatedProperties(agent, agentDrivenProperties, weaponComponentData, secondaryItem);
		}


		private AgentDrivenProperties InitializeHumanAgentStats(Agent agent, AgentDrivenProperties agentDrivenProperties, AgentBuildData agentBuildData, MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter)
		{
			FillAgentStatsFromData(ref agentDrivenProperties, agent, mPHeroClassForCharacter, agentBuildData?.AgentMissionPeer, agentBuildData?.OwningAgentMissionPeer);
			agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, MultiplayerOptions.OptionType.UseRealisticBlocking.GetBoolValue() ? 1f : 0f);
			agent.BaseHealthLimit = mPHeroClassForCharacter.Health;
			agent.HealthLimit = agent.BaseHealthLimit;
			agent.Health = agent.HealthLimit;
			return agentDrivenProperties;
		}

		private static void InitializeHorseAgentStats(Agent agent, Equipment spawnEquipment, AgentDrivenProperties agentDrivenProperties)
		{
			agentDrivenProperties.AiSpeciesIndex = agent.Monster.FamilyType;
			agentDrivenProperties.AttributeRiding = 0.8f + ((spawnEquipment[EquipmentIndex.HorseHarness].Item != null) ? 0.2f : 0f);
			float num = 0f;
			for (int i = 1; i < 12; i++)
			{
				if (spawnEquipment[i].Item != null)
				{
					num += (float)spawnEquipment[i].GetModifiedMountBodyArmor();
				}
			}

			agentDrivenProperties.ArmorTorso = num;
			_ = spawnEquipment[EquipmentIndex.ArmorItemEndSlot].Item.HorseComponent;
			EquipmentElement equipmentElement = spawnEquipment[EquipmentIndex.ArmorItemEndSlot];
			EquipmentElement harness = spawnEquipment[EquipmentIndex.HorseHarness];
			agentDrivenProperties.MountChargeDamage = (float)equipmentElement.GetModifiedMountCharge(in harness) * 0.01f;
			agentDrivenProperties.MountDifficulty = equipmentElement.Item.Difficulty;
		}

		private void FillAgentStatsFromData(ref AgentDrivenProperties agentDrivenProperties, Agent agent, MultiplayerClassDivisions.MPHeroClass heroClass, MissionPeer missionPeer, MissionPeer owningMissionPeer)
		{
			MissionPeer missionPeer2 = missionPeer ?? owningMissionPeer;
			if (missionPeer2 != null)
			{
				MPPerkObject.MPOnSpawnPerkHandler onSpawnPerkHandler = MPPerkObject.GetOnSpawnPerkHandler(missionPeer2);
				bool isPlayer = missionPeer != null;
				for (int i = 0; i < 55; i++)
				{
					DrivenProperty drivenProperty = (DrivenProperty)i;
					float stat = agentDrivenProperties.GetStat(drivenProperty);
					if (drivenProperty == DrivenProperty.ArmorHead || drivenProperty == DrivenProperty.ArmorTorso || drivenProperty == DrivenProperty.ArmorLegs || drivenProperty == DrivenProperty.ArmorArms)
					{
						agentDrivenProperties.SetStat(drivenProperty, stat + (float)heroClass.ArmorValue + onSpawnPerkHandler.GetDrivenPropertyBonusOnSpawn(isPlayer, drivenProperty, stat));
					}
					else
					{
						agentDrivenProperties.SetStat(drivenProperty, stat + onSpawnPerkHandler.GetDrivenPropertyBonusOnSpawn(isPlayer, drivenProperty, stat));
					}
				}
			}

			float topSpeedReachDuration = (heroClass.IsTroopCharacter(agent.Character) ? heroClass.TroopTopSpeedReachDuration : heroClass.HeroTopSpeedReachDuration);
			agentDrivenProperties.TopSpeedReachDuration = topSpeedReachDuration;
			float managedParameter = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMinMultiplier);
			float managedParameter2 = ManagedParameters.Instance.GetManagedParameter(ManagedParametersEnum.BipedalCombatSpeedMaxMultiplier);
			float num = (heroClass.IsTroopCharacter(agent.Character) ? heroClass.TroopCombatMovementSpeedMultiplier : heroClass.HeroCombatMovementSpeedMultiplier);
			agentDrivenProperties.CombatMaxSpeedMultiplier = managedParameter + (managedParameter2 - managedParameter) * num;
		}

		protected new void SetAiRelatedProperties(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedItem, WeaponComponentData secondaryItem)
		{
			int meleeSkill = GetMeleeSkill(agent, equippedItem, secondaryItem);
			SkillObject skill = equippedItem == null ? DefaultSkills.Athletics : equippedItem.RelevantSkill;
			int effectiveSkill = GetEffectiveSkill(agent, skill);
			float num = CalculateAILevel(agent, meleeSkill);
			float num2 = CalculateAILevel(agent, effectiveSkill);
			float num3 = num + agent.Defensiveness;
			agentDrivenProperties.AiRangedHorsebackMissileRange = 0.3f + 0.4f * num2;
			agentDrivenProperties.AiFacingMissileWatch = -0.96f + num * 0.06f;
			agentDrivenProperties.AiFlyingMissileCheckRadius = 8f - 6f * num;
			agentDrivenProperties.AiShootFreq = 0.3f + 0.7f * num2;
			agentDrivenProperties.AiWaitBeforeShootFactor = agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : 1f - 0.5f * num2;
			agentDrivenProperties.AIBlockOnDecideAbility = MBMath.Lerp(0.5f, 0.99f, MBMath.ClampFloat(MathF.Pow(num, 0.5f), 0f, 1f));
			agentDrivenProperties.AIParryOnDecideAbility = MBMath.Lerp(0.5f, 0.95f, MBMath.ClampFloat(num, 0f, 1f));
			agentDrivenProperties.AiTryChamberAttackOnDecide = (num - 0.15f) * 0.1f;
			agentDrivenProperties.AIAttackOnParryChance = 0.08f - 0.02f * agent.Defensiveness;
			agentDrivenProperties.AiAttackOnParryTiming = -0.2f + 0.3f * num;
			agentDrivenProperties.AIDecideOnAttackChance = 0.5f * agent.Defensiveness;
			agentDrivenProperties.AIParryOnAttackAbility = MBMath.ClampFloat(num, 0f, 1f);
			agentDrivenProperties.AiKick = -0.1f + (num > 0.4f ? 0.4f : num);
			agentDrivenProperties.AiAttackCalculationMaxTimeFactor = num;
			agentDrivenProperties.AiDecideOnAttackWhenReceiveHitTiming = -0.25f * (1f - num);
			agentDrivenProperties.AiDecideOnAttackContinueAction = -0.5f * (1f - num);
			agentDrivenProperties.AiDecideOnAttackingContinue = 0.1f * num;
			agentDrivenProperties.AIParryOnAttackingContinueAbility = MBMath.Lerp(0.5f, 0.95f, MBMath.ClampFloat(num, 0f, 1f));
			agentDrivenProperties.AIDecideOnRealizeEnemyBlockingAttackAbility = MBMath.ClampFloat(MathF.Pow(num, 2.5f) - 0.1f, 0f, 1f);
			agentDrivenProperties.AIRealizeBlockingFromIncorrectSideAbility = MBMath.ClampFloat(MathF.Pow(num, 2.5f) - 0.01f, 0f, 1f);
			agentDrivenProperties.AiAttackingShieldDefenseChance = 0.2f + 0.3f * num;
			agentDrivenProperties.AiAttackingShieldDefenseTimer = -0.3f + 0.3f * num;
			agentDrivenProperties.AiRandomizedDefendDirectionChance = 1f - MathF.Pow(num, 3f);
			agentDrivenProperties.AiShooterError = 0.008f;
			agentDrivenProperties.AISetNoAttackTimerAfterBeingHitAbility = MBMath.Lerp(0.33f, 1f, num);
			agentDrivenProperties.AISetNoAttackTimerAfterBeingParriedAbility = MBMath.Lerp(0.2f, 1f, num * num);
			agentDrivenProperties.AISetNoDefendTimerAfterHittingAbility = MBMath.Lerp(0.1f, 0.99f, num * num);
			agentDrivenProperties.AISetNoDefendTimerAfterParryingAbility = MBMath.Lerp(0.15f, 1f, num * num);
			agentDrivenProperties.AIEstimateStunDurationPrecision = 1f - MBMath.Lerp(0.2f, 1f, num);
			agentDrivenProperties.AIHoldingReadyMaxDuration = MBMath.Lerp(0.25f, 0f, MathF.Min(1f, num * 2f));
			agentDrivenProperties.AIHoldingReadyVariationPercentage = num;
			agentDrivenProperties.AiRaiseShieldDelayTimeBase = -0.75f + 0.5f * num;
			agentDrivenProperties.AiUseShieldAgainstEnemyMissileProbability = 0.1f + num * 0.6f + num3 * 0.2f;
			agentDrivenProperties.AiCheckMovementIntervalFactor = 0.005f * (1.1f - num);
			agentDrivenProperties.AiMovementDelayFactor = 4f / (3f + num2);
			agentDrivenProperties.AiParryDecisionChangeValue = 0.05f + 0.7f * num;
			agentDrivenProperties.AiDefendWithShieldDecisionChanceValue = MathF.Min(2f, 0.5f + num + 0.6f * num3);
			agentDrivenProperties.AiMoveEnemySideTimeValue = -2.5f + 0.5f * num;
			agentDrivenProperties.AiMinimumDistanceToContinueFactor = 2f + 0.3f * (3f - num);
			agentDrivenProperties.AiHearingDistanceFactor = 1f + num;
			agentDrivenProperties.AiChargeHorsebackTargetDistFactor = 1.5f * (3f - num);
			agentDrivenProperties.AiWaitBeforeShootFactor = agent.PropertyModifiers.resetAiWaitBeforeShootFactor ? 0f : 1f - 0.5f * num2;

			// AI accuracy decrease
			float shootingErrorMultiplier = 1f - num2;
			agentDrivenProperties.AiRangerLeadErrorMin = (0f - shootingErrorMultiplier) * 0.35f;
			agentDrivenProperties.AiRangerLeadErrorMax = shootingErrorMultiplier * 0.2f;
			agentDrivenProperties.AiRangerVerticalErrorMultiplier = shootingErrorMultiplier * 0.1f;
			agentDrivenProperties.AiRangerHorizontalErrorMultiplier = shootingErrorMultiplier * ((float)Math.PI / 90f);

			agentDrivenProperties.AIAttackOnDecideChance = MathF.Clamp(0.1f * CalculateAIAttackOnDecideMaxValue() * (3f - agent.Defensiveness), 0.05f, 1f);
			agentDrivenProperties.SetStat(DrivenProperty.UseRealisticBlocking, agent.Controller != Agent.ControllerType.Player ? 1f : 0f);
		}

		/// <summary>
		/// Calculates the inaccuracy of a weapon for a given agent.
		/// </summary>
		/// <returns>The calculated weapon inaccuracy.</returns>
		public override float GetWeaponInaccuracy(Agent agent, WeaponComponentData weapon, int weaponSkill)
		{
			// Multiplier based on weapon class
			float rangeMultiplier;
			switch (weapon.WeaponClass)
			{
				case WeaponClass.Musket: rangeMultiplier = 1.8f; break;
				case WeaponClass.Pistol: rangeMultiplier = 1.6f; break;
				case WeaponClass.Crossbow: rangeMultiplier = 1.2f; break;
				case WeaponClass.Bow: rangeMultiplier = 1f; break;
				default: rangeMultiplier = 1f; break;
			}

			float weaponInaccuracy = 0f;

			// Calculate inaccuracy for ranged weapons
			if (weapon.IsRangedWeapon)
			{
				// Inaccuracy formula for ranged weapons
				// The formula takes into account weapon accuracy and skill level
				float weaponInaccuracyBase = 100f - weapon.Accuracy;
				float modifier = 1f - 0.002f * weaponSkill * rangeMultiplier;
				weaponInaccuracy = weaponInaccuracyBase * modifier * 0.002f;
			}
			// Calculate inaccuracy for weapons with the WideGrip flag
			else if (weapon.WeaponFlags.HasAllFlags(WeaponFlags.WideGrip))
			{
				// Inaccuracy formula for weapons with WideGrip flag
				// The formula reduces inaccuracy based on weapon skill level
				weaponInaccuracy = 1f - weaponSkill * 0.01f;
			}

			// Reduce inaccuracy for players
			if (agent.MissionPeer != null)
			{
				weaponInaccuracy *= 0.5f;
			}

			// Ensure the calculated inaccuracy is not negative
			return MathF.Max(weaponInaccuracy, 0f);
		}

		public ExtendedAgentStatCalculateModel()
		{
		}

		public override float GetWeaponDamageMultiplier(Agent agent, WeaponComponentData weapon)
		{
			return 1f;
		}

		public override float GetKnockBackResistance(Agent agent)
		{
			return agent.Character.KnockbackResistance;
		}

		public override float GetDismountResistance(Agent agent)
		{
			return agent.Character.DismountResistance;
		}

		private int GetSkillValueForItem(BasicCharacterObject characterObject, ItemObject primaryItem)
		{
			return characterObject.GetSkillValue((primaryItem != null) ? primaryItem.RelevantSkill : DefaultSkills.Athletics);
		}

		private void SetMountedWeaponPenaltiesOnAgent(Agent agent, AgentDrivenProperties agentDrivenProperties, WeaponComponentData equippedWeaponComponent)
		{
			int effectiveSkill = GetEffectiveSkill(agent, DefaultSkills.Riding);
			float num = 0.3f - (float)effectiveSkill * 0.003f;
			if (num > 0f)
			{
				float val = agentDrivenProperties.SwingSpeedMultiplier * (1f - num);
				float val2 = agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier * (1f - num);
				float val3 = agentDrivenProperties.ReloadSpeed * (1f - num);
				float val4 = agentDrivenProperties.WeaponBestAccuracyWaitTime * (1f + num);
				agentDrivenProperties.SwingSpeedMultiplier = Math.Max(0f, val);
				agentDrivenProperties.ThrustOrRangedReadySpeedMultiplier = Math.Max(0f, val2);
				agentDrivenProperties.ReloadSpeed = Math.Max(0f, val3);
				agentDrivenProperties.WeaponBestAccuracyWaitTime = Math.Max(0f, val4);
			}

			float num2 = 15f - (float)effectiveSkill * 0.15f;
			if (num2 > 0f)
			{
				float val5 = agentDrivenProperties.WeaponInaccuracy * (1f + num2);
				agentDrivenProperties.WeaponInaccuracy = Math.Max(0f, val5);
			}
		}

		public static float CalculateMaximumSpeedMultiplier(Agent agent)
		{
			MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(agent.Character);
			if (!mPHeroClassForCharacter.IsTroopCharacter(agent.Character))
			{
				return mPHeroClassForCharacter.HeroMovementSpeedMultiplier;
			}

			return mPHeroClassForCharacter.TroopMovementSpeedMultiplier;
		}
	}
}