using Alliance.Common.Extensions.PlayerSpawn.NetworkMessages;
using Alliance.Common.Extensions.UsableEntity.Behaviors;
using Alliance.Common.Extensions.UsableEntity.Interfaces;
using Alliance.Common.Extensions.UsableEntity.Utilities;
using System;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using static Alliance.Common.Extensions.UsableEntity.Behaviors.UsableEntityBehavior;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Extensions.UsableEntity.Handlers
{
	public class PickUpItemHandler : IInteractionHandler
	{
		UsableEntityBehavior UsableEntityBehavior => Mission.Current.GetMissionBehavior<UsableEntityBehavior>();

		public bool CanHandle(GameEntity entity)
		{
			return entity != null
				&& entity.HasTag(AllianceTags.INTERACTIVE_TAG)
				&& entity.GetTagValue(AllianceTags.ITEM_PREFIX_TAG) != String.Empty;
		}

		public bool CanInteract(Agent agent, GameEntity entity)
		{
			return true;
		}

		public TextObject GetInteractionText(Agent agent, GameEntity entity)
		{
			TextObject to = new TextObject("Press {KEY} to equip " + entity.Name);
			to.SetTextVariable("KEY", HyperlinkTexts.GetKeyHyperlinkText(HotKeyManager.GetHotKeyId("CombatHotKeyCategory", 13)));
			return to;
		}

		public void Interact(Agent agent, InteractionTarget target)
		{
			if (GameNetwork.IsServer)
			{
				string itemName = target.Entity.GetTagValue(AllianceTags.ITEM_PREFIX_TAG);
				ItemObject itemObject = MBObjectManager.Instance.GetObject<ItemObject>(itemName);
				MissionWeapon missionWeapon = new MissionWeapon(itemObject, null, agent.Team.Banner);
				if (itemObject.IsBannerItem ||
					itemObject.HasWeaponComponent && itemObject.WeaponComponent?.PrimaryWeapon?.WeaponClass == WeaponClass.Boulder)
				{
					agent.EquipWeaponToExtraSlotAndWield(ref missionWeapon);
				}
				else
				{
					EquipmentIndex slot = EquipmentIndex.WeaponItemBeginSlot;
					while (!agent.Equipment[slot].IsEmpty && slot < EquipmentIndex.Weapon3)
					{
						slot++;
					}
					agent.EquipWeaponWithNewEntity(slot, ref missionWeapon);
					agent.TryToWieldWeaponInSlot(slot, Agent.WeaponWieldActionType.WithAnimation, true);
				}

				if (!target.Entity.HasTag(AllianceTags.NO_HIDE_ON_USE_TAG))
				{
					UsableEntityBehavior?.HideEntity(target.ID);
				}

				Log($"Agent {agent.Name} ({agent.MissionPeer?.Name}) used entity {target.Entity.Name}({target.ID}) and equipped {itemName}", LogLevel.Debug);
			}
			else
			{
				Log($"Requesting to use entity {target.Entity.Name}({target.ID})", LogLevel.Debug);
				UsableEntityMsg.RequestUseEntity(target);
			}
		}
	}
}
