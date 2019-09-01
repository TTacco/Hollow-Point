using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using ModCommon.Util;
using UnityEngine;


namespace HollowPoint
{
    public class HP_DamageCalculator : MonoBehaviour
    {
        //Put this at the prefabs class
        public static GameObject blockHitPrefab;

        public void Awake()
        {
            StartCoroutine(InitDamageCalculator());       
        }

        public IEnumerator InitDamageCalculator()
        {
            while(HeroController.instance == null && PlayerData.instance == null)
            {
                yield return null;
            }


            On.HealthManager.Hit += BulletDamage;
        }

        //Handles the damage
        public void BulletDamage(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (!hitInstance.Source.name.Contains("bullet"))
            {
                orig(self, hitInstance);
                return;
            }

            int damage = 3;

            if (hitInstance.Source.GetComponent<HP_BulletBehaviour>().special) damage *= 2;

            Modding.Logger.Log(hitInstance.Source.name);
            // TODO: Put these in the weapon handler section
            //damage.AttackType = AttackTypes.Generic;
            // damage.DamageDealt = HP_WeaponHandler.currentGun.gunDamage;
            DamageEnemies.HitEnemy(self, damage, HP_BulletBehaviour.damage, 0);
            return;

            Vector3 bulletOriginPosition = hitInstance.Source.GetComponent<HP_BulletBehaviour>().bulletOriginPosition;

            HP_Gun gunTheBulletCameFrom = HP_WeaponHandler.currentGun;
            float finalBulletDamage = gunTheBulletCameFrom.gunDamage;
            float travelDistance = Vector3.Distance(bulletOriginPosition, self.transform.position) * 10;
            float damageDropOff = Mathf.Floor(travelDistance / gunTheBulletCameFrom.gunDamMultiplier); //gunDamMult basically shows how much distance does it take before the bullet loses 1/4th of its damage
            if (damageDropOff == 0)
            {
                finalBulletDamage = gunTheBulletCameFrom.gunDamage;
            }
            else
            {
                //finalBulletDamage *= HP_HeatHandler.currentMultiplier;
            }

            if (damageDropOff > 3) //at losing its damage for the 5th time, it now does no damage
            {
                finalBulletDamage = 0;
            }
            //HP_DamageNumber.ShowDamageNumbers("" + finalBulletDamage, self, c); //OUTPUTS THE DAMAGE NUMBERS ON THE HEAD   

            //if the damage is from a bullet, override the damage causing function because the bullets do not function well
            //with the regular HealthManager.Hit() method, and instead we apply our own damage with the use of our self defined "DamageEnemies" class

            //Modding.Logger.Log(travelDistance);
            //DamageEnemies.HitEnemy(self, (int)finalBulletDamage, HP_BulletBehaviour.damage, 0); //note the 2nd "damage" var is a hitinstance, not an integer


        }



        public void Destroy()
        { 

        }
    }
}
