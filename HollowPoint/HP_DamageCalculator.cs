using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using ModCommon.Util;
using UnityEngine;
using GlobalEnums;


namespace HollowPoint
{
    public class HP_DamageCalculator : MonoBehaviour
    {
        //Put this at the prefabs class
        public static GameObject blockHitPrefab;
        public static System.Random rand = new System.Random();

        public static bool heroDamageCoroutineActive = false;

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

           

            On.HealthManager.Hit += EnemyIsHit;
            On.HeroController.TakeDamage += PlayerDamaged;
        }

        public void PlayerDamaged(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            if (go.name.Contains("KnightMadeExplosion"))
            {
                if (!heroDamageCoroutineActive)
                {
                    heroDamageCoroutineActive = true;
                    StartCoroutine(RocketJump(go));

                    HeroController.instance.GetAttr<HeroAudioController>("audioCtrl").PlaySound(GlobalEnums.HeroSounds.TAKE_HIT);
                    GameObject takeDam = HeroController.instance.GetAttr<GameObject>("takeHitDoublePrefab");
                    Instantiate(takeDam, HeroController.instance.transform.position, Quaternion.identity).SetActive(true);
                }
            }
            else if (HP_AttackHandler.slowWalk) //take double damage when bursting
            {
                orig(self, go, damageSide, (int) damageAmount, hazardType);
            }
            else
            {
                orig(self, go, damageSide, damageAmount, hazardType);
            }

        }
        
        public IEnumerator RocketJump(GameObject damagerGO)
        {
            Modding.Logger.Log("Player has been damaged by " + damagerGO.name);
            float explodeX = damagerGO.transform.position.x;
            float explodeY = damagerGO.transform.position.y;

            float knightX = HeroController.instance.transform.position.x;
            float knightY = HeroController.instance.transform.position.y;

            float slope = (explodeY - knightY) / (explodeX - knightX);

            Modding.Logger.Log("The slope is " + slope);

            float angle = (float) Math.Atan(slope);
            angle  = (float) (angle * 180 / Math.PI);

            Modding.Logger.Log("angle is " + angle);
            StartCoroutine(LaunchTowardsAngle(10f, angle));

            yield return new WaitForSeconds(1.2f);
            Modding.Logger.Log("Coroutine has ended");
            heroDamageCoroutineActive = false;
        }

        public IEnumerator LaunchTowardsAngle(float recoilStrength, float applyForceFromDegree)
        {
            //TODO: Develop the direction launch soon 

            Rigidbody2D knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");

            float deg = applyForceFromDegree;
            deg = Math.Abs(deg);
            if (applyForceFromDegree < 100 && applyForceFromDegree > 80 )
            {
                deg = 90;

            }

            deg = deg % 360;

            float radian = deg * Mathf.Deg2Rad;

            float xDeg = (float)((4 * recoilStrength) * Math.Cos(radian));
            float yDeg = (float)((4 * recoilStrength) * Math.Sin(radian));

            xDeg = (xDeg == 0) ? 0 : xDeg;
            yDeg = (yDeg == 0) ? 0 : yDeg;

            HeroController.instance.cState.shroomBouncing = true;

            if (deg == 90 || deg == 270)
            {
                knight.velocity = new Vector2(0, yDeg);
                yield break;
            }

            if (HeroController.instance.cState.facingRight)
            {
                //Modding.Logger.Log(HeroController.instance.GetAttr<float>("RECOIL_HOR_VELOCITY"));
                HeroController.instance.SetAttr<int>("recoilSteps", 0);
                HeroController.instance.cState.recoilingLeft = true;
                HeroController.instance.cState.recoilingRight = false;
                HeroController.instance.SetAttr<bool>("recoilLarge", true);

                knight.velocity = new Vector2(-xDeg, yDeg);
            }
            else
            {
                //Modding.Logger.Log(HeroController.instance.GetAttr<float>("RECOIL_HOR_VELOCITY"));
                HeroController.instance.SetAttr<int>("recoilSteps", 0);
                HeroController.instance.cState.recoilingLeft = false;
                HeroController.instance.cState.recoilingRight = true;
                HeroController.instance.SetAttr<bool>("recoilLarge", true);

                knight.velocity = new Vector2(xDeg, yDeg);
            }

            yield return null;
        }

        //Handles the damage
        public void EnemyIsHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            //Modding.Logger.Log(self.gameObject.name + " " + hitInstance.Source.name);
            if (hitInstance.Source.name.Contains("Gas"))
            {
                try
                {
                    hitInstance.DamageDealt = 20;
                    Modding.Logger.Log("[Damage Dealt by] " + hitInstance.Source.name);
                }
                catch(Exception e)
                {
                    Modding.Logger.Log("[Damage Calculator] " + e);
                }
                orig(self, hitInstance);
                return;
            }

            if (!hitInstance.Source.name.Contains("bullet"))
            {
                hitInstance.DamageDealt = 2 + PlayerData.instance.nailSmithUpgrades * 3;
                orig(self, hitInstance);
                return;
            }

            int soulGainAmt = 0;

            try
            {
                HP_BulletBehaviour hpbb = hitInstance.Source.GetComponent<HP_BulletBehaviour>();
                soulGainAmt = 0; //(hpbb.isSingleFire) ? 4 : 0;
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HP_DAMAGECALCULATOR](EnemyIsHit) could not find HP_BulletBehaviour component in hitInstance");
                soulGainAmt = 0;
            }
            int damage = rand.Next(3, 6) + PlayerData.instance.nailSmithUpgrades * 5;
            int bloodAmount = (int)(damage / (3 + (PlayerData.instance.nailSmithUpgrades * 3))) - 1;  

            // TODO: Put these in the weapon handler section
            //damage.AttackType = AttackTypes.Generic;
            // damage.DamageDealt = HP_WeaponHandler.currentGun.gunDamage;


            //if (hitInstance.Source.GetComponent<HP_BulletBehaviour>().special) damage *= 2;

            StartCoroutine(SplatterBlood(self.gameObject, bloodAmount));

            DamageEnemies.HitEnemy(self, damage, HP_BulletBehaviour.bulletHitInstance, soulGainAmt);
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

        public IEnumerator SplatterBlood(GameObject target, int repeat)
        {
            for (int i = 0; i < repeat; i++)
            {
                GameObject bloodSplat = Instantiate(HP_Prefabs.blood, target.transform.position, Quaternion.identity);
                bloodSplat.SetActive(true);
            }
            yield return new WaitForEndOfFrame();


        }

        public void Destroy()
        { 

        }
    }

    
}
