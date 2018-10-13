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

            // TODO: Add possible optional damage multiplier information below.

            /*
            double multiplier = 1;
            if (PlayerData.instance.GetBool("equippedCharm_25"))
            {
                multiplier *= 1.5;
            }
            if (PlayerData.instance.GetBool("equippedCharm_6") && PlayerData.instance.GetInt("health") == 1)
            {
                multiplier *= 1.75f;
            }

            if (gng_bindings.hasNailBinding())
            {
                multiplier *= 0.3;
            }
            realDamage = (int) Math.Round(realDamage * multiplier);
            
            */

            if (realDamage <= 0)
            {
                return;
            }

            if (targetHP == null) return;

            /*
             * Play animations and such...
             * Mostly code copied from the healthmanager class itself.
             */

            int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(targetHP.transform));
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            GameObject sendHitGO = targetHP.GetAttr<GameObject>("sendHitGO");
            if (sendHitGO != null)
            {
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            }

            //GameObject fireballHitPrefab = targetHP.GetAttr<GameObject>("fireballHitPrefab");
            //Vector3? effectOrigin = targetHP.GetAttr<Vector3?>("effectOrigin");

            //if (fireballHitPrefab != null && effectOrigin != null)
            //{
            //    fireballHitPrefab.Spawn(targetHP.transform.position + (Vector3)effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            //}


            GameObject HitPrefab = targetHP.GetAttr<GameObject>("fireballHitPrefab");
            Vector3? effectOrigin = targetHP.GetAttr<Vector3?>("effectOrigin");

            if (HitPrefab != null && effectOrigin != null)
            {
                HitPrefab.Spawn(targetHP.transform.position + (Vector3)effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }

            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TOOK DAMAGE", false);


            //if ((UnityEngine.Object)targetHP.GetComponent<Recoil>() != (UnityEngine.Object)null)
            //    targetHP.GetComponent<Recoil>().RecoilByDirection(cardinalDirection, hitInstance.MagnitudeMultiplier);

            FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);

            // Actually do damage to target.


            try
            {
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0,2)]);
            }
            catch(Exception e)
            {
                Modding.Logger.Log("Enemy Hurt Exception Thrown " + e);
            }

            if (targetHP.damageOverride)
            {
                targetHP.hp -= 1;
            }
            else
            {
                targetHP.hp -= realDamage;
                HeroController.instance.AddMPCharge(soulGain);
            }

            // Trigger Kill animation
            if (targetHP.hp <= 0f)
            {
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyDeadSFX[soundRandom.Next(0, 2)]);
                targetHP.Die(0f, AttackTypes.Spell, true);
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
                stunControlFSM.SendEvent("STUN DAMAGE");
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

    }
}