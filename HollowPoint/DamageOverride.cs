﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using static Modding.Logger;
using static HollowPoint.HollowPointEnums;
using UnityEngine;
using GlobalEnums;
using Vasi;

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

        public void PlayerDamaged(On.HeroController.orig_TakeDamage orig, HeroController self, GameObject go, CollisionSide damageSide, int damageAmount, int hazardType)
        {
            //Log("it fucking hurts oh god oh fuck damage dealt " + damageAmount);
            if (go.name.Contains("Gas Explosion") && PlayerData.instance.equippedCharm_5)
            {
                Log("negating bomb damage weary");
                orig(self, go, damageSide, damageAmount * 0, hazardType);
                return;
            }

            if (damageAmount > 0) Stats.instance.TakeAdrenalineEnergyDamage(damageAmount);

            orig(self, go, damageSide, damageAmount, hazardType);
        }


        private void HealthManager_Die(On.HealthManager.orig_Die orig, HealthManager self, float? attackDirection, AttackTypes attackType, bool ignoreEvasion)
        {
            //Log("ENEMY HAS DIED");

            //HollowPointPrefabs.SpawnObjectFromDictionary("Hatchling", self.transform.position, Quaternion.identity);

            orig(self, attackDirection, attackType, ignoreEvasion);
        }



           

        //Intercepts HealthManager's Hit method and allows me to override it with my own calculation
        public void HealthManager_HitHook(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            //Alternative hit damages from other sources like weaver or explosions 
            string srcName = hitInstance.Source.name;

            if (srcName.Contains("Gas"))
            {
                //Explosion damage
                hitInstance.DamageDealt = 3 + (PlayerData.instance.nailSmithUpgrades * 5);
                orig(self, hitInstance);
                return;
            }
            if (srcName.Contains("Dung"))
            {
                //Explosion damage
                Log("Dung damage!");
                orig(self, hitInstance);
                return;
            }
            else if (srcName.Contains("Damager"))
            {
                //Glowing Womblings
                //HeroController.instance.AddMPCharge(15);
                orig(self, hitInstance);
                return;
            }
            else if (srcName.Contains("Great Slash") || srcName.Contains("Dash Slash"))
            {
                Log("Player is nail art... ing?");


                orig(self, hitInstance);
                return;
            }
            else if (srcName.Contains("Slash"))
            {
                Log("Player is slashing!");
                hitInstance.DamageDealt = 3 + (PlayerData.instance.nailSmithUpgrades * 3);
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

            int damage = hpbb.bulletDamage + (PlayerData.instance.nailSmithUpgrades * hpbb.bulletDamageScale);
            int soulGainAmt = Stats.SoulGainPerHit();
            //StartCoroutine(SplatterBlood(self.gameObject.transform.position, 1, cardinalDirection * 90));
            if (hpbb.appliesDamageOvertime)
            {
                EnemyDamageOvertime edo = self.gameObject.GetComponent<EnemyDamageOvertime>();

                if (edo == null) self.gameObject.AddComponent<EnemyDamageOvertime>();
                else
                {
                    edo.IncreaseStack();
                }
            }
            DamageEnemyOverride(self, damage, BulletBehaviour.bulletDummyHitInstance, soulGainAmt, hpbb);
        }
      
        //+++DAMAGE OVERRIDE+++
        public static void DamageEnemyOverride(HealthManager targetHP, int damageDealt, HitInstance hitInstance, int soulGain, BulletBehaviour hpbb)
        {
            //TODO: this specifics might add up later, Moss Charger is just one of the few except and there maybe many more
            if (targetHP == null) return;

            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(targetHP.transform));
            GameObject blockHitPrefab = Mirror.GetField<HealthManager, GameObject>(targetHP, "blockHitPrefab");

            bool specialEnemy = (targetHP.name.Contains("Charger"));
            if (targetHP.IsBlockingByDirection(cardinalDirection, AttackTypes.Nail) && !specialEnemy)
            {
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "BLOCKED HIT", false);
                GameObject blockHit = blockHitPrefab.Spawn();
                blockHit.transform.position = targetHP.transform.position;
                blockHit.transform.Rotate(new Vector3(0, 0, 90 * cardinalDirection));
                return;
            }

            if (false && !targetHP.IsInvincible) //enable disable damage overtime
            {
                EnemyDamageOvertime dm = targetHP.gameObject.GetComponent<EnemyDamageOvertime>();
                if (dm == null) targetHP.gameObject.AddComponent<EnemyDamageOvertime>();
                else dm.IncreaseStack();
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
            GameObject sendHitTo = Mirror.GetField<HealthManager, GameObject>(targetHP, "sendHitTo");
            if (sendHitTo != null)
            {
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            }

            GameObject HitPrefab = Mirror.GetField<HealthManager, GameObject>(targetHP, "strikeNailPrefab");
            GameObject ImpactPrefab = Mirror.GetField<HealthManager, GameObject>(targetHP, "slashImpactPrefab");
            Vector3 effectOrigin = Mirror.GetField<HealthManager, Vector3>(targetHP, "effectOrigin");
            if (HitPrefab != null && effectOrigin != null)
            {
                HitPrefab.Spawn(targetHP.transform.position + effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }
            if (ImpactPrefab != null && effectOrigin != null)
            {
                ImpactPrefab.Spawn(targetHP.transform.position + effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }
            SpriteFlash f = targetHP.gameObject.GetComponent<SpriteFlash>();
            if (f != null) f.flashWhiteQuick();

            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TOOK DAMAGE", false);
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TAKE DAMAGE", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);

            // Actually do damage to target.
            LoadAssets.sfxDictionary.TryGetValue("enemyhurt" + rand.Next(1, 4) + ".wav", out AudioClip hurtSound);
            HeroController.instance.GetComponent<AudioSource>().PlayOneShot(hurtSound);

            if (targetHP.damageOverride)
            {
                targetHP.hp -= 1;
            }
            else
            {
                targetHP.hp -= damageDealt; // the actual damage          
                if(hpbb.canGainEnergyCharges) HeroController.instance.AddMPCharge(Stats.instance.current_soulGainedPerHit);
                Stats.instance.IncreaseAdrenalineChargeEnergy();
                //TODO: change this audio source location to the sound handler
                AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.EnemyHitSFXGO);
            }
            // Trigger Enemy Kill
            if (targetHP.hp <= 0f)
            {
                EnemyDeathEvent(targetHP, cardinalDirection, true);
                return;
            }

            bool hasAlternateHitAnimation = Mirror.GetField<HealthManager, bool>(targetHP, "hasAlternateHitAnimation");
            string alternateHitAnimation = Mirror.GetField<HealthManager, string>(targetHP, "alternateHitAnimation");
            if (/*hasAlternateHitAnimation != null && */hasAlternateHitAnimation && targetHP.GetComponent<tk2dSpriteAnimator>() && alternateHitAnimation != null)
            {
                targetHP.GetComponent<tk2dSpriteAnimator>().Play(alternateHitAnimation);
            }

            PlayMakerFSM stunControlFSM = targetHP.gameObject.GetComponents<PlayMakerFSM>().FirstOrDefault(component => component.FsmName == "Stun Control" || component.FsmName == "Stun");
            //if (stunControlFSM != null) stunControlFSM.SendEvent("STUN DAMAGE");
        }

        public static void EnemyDeathEvent(HealthManager hm, float deathDirection, bool playDeathFromBulletSound)
        {
            if (playDeathFromBulletSound) AudioHandler.instance.PlayMiscSoundEffect(AudioHandler.HollowPointSoundType.EnemyKillSFXGO);

            hm.Die(deathDirection * 90, AttackTypes.Spell, true);
            HeroController.instance.AddMPCharge(Stats.instance.AddSoulOnEnemyKill());
            Stats.instance.IncreaseAdrenalineChargeEnergy();
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


        public static IEnumerator SplatterBlood(Vector3 spawnPos, int repeat, float directionOfBlood)
        {
            for (int i = 0; i < repeat; i++)
            {

                GameObject bloodSplat = Instantiate(HollowPointPrefabs.blood, spawnPos, Quaternion.identity);
                bloodSplat.transform.localScale = new Vector3(0.25f, 0.25f, 0.1f);
                bloodSplat.transform.Rotate(new Vector3(0, 0, directionOfBlood));
                bloodSplat.SetActive(true);
            }
            yield return new WaitForEndOfFrame();
        }

    }

}
