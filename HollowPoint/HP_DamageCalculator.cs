using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using static Modding.Logger;
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
        GameObject healSoundGO;

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

            healSoundGO = new GameObject("healSoundGO", typeof(AudioSource));
            DontDestroyOnLoad(healSoundGO);

            rand = new System.Random();
            On.HealthManager.Hit += HealthManager_Hit_Hook;
            On.HeroController.TakeDamage += PlayerDamaged;
        }

        public void PlayerDamaged(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {

            if (go.name.Contains("Gas Explosion") && PlayerData.instance.equippedCharm_5)
            {
                Log("negating bomb damage weary");
                if (!heroDamageCoroutineActive)
                {
                    heroDamageCoroutineActive = true;
                    StartCoroutine(RocketJump(go));
                    //HeroController.instance.GetAttr<HeroAudioController>("audioCtrl").PlaySound(GlobalEnums.HeroSounds.TAKE_HIT);
                    //GameObject takeDam = HeroController.instance.GetAttr<GameObject>("takeHitDoublePrefab");
                    //Instantiate(takeDam, HeroController.instance.transform.position, Quaternion.identity).SetActive(true);  
                    orig(self, go, damageSide, 0, hazardType);
                    return;
                }
            }
            //Adrenaline from fragile heart

            if (!HP_Stats.hasActivatedAdrenaline && (PlayerData.instance.health <= damageAmount + 1) && PlayerData.instance.equippedCharm_27)
            {
                HP_Stats.hasActivatedAdrenaline = true;
                HeroController.instance.AddMPCharge(99);
                orig(self, go, damageSide, 0, hazardType);
                return;
            }
            else if (!HP_Stats.hasActivatedAdrenaline && (PlayerData.instance.health <= damageAmount + 1)) 
            {

                LoadAssets.sfxDictionary.TryGetValue("focussound.wav", out AudioClip ac);
                AudioSource aud = healSoundGO.GetComponent<AudioSource>();
                aud.PlayOneShot(ac);

                GameObject artChargeFlash = Instantiate(HeroController.instance.artChargedFlash, HeroController.instance.transform.position, Quaternion.identity);
                artChargeFlash.SetActive(true);
                artChargeFlash.transform.SetParent(HeroController.instance.transform);
                Destroy(artChargeFlash, 0.5f);

                GameObject dJumpFlash = Instantiate(HeroController.instance.dJumpFlashPrefab, HeroController.instance.transform.position, Quaternion.identity);
                dJumpFlash.SetActive(true);
                dJumpFlash.transform.SetParent(HeroController.instance.transform);
                Destroy(dJumpFlash, 0.5f);

                HP_Stats.hasActivatedAdrenaline = true;

                HeroController.instance.AddHealth(3);
                orig(self, go, damageSide, 0, hazardType);
                return;
            }
            else if (PlayerData.instance.equippedCharm_6)
            {
                orig(self, go, damageSide, damageAmount*2, hazardType);
                return;
            }



            orig(self, go, damageSide, damageAmount, hazardType);
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

        //Intercepts HealthManager's Hit method and allows me to override it with my own calculation
        public void HealthManager_Hit_Hook(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            //Modding.Logger.Log(self.gameObject.name + " " + hitInstance.Source.name);
            //Alternative hit damages from other sources like weaver or explosions 
            Modding.Logger.Log(self.name);

            if (hitInstance.Source.name.Contains("Gas"))
            {
                
                hitInstance.DamageDealt = 15 + (PlayerData.instance.nailSmithUpgrades * 5);
                orig(self, hitInstance);
                return;
            }
            else if (hitInstance.Source.name.Contains("Damager"))
            {
                HeroController.instance.AddMPCharge(15);
                orig(self, hitInstance);
                return;
            }

            if (!hitInstance.Source.name.Contains("bullet"))
            {
                hitInstance.DamageDealt = 3 + PlayerData.instance.nailSmithUpgrades * 3;
                orig(self, hitInstance);
                return;
            }

            HP_BulletBehaviour hpbb = hitInstance.Source.GetComponent<HP_BulletBehaviour>();
            Vector3 bulletOriginPosition = hitInstance.Source.GetComponent<HP_BulletBehaviour>().bulletOriginPosition;

            //==============Soul Gain Amount============
            int soulGainAmt = HP_Stats.CalculateSoulGain();

            //==============Damage=================
            int damage = HP_Stats.CalculateDamage(bulletOriginPosition, self.transform.position);

            DamageEnemies.HitEnemy(self, damage, HP_BulletBehaviour.bulletHitInstance, soulGainAmt);
            Modding.Logger.Log("DamageCalculator, damage dealt is " + damage + " against " + self.name);

            //==============Blood Splatter Effect================= 
            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(self.transform));
            if (!self.IsInvincible) StartCoroutine(SplatterBlood(self.gameObject, 1, cardinalDirection * 90));
        }

        public static IEnumerator SplatterBlood(GameObject target, int repeat, float directionOfBlood)
        {
            for (int i = 0; i < repeat; i++)
            {
                GameObject bloodSplat = Instantiate(HP_Prefabs.blood, target.transform.position, Quaternion.identity);
                bloodSplat.transform.Rotate(new Vector3(0, 0, directionOfBlood));
                bloodSplat.SetActive(true);
            }
            yield return new WaitForEndOfFrame();
        }

        public void OnDestroy()
        {
            On.HealthManager.Hit -= HealthManager_Hit_Hook;
            On.HeroController.TakeDamage -= PlayerDamaged;
            Destroy(gameObject.GetComponent<HP_DamageCalculator>());
        }
    }
}
