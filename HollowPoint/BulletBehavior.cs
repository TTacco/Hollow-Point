using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Collections;
using static Modding.Logger;

namespace HollowPoint
{
    public class BulletBehavior : MonoBehaviour
    {
        HealthManager hm;

        private HitInstance damage = new HitInstance
        {
            AttackType = AttackTypes.NailBeam,
            DamageDealt = 3,
            Multiplier = 1,
            IgnoreInvulnerable = true,

            //CircleDirection = false,
            //IsExtraDamage = false,
            //Direction = 1,
            //MoveAngle = 1,
            //MoveDirection = false,
            //MagnitudeMultiplier = 1,
            //SpecialType = SpecialTypes.None,

        };

        public void Start()
        {
            On.HealthManager.Hit += BulletDamage;
        }

        public void Update()
        {
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            hm = col.GetComponentInChildren<HealthManager>();
            damage.Source = gameObject;

            if (col.gameObject.GetComponentInChildren<HealthManager>() != null)
            {
                //Damages the enemy and destroys the bullet
                hm.Hit(damage);
                Destroy(gameObject);
                Log(col.name + " has destroyed the GO");
            }
            else if (!col.name.Contains("Knight") && !col.name.Contains("Particle") && CloseToKnight(gameObject))
            {
                //Destroy the bullet because its a wall
                Log("GO destroyed at " + gameObject.transform.position);
                Log("Player was at " + HeroController.instance.transform.position);

                Destroy(gameObject);
                Log(col.name + " has destroyed the GO");
            }
        }

        //Returns false if the bullet is close enough to the knight, thus making sure the bullet is NOT destroyed by the Knight's own hitbox
        bool CloseToKnight(GameObject bulletGO)
        {
            Vector3 tempKnightP = HeroController.instance.transform.position;
            Vector3 tempBulletP = bulletGO.transform.position;

            double distance = Math.Pow(tempKnightP.x - tempBulletP.x, 2) + Math.Pow(tempKnightP.y - tempBulletP.y, 2);

            if (distance < 2.3)
                return false;

            return true;
        }

        public void BulletDamage(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (hitInstance.Source.name.Contains("bullet"))
            {
                DamageEnemies.HitEnemy(self, hitInstance.DamageDealt, hitInstance, 0);
            }
            else
            {
                orig(self, hitInstance);
            }
        }

        public void OnDestroy()
        {
            On.HealthManager.Hit -= BulletDamage;
        }

    }
}


//public Ammunition bulletType;
//private int enemiesHit = 0;
//private bool canHitEnemy = true;

//// Returns true if bullet can hit an enemy and false otherwise.
//public bool enemyHit()
//{
//    if (!canHitEnemy) return false;
//    canHitEnemy = false;
//    StartCoroutine(enemyCooldown());
//    enemiesHit++;
//    if (bulletType.PierceNumber == enemiesHit)
//    {
//        StartCoroutine(noMoreEnemies());
//    }
//    return true;
//}

//private IEnumerator noMoreEnemies()
//{
//    yield return null;
//    foreach (Transform child in gameObject.transform)
//    {
//        Destroy(child.gameObject);
//    }
//    Destroy(gameObject);
//}

//private IEnumerator enemyCooldown()
//{
//    yield return new WaitForSeconds(bulletType.hitCooldown);
//    canHitEnemy = true;
//}