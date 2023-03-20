using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class BasicEnemy : MonoBehaviour
    {
        [Header("Stats")]
        public int health;
        public GameObject poofEffect;

        public void TakeDamage(int damage)
        {
            health -= damage;

            if (health <= 0)
            {
                Destroy(gameObject);
                if (poofEffect != null)
                {
                    Instantiate(poofEffect, transform.position, Quaternion.identity);
                }
            }
        }
    }
}
