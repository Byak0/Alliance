using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade;
using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Core.ExtendedXML.Extension;
using System.Data.SqlTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.CharacterDeveloper;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Effects;
using static TaleWorlds.MountAndBlade.UsableMissionObject;
using Alliance.Common.Core.Configuration.Models;


namespace Alliance.Client.Extensions.ExNativeUI.AgentStatus.ViewModels
{
	//replace AgentInteractionInterfaceVM
	public class AgentInteractionInterfaceVMCustom : ViewModel
	{
		private readonly Mission _mission;

		private bool _currentObjectInteractable;

		private IFocusable _currentFocusedObject;

		private bool _isActive;

		private string _secondaryInteractionMessage;

		private string _primaryInteractionMessage;

		private int _focusType;

		private int _targetHealth;

		private bool _showHealthBar;

		private bool _isFocusedOnExit;

		private string _backgroundColor;

		private string _textColor;

		private bool _IsRaceConditionRespected;

		private bool IsPlayerActive
		{
			get
			{
				Agent main = Agent.Main;
				if (main == null)
				{
					return false;
				}

				return main.Health > 0f;
			}
		}

		[DataSourceProperty]
		public bool IsFocusedOnExit
		{
			get
			{
				return _isFocusedOnExit;
			}
			set
			{
				if (value != _isFocusedOnExit)
				{
					_isFocusedOnExit = value;
					OnPropertyChangedWithValue(value, "IsFocusedOnExit");
				}
			}
		}

		[DataSourceProperty]
		public int TargetHealth
		{
			get
			{
				return _targetHealth;
			}
			set
			{
				if (value != _targetHealth)
				{
					_targetHealth = value;
					OnPropertyChangedWithValue(value, "TargetHealth");
				}
			}
		}

		[DataSourceProperty]
		public bool ShowHealthBar
		{
			get
			{
				return _showHealthBar;
			}
			set
			{
				if (value != _showHealthBar)
				{
					_showHealthBar = value;
					OnPropertyChangedWithValue(value, "ShowHealthBar");
				}
			}
		}

		[DataSourceProperty]
		public int FocusType
		{
			get
			{
				return _focusType;
			}
			set
			{
				if (_focusType != value)
				{
					_focusType = value;
					OnPropertyChangedWithValue(value, "FocusType");
				}
			}
		}

		[DataSourceProperty]
		public string PrimaryInteractionMessage
		{
			get
			{
				return _primaryInteractionMessage;
			}
			set
			{
				if (_primaryInteractionMessage != value)
				{
					_primaryInteractionMessage = value;
					OnPropertyChangedWithValue(value, "PrimaryInteractionMessage");
					if (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value))
					{
						IsFocusedOnExit = false;
					}
				}
			}
		}

		[DataSourceProperty]
		public string SecondaryInteractionMessage
		{
			get
			{
				return _secondaryInteractionMessage;
			}
			set
			{
				if (_secondaryInteractionMessage != value)
				{
					_secondaryInteractionMessage = value;
					OnPropertyChangedWithValue(value, "SecondaryInteractionMessage");
				}
			}
		}

		[DataSourceProperty]
		public string BackgroundColor
		{
			get
			{
				return _backgroundColor;
			}
			set
			{
				if (_backgroundColor != value)
				{
					_backgroundColor = value;
					OnPropertyChangedWithValue(value, "BackgroundColor");
				}
			}
		}

		[DataSourceProperty]
		public string TextColor
		{
			get
			{
				return _textColor;
			}
			set
			{
				if (_textColor != value)
				{
					_textColor = value;
					OnPropertyChangedWithValue(value, "TextColor");
				}
			}
		}

		[DataSourceProperty]
		public bool IsActive
		{
			get
			{
				return _isActive;
			}
			set
			{
				if (value != _isActive)
				{
					_isActive = value;
					OnPropertyChangedWithValue(value, "IsActive");
					if (!value)
					{
						ShowHealthBar = false;
						PrimaryInteractionMessage = "";
						SecondaryInteractionMessage = "";
					}
				}
			}
		}

		public AgentInteractionInterfaceVMCustom(Mission mission)
		{
			_mission = mission;
			IsActive = false;
		}

		internal void Tick()
		{
			if (IsActive && _mission.Mode == MissionMode.StartUp && _currentFocusedObject is Agent && ((Agent)_currentFocusedObject).IsEnemyOf(_mission.MainAgent))
			{
				IsActive = false;
			}
		}

		internal void CheckAndClearFocusedAgent(Agent agent)
		{
			if (_currentFocusedObject != null && _currentFocusedObject as Agent == agent)
			{
				IsActive = false;
				ResetFocus();
				SecondaryInteractionMessage = "";
			}
		}

