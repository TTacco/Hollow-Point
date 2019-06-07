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
    class HP_AttackHandler : MonoBehaviour
    {

        float defaultAttackSpeed = 0.41f;
        float defaultAttackSpeed_CH = 0.25f;

        float defaultAnimationSpeed = 0.35f;
        float defaultAnimationSpeed_CH = 0.28f;


        public void Awake()
        {
            On.NailSlash.StartSlash += OnSlash;

            StartCoroutine(InitRoutine());
        }

        public IEnumerator InitRoutine()
        {
            while(HeroController.instance == null)
            {
                yield return null;
            }
        }

        public void Update()
        {
            if (HP_WeaponHandler.currentGun.gunName != "Nail")
            {
                HeroController.instance.ATTACK_COOLDOWN_TIME = 0.15f;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 0.15f;

                HeroController.instance.ATTACK_DURATION = 0;
                HeroController.instance.ATTACK_DURATION_CH = 0;
            }
            else
            {
                HeroController.instance.ATTACK_COOLDOWN_TIME = defaultAnimationSpeed;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = defaultAnimationSpeed_CH;

                HeroController.instance.ATTACK_DURATION = defaultAttackSpeed;
                HeroController.instance.ATTACK_DURATION_CH = defaultAttackSpeed_CH;
            }
        }

        public void OnSlash(On.NailSlash.orig_StartSlash orig, NailSlash self)
        { 
            switch (HP_WeaponHandler.currentGun.gunName)
            {
                case "Nail":
                    orig(self);
                    break;
                case "Shotgun":
                    StartCoroutine(ShotgunShot(8));
                    StartGunAnims();
                    break;
                default:
                   GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.2f * 1, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                   Destroy(bullet, 0.35f);
                   StartGunAnims();
                   PlayGunSounds("Rifle");
                   break;
            }

            GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
        }

        public IEnumerator ShotgunShot(int pellets)
        {
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();
                Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.2f * 1, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
            }
        }

        public void StartGunAnims()
        {
            HP_Sprites.startShake = true;
            HP_Sprites.isFiring = false;
            HP_Sprites.isFiring = true;
            HP_Sprites.lowerGunTimer = 0.3f;
        }

        public void PlayGunSounds(String gunName)
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_rifle.wav", out AudioClip ac);
                HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(ac);
            }
            catch (Exception e)
            {
                Modding.Logger.Log("Play Gun Sounds Exception HP_AttackHandler.cs " + e);
            }
        }

    }
}
