using Alliance.Common.Core.ExtendedXML.Models;
using Alliance.Common.Extensions.ClassLimiter.NetworkMessages.FromClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using static Alliance.Common.Core.ExtendedXML.Extension.ExtendedXMLExtension;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Client.Extensions.UsableItem.Views
{
    /// <summary>
    /// Interact with extended items.
    /// </summary>
    [DefaultView]
    public class UsableItemView : MissionView
    {
        private bool _usableItemInHand = false;
        private EquipmentIndex _itemSlot = EquipmentIndex.None;
        private Dictionary<EquipmentIndex, ExtendedItem> _usableItems = new Dictionary<EquipmentIndex, ExtendedItem>();
        private Dictionary<EquipmentIndex, DateTime> _lastUsage = new Dictionary<EquipmentIndex, DateTime>();

        public UsableItemView() { }

        public override void OnBehaviorInitialize()
        {
            InitializeMainAgentPropterties();
        }

        public override void OnMissionScreenFinalize()
        {
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