		public void OnFocusedHealthChanged(IFocusable focusable, float healthPercentage, bool hideHealthbarWhenFull)
		{
			SetHealth(healthPercentage, hideHealthbarWhenFull);
		}

		internal void OnFocusGained(Agent mainAgent, IFocusable focusableObject, bool isInteractable)
		{
			if (!IsPlayerActive || (_currentFocusedObject == focusableObject && _currentObjectInteractable == isInteractable))
			{
				return;
			}

			ResetFocus();
			_currentFocusedObject = focusableObject;
			_currentObjectInteractable = isInteractable;
			if (focusableObject is Agent agent)
			{
				if (agent.IsHuman)
				{
					SetAgent(mainAgent, agent, isInteractable);
				}
				else
				{
					SetMount(mainAgent, agent, isInteractable);
				}
			}
			else if (focusableObject is UsableMissionObject usableMissionObject)
			{
				if (usableMissionObject is SpawnedItemEntity spawnedItemEntity)
				{
					bool canQuickPickup = Agent.Main.CanQuickPickUp(spawnedItemEntity);
					SetItem(spawnedItemEntity, canQuickPickup, isInteractable);
				}
				else
				{
					SetUsableMissionObject(usableMissionObject, isInteractable);
				}
			}
			else if (focusableObject is UsableMachine machine)
			{
				SetUsableMachine(machine, isInteractable);
			}
			else if (focusableObject is DestructableComponent machine2)
			{
				SetDestructibleComponent(machine2, isInteractable: false);
			}
		}

		internal void OnFocusLost(Agent agent, IFocusable focusableObject)
		{
			ResetFocus();
			IsActive = false;
		}

		internal void OnAgentInteraction(Agent userAgent, Agent agent)
		{
			if (_mission.Mode == MissionMode.Stealth && agent.IsHuman && agent.IsActive() && !agent.IsEnemyOf(userAgent))
			{
				SetAgent(userAgent, agent, isInteractable: true);
			}
		}

		private void SetItem(SpawnedItemEntity item, bool canQuickPickup, bool isInteractable)
		{
			IsFocusedOnExit = false;
			EquipmentIndex possibleSlotIndex;
			ItemObject weaponToReplaceOnQuickAction = Agent.Main.GetWeaponToReplaceOnQuickAction(item, out possibleSlotIndex);
			bool fillUp = possibleSlotIndex != EquipmentIndex.None && !Agent.Main.Equipment[possibleSlotIndex].IsEmpty && Agent.Main.Equipment[possibleSlotIndex].IsAnyConsumable() && Agent.Main.Equipment[possibleSlotIndex].Amount < Agent.Main.Equipment[possibleSlotIndex].ModifiedMaxAmount;
			TextObject actionMessage = item.GetActionMessage(weaponToReplaceOnQuickAction, fillUp);
			TextObject descriptionMessage = item.GetDescriptionMessage(fillUp);
			if (TextObject.IsNullOrEmpty(actionMessage) || TextObject.IsNullOrEmpty(descriptionMessage))
			{
				return;
			}

			FocusType = 0;
			if (isInteractable)
			{
				MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
				if (canQuickPickup)
				{
					Agent main = Agent.Main;
					// Check if Item have a race restriction and display a message if needed (Custom)
					ItemObject objecttampon = item.WeaponCopy.Item;
					ExtendedItem itemEx = objecttampon.GetExtendedItem();
					IsRaceConditionRespected(main, itemEx);
					if (!_IsRaceConditionRespected && itemEx != null && itemEx.Race_condition != null && itemEx.Race_condition != "" && Config.Instance.EnableRaceRestrictionOnStuff)
					{
						PrimaryInteractionMessage = ($"This item can only be used by {itemEx.Race_condition}");
						// End of Custom Code 
					}
					else {
						if (main != null && main.CanInteractableWeaponBePickedUp(item))
						{


							PrimaryInteractionMessage = actionMessage.ToString();
							MBTextManager.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
							MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_select_item_to_replace"));
							SecondaryInteractionMessage = GameTexts.FindText("str_hold_key_action").ToString() + GetWeaponSpecificText(item);
						}
						else
						{
							PrimaryInteractionMessage = actionMessage.ToString();
							MBTextManager.SetTextVariable("STR1", descriptionMessage);
							MBTextManager.SetTextVariable("STR2", GetWeaponSpecificText(item));
							SecondaryInteractionMessage = GameTexts.FindText("str_STR1_space_STR2").ToString();
						}
					}
				}
				else
				{
					MBTextManager.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
					MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_select_item_to_replace"));
					PrimaryInteractionMessage = GameTexts.FindText("str_hold_key_action").ToString();
					MBTextManager.SetTextVariable("STR1", descriptionMessage);
					MBTextManager.SetTextVariable("STR2", GetWeaponSpecificText(item));
					SecondaryInteractionMessage = GameTexts.FindText("str_STR1_space_STR2").ToString();
				}
			}
			else
			{
				PrimaryInteractionMessage = item.GetInfoTextForBeingNotInteractable(Agent.Main).ToString();
				MBTextManager.SetTextVariable("STR1", descriptionMessage);
				MBTextManager.SetTextVariable("STR2", GetWeaponSpecificText(item));
				SecondaryInteractionMessage = GameTexts.FindText("str_STR1_space_STR2").ToString();
			}

			IsActive = true;
		}

