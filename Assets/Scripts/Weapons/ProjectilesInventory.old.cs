using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine;
using Gameplay;

namespace Weapons
{
    public class ProjectilesInventory : MonoBehaviour
    {
        [Tooltip("List of projectile types that can be fired by this projectile.")]
        public List<ProjectileStandard> Projectiles = new List<ProjectileStandard>();

        int ActiveProjectileIndex;

        public UnityAction<ProjectileStandard> OnSwitchedToProjectile;
        public UnityAction<ProjectileStandard, int> OnAddedProjectile;
        public UnityAction<ProjectileStandard, int> OnRemovedProjectile;

        ProjectileStandard[] _projectileSlots = new ProjectileStandard[9];
        int _projectileSwitchNewProjectileIndex;

        void Start()
        {
            ActiveProjectileIndex = 0;

            // Add all projectiles to the inventory
            foreach (var projectile in Projectiles)
            {
                AddProjectile(projectile);
            }
        }

        bool AddProjectile(ProjectileStandard projectilePrefab)
        {
            if (HasProjectile(projectilePrefab) != null)
            {
                return false;
            }

            // search our projectile slots for the first free one, assign the projectile to it, and return true if we found one. Return false otherwise
            for (int i = 0; i < _projectileSlots.Length; i++)
            {
                // only add the projectile if the slot is free
                if (_projectileSlots[i] == null)
                {
                    _projectileSlots[i] = projectilePrefab;
                    OnAddedProjectile?.Invoke(projectilePrefab, i);

                    return true;
                }
            }

            if (GetActiveProjectile() == null)
            {
                SwitchProjectile(true);
            }

            return false;
        }

        public ProjectileStandard GetActiveProjectile()
        {
            return GetProjectileAtSlotIndex(ActiveProjectileIndex);
        }


        ProjectileStandard HasProjectile(ProjectileStandard projectilePrefab)
        {
            for (var index = 0; index < _projectileSlots.Length; index++)
            {
                var proj = _projectileSlots[index];
                if (proj != null && proj == projectilePrefab)
                {
                    return proj;
                }
            }

            return null;
        }

        public void SwitchProjectile(bool ascendingOrder)
        {
            int newProjectileIndex = -1;
            int closestSlotDistance = _projectileSlots.Length;
            for (int i = 0; i < _projectileSlots.Length; i++)
            {
                // If the projectile at this slot is valid, calculate its "distance" from the active slot index (either in ascending or descending order)
                // and select it if it's the closest distance yet
                if (i != ActiveProjectileIndex && GetProjectileAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenProjectileSlots(ActiveProjectileIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newProjectileIndex = i;
                    }
                }
            }
            SwitchToProjectileIndex(newProjectileIndex);
        }

        public void SwitchToProjectileIndex(int newProjectileIndex, bool force = false)
        {
            if (force || (newProjectileIndex != ActiveProjectileIndex && newProjectileIndex >= 0))
            {
                ActiveProjectileIndex = newProjectileIndex;

                ProjectileStandard newProjectile = GetProjectileAtSlotIndex(ActiveProjectileIndex);
                OnSwitchedToProjectile?.Invoke(newProjectile);

            }
        }

        ProjectileStandard GetProjectileAtSlotIndex(int index)
        {
            // find the active projectile in our projectile slots based on index
            if (index >= 0 && index < _projectileSlots.Length)
            {
                return _projectileSlots[index];
            }
            return null;
        }

        int GetDistanceBetweenProjectileSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
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
                distanceBetweenSlots = _projectileSlots.Length + distanceBetweenSlots;
            }

            return distanceBetweenSlots;
        }
    }
}
