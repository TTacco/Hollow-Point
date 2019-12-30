using System.Linq;
using ModCommon.Util;
using UnityEngine;
using System;
using System.Collections;


namespace HollowPoint
{
    public static class DamageEnemies
    {
        static System.Random soundRandom = new System.Random();

        // This function does damage to the enemy using the damage numbers given by the weapon type
        public static void HitEnemy(HealthManager targetHP, int expectedDamage, HitInstance hitInstance, int soulGain)
        {
            int realDamage = expectedDamage;

            //TODO: this specifics might add up later, Moss Charger is just one of the few except and there maybe many more
            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(targetHP.transform));
            GameObject blockHitPrefab = targetHP.GetAttr<GameObject>("blockHitPrefab");

            if ((targetHP.IsInvincible && !targetHP.name.Contains("Moss Charger")) && !PlayerData.instance.equippedCharm_15)
            {
                GameObject blockHit = blockHitPrefab.Spawn();
                blockHit.transform.position = targetHP.transform.position;
                return;
            }

            Recoil recoil = targetHP.gameObject.GetComponent<Recoil>();

            if (recoil != null && PlayerData.instance.equippedCharm_15)
            {
                recoil.RecoilByDirection(cardinalDirection, 1.5f);
            }

            if (realDamage <= 0)
            {
                return;
            }

            if (targetHP == null) return;

            /*
             * Play animations and such...
             * Mostly code copied from the healthmanager class itself.
             */




            //Modding.Logger.Log("Cardinal is " + cardinalDirection);
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            GameObject sendHitGO = targetHP.GetAttr<GameObject>("sendHitGO");
            if (sendHitGO != null)
            {
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            }

            GameObject HitPrefab = targetHP.GetAttr<GameObject>("fireballHitPrefab");
            Vector3? effectOrigin = targetHP.GetAttr<Vector3?>("effectOrigin");

            if (HitPrefab != null && effectOrigin != null)
            {
                HitPrefab.Spawn(targetHP.transform.position + (Vector3)effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }

            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TOOK DAMAGE", false);
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TAKE DAMAGE", false);

            FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);

            
            // Actually do damage to target.
            try
            {
                //TODO: change this audio source
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                LoadAssets.sfxDictionary.TryGetValue("enemyhurt" + soundRandom.Next(1, 4) + ".wav", out AudioClip ac);
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(ac);
            }
            catch (Exception e)
            {
                Modding.Logger.Log("Enemy Hurt Exception Thrown " + e);
            }

            if (targetHP.damageOverride)
            {
                targetHP.hp -= 1;
            }
            else
            {
                targetHP.hp -= realDamage; // the actual damage                                                       
                HeroController.instance.AddMPCharge(soulGain);
            }

            // Trigger Kill animation
            if (targetHP.hp <= 0f)
            {
                LoadAssets.sfxDictionary.TryGetValue("enemydead" + soundRandom.Next(1, 4) + ".wav", out AudioClip ac);
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(ac);
                targetHP.Die(cardinalDirection * 90, AttackTypes.Spell, true);
                return;
            }


            bool? hasAlternateHitAnimation = targetHP.GetAttr<bool?>("hasAlternateHitAnimation");
            string alternateHitAnimation = targetHP.GetAttr<string>("alternateHitAnimation");
            if (hasAlternateHitAnimation != null && (bool)hasAlternateHitAnimation && targetHP.GetComponent<tk2dSpriteAnimator>() && alternateHitAnimation != null)
            {
                targetHP.GetComponent<tk2dSpriteAnimator>().Play(alternateHitAnimation);
            }


            PlayMakerFSM stunControlFSM = targetHP.gameObject.GetComponents<PlayMakerFSM>().FirstOrDefault(component =>
                component.FsmName == "Stun Control" || component.FsmName == "Stun");
            if (stunControlFSM != null)
            {
                //stunControlFSM.SendEvent("STUN DAMAGE");
            }

            /*
             * Uncomment below for a sick looking enter the gungeon style freeze frame or for camera shake.
             */
            //GameManager.instance.FreezeMoment(1);
            //GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");
            SpriteFlash f = targetHP.gameObject.GetComponent<SpriteFlash>();
            if (f != null)
            {
                f.flashWhiteQuick();
            }

        }

        private static HealthManager getHealthManagerRecursive(GameObject target)
        {
            HealthManager targetHP = target.GetComponent<HealthManager>();
            int i = 6;
            while (targetHP == null && i > 0)
            {
                targetHP = target.GetComponent<HealthManager>();
                if (target.transform.parent == null)
                {
                    return targetHP;
                }
                target = target.transform.parent.gameObject;
                i--;
            }

            return targetHP;
        }

        /*
        public IEnumerator SplatterBlood(int repeat)
        {
            for (int i = 0; i < repeat; i++)
            {
                GameObject bloodSplat = Instantiate(HP_Prefabs.blood, gameObject.transform.position, Quaternion.identity);
                bloodSplat.SetActive(true);
            }
            yield return new WaitForEndOfFrame();


        }

        StartCoroutine(SplatterBlood(2));
        */

    }
}