		private void SetUsableMissionObject(UsableMissionObject usableObject, bool isInteractable)
		{
			FocusType = (int)usableObject.FocusableObjectType;
			IsFocusedOnExit = false;
			if (!string.IsNullOrEmpty(usableObject.ActionMessage.ToString()) && !string.IsNullOrEmpty(usableObject.DescriptionMessage.ToString()))
			{
				PrimaryInteractionMessage = (isInteractable ? usableObject.ActionMessage.ToString() : " ");
				SecondaryInteractionMessage = usableObject.DescriptionMessage.ToString();
				IsFocusedOnExit = usableObject.FocusableObjectType == FocusableObjectType.Door || usableObject.FocusableObjectType == FocusableObjectType.Gate;
			}
			else
			{
				UsableMachine usableMachineFromPoint = GetUsableMachineFromPoint(usableObject);
				if (usableMachineFromPoint != null)
				{
					PrimaryInteractionMessage = usableMachineFromPoint.GetDescriptionText(usableObject.GameEntity) ?? "";
					SecondaryInteractionMessage = ((!isInteractable) ? "" : (usableMachineFromPoint.GetActionTextForStandingPoint(usableObject)?.ToString() ?? ""));
				}
			}

			IsActive = true;
		}

		private void SetUsableMachine(UsableMachine machine, bool isInteractable)
		{
			PrimaryInteractionMessage = machine.GetDescriptionText(machine.GameEntity) ?? "";
			SecondaryInteractionMessage = " ";
			if (machine is CastleGate)
			{
				FocusType = 1;
			}

			if (machine.DestructionComponent != null)
			{
				TargetHealth = (int)(100f * machine.DestructionComponent.HitPoint / machine.DestructionComponent.MaxHitPoint);
				ShowHealthBar = true;
			}

			IsActive = true;
		}

		private void SetDestructibleComponent(DestructableComponent machine, bool isInteractable)
		{
			string descriptionText = machine.GetDescriptionText(machine.GameEntity);
			bool flag = descriptionText != "" && descriptionText != null;
			PrimaryInteractionMessage = (flag ? descriptionText : "null");
			SecondaryInteractionMessage = " ";
			TargetHealth = (int)(100f * machine.HitPoint / machine.MaxHitPoint);
			ShowHealthBar = machine.HitPoint < machine.MaxHitPoint;
			IsActive = flag;
		}

