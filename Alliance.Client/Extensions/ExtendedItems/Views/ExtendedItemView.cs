using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions.UsableItems.NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Core.ExtendedXML.Extension.ExtendedXMLExtension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.ExtendedItems.Views
{
	/// <summary>
	/// Interact with extended items.
	/// </summary>
	[DefaultView]
	public class ExtendedItemView : MissionView
	{
		public struct AttachedPrefab
		{
			public GameEntity Prefab;
			public sbyte BoneType;
		}

		private bool _usableItemInHand = false;
		private EquipmentIndex _itemSlot = EquipmentIndex.None;
		private Dictionary<EquipmentIndex, ExtendedItem> _usableItems = new Dictionary<EquipmentIndex, ExtendedItem>();
		private Dictionary<EquipmentIndex, DateTime> _lastUsage = new Dictionary<EquipmentIndex, DateTime>();
		private Dictionary<Agent, AttachedPrefab> _extendedMainItemsWithPrefab = new Dictionary<Agent, AttachedPrefab>();
		private Dictionary<Agent, AttachedPrefab> _extendedOffHandtemsWithPrefab = new Dictionary<Agent, AttachedPrefab>();

		public ExtendedItemView() { }

		public override void OnBehaviorInitialize()
		{
			InitializeMainAgentPropterties();
		}

		private (ItemObject item, ExtendedItem itemEx, sbyte boneType) GetWieldedItemAndExtendedInfo(Agent agent, bool isLeftHand)
		{
			sbyte boneType;
			ItemObject item;
			ExtendedItem itemEx;
			if (isLeftHand && agent.WieldedOffhandWeapon.Item != null)
			{
				item = agent.WieldedOffhandWeapon.Item;
				itemEx = item.GetExtendedItem();
				if (itemEx != null)
				{
					boneType = agent.Monster.OffHandItemBoneIndex;
					return (item, itemEx, boneType);
				}
			}

			if (!isLeftHand && agent.WieldedWeapon.Item != null)
			{
				item = agent.WieldedWeapon.Item;
				itemEx = item.GetExtendedItem();
				if (itemEx != null)
				{
					boneType = agent.Monster.MainHandItemBoneIndex;
					return (item, itemEx, boneType);
				}
			}

			return (null, null, 0);
		}

		private void SetPrefabGlobalFrame(Agent agent, sbyte boneType, GameEntity prefab)
		{
			Skeleton skeleton = agent.AgentVisuals?.GetSkeleton();
			if (skeleton == null)
			{
				Log($"Failed to get agents visuals or skeleton for {agent.Name}", LogLevel.Error);
				return;
			}
			MatrixFrame agentGlobalFrame = agent.AgentVisuals.GetGlobalFrame();
			MatrixFrame localWeaponFrame = skeleton.GetBoneEntitialFrameWithIndex(boneType);
			Vec3 weaponGlobalPosition = agentGlobalFrame.TransformToParent(localWeaponFrame.origin);
			Mat3 weaponGlobalRotation = agentGlobalFrame.rotation.TransformToParent(localWeaponFrame.rotation);
			prefab.SetGlobalFrame(new MatrixFrame(weaponGlobalRotation, weaponGlobalPosition));
		}

		private void RemoveExistingPrefabs(Agent agent)
		{
			if (_extendedMainItemsWithPrefab.TryGetValue(agent, out AttachedPrefab attachedPrefabMH))
			{
				try
				{
					attachedPrefabMH.Prefab.Remove(0);
					_extendedMainItemsWithPrefab.Remove(agent);
					Log($"Removed prefab {attachedPrefabMH.Prefab}", LogLevel.Debug);
				}
				catch (Exception e)
				{
					Log($"Failed to remove prefab {attachedPrefabMH.Prefab} - {e.Message}", LogLevel.Error);
				}
			}

			if (_extendedOffHandtemsWithPrefab.TryGetValue(agent, out AttachedPrefab attachedPrefabOH))
			{
				try
				{
					attachedPrefabOH.Prefab.Remove(0);
					_extendedOffHandtemsWithPrefab.Remove(agent);
					Log($"Removed prefab {attachedPrefabMH.Prefab}", LogLevel.Debug);
				}
				catch (Exception e)
				{
					Log($"Failed to remove prefab {attachedPrefabOH.Prefab} - {e.Message}", LogLevel.Error);
				}
			}
		}

		// Triggered by Patch_MissionNetworkComponent.Prefix_HandleServerEventSetWieldedItemIndex
		public void OnAgentWieldedItemChange(Agent agent)
		{
			RemoveExistingPrefabs(agent);

			(ItemObject item, ExtendedItem itemEx, sbyte boneType) = GetWieldedItemAndExtendedInfo(agent, false);
			if (itemEx != null && !string.IsNullOrEmpty(itemEx.Prefab))
			{
				// TODO TEST 
				//agent.AddSynchedPrefabComponentToBone(itemEx.Prefab, boneType);

				GameEntity prefab = GameEntity.Instantiate(Mission.Current.Scene, itemEx.Prefab, agent.Frame);
				SetPrefabGlobalFrame(agent, boneType, prefab);

				_extendedMainItemsWithPrefab[agent] = new AttachedPrefab { Prefab = prefab, BoneType = boneType };
				Log($"Added prefab {itemEx.Prefab} to {itemEx.StringId}", LogLevel.Debug);
			}

			(ItemObject item2, ExtendedItem itemEx2, sbyte boneType2) = GetWieldedItemAndExtendedInfo(agent, true);
			if (itemEx2 != null && !string.IsNullOrEmpty(itemEx2.Prefab))
			{
				GameEntity prefab2 = GameEntity.Instantiate(Mission.Current.Scene, itemEx2.Prefab, agent.Frame);
				SetPrefabGlobalFrame(agent, boneType, prefab2);

				_extendedOffHandtemsWithPrefab[agent] = new AttachedPrefab { Prefab = prefab2, BoneType = boneType2 };
				Log($"Added prefab {itemEx2.Prefab} to {itemEx2.StringId}", LogLevel.Debug);
			}
		}

		public void InitializeMainAgentPropterties()
		{
			Mission.Current.OnMainAgentChanged += OnMainAgentChanged;
			OnMainAgentChanged(null, null);
			OnMainAgentWeaponChange();
		}

		private void OnMainAgentChanged(object sender, PropertyChangedEventArgs e)
		{
			if (Agent.Main != null)
			{
				Agent main = Agent.Main;
				main.OnMainAgentWieldedItemChange += OnMainAgentWeaponChange;
				OnMainAgentWeaponChange();
				RefreshUsableItems();
			}
		}

		private void RefreshUsableItems()
		{
			MissionEquipment equipment = Agent.Main?.Equipment;
			_usableItems = new Dictionary<EquipmentIndex, ExtendedItem>();
			if (equipment != null)
			{
				for (int slot = (int)EquipmentIndex.WeaponItemBeginSlot; slot < (int)EquipmentIndex.NumAllWeaponSlots; slot++)
				{
					ExtendedItem itemEx = equipment[slot].Item?.GetExtendedItem();
					if (itemEx != null && itemEx.Usable)
					{
						_usableItems[(EquipmentIndex)slot] = itemEx;
						Log($"Added {itemEx.StringId} with {itemEx.Effects.Count} effects", LogLevel.Debug);
					}
				}
			}
		}

		public void OnMainAgentWeaponChange()
		{
			if (Agent.Main != null)
			{
				EquipmentIndex offHandItemIndex = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand);
				if (offHandItemIndex > EquipmentIndex.None && offHandItemIndex < EquipmentIndex.NumAllWeaponSlots)
				{
					MissionWeapon offHandWeapon = Agent.Main.Equipment[offHandItemIndex];
					ExtendedItem itemEx = offHandWeapon.Item?.GetExtendedItem();
					if (itemEx != null && itemEx.Usable)
					{
						_itemSlot = offHandItemIndex;
						_usableItems[_itemSlot] = itemEx;
						_usableItemInHand = true;
						Log($"You equipped {offHandWeapon.GetModifiedItemName()} - {itemEx.StringId}", LogLevel.Debug);
					}
				}
				else
				{
					_itemSlot = EquipmentIndex.None;
					_usableItemInHand = false;
				}
			}
		}

		public override void OnMissionScreenTick(float dt)
		{
			for (int i = 0; i < _extendedOffHandtemsWithPrefab.Count; i++)
			{
				KeyValuePair<Agent, AttachedPrefab> item = _extendedOffHandtemsWithPrefab.ElementAt(i);
				if (item.Key.AgentVisuals?.GetSkeleton() == null)
				{
					RemoveExistingPrefabs(item.Key);
					continue;
				}
				SetPrefabGlobalFrame(item.Key, item.Value.BoneType, item.Value.Prefab);
			}

			for (int i = 0; i < _extendedMainItemsWithPrefab.Count; i++)
			{
				KeyValuePair<Agent, AttachedPrefab> item = _extendedMainItemsWithPrefab.ElementAt(i);
				if (item.Key.AgentVisuals?.GetSkeleton() == null)
				{
					RemoveExistingPrefabs(item.Key);
					continue;
				}
				SetPrefabGlobalFrame(item.Key, item.Value.BoneType, item.Value.Prefab);
			}

			if (_usableItemInHand && Input.IsKeyReleased(InputKey.Q))
			{
				UseItem(_itemSlot);
			}
		}

		public void UseItem(EquipmentIndex equipmentIndex)
		{
			// If item was already used recently, prevent its usage
			if (_lastUsage.TryGetValue(equipmentIndex, out DateTime lastUsage)
				&& lastUsage.AddSeconds(_usableItems[equipmentIndex].Cooldown) > DateTime.Now)
			{
				Log($"Item on cooldown !", LogLevel.Debug);
				return;
			}

			Log($"Using item {_usableItems[equipmentIndex].StringId} !", LogLevel.Debug);
			_lastUsage[equipmentIndex] = DateTime.Now;

			GameNetwork.BeginModuleEventAsClient();
			GameNetwork.WriteMessage(new RequestUseItem(equipmentIndex));
			GameNetwork.EndModuleEventAsClient();
		}
	}
}
