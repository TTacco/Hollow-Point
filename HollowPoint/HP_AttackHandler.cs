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

        public static Rigidbody2D knight;
        public static GameObject damageNumberTestGO;

        public static bool firingSpecialShot = false;

        public static GameObject enemyBelow;

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

            knight = HeroController.instance.GetAttr<Rigidbody2D>("rb2d");

            damageNumberTestGO = new GameObject("damageNumberTESTCLONE", typeof(Text), typeof(CanvasRenderer), typeof(RectTransform));
            DontDestroyOnLoad(damageNumberTestGO);

            /*
            enemyBelow = new GameObject("ColliderChecker", typeof(HP_Component_EnemyBelow), typeof(BoxCollider2D), typeof(SpriteRenderer));

            LoadAssets.spriteDictionary.TryGetValue("exampletext.png", out Texture2D rifleTextureInit);
            enemyBelow.GetComponent<SpriteRenderer>().sprite = Sprite.Create(rifleTextureInit,
                new Rect(0, 0, rifleTextureInit.width, rifleTextureInit.height),
                new Vector2(0.5f, 0.5f), 50);

            enemyBelow.GetComponent<BoxCollider2D>().enabled = false;         

            GameObject go = Instantiate(enemyBelow);
            DontDestroyOnLoad(go);
            */
        }

        public void Update()
        {

            if (HP_WeaponHandler.currentGun.gunName != "Nail") // && !HP_HeatHandler.overheat
            {
                HeroController.instance.ATTACK_DURATION = 0f;
                HeroController.instance.ATTACK_DURATION_CH = 0f;

                HeroController.instance.ATTACK_COOLDOWN_TIME = 0.2f;
                HeroController.instance.ATTACK_COOLDOWN_TIME_CH = 0.2f;
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
            if(HP_DirectionHandler.finalDegreeDirection == 90 || HP_DirectionHandler.finalDegreeDirection == 270) directionMultiplier = 0;

            if (HP_WeaponHandler.currentGun.gunName == "Nail")
            {
                orig(self);
                return;        
            }

            /*
            switch (HP_WeaponHandler.currentGun.gunName)
            {
                case "Rifle":
                    //StartCoroutine(KnockbackRecoil(1));
                    StartCoroutine(BurstShot(5, directionMultiplier));
                    GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");
                    break;              
            }
            */

            if (HP_HeatHandler.currentEnergy >= 100)
            {
                //Special Attack

                //StartCoroutine(BurstShot(5, directionMultiplier));
                GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.7f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                bullet.GetComponent<HP_BulletBehaviour>().special = true;
                bullet.GetComponent<HP_BulletBehaviour>().pierce = false;

                GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");

                HP_HeatHandler.currentEnergy = 0;
          
                HP_Sprites.StartGunAnims();
                HP_Sprites.StartFlash();

                PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
                HP_HeatHandler.IncreaseHeat();

                Destroy(bullet, 0.3f);
            }
            else if(!firingSpecialShot)
            {
                GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.7f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                bullet.GetComponent<HP_BulletBehaviour>().pierce = false;

                //GameCameras.instance.cameraShakeFSM.SendEvent("SmallShake");

                HP_Sprites.StartGunAnims();
                HP_Sprites.StartFlash();

                PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
                HP_HeatHandler.IncreaseHeat();

                Destroy(bullet, 0.3f);
            }

        }

        public IEnumerator BurstShot(int burst, float directionMultiplier)
        {
            firingSpecialShot = true;
            for (int i = 0; i < burst; i++)
            {
                GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.7f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                yield return new WaitForSeconds(0.07f);
                PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
                Destroy(bullet, 1f);
            }
            firingSpecialShot = false;
        }

        public IEnumerator ShotgunShot(int pellets, float directionMultiplier)
        {
            PlayGunSounds(HP_WeaponHandler.currentGun.gunName);
            for (int i = 0; i < pellets; i++)
            {
                yield return new WaitForEndOfFrame();
                GameObject bullet = Instantiate(HP_BulletHandler.bulletPrefab, HeroController.instance.transform.position + new Vector3(0.7f * directionMultiplier, -0.7f, -0.002f), new Quaternion(0, 0, 0, 0));
                bullet.GetComponent<HP_BulletBehaviour>();
                Destroy(bullet, 0.20f);
            }
        }

        public IEnumerator KnockbackRecoil(float recoilStrength)
        {
            //TODO: Develop the direction launch soon 
            float deg = HP_DirectionHandler.finalDegreeDirection + 180;

            deg = deg % 360;

            float radian = deg * Mathf.Deg2Rad;

            float xDeg = (float) ((4 * recoilStrength) * Math.Cos(radian));
            float yDeg = (float) ((4 * recoilStrength) * Math.Sin(radian));

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


        public void PlayGunSounds(String gunName)
        {
            try
            {
                //HeroController.instance.spellControl.gameObject.GetComponent<AudioSource>().PlayOneShot(LoadAssets.enemyHurtSFX[soundRandom.Next(0, 2)]);
                LoadAssets.sfxDictionary.TryGetValue("shoot_sfx_" + gunName.ToLower() + ".wav", out AudioClip ac);
                AudioSource audios = HP_Sprites.gunSpriteGO.GetComponent<AudioSource>();
                audios.clip = ac;
                //HP_Sprites.gunSpriteGO.GetComponent<AudioSource>().PlayOneShot(ac);
                audios.pitch = UnityEngine.Random.Range(0.8f, 1.5f);

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
