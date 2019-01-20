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
    class HP_Handler : MonoBehaviour
    {
        public static double currentHeat = 0;
        double maxHeat = 100;
        float switchCoolDown = 0;
        float timeFrame = 1.7f;

        float defaultAttackSpeed;
        float defaultAttackSpeed_CH;

        float defaultAnimationSpeed;
        float defaultAnimationSpeed_CH;


        public static bool gunActive = false;
        public static bool gunOverheat = false;
        bool canSwitch = true;

        float timeTick = 0f;
        private Rigidbody2D rigidbody2D;

        public void Awake()
        {
            On.NailSlash.StartSlash += Start_Slash;

                StartCoroutine(InitRoutine());
        }

        public IEnumerator InitRoutine()
        {
            do
            {
                yield return null;
            } while (HeroController.instance == null);

            rigidbody2D = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");

            defaultAttackSpeed = 0.41f;
            defaultAttackSpeed_CH = 0.25f;

            defaultAnimationSpeed = 0.35f;
            defaultAnimationSpeed_CH = 0.28f;

            //Modding.Logger.Log("ATTACK RECOVERY SPEED" + HeroController.instance.ATTACK_RECOVERY_TIME);
        }

        public void Update()
        {
            //Attack Speed, if the gun is active set all attack speeds in to has really fast
            //Otherwise, stick with the default attack speed        
            if (gunActive)
            {
                HeroController.instance.ATTACK_COOLDOWN_TIME = 0; 
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 0;

                HeroController.instance.ATTACK_DURATION = 0;
                HeroController.instance.ATTACK_DURATION_CH = 0;

                //HeroController.instance.ATTACK_RECOVERY_TIME = 5f;
            }
            else
            {
                HeroController.instance.ATTACK_COOLDOWN_TIME = defaultAnimationSpeed;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = defaultAnimationSpeed_CH;

                HeroController.instance.ATTACK_DURATION = defaultAttackSpeed;
                HeroController.instance.ATTACK_DURATION_CH = defaultAttackSpeed_CH;

                //HeroController.instance.ATTACK_RECOVERY_TIME = 0.1f;
            }


            //Heat timeFrame is higher when the gun is INACTIVE, this means timeTick reaches 1 faster which means the passive heat increases faster
            //TLDR: gun active = slower heat build up
            timeFrame = (gunActive) ? 1.2f : 13.7f ;

            //Passive Heat While Activated
            timeTick += (timeFrame * Time.deltaTime);
            if(timeTick > 1)
            {
                timeTick = 0;
                (gunActive ? (Action)IncreaseHeat : DecreaseHeat)();
            }

        }

        public void IncreaseHeat()
        {
            if (currentHeat < 100)
            {
                currentHeat++;
            }
        }

        public void DecreaseHeat()
        {
            if (currentHeat > 0)
            {
                currentHeat -= 1;
            }
        }

        public void Start_Slash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        {
            if (gunActive)
            {
                currentHeat += 3;
                ShootBullet();             
            }
            else
            {
                orig(self);
            }
        }

        public void ShootBullet()
        {
            //GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
            float xDir = (HeroController.instance.cState.facingRight) ? 1 : -1;
            float wallSlideFire = (HeroController.instance.cState.wallSliding) ? -1 : 1;

            GameObject bulletClone = Instantiate(HP_BulletPrefab.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.2f*xDir,-0.7f,-0.002f), new Quaternion(0,0,0,0));

            bulletClone.GetComponent<BoxCollider2D>().enabled = true;

            HP_Sprites.startShake = true;
            HP_Sprites.isFiring = false;
            HP_Sprites.isFiring = true;

            HP_Sprites.lowerGunTimer = 0.3f;

            PlaySound();

            bulletClone.GetComponent<HP_Behaviour_Bullet>().xSpeed = HP_DirectionHandler.xVelocity * (xDir * wallSlideFire);
            bulletClone.GetComponent<HP_Behaviour_Bullet>().ySpeed = HP_DirectionHandler.yVelocity;

            Destroy(bulletClone, 1.5f);
        }

        public void PlaySound()
        {
            HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.bulletSoundFX);
        }

        public void OnDestroy()
        {
            On.NailSlash.StartSlash -= Start_Slash;

            HeroController.instance.ATTACK_COOLDOWN_TIME = defaultAnimationSpeed;
            HeroController.instance.ATTACK_COOLDOWN_TIME_CH = defaultAnimationSpeed_CH;

            HeroController.instance.ATTACK_DURATION = defaultAttackSpeed;
            HeroController.instance.ATTACK_DURATION_CH = defaultAttackSpeed_CH;
        }

    }
}
