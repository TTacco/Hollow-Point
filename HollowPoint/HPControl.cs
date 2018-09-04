using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MonoMod.Utils;
using MonoMod;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using ModCommon;
using ModCommon.Util;
using GlobalEnums;
using Object = UnityEngine.Object;


namespace HollowPoint
{
    class HPControl : MonoBehaviour
    {
        private bool reloading;
        private GameObject fireball;
        private PlayMakerFSM fireballFSM;
        private PlayMakerFSM fireballControlFSM;
        private readonly System.Random recoilNum = new System.Random();
        private float recoilVal;

        AttackDirection ad;


        //INTIALIZATION
        public void Awake()
        {
            ModHooks.Instance.ObjectPoolSpawnHook += BooletSize;
        }

        public void Start()
        {
            ModHooks.Instance.AttackHook += Attack_Hook;
            On.NailSlash.StartSlash += Start_Slash;
            On.HealthManager.Hit += spellDam;

            StartCoroutine(CoroutineTest());
        }

        public void spellDam(On.HealthManager.orig_Hit orig, HealthManager self, HitInstance hitInstance)
        {
            if (hitInstance.AttackType == AttackTypes.Spell && hitInstance.Source.name.StartsWith("bullet"))
            {
                Log("Bullet impact with name " + hitInstance.Source.name);
                BulletBehavior b = fireball.GetComponent<BulletBehavior>();
                if (!b.enemyHit())
                    return;
                if (self.IsBlockingByDirection(
                    DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(self.transform)),
                    hitInstance.AttackType))
                {
                    orig(self, hitInstance);
                    return;
                }

                hitEnemy(self, b.bulletType.Damage, hitInstance, b.bulletType.SoulGain);
            }
            else
            {
                orig(self, hitInstance);
            }
        }

