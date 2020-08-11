using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using static Modding.Logger;
using static HollowPoint.HollowPointEnums;
using ModCommon.Util;
using UnityEngine;
using GlobalEnums;

namespace HollowPoint
{
    public class DamageOverride : MonoBehaviour
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

            On.HeroController.TakeDamage += PlayerDamaged;
            On.HealthManager.Hit += HealthManager_HitHook;
            On.HealthManager.Die += HealthManager_Die;
        }

        private void HealthManager_Die(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            Log("ENEMY HAS DIED");


            orig(self, attackDirection, attackType, ignoreEvasion);
        }

        public void PlayerDamaged(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            Log("it fucking hurts oh god oh fuck damage dealt " + damageAmount);

            Stats.Stats_TakeDamageEvent();
            if (go.name.Contains("Gas Explosion") && PlayerData.instance.equippedCharm_5)
            {
                Log("negating bomb damage weary");
                //TODO: remove this because it sometimes causes the player to still receive damage when rocket jumping
            }
            //Adrenaline from fragile heart

            if (!Stats.hasActivatedAdrenaline && (PlayerData.instance.health <= damageAmount + 1) && PlayerData.instance.equippedCharm_27)
            {
                Stats.hasActivatedAdrenaline = true;
                HeroController.instance.AddMPCharge(99);
                orig(self, go, damageSide, 0, hazardType);
                return;
            }
            else if (!Stats.hasActivatedAdrenaline && (PlayerData.instance.health <= damageAmount + 1)) 
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

                Stats.hasActivatedAdrenaline = true;

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

        //Intercepts HealthManager's Hit method and allows me to override it with my own calculation
        public void HealthManager_HitHook(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            //Alternative hit damages from other sources like weaver or explosions 
            string srcName = hitInstance.Source.name;
            Log("[DamageOverride] Source Name is " + srcName);

            if (srcName.Contains("Gas"))
            {
                //Explosion damage
                hitInstance.DamageDealt = 15 + (PlayerData.instance.nailSmithUpgrades * 5);
                orig(self, hitInstance);
                return;
            }
            else if (srcName.Contains("Damager"))
            {
                //Glowing Womblings
                HeroController.instance.AddMPCharge(15);
                orig(self, hitInstance);
                return;
            }
            else if (srcName.Contains("Great Slash") || srcName.Contains("Dash Slash"))
            {
                Log("Player is nail art... ing?");
                hitInstance.DamageDealt = 5;

                float dir = (HeroController.instance.cState.facingRight) ? 180 : 0;
                Stats.IncreaseAdrenalinePoints(35);
                StartCoroutine(SplatterBlood(self.gameObject.transform.position, 10, dir));

                orig(self, hitInstance);
                return;
            }
            else if (srcName.Contains("Slash"))
            {
                Log("Player is slashing!");
                hitInstance.DamageDealt = 2 + (PlayerData.instance.nailSmithUpgrades * 2);
                orig(self, hitInstance);
                return;
            }
            else if (!srcName.Contains("bullet"))
            {           
                orig(self, hitInstance);
                return;
            }

            BulletBehaviour hpbb = hitInstance.Source.GetComponent<BulletBehaviour>();
            Vector3 bulletOriginPosition = hitInstance.Source.GetComponent<BulletBehaviour>().bulletOriginPosition;   
            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(self.transform));

            int damage = Stats.CalculateDamage(bulletOriginPosition, self.transform.position, hpbb);
            int soulGainAmt = Stats.CalculateSoulGain();

            //Log("DamageCalculator, damage dealt is " + damage + " against " + self.name)
            //StartCoroutine(DamageOverTime(self)); //damage enemy overtime
            StartCoroutine(SplatterBlood(self.gameObject.transform.position, 1, cardinalDirection * 90));
            //HealthManagerOverride.DamageEnemyOverride(self, damage, BulletBehaviour.bulletDummyHitInstance, soulGainAmt, hpbb);     
            DamageEnemyOverride(self, damage, BulletBehaviour.bulletDummyHitInstance, soulGainAmt, hpbb);
        }

        public static IEnumerator SplatterBlood(Vector3 spawnPos, int repeat, float directionOfBlood)
        {
            for (int i = 0; i < repeat; i++)
            {
        
                GameObject bloodSplat = Instantiate(HollowPointPrefabs.blood, spawnPos, Quaternion.identity);
                bloodSplat.transform.localScale = new Vector3(0.25f,0.25f, 0.1f);
                bloodSplat.transform.Rotate(new Vector3(0, 0, directionOfBlood));
                bloodSplat.SetActive(true);
            }
            yield return new WaitForEndOfFrame();
        }

        public static IEnumerator DamageOverTime(HealthManager hm)
        {
            //ParticleSystem fire = Instantiate(burnEffects, hm.transform.position, Quaternion.identity);      

            for (int i = 0; i < 8; i++)
            {
                yield return new WaitForEndOfFrame();
                if (hm == null)
                {
                    Log("[DamageOverride] Game Object/HealthManager null, cannot apply burning");
                    yield break;
                }

                hm.hp -= 2;
                //ameObject bloodSplat = Instantiate(HollowPointPrefabs.blood, hm.gameObject.transform.position, Quaternion.identity);
                //bloodSplat.transform.localScale = new Vector3(0.25f, 0.25f, 0.1f);
                //bloodSplat.SetActive(true);
                SpriteFlash f = hm.gameObject.GetComponent<SpriteFlash>();
                if (f != null) f.flashWhiteQuick();
                if (hm.hp <= 0)
                {
                    EnemyDeathEvent(hm, 90, false);
                    yield break;
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        public static void DamageEnemyOverride(HealthManager targetHP, int damageDealt, HitInstance hitInstance, int soulGain, BulletBehaviour hpbb)
        {
            //TODO: this specifics might add up later, Moss Charger is just one of the few except and there maybe many more
            if (targetHP == null) return;

            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(targetHP.transform));
            GameObject blockHitPrefab = targetHP.GetAttr<GameObject>("blockHitPrefab");

            bool specialEnemy = (targetHP.name.Contains("Charger"));
            if (targetHP.IsBlockingByDirection(cardinalDirection, AttackTypes.Nail) && !specialEnemy || damageDealt <= 0)
            {
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "BLOCKED HIT", false);
                GameObject blockHit = blockHitPrefab.Spawn();
                blockHit.transform.position = targetHP.transform.position;
                blockHit.transform.Rotate(new Vector3(0, 0, 90 * cardinalDirection));
                return;
            }
            //bool specialEnemy = (targetHP.name.Contains("Moss Charger") || targetHP.name.Contains("Mushroom Brawler")); 

            /*
            if (targetHP.IsInvincible && !specialEnemy && !PlayerData.instance.equippedCharm_25)
            {
                GameObject blockHit = blockHitPrefab.Spawn();
                blockHit.transform.position = targetHP.transform.position;
               return;
            }
            */
            if (targetHP.gameObject.name.Contains("Blocker")) //double damage baldurs
            {
                damageDealt = damageDealt * 4;
            }

            Recoil recoil = targetHP.gameObject.GetComponent<Recoil>();
            //if (recoil != null && PlayerData.instance.equippedCharm_15)
            if (recoil != null)
            {
                recoil.RecoilByDirection(cardinalDirection, 0.25f);
            }

            /*
             * Mostly code copied from the healthmanager class itself.
             */

            FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            GameObject sendHitGO = targetHP.GetAttr<GameObject>("sendHitGO");
            if (sendHitGO != null)
            {
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            }

            GameObject HitPrefab = targetHP.GetAttr<GameObject>("strikeNailPrefab");
            GameObject ImpactPrefab = targetHP.GetAttr<GameObject>("slashImpactPrefab");
            Vector3? effectOrigin = targetHP.GetAttr<Vector3?>("effectOrigin");
            if (HitPrefab != null && effectOrigin != null)
            {
                HitPrefab.Spawn(targetHP.transform.position + (Vector3)effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }
            if (ImpactPrefab != null && effectOrigin != null)
            {
                ImpactPrefab.Spawn(targetHP.transform.position + (Vector3)effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }
            SpriteFlash f = targetHP.gameObject.GetComponent<SpriteFlash>();
            if (f != null) f.flashWhiteQuick();

            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TOOK DAMAGE", false);
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TAKE DAMAGE", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);

            // Actually do damage to target.
            LoadAssets.sfxDictionary.TryGetValue("enemyhurt" + rand.Next(1, 4) + ".wav", out AudioClip hurtSound);
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(hurtSound);

            if (targetHP.damageOverride)
            {
                targetHP.hp -= 1;
            }
            else
            {
                targetHP.hp -= damageDealt; // the actual damage          

                //int sg = (ds.Equals(DamageSeverity.Minor)) ? 0 : soulGain;
                HeroController.instance.AddMPCharge(3);

                if (Stats.canGainAdrenaline)
                {
                    Stats.IncreaseAdrenalinePoints(3);
                }
            }

            // Trigger Enemy Kill
            if (targetHP.hp <= 0f)
            {
                EnemyDeathEvent(targetHP, cardinalDirection, true);
                return;
            }

            bool? hasAlternateHitAnimation = targetHP.GetAttr<bool?>("hasAlternateHitAnimation");
            string alternateHitAnimation = targetHP.GetAttr<string>("alternateHitAnimation");
            if (hasAlternateHitAnimation != null && (bool)hasAlternateHitAnimation && targetHP.GetComponent<tk2dSpriteAnimator>() && alternateHitAnimation != null)
            {
                targetHP.GetComponent<tk2dSpriteAnimator>().Play(alternateHitAnimation);
            }

            PlayMakerFSM stunControlFSM = targetHP.gameObject.GetComponents<PlayMakerFSM>().FirstOrDefault(component => component.FsmName == "Stun Control" || component.FsmName == "Stun");
            //if (stunControlFSM != null) stunControlFSM.SendEvent("STUN DAMAGE");
        }

        public static void EnemyDeathEvent(HealthManager hm, float deathDirection, bool playCustomDeathSound)
        {
            if (playCustomDeathSound)
            {
                LoadAssets.sfxDictionary.TryGetValue("enemydead" + rand.Next(1, 4) + ".wav", out AudioClip deadSound);
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(deadSound);
            }
            hm.Die(deathDirection * 90, AttackTypes.Spell, true);
            HeroController.instance.AddMPCharge(Stats.MPChargeOnKill());
            Stats.ExtendAdrenalineTime(2);
            //GameManager.instance.FreezeMoment(1);

            //Log("Spawning Weavers");
            //GameObject weaverPrefab = HollowPointPrefabs.prefabDictionary["Weaverling"];
            //GameObject weaverClone = Instantiate(weaverPrefab, HeroController.instance.transform.position, Quaternion.identity);
            //Destroy(weaverClone, 10f);
        }

        public void OnDestroy()
        {
            On.HealthManager.Hit -= HealthManager_HitHook;
            On.HeroController.TakeDamage -= PlayerDamaged;
            Destroy(gameObject.GetComponent<DamageOverride>());
        }
    }
}
