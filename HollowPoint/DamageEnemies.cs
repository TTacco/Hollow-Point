using System.Linq;
using ModCommon.Util;
using UnityEngine;
using System;

namespace HollowPoint
{
    public static class DamageEnemies
    {
        // This function does damage to the enemy using the damage numbers given by the weapon type
        public static void hitEnemy(HealthManager targetHP, int expectedDamage, HitInstance hitInstance, int soulGain)
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

            try
            {
                int cardinalDirection = DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(targetHP.transform));
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
                GameObject sendHitGO = targetHP.GetAttr<GameObject>("sendHitGO");
                if (sendHitGO != null)
                {
                    FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
                }

                GameObject fireballHitPrefab = targetHP.GetAttr<GameObject>("fireballHitPrefab");
                Vector3? effectOrigin = targetHP.GetAttr<Vector3?>("effectOrigin");

                if (fireballHitPrefab != null && effectOrigin != null)
                {
                    fireballHitPrefab.Spawn(targetHP.transform.position + (Vector3)effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
                }

                FSMUtility.SendEventToGameObject(targetHP.gameObject, "TOOK DAMAGE", false);

                if ((UnityEngine.Object)targetHP.GetComponent<Recoil>() != (UnityEngine.Object)null)
                    targetHP.GetComponent<Recoil>().RecoilByDirection(cardinalDirection, hitInstance.MagnitudeMultiplier);

                FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
                FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);

            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HOLLOW POINT DEBUG] Over here at line 50~ " + e);
            }

            // Actually do damage to target.
            try
            {
                if (targetHP.damageOverride)
                {
                    targetHP.hp -= 1;
                }
                else
                {
                    targetHP.hp -= realDamage;
                    HeroController.instance.AddMPCharge(soulGain);
                }
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HOLLOW POINT DEBUG] Over here at line 80~ at the Actually Do Damage section " + e);
            }

            // Trigger Kill animation
            try
            {
                if (targetHP.hp <= 0f)
                {
                    targetHP.Die(0f, AttackTypes.Nail, true);
                    return;
                }


                bool? hasAlternateHitAnimation = targetHP.GetAttr<bool?>("hasAlternateHitAnimation");
                string alternateHitAnimation = targetHP.GetAttr<string>("alternateHitAnimation");
                if (hasAlternateHitAnimation != null && (bool)hasAlternateHitAnimation && targetHP.GetComponent<tk2dSpriteAnimator>() && alternateHitAnimation != null)
                {
                    targetHP.GetComponent<tk2dSpriteAnimator>().Play(alternateHitAnimation);
                }

            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HOLLOW POINT DEBUG] Over here at line 100~ at the Trigger Kill Animation section" + e);
            }


            try
            {
                PlayMakerFSM stunControlFSM = targetHP.gameObject.GetComponents<PlayMakerFSM>().FirstOrDefault(component =>
                    component.FsmName == "Stun Control" || component.FsmName == "Stun");
                if (stunControlFSM != null)
                {
                    stunControlFSM.SendEvent("STUN DAMAGE");
                }
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HOLLOW POINT DEBUG] Over here at line 120~ at the Stun Damage" + e);
            }

            /*
             * Uncomment below for a sick looking enter the gungeon style freeze frame or for camera shake.
             */
            //GameManager.instance.FreezeMoment(1);
            GameCameras.instance.cameraShakeFSM.SendEvent("EnemyKillShake");

            try{
                SpriteFlash f = targetHP.gameObject.GetComponent<SpriteFlash>();
                if (f != null)
                {
                    f.flashWhiteQuick();
                }
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HOLLOW POINT DEBUG] Over here at line 140~ at the flash quick" + e);
            }

        }

        private static HealthManager getHealthManagerRecursive(GameObject target)
        {
                HealthManager targetHP = target.GetComponent<HealthManager>();
            try
            {
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
            }
            catch (Exception e)
            {
                Modding.Logger.Log("[HOLLOW POINT DEBUG] Over here at line 150~ at the getHealthManagerRecursive" + e);
            }

            return targetHP;
        }

    }
}