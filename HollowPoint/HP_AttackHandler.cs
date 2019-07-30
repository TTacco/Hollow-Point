using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using MonoMod.Utils;
using MonoMod;
using HutongGames;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using Modding;
using ModCommon;
using ModCommon.Util;
using Object = UnityEngine.Object;

namespace HollowPoint
{
    class HP_AttackHandler : MonoBehaviour
    {

        float defaultAttackSpeed = 0.41f;
        float defaultAttackSpeed_CH = 0.25f;

        float defaultAnimationSpeed = 0.35f;
        float defaultAnimationSpeed_CH = 0.28f;

        public static GameObject damageNumberTestGO;

        float cooldown = 0;
        float cooldown_CH = 0;

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
                damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
                DontDestroyOnLoad(damageNumberTestGO);
            }
        }

        public void Update()
        {
            if (HP_WeaponHandler.currentGun.gunName != "Nail" && !HP_HeatHandler.overheat)
            {
                HeroController.instance.ATTACK_DURATION = 0;
                HeroController.instance.ATTACK_DURATION_CH = 0;

                HeroController.instance.ATTACK_COOLDOWN_TIME = HP_WeaponHandler.currentGun.gunCooldown;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = HP_WeaponHandler.currentGun.gunCooldown;
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
            //creates the offset of where the bullet will spawn depending no the player facing right or wall sliding, to avoid bullets spawning inside walls
            float directionMultiplier = (HeroController.instance.cState.facingRight) ? 1f : -1f;
            float wallClimbMultiplier = (HeroController.instance.cState.wallSliding) ? -1f : 1f;
            directionMultiplier *= wallClimbMultiplier;

            if (HP_HeatHandler.overheat)
            {
                orig(self);
                return;
            }

            switch (HP_WeaponHandler.currentGun.gunName)
            {
                case "Nail":
                    orig(self);
                    break;
                case "Submachinegun":
                    StartCoroutine(SMGShots(3));
                    GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
                    break;
                case "Shotgun":
                    StartCoroutine(ShotgunShot(8));
                    GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
                    ShotgunRecoil();
                    break;
                default:
                    GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.3f * directionMultiplier, -0.8f, -0.00001f), new Quaternion(0, 0, 0, 0));
                    //Destroy(bullet, 0.40f);
                    PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
                    GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
                    break;
            }
           
            HP_HeatHandler.IncreaseHeat();
            HP_Sprites.StartGunAnims();
            HP_Sprites.StartFlash();
        }

        public IEnumerator Test()
        {
            float time = 1;
            Rigidbody2D knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");
            while (time > 0)
            {
                knight.velocity = new Vector2(knight.velocity.x, 30);
                time -= Time.deltaTime;
            }

            yield return null;
        }

        public IEnumerator SMGShots(int burst)
        {
            for (int i = 0; i < burst; i++)
            {
                GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                yield return new WaitForSeconds(0.07f);
                PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
            }
        }

        public IEnumerator ShotgunShot(int pellets)
        {
            PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();
                GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                bullet.GetComponent<HP_BulletBehaviour>();
                //Destroy(bullet, 0.28f);
            }
        }

        public void ShotgunRecoil()
        {
            if (HeroController.instance.cState.facingRight)
            {
                HeroController.instance.RecoilLeftLong();
            }
            else
            {
                HeroController.instance.RecoilRightLong();
            }
        }

        public void PlayGunSounds(String gunName)
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
                audios.clip = ac;
                //HP_Sprites.gunSpriteGO.GetComponent<AudioSource>().PlayOneShot(ac);
                audios.pitch = UnityEngine.Random.Range(1f, 1.4f);

                audios.PlayOneShot(audios.clip);
                
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(ac);
  
            }
            catch (Exception e)
            {
                Modding.Logger.Log("HP_AttackHandler.cs, cannot find the SFX " + gunName + " " + e);
            }
        }

    }
}
