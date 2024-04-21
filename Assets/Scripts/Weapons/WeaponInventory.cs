using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using Gameplay;

namespace Weapons
{
    public class WeaponsInventory : MonoBehaviour
    {
        [Tooltip("List of weapon types that can be fired by this weapon.")]
        public List<WeaponController> Weapons = new List<WeaponController>();

        int ActiveWeaponIndex;

        public UnityAction<WeaponController> OnSwitchedToWeapon;
        public UnityAction<WeaponController, int> OnAddedWeapon;
        public UnityAction<WeaponController, int> OnRemovedWeapon;

        WeaponController[] _weaponSlots = new WeaponController[9];
        int _weaponSwitchNewWeaponIndex;

        void Start()
        {
            ActiveWeaponIndex = 0;

            // Add all weapons to the inventory
            foreach (var weapon in Weapons)
            {
                AddWeapon(weapon);
            }
        }

        bool AddWeapon(WeaponController weaponPrefab)
        {
            if (HasWeapon(weaponPrefab) != null)
            {
                return false;
            }

            // search our weapon slots for the first free one, assign the weapon to it, and return true if we found one. Return false otherwise
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                // only add the weapon if the slot is free
                if (_weaponSlots[i] == null)
                {
                    _weaponSlots[i] = weaponPrefab;
                    OnAddedWeapon?.Invoke(weaponPrefab, i);

                    return true;
                }
            }

            if (GetActiveWeapon() == null)
            {
                SwitchWeapon(true);
            }

            return false;
        }

        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }


        WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            for (var index = 0; index < _weaponSlots.Length; index++)
            {
                var proj = _weaponSlots[index];
                if (proj != null && proj == weaponPrefab)
                {
                    return proj;
                }
            }

            return null;
        }

        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;
            int closestSlotDistance = _weaponSlots.Length;
            for (int i = 0; i < _weaponSlots.Length; i++)
            {
                // If the weapon at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
                // and select it if it's the closest distance yet
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }
            SwitchToWeaponIndex(newWeaponIndex);
        }

        public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
        {
            if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
            {
                ActiveWeaponIndex = newWeaponIndex;

                WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                OnSwitchedToWeapon?.Invoke(newWeapon);

            }
        }

        WeaponController GetWeaponAtSlotIndex(int index)
        {
            // find the active weapon in our weapon slots based on index
            if (index >= 0 && index < _weaponSlots.Length)
            {
                return _weaponSlots[index];
            }
            return null;
        }

        int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots = 0;

            if (ascendingOrder)
            {
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;
            }
            else
            {
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);
            }

            if (distanceBetweenSlots < 0)
            {
                distanceBetweenSlots = _weaponSlots.Length + distanceBetweenSlots;
            }

            return distanceBetweenSlots;
        }
    }
}
