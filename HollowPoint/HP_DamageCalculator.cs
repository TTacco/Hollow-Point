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

            On.HealthManager.Hit += EnemyIsHit;
            On.HeroController.TakeDamage += PlayerDamaged;
        }

        public void PlayerDamaged(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {

            if (go.name.Contains("KnightMadeExplosion") && PlayerData.instance.equippedCharm_5)
            {
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

        //Handles the damage
        public void EnemyIsHit(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            //Modding.Logger.Log(self.gameObject.name + " " + hitInstance.Source.name);
            //Alternative hit damages from other sources like weaver or explosions 
            Modding.Logger.Log(self.name);

            if (hitInstance.Source.name.Contains("Gas"))
            {
                
                hitInstance.DamageDealt = 15 + (PlayerData.instance.nailSmithUpgrades * 10);
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

            int soulGainAmt = 0;

            HP_BulletBehaviour hpbb = null;

            hpbb = hitInstance.Source.GetComponent<HP_BulletBehaviour>();

            //==============Soul Gain Amount============
            soulGainAmt = 7; 
            soulGainAmt += (PlayerData.instance.equippedCharm_20) ? 3 : 0;
            soulGainAmt += (PlayerData.instance.equippedCharm_21) ? 5 : 0;

            int damage = rand.Next(3, 5) + PlayerData.instance.nailSmithUpgrades * 4;

            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(self.transform));
            if(!self.IsInvincible) StartCoroutine(SplatterBlood(self.gameObject, 2, cardinalDirection * 90));

            //==============Damage=================
            //Fluke damage
            if (PlayerData.instance.equippedCharm_11)
            {
                damage = (int) (damage / 2f);
                soulGainAmt = (int)(soulGainAmt / 3f);
            }
            //For MoP damage
            if (PlayerData.instance.equippedCharm_13)
            {
                Vector3 bulletOriginPosition = hitInstance.Source.GetComponent<HP_BulletBehaviour>().bulletOriginPosition;
                float travelDistance = Vector3.Distance(bulletOriginPosition, self.transform.position);
                damage = (int) Mathf.Floor(travelDistance * 1.5f) + damage;
                Modding.Logger.Log("Travel distance " + travelDistance);
                soulGainAmt += (int) travelDistance;
            }
            if (PlayerData.instance.equippedCharm_6)
            {
                damage = (int) (damage * 1.25f);
            }

            Modding.Logger.Log("DamageCalculator, damage dealt is " + damage);

            DamageEnemies.HitEnemy(self, damage, HP_BulletBehaviour.bulletHitInstance, soulGainAmt);
            return;
            //HP_DamageNumber.ShowDamageNumbers("" + finalBulletDamage, self, c); //OUTPUTS THE DAMAGE NUMBERS ON THE HEAD   

            //if the damage is from a bullet, override the damage causing function because the bullets do not function well
            //with the regular HealthManager.Hit() method, and instead we apply our own damage with the use of our self defined "DamageEnemies" class

            //Modding.Logger.Log(travelDistance);
            //DamageEnemies.HitEnemy(self, (int)finalBulletDamage, HP_BulletBehaviour.damage, 0); //note the 2nd "damage" var is a hitinstance, not an integer


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
            On.HealthManager.Hit -= EnemyIsHit;
            On.HeroController.TakeDamage -= PlayerDamaged;
            Destroy(gameObject.GetComponent<HP_DamageCalculator>());
        }
    }

    
}