		private void SetAgent(Agent mainAgent, Agent focusedAgent, bool isInteractable)
		{
			IsFocusedOnExit = false;
			bool isActive = true;
			FocusType = 3;
			if (focusedAgent.MissionPeer != null)
			{
				PrimaryInteractionMessage = focusedAgent.MissionPeer.DisplayedName;
			}
			else
			{
				PrimaryInteractionMessage = focusedAgent.Name.ToString();
			}

			if (isInteractable && (_mission.Mode == MissionMode.StartUp || _mission.Mode == MissionMode.Duel || _mission.Mode == MissionMode.Battle || _mission.Mode == MissionMode.Stealth) && focusedAgent.IsHuman)
			{
				MBTextManager.SetTextVariable("USE_KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
				if (focusedAgent.IsActive())
				{
					if (_mission.Mode == MissionMode.Duel)
					{
						DuelMissionRepresentative obj = Agent.Main.MissionRepresentative as DuelMissionRepresentative;
						TextObject text = ((obj != null && obj.CheckHasRequestFromAndRemoveRequestIfNeeded(focusedAgent.MissionPeer)) ? GameTexts.FindText("str_ui_respond") : GameTexts.FindText("str_ui_duel"));
						MBTextManager.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
						MBTextManager.SetTextVariable("ACTION", text);
						SecondaryInteractionMessage = GameTexts.FindText("str_key_action").ToString();
					}
					else if (_mission.Mode == MissionMode.Stealth && !focusedAgent.IsEnemyOf(mainAgent))
					{
						MBTextManager.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
						MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_ui_prison_break"));
						SecondaryInteractionMessage = GameTexts.FindText("str_key_action").ToString();
					}
					else if (focusedAgent.IsEnemyOf(mainAgent))
					{
						isActive = false;
					}
					else if (!Mission.Current.IsAgentInteractionAllowed())
					{
						isActive = false;
					}
					else if (_mission.Mode != MissionMode.Battle)
					{
						MBTextManager.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
						MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_ui_talk"));
						SecondaryInteractionMessage = GameTexts.FindText("str_key_action").ToString();
					}
					else
					{
						FocusType = -1;
					}
				}
				else if (_mission.Mode != MissionMode.Battle)
				{
					MBTextManager.SetTextVariable("KEY", GameTexts.FindText("str_ui_agent_interaction_use"));
					MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_ui_search"));
					SecondaryInteractionMessage = GameTexts.FindText("str_key_action").ToString();
				}
			}

			IsActive = isActive;
		}

		private void SetMount(Agent agent, Agent focusedAgent, bool isInteractable)
		{
			IsFocusedOnExit = false;
			if (!focusedAgent.IsActive() || !focusedAgent.IsMount)
			{
				return;
			}

			string keyHyperlinkText = HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13));
			SecondaryInteractionMessage = focusedAgent.Name.ToString();
			FocusType = 2;
			if (focusedAgent.RiderAgent == null)
			{
				if ((float)MissionGameModels.Current.AgentStatCalculateModel.GetEffectiveSkill(agent, DefaultSkills.Riding) < focusedAgent.GetAgentDrivenPropertyValue(DrivenProperty.MountDifficulty))
				{
					PrimaryInteractionMessage = GameTexts.FindText("str_ui_riding_skill_not_adequate_to_mount").ToString();
				}
				else if ((agent.GetAgentFlags() & AgentFlag.CanRide) != 0)
				{
					MBTextManager.SetTextVariable("KEY", keyHyperlinkText);
					MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_ui_mount"));
					PrimaryInteractionMessage = GameTexts.FindText("str_key_action").ToString();
				}

				ShowHealthBar = false;
			}
			else if (focusedAgent.RiderAgent == agent)
			{
				MBTextManager.SetTextVariable("KEY", keyHyperlinkText);
				MBTextManager.SetTextVariable("ACTION", GameTexts.FindText("str_ui_dismount"));
				PrimaryInteractionMessage = GameTexts.FindText("str_key_action").ToString();
			}

			IsActive = true;
		}

		private void SetHealth(float healthPercentage, bool hideHealthBarWhenFull)
		{
			TargetHealth = (int)(100f * healthPercentage);
			if (hideHealthBarWhenFull)
			{
				ShowHealthBar = TargetHealth < 100;
			}
			else
			{
				ShowHealthBar = true;
			}
		}

		public void ResetFocus()
		{
			_currentFocusedObject = null;
			PrimaryInteractionMessage = "";
			FocusType = -1;
		}

		private UsableMachine GetUsableMachineFromPoint(UsableMissionObject standingPoint)
		{
			GameEntity gameEntity = standingPoint.GameEntity;
			while ((object)gameEntity != null && !gameEntity.HasScriptOfType<UsableMachine>())
			{
				gameEntity = gameEntity.Parent;
			}

			UsableMachine result = null;
			if (gameEntity != null)
			{
				result = gameEntity.GetFirstScriptOfType<UsableMachine>();
			}

			return result;
		}

		private string GetWeaponSpecificText(SpawnedItemEntity spawnedItem)
		{
			MissionWeapon weaponCopy = spawnedItem.WeaponCopy;
			WeaponComponentData currentUsageItem = weaponCopy.CurrentUsageItem;
			if (currentUsageItem != null && currentUsageItem.IsShield)
			{
				MBTextManager.SetTextVariable("LEFT", weaponCopy.HitPoints);
				MBTextManager.SetTextVariable("RIGHT", weaponCopy.ModifiedMaxHitPoints);
				return GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis").ToString();
			}

			WeaponComponentData currentUsageItem2 = weaponCopy.CurrentUsageItem;
			if (currentUsageItem2 != null && currentUsageItem2.IsAmmo && weaponCopy.ModifiedMaxAmount > 1 && !spawnedItem.IsStuckMissile())
			{
				MBTextManager.SetTextVariable("LEFT", weaponCopy.Amount);
				MBTextManager.SetTextVariable("RIGHT", weaponCopy.ModifiedMaxAmount);
				return GameTexts.FindText("str_LEFT_over_RIGHT_in_paranthesis").ToString();
			}

			return "";


		}

		// Custom new fonction
		private void IsRaceConditionRespected(Agent main, ExtendedItem itemEx)
		{
			if (itemEx == null || itemEx.Race_condition == "" )
			{
				_IsRaceConditionRespected = false;
				return;
			}
			string[] _listofrace = itemEx.Race_condition.Split(',');

			foreach (string race in _listofrace)
			{
				if (main.Character.Race == TaleWorlds.Core.FaceGen.GetRaceOrDefault(race))
				{
					_IsRaceConditionRespected = true;
					return;
				}
			}
			_IsRaceConditionRespected = false;
		}
	}
}