        public IEnumerator CoroutineTest()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);
            Modding.Logger.Log("Initialized");           
        }


        //SHOOT/FIRE METHOD
        public void Attack_Hook(AttackDirection ad)
        {
            this.ad = ad;
        }

        public void Start_Slash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (ad != AttackDirection.downward && !AmmunitionControl.reloading)
            {
                StartCoroutine(CheckIfNull(HeroController.instance.cState.facingRight));
                if (AmmunitionControl.currAmmoType.AmmoName.Contains("9MM"))
                {
                    StartCoroutine(BurstFire(5));
                }
                else if (AmmunitionControl.currAmmoType.AmmoName.Contains("5.56"))
                {
                    StartCoroutine(BurstFire(3));
                }
                else
                {
                    AmmunitionControl.currAmmoType.CurrAmmo--;
                    Schutz(ad);
                }

                if(AmmunitionControl.currAmmoType.CurrAmmo <= 0)
                {
                    Modding.Logger.Log("START RELOADING NOW");
                    AmmunitionControl.reloading = true;
                }
            }
            else
            {
                orig(self);
            }
        }

        public IEnumerator BurstFire(int x)
        {
            for(int i = x; i > 0 && AmmunitionControl.currAmmoType.CurrAmmo > 0; i--)
            {
                Schutz(ad);
                AmmunitionControl.currAmmoType.CurrAmmo--;
                yield return new WaitForSeconds(0.10f);
            }

            yield return null;
        }
    
        public void Schutz(AttackDirection aDir)
        {
            fireball = Instantiate(HeroController.instance.spell1Prefab, HeroController.instance.transform.position - new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 0));
            
            fireball.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);

            fireballFSM = fireball.LocateMyFSM("Fireball Cast");

            fireballFSM.FsmVariables.GetFsmFloat("Fire Speed").Value = 70;

            //Shooting toward the right, removes audio and shake
            if (HeroController.instance.cState.facingRight && aDir == AttackDirection.normal)
            {
                fireball.transform.position += new Vector3(0.80f, -0.5f, 0f);
                fireballFSM.GetAction<SendEventByName>("Cast Right", 1).sendEvent = "";
                fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Right", 6).volume = 0;
                fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).position = new Vector3(0, 0, 0);

                //add bullet deviation/recoil
                recoilVal = recoilNum.Next(-7, 7);
                fireball.transform.Rotate(new Vector3(0, 0, recoilVal));
                fireballFSM.GetAction<SetVelocityAsAngle>("Cast Right", 9).angle = 0 + recoilVal;
            }
            //Shooting toward the left, removes audio and shake
            else if (!HeroController.instance.cState.facingRight && aDir == AttackDirection.normal)
            {
                fireball.transform.position += new Vector3(-0.80f, -0.5f, 0f);
                fireballFSM.GetAction<SendEventByName>("Cast Left", 1).sendEvent = "";
                fireballFSM.GetAction<AudioPlayerOneShotSingle>("Cast Left", 3).volume = 0;
                fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).position = new Vector3(0, 0, 0);

                //add bullet deviation
                recoilVal = recoilNum.Next(-7, 7);
                fireball.transform.Rotate(new Vector3(0, 0, recoilVal));
                fireballFSM.GetAction<SetVelocityAsAngle>("Cast Left", 6).angle = 180 + recoilVal;
            }
        }

        public IEnumerator CheckIfNull(bool facingRight)
        {
            do
            {
                yield return null;
            }
            while (fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control") == null);
            //_fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Right", 7).storeObject.Value == null || _fireballFSM.GetAction<SpawnObjectFromGlobalPool>("Cast Left", 4).storeObject.Value == null          

            fireballControlFSM = fireballFSM.FsmVariables.GetFsmGameObject("Fireball").Value.LocateMyFSM("Fireball Control");

            Log("Wall Impact reached");
            fireballControlFSM.GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";

            yield return null;
        }

        //BULLET SIZE CHANGES
        public GameObject BooletSize(GameObject go)
        {
            if (go.name.Contains("Fireball") || go.name.StartsWith("bullet"))
            {
                Log("Fireball");
                StartCoroutine(ShrinkBooletSize(go));
            }
            return go;
        }

        public IEnumerator ShrinkBooletSize(GameObject go)
        {
            yield return new WaitForEndOfFrame();
            go.GetComponent<Transform>().localScale = new Vector3(0.5f, 0.2f, 1f);
            go.LocateMyFSM("Fireball Control").GetAction<SendEventByName>("Wall Impact", 2).sendEvent = "";
            go.LocateMyFSM("Fireball Control").GetAction<SetFsmInt>("Set Damage", 2).setValue = 1;
            go.name = "bullet" + AmmunitionControl.currAmmoType.AmmoName;
            fireball.GetOrAddComponent<BulletBehavior>().bulletType = AmmunitionControl.currAmmoType;
        }

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

            int cardinalDirection =
                DirectionUtils.GetCardinalDirection(hitInstance.GetActualDirection(targetHP.transform));
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            
            
            GameObject sendHitGO = targetHP.GetAttr<GameObject>("sendHitGO");
            if (sendHitGO != null)
            {
                Log("Wow I did reflection correctly");
                FSMUtility.SendEventToGameObject(targetHP.gameObject, "HIT", false);
            }
            
            GameObject fireballHitPrefab = targetHP.GetAttr<GameObject>("fireballHitPrefab");
            Vector3? effectOrigin = targetHP.GetAttr<Vector3?>("effectOrigin");
            
            if (fireballHitPrefab != null && effectOrigin != null)
            {
                Log("Wow I did reflection correctly x2");
                fireballHitPrefab.Spawn(targetHP.transform.position + (Vector3) effectOrigin, Quaternion.identity).transform.SetPositionZ(0.0031f);
            }
            
            
            FSMUtility.SendEventToGameObject(targetHP.gameObject, "TOOK DAMAGE", false);

            if ((UnityEngine.Object) targetHP.GetComponent<Recoil>() != (UnityEngine.Object) null)
                targetHP.GetComponent<Recoil>().RecoilByDirection(cardinalDirection, hitInstance.MagnitudeMultiplier);

            FSMUtility.SendEventToGameObject(hitInstance.Source, "HIT LANDED", false);
            FSMUtility.SendEventToGameObject(hitInstance.Source, "DEALT DAMAGE", false);
            
            

            // Actually do damage to target.
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
                targetHP.Die(0f, AttackTypes.Nail, true);
                return;
            }
            
            bool? hasAlternateHitAnimation = targetHP.GetAttr<bool?>("hasAlternateHitAnimation");
            string alternateHitAnimation = targetHP.GetAttr<string>("alternateHitAnimation");
            if (hasAlternateHitAnimation != null && (bool) hasAlternateHitAnimation && targetHP.GetComponent<tk2dSpriteAnimator>() && alternateHitAnimation != null)
            {
                Log("Wow I did reflection correctly x3");
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

       
        //MISC
        public static void Log(string s)
        {
            Modding.Logger.Log("[Hollow Point] " + s);
        }

        public void OnDestroy()
        {
            ModHooks.Instance.AttackHook -= Attack_Hook;
            On.NailSlash.StartSlash -= Start_Slash;
            On.HealthManager.Hit -= spellDam;
            Destroy(fireball);
            Destroy(fireballFSM);
            Destroy(fireballControlFSM);
        }

    }
}
