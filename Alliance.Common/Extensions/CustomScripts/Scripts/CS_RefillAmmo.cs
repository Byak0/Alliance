using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
    /// <summary>
    /// This script is attached to an object and allows interaction with it.
    /// On use, it will refill user's ammo until NumberOfUseMax is reached (-1 for no limit)
    /// Up to 3 actions/animations can be set up and chained before activating the refill.
    /// The script is built to handle most animations but the result is not guaranteed.
    /// Some animations may still cause issues. Use AnimationMaxDuration to prevent long or looping animations.
    /// </summary>
    public class CS_RefillAmmo : CS_UsableObject
    {
        public AmmoClass ammoType = AmmoClass.All;

        protected CS_RefillAmmo()
        {
        }

        protected override void OnInit()
        {
            base.OnInit();
            if (ammoType == AmmoClass.All) return;
            foreach (StandingPoint standingPoint in StandingPoints)
            {
                if (standingPoint.GetType() == typeof(StandingPointWithWeaponRequirement))
                {
                    (standingPoint as StandingPointWithWeaponRequirement).InitRequiredWeaponClasses((WeaponClass)ammoType);
                }
            }
        }

        protected override void AfterUse(Agent userAgent, bool actionCompleted = true)
        {
            base.AfterUse(userAgent);

            for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumAllWeaponSlots; equipmentIndex++)
            {
                if (!userAgent.Equipment[equipmentIndex].IsEmpty)
                {
                    if (ammoType == AmmoClass.All && Enum.IsDefined(typeof(AmmoClass), (int)userAgent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass) && userAgent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass != (WeaponClass)AmmoClass.All
                    || (WeaponClass)ammoType == userAgent.Equipment[equipmentIndex].CurrentUsageItem.WeaponClass)
                    {
                        if (userAgent.Equipment[equipmentIndex].Amount < userAgent.Equipment[equipmentIndex].ModifiedMaxAmount)
                        {
                            userAgent.SetWeaponAmountInSlot(equipmentIndex, userAgent.Equipment[equipmentIndex].ModifiedMaxAmount, true);
                        }
                    }
                }
            }
        }

        static CS_RefillAmmo()
        {
        }

        public enum AmmoClass
        {
            All = WeaponClass.Undefined,
            Arrow = WeaponClass.Arrow,
            Bolt = WeaponClass.Bolt,
            Cartridge = WeaponClass.Cartridge,
            Stone = WeaponClass.Stone,
            ThrowingAxe = WeaponClass.ThrowingAxe,
            ThrowingKnife = WeaponClass.ThrowingKnife,
            Javelin = WeaponClass.Javelin
        }
    }
}
