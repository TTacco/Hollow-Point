using System.Collections;
using ModCommon;
using UnityEngine;

namespace HollowPoint
{
    public class BulletBehavior : MonoBehaviour
    {
        public Ammunition bulletType;
        private int enemiesHit = 0;
        private bool canHitEnemy = true;

        // Returns true if bullet can hit an enemy and false otherwise.
        public bool enemyHit()
        {
            if (!canHitEnemy) return false;
            canHitEnemy = false;
            StartCoroutine(enemyCooldown());
            enemiesHit++;
            if (bulletType.PierceNumber == enemiesHit)
            {
                StartCoroutine(noMoreEnemies());
            }
            return true;
        }

        private IEnumerator noMoreEnemies()
        {
            yield return null;
            foreach (Transform child in gameObject.transform)
            {
                Destroy(child.gameObject);
            }
            Destroy(gameObject);
        }

        private IEnumerator enemyCooldown()
        {
            yield return new WaitForSeconds(bulletType.hitCooldown);
            canHitEnemy = true;
        }
        
        

    }
